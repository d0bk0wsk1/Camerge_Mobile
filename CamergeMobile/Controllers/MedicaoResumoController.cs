using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CamergeMobile.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class MedicaoResumoController : ControllerBase
	{
		private readonly IMedicaoResumoReportService _medicaoResumoReportService;
		private readonly IPerfilAgenteService _perfilAgenteService;
		private readonly IReportCacheLogService _reportCacheLogService;
		private readonly IReportCacheItemLogService _reportCacheItemLogService;

		public MedicaoResumoController(IMedicaoResumoReportService medicaoResumoReportService,
			IPerfilAgenteService perfilAgenteService,
			IReportCacheLogService reportCacheLogService,
			IReportCacheItemLogService reportCacheItemLogService)
		{
			_medicaoResumoReportService = medicaoResumoReportService;
			_perfilAgenteService = perfilAgenteService;
			_reportCacheLogService = reportCacheLogService;
			_reportCacheItemLogService = reportCacheItemLogService;
		}

		//
		// GET: /Admin/MedicaoResumo/
		public ActionResult Index()
		{
			var forceReload = Request["forceReload"].ToBoolean();

			var data = GetReport(Request["agente"], Request["date"], forceReload);

			if (forceReload)
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
			return AdminContent("MedicaoResumo/MedicaoResumoReport.aspx", data);
		}

		public ActionResult MedicaoResumoReportMin()
		{
			var forceReload = Request["forceReload"].ToBoolean();

			string agente = Request["agente"];
			string date = Request["date"];

			if (UserSession.IsPerfilAgente)
			{
				var dateMedicao = (DateTime.Today.Day <= 6)
					? Dates.GetFirstDayOfMonth(DateTime.Today.AddMonths(-1))
					: Dates.GetFirstDayOfMonth(DateTime.Today);

				agente = UserSession.Agentes.First().ID.ToString();
				date = dateMedicao.ToString("yyyy-MM-dd");
			}

			var data = GetReport(agente, date, forceReload);

			if (forceReload)
			{
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
			}
			return AdminContent("MedicaoResumo/MedicaoResumoReportMin.aspx", data);
		}

		public ListViewModel GetReport(string agente, string date, bool forceReload)
		{
			var data = new ListViewModel();

			if (UserSession.IsPerfilAgente)
			{
				if (UserSession.Agentes.Any(i => i.PerfilAgenteList.Any()))
				{
					var tipoRelacao = ((UserSession.IsCliente) ? PerfilAgente.TiposRelacao.Cliente.ToString() : PerfilAgente.TiposRelacao.Potencial.ToString());

					var perfisAgente = _perfilAgenteService.GetByAgentes(UserSession.Agentes);
					if (perfisAgente.Any())
						perfisAgente = perfisAgente.Where(i => i.IsActive && i.TipoRelacao == tipoRelacao).ToList();

					data.PerfisAgente = perfisAgente;
				}
			}
			else if (agente.IsNotBlank())
			{
				if (agente == "0")
					data.PerfisAgente = _perfilAgenteService.GetByTipoRelacao(PerfilAgente.TiposRelacao.Cliente.ToString(), true);
				else
					data.PerfisAgente = _perfilAgenteService.GetByAgenteId(agente.ToInt(0), PerfilAgente.TiposRelacao.Cliente.ToString());
			}

			DateTime parsedDate;
			if (data.PerfisAgente.Any() && DateTime.TryParse(date, out parsedDate))
			{
				var ativos = new List<Ativo>();
				var start = DateTime.Now;

				foreach (var perfilAgente in data.PerfisAgente.Where(i => i.TipoRelacao == PerfilAgente.TiposRelacao.Cliente.ToString()))
					ativos.AddRange(perfilAgente.AtivoList);

				if (ativos.Any())
				{
					data.Ativos = ativos;
					data.TipoLeitura = ((ativos.Any(i => i.IsGerador)) ? Medicao.TiposLeitura.Geracao.ToString() : Medicao.TiposLeitura.Consumo.ToString());
				}

				data.Date = parsedDate;
				if (data.PerfisAgente.Any(p => p.AtivoList.Any()))
					data.ResumosMedicaoMes = _medicaoResumoReportService.LoadResumos(data.PerfisAgente, parsedDate, forceReload);

				if (forceReload)
				{
					foreach (var perfilAgente in data.PerfisAgente)
					{
						foreach (var ativo in perfilAgente.AtivoList)
							_reportCacheItemLogService.Insert(
								_reportCacheLogService.GetByInserting(ativo.ID.Value, start, null, parsedDate),
								"Medição Resumo", "ForceReload");
					}
				}
			}

			data.ConsolidadosMedicaoMes = GetConsolidado(data.PerfisAgente, data.ResumosMedicaoMes);

			return data;
		}

		private MedicaoResumoMedicaoMesDto GetConsolidado(IEnumerable<PerfilAgente> perfisAgente,
			List<MedicaoResumoMedicaoMesDto> resumos)
		{
			if (((resumos != null) && (resumos.Any())) && (perfisAgente.Any()))
			{
				// Consumo
				var consumoMWm = resumos.Sum(i => i.ConsumoMWm);
				var consumoMWmAnoAnterior = resumos.Where(i => i.ConsumoMWmAnoAnterior != null).Sum(i => i.ConsumoMWmAnoAnterior);
				var consumoMWmMesAnterior = resumos.Where(i => i.ConsumoMWmMesAnterior != null).Sum(i => i.ConsumoMWmMesAnterior);
				var ativosConsumoNotNull = resumos.Where(i => i.HorasConsumo != null);
				var horasConsumo = ativosConsumoNotNull.Sum(i => i.HorasConsumo) / ativosConsumoNotNull.Count();

				var consumoMWmAnoAnteriorWhenNull = resumos.Where(i => i.ConsumoMWmAnoAnterior == null).Sum(i => i.ConsumoMWm);
				if (consumoMWmAnoAnteriorWhenNull > 0)
					consumoMWmAnoAnterior += consumoMWmAnoAnteriorWhenNull;

				var consumoMWmMesAnteriorWhenNull = resumos.Where(i => i.ConsumoMWmMesAnterior == null).Sum(i => i.ConsumoMWm);
				if (consumoMWmMesAnteriorWhenNull > 0)
					consumoMWmMesAnterior += consumoMWmMesAnteriorWhenNull;

				// Geração
				var geracaoMWm = resumos.Sum(i => i.GeracaoMWm);
				var geracaoMWmAnoAnterior = (resumos.Any(i => i.GeracaoMWmAnoAnterior != null)) ? resumos.Sum(i => i.GeracaoMWmAnoAnterior) : null;
				var geracaoMWmMesAnterior = (resumos.Any(i => i.GeracaoMWmMesAnterior != null)) ? resumos.Sum(i => i.GeracaoMWmMesAnterior) : null;
				var ativosGeracaoNotNull = resumos.Where(i => i.HorasGeracao != null);
				var horasGeracao = ativosGeracaoNotNull.Sum(i => i.HorasGeracao) / ativosGeracaoNotNull.Count();

				return new MedicaoResumoMedicaoMesDto()
				{
					ConsumoMWh = resumos.Sum(i => i.ConsumoMWh),
					ConsumoMWm = consumoMWm,
					ConsumoMWmAnoAnterior = consumoMWmAnoAnterior,
					ConsumoMWmMesAnterior = consumoMWmMesAnterior,
					HorasConsumo = horasConsumo,
					PrevisaoConsumoMWh = resumos.Sum(i => i.PrevisaoConsumoMWh),
					PrevisaoConsumoMWm = resumos.Sum(i => i.PrevisaoConsumoMWm),
					TempoConsumo = Fmt.FormatTotalHoursInDetailedString(horasConsumo ?? 0),
					// VariacaoAnoAnteriorConsumo = (consumoMWmAnoAnterior.HasValue) ? (double?)(((consumoMWm / consumoMWmAnoAnterior.Value) - 1) * 100) : null,
					// VariacaoMesAnteriorConsumo = (consumoMWmMesAnterior.HasValue) ? (double?)(((consumoMWm / consumoMWmMesAnterior.Value) - 1) * 100) : null,
					GeracaoMWh = resumos.Sum(i => i.GeracaoMWh),
					GeracaoMWm = geracaoMWm,
					GeracaoMWmAnoAnterior = geracaoMWmAnoAnterior,
					GeracaoMWmMesAnterior = geracaoMWmMesAnterior,
					HorasGeracao = horasGeracao,
					PrevisaoGeracaoMWh = resumos.Sum(i => i.PrevisaoGeracaoMWh),
					PrevisaoGeracaoMWm = resumos.Sum(i => i.PrevisaoGeracaoMWm),
					TempoGeracao = Fmt.FormatTotalHoursInDetailedString(horasGeracao ?? 0),
					// VariacaoAnoAnteriorGeracao = ((geracaoMWmAnoAnterior != null) && (geracaoMWmAnoAnterior > 0)) ? (double?)(((geracaoMWm / geracaoMWmAnoAnterior.Value) - 1) * 100) : 0,
					// VariacaoMesAnteriorGeracao = ((geracaoMWmMesAnterior != null) && (geracaoMWmMesAnterior > 0)) ? (double?)(((geracaoMWm / geracaoMWmMesAnterior.Value) - 1) * 100) : 0,
					UltimoDadoRecebido = resumos.Max(i => i.UltimoDadoRecebido)
				};
			}
			return null;
		}

		public class ListViewModel
		{
			public IEnumerable<PerfilAgente> PerfisAgente = new List<PerfilAgente>();
			public List<Ativo> Ativos;
			public List<MedicaoResumoMedicaoMesDto> ResumosMedicaoMes;
			public MedicaoResumoMedicaoMesDto ConsolidadosMedicaoMes;
			public DateTime Date { get; set; }
			public string TipoLeitura { get; set; }
		}
	}
}
