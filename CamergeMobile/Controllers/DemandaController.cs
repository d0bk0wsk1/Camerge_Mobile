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
	public class DemandaController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IAtivoService _ativoService;
		private readonly IDemandaContratadaService _demandaContratadaService;
		private readonly IDemandaReportService _demandaReportService;
		private readonly IMedidorService _medidorService;
		private readonly IReportCacheLogService _reportCacheLogService;
		private readonly IReportCacheItemLogService _reportCacheItemLogService;

		public DemandaController(IAgenteService agenteService,
			IAtivoService ativoService,
			IDemandaContratadaService demandaContratadaService,
			IDemandaReportService demandaReportService,
			IMedidorService medidorService,
			IReportCacheLogService reportCacheLogService,
			IReportCacheItemLogService reportCacheItemLogService)
		{
			_agenteService = agenteService;
			_ativoService = ativoService;
			_demandaContratadaService = demandaContratadaService;
			_demandaReportService = demandaReportService;
			_medidorService = medidorService;
			_reportCacheLogService = reportCacheLogService;
			_reportCacheItemLogService = reportCacheItemLogService;
		}

		//
		// GET: /Admin/Demanda/
		public ActionResult Index()
		{
			var data = new ListViewModel();
			var forceReload = Request["forceReload"].ToBoolean();

			if (Request["tipoleitura"] == null && UserSession.IsPerfilAgente)
			{
				data.TipoLeitura = _agenteService.AgentesHasGerador(UserSession.Agentes) ? "Geracao" : "Consumo";
			}

			if ((Request["ativos"].IsNotBlank()) && (Request["tarifacao"] != null))
			{
				data.Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));
				data.Tarifacao = Request["tarifacao"];
				data.TipoLeitura = Request["tipoleitura"];

				if ((data.Ativos.Any()) && (TipoLeituraIsValid(data.TipoLeitura)))
				{
					if (data.Ativos.Any(ativo => UserSession.LoggedInUserCanSeeAtivo(ativo)))
					{
						//var dc = _demandaContratadaService.GetDemandaContratadaEmVigencia(data.Ativos.First());
						//if (dc != null)
						//{
						//	data.TipoDemandaContratada = dc.Tipo;
						//}

						//if (data.TipoDemandaContratada == DemandaContratada.Tipos.Azul.ToString())
						//{
						//	data.Tarifacao = Request["tarifacao"] == AgenteConectado.Tarifacoes.ForaPonta.ToString() ? AgenteConectado.Tarifacoes.ForaPonta.ToString() : AgenteConectado.Tarifacoes.Ponta.ToString();
						//}
						//else
						//{
						//	data.Tarifacao = AgenteConectado.Tarifacoes.ForaPonta.ToString();
						//}

						var start = DateTime.Now;

						if (data.TipoLeitura == Medicao.TiposLeitura.Consumo.ToString())
							data.MensagemMedidor = _medidorService.GetMensagemIfAtivosHaveMedidor(data.Ativos);

						Medicao.TiposLeitura qualLeitura;
						Enum.TryParse(data.TipoLeitura, out qualLeitura);

						data.MedicoesAno = _demandaReportService.LoadMedicoesAno(data.Ativos, data.Tarifacao, qualLeitura, null, forceReload);

						if (forceReload)
						{
							foreach (var ativo in data.Ativos)
								_reportCacheItemLogService.Insert(
									_reportCacheLogService.GetByInserting(ativo.ID.Value, start),
									"Demanda", "ForceReload"); // acho que tem que alterar isso aqui
						}
					}
					else
					{
						data.Ativos = new List<Ativo>();
						Response.StatusCode = 403;
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
			return AdminContent("Demanda/DemandaReport.aspx", data);
		}

		private Boolean TipoLeituraIsValid(String tipoLeitura)
		{
			return tipoLeitura == Medicao.TiposLeitura.Geracao.ToString() || tipoLeitura == Medicao.TiposLeitura.Consumo.ToString();
		}

		public class ListViewModel
		{
			public String Tarifacao;
			public String TipoLeitura;
			public String TipoDemandaContratada;
			public String MensagemMedidor;
			public List<Ativo> Ativos = new List<Ativo>();
			public List<DemandaMedicaoMesDto> MedicoesAno = new List<DemandaMedicaoMesDto>();
			public String GetValoresMedida(List<DemandaMedicaoMesDto> medicoes)
			{
				var valores = new List<Double>();
				var ultimoMes = medicoes.Any() ? medicoes.Max(m => m.Mes.Month) : 0;
				for (var i = 1; i <= ultimoMes; i++)
				{
					var medido = medicoes.Where(m => m.Mes.Month == i).Select(m => m.Medida).FirstOrDefault();
					var ultrapassagem = medicoes.Where(m => m.Mes.Month == i).Select(m => m.Ultrapassagem).FirstOrDefault();
					valores.Add(medido - ultrapassagem); // Remove a ultrapassagem porque ela vai ser empilhada no gráfico
				}
				return valores.Select(m => m.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}
			public String GetValoresUltrapassagem(List<DemandaMedicaoMesDto> medicoes)
			{
				var valores = new List<Double>();
				var ultimoMes = medicoes.Any() ? medicoes.Max(m => m.Mes.Month) : 0;
				for (var i = 1; i <= ultimoMes; i++)
				{
					var medido = medicoes.Where(m => m.Mes.Month == i).Select(m => m.Medida).FirstOrDefault();
					var ultrapassagem = medicoes.Where(m => m.Mes.Month == i).Select(m => m.Ultrapassagem).FirstOrDefault();
					// valores.Add(ultrapassagem > 0 ? medido : 0);
					valores.Add(ultrapassagem);
				}
				return valores.Select(m => m.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}
			public String GetValoresContratada(Int32 ano, List<DemandaMedicaoMesDto> medicoes)
			{
				var valores = new List<Double>();
				var ultimoMes = medicoes.Any() ? medicoes.Max(m => m.Mes.Month) : 0;
				for (var i = 1; i <= ultimoMes; i++)
				{
					var contratada = medicoes.Where(m => m.Mes.Month == i).Select(m => m.Contratada).FirstOrDefault();
					if (contratada.ToInt() == 0)
					{

						var mes = new DateTime(ano, i, 1);

						//var demandaContratadaMes = DemandaContratada.Load(new SqlQuery("WHERE ativo_id = " + Ativo.ID.Value + " AND to_char(mes_vigencia, 'YYYY-MM') = '" + mes.ToString("yyyy-MM") + "'"));
						//if (demandaContratadaMes == null)
						//{ // Se não tem demanda pra esse mês, pega a última cadastrada
						//	demandaContratadaMes = DemandaContratada.Load(new SqlQuery("WHERE ativo_id = " + Ativo.ID.Value + " AND mes_vigencia < '" + mes.ToString("yyyy-MM-dd") + "' ORDER BY mes_vigencia DESC LIMIT 1"));
						//}
						//if (demandaContratadaMes != null)
						//{
						//	contratada = Tarifacao == AgenteConectado.Tarifacoes.Ponta.ToString() ? demandaContratadaMes.Ponta.Value : demandaContratadaMes.ForaPonta.Value;
						//}
					}
					valores.Add(contratada);
				}
				return valores.Select(m => m.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}
		}
	}
}
