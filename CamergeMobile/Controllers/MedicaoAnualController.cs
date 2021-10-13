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
	public class MedicaoAnualController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IAtivoService _ativoService;
		private readonly IMedicaoAnualReportService _medicaoAnualReportService;
		private readonly IReportCacheLogService _reportCacheLogService;
		private readonly IReportCacheItemLogService _reportCacheItemLogService;

		public MedicaoAnualController(IAgenteService agenteService,
			IAtivoService ativoService,
			IMedicaoAnualReportService medicaoAnualReportService,
			IReportCacheLogService reportCacheLogService,
			IReportCacheItemLogService reportCacheItemLogService)
		{
			_agenteService = agenteService;
			_ativoService = ativoService;
			_medicaoAnualReportService = medicaoAnualReportService;
			_reportCacheLogService = reportCacheLogService;
			_reportCacheItemLogService = reportCacheItemLogService;
		}

		//
		// GET: /Admin/MedicaoAnual/
		public ActionResult Index()
		{
			var data = new ListViewModel();

			var forceReload = Request["forceReload"].ToBoolean();

			if (Request["tipoleitura"] == null && UserSession.IsPerfilAgente)
			{
				data.TipoLeitura = _agenteService.AgentesHasGerador(UserSession.Agentes) ? "Geracao" : "Consumo";
			}

			if (Request["ativos"].IsNotBlank())
			{
				data.Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));
				data.UnidadeMedida = Request["unidade"] == "MWh" ? "MWh" : "MWm";
				data.TipoLeitura = Request["tipoleitura"];

				if (data.Ativos.Any() && TipoLeituraIsValid(data.TipoLeitura))
				{
					var isAllowed = true;

					if (data.Ativos.Any(ativo => !UserSession.LoggedInUserCanSeeAtivo(ativo)))
					{
						data.Ativos = new List<Ativo>();
						Response.StatusCode = 403;
						isAllowed = false;
					}

					if (isAllowed)
					{
						var start = DateTime.Now;

						data.MedicoesAno = _medicaoAnualReportService.LoadMedicoesAno(data.Ativos, data.TipoLeitura, null, forceReload);
						data.GarantiaFisicaPotencia = _medicaoAnualReportService.GetGarantiaFisicaPotencia(data.Ativos);
						data.ValoresGF = _medicaoAnualReportService.GetValoresGarantiaFisica(data.GarantiaFisicaPotencia, data.UnidadeMedida);

						if (data.Ativos.Count() == 1 && data.MedicoesAno.Any())
						{
							var maxDate = data.MedicoesAno.Max(i => i.Mes);
							var minDate = data.MedicoesAno.Min(i => i.Mes);

							data.FeriasVigentes = data.Ativos.First().FeriasList;
						}

						if (forceReload)
						{
							foreach (var ativo in data.Ativos)
								_reportCacheItemLogService.Insert(
									_reportCacheLogService.GetByInserting(ativo.ID.Value, start),
									"Medição Anual", "ForceReload");
						}
					}
				}
			}
			else
			{
				if (UserSession.Agentes != null)
				{
					var ativo = _ativoService.GetByAgentes(UserSession.Agentes, PerfilAgente.TiposRelacao.Cliente.ToString());
					if (ativo != null)
						data.Ativos = new List<Ativo>() { ativo };
				}
			}

			if (forceReload)
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
		return AdminContent("MedicaoAnual/MedicaoAnualReport.aspx", data);
		}

		private bool TipoLeituraIsValid(String tipoLeitura)
		{
			foreach (var value in Enum.GetValues(typeof(Medicao.TiposLeitura)))
			{
				if (value.ToString() == tipoLeitura)
				{
					return true;
				}
			}
			return false;
		}

		public class ListViewModel
		{
			public string UnidadeMedida;
			public string TipoLeitura;
			public List<Ativo> Ativos = new List<Ativo>();
			public List<Ferias> FeriasVigentes = new List<Ferias>();
			public List<MedicaoAnualMedicaoMesDto> MedicoesAno;
			public double? GarantiaFisicaPotencia;
			public string GetValores(String unidadeMedida, List<MedicaoAnualMedicaoMesDto> medicoes)
			{
				var valores = new List<double>();

				var ultimoMes = medicoes.Any() ? medicoes.Max(m => m.Mes.Month) : 0;

				for (var i = 1; i <= ultimoMes; i++)
					valores.Add(medicoes.Where(m => m.Mes.Month == i).Select(m => unidadeMedida == "MWh" ? m.MWh : m.MWm).FirstOrDefault());

                var retorno = valores.Select(m =>
                    m == 0.0
                    ? "null" // null will remove the point from the chart
                    : m.ToString("N3").Remove(".").Replace(",", ".")
                ).Join(",");

                return retorno;
			}
			public String ValoresGF;
		}
	}
}
