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
	public class MedicaoMensalController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IAtivoService _ativoService;
		private readonly IMedicaoMensalReportService _medicaoMensalReportService;
		private readonly IMedicaoUltimoDadoService _medicaoUltimoDadoService;
		private readonly IMedidorService _medidorService;
		private readonly IReportCacheLogService _reportCacheLogService;
		private readonly IReportCacheItemLogService _reportCacheItemLogService;

		public MedicaoMensalController(IAgenteService agenteService,
			IAtivoService ativoService,
			IMedicaoMensalReportService medicaoMensalReportService,
			IMedicaoUltimoDadoService medicaoUltimoDadoService,
			IMedidorService medidorService,
			IReportCacheLogService reportCacheLogService,
			IReportCacheItemLogService reportCacheItemLogService)
		{
			_agenteService = agenteService;
			_ativoService = ativoService;
			_medicaoMensalReportService = medicaoMensalReportService;
			_medicaoUltimoDadoService = medicaoUltimoDadoService;
			_medidorService = medidorService;
			_reportCacheLogService = reportCacheLogService;
			_reportCacheItemLogService = reportCacheItemLogService;
		}

		//
		// GET: /Admin/MedicaoMensal/
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

				DateTime parsedDate;

				data.TipoLeitura = Request["tipoleitura"];
				if ((string.IsNullOrEmpty(data.TipoLeitura)) && (data.Ativos.Count() == 1))
					data.TipoLeitura = ((data.Ativos.First().IsConsumidor) ? Medicao.TiposLeitura.Consumo.ToString() : Medicao.TiposLeitura.Geracao.ToString());

				data.UnidadeMedidaGeracaoConsumo = Request["unidade"] == "MWm" ? "MWm" : "MWh";

				if (data.Ativos.Any() && TipoLeituraIsValid(data.TipoLeitura) && DateTime.TryParse(Request["date"], out parsedDate))
				{
					parsedDate = Dates.GetFirstDayOfMonth(parsedDate);

					var isAllowed = true;
					if (data.Ativos.Any(ativo => !UserSession.LoggedInUserCanSeeAtivo(ativo)))
					{
						data.Ativos = new List<Ativo>();
						Response.StatusCode = 403;
						isAllowed = false;
					}

					if (data.Ativos.Count() == 1)
						data.Agente = data.Ativos.First().PerfilAgente.Agente;

					if (data.TipoLeitura == Medicao.TiposLeitura.Consumo.ToString())
						data.MensagemMedidor = _medidorService.GetMensagemIfAtivosHaveMedidor(data.Ativos);

					data.UnidadeMedida = _medicaoMensalReportService.GetUnidadeMedida(data.TipoLeitura, data.UnidadeMedidaGeracaoConsumo);
					data.IsTensaoOrCorrente = data.TipoLeitura == Medicao.TiposLeitura.Tensao.ToString() || data.TipoLeitura == Medicao.TiposLeitura.Corrente.ToString();

					if (data.Ativos.Count() == 1)
						data.MensagemAtualizacao = _medicaoUltimoDadoService.GetMensagemAtualizacao(data.Ativos.First(), parsedDate);

					if (isAllowed)
					{
						var start = DateTime.Now;

						if (data.TipoLeitura == Medicao.TiposLeitura.Consumo.ToString())
						{
							data.Resumo = _medicaoMensalReportService.LoadMedicoesMesConsumo(data.Ativos, parsedDate, forceReload);
							if (data.Resumo.MedicoesMesConsumo.Any())
							{
								var diasHorasFaltantes = data.Resumo.MedicoesMesConsumo.Where(i => i.HorasFaltantes != null);
								if (diasHorasFaltantes.Any())
									data.MensagemAtualizacaoHelper = string.Join("&#13;", diasHorasFaltantes.Select(i => string.Format("Data: {0:dd/MM/yyyy} - ({1})", i.Dia, i.HorasFaltantes)));
							}
						}
						else if (data.TipoLeitura == Medicao.TiposLeitura.Geracao.ToString())
						{
                            if (data.Ativos.Where(w => w.IsGeradorGD).Count() > 0)
                            {
                                data.Resumo = _medicaoMensalReportService.LoadMedicoesMesGeracaoGD(data.Ativos, parsedDate, forceReload);
                                if (data.Resumo.MedicoesMesConsumo.Any())
                                {
                                    var diasHorasFaltantes = data.Resumo.MedicoesMesConsumo.Where(i => i.HorasFaltantes != null);
                                    if (diasHorasFaltantes.Any())
                                        data.MensagemAtualizacaoHelper = string.Join("&#13;", diasHorasFaltantes.Select(i => string.Format("Data: {0:dd/MM/yyyy} - ({1})", i.Dia, i.HorasFaltantes)));
                                }
                            }
                            else
                            {
                                data.ResumoGeracao = _medicaoMensalReportService.LoadMedicoesMesGeracao(data.Ativos, parsedDate, forceReload);
                                if (data.ResumoGeracao.MedicoesMes.Any())
                                {
                                    var diasHorasFaltantes = data.ResumoGeracao.MedicoesMes.Where(i => i.HorasFaltantes != null);
                                    if (diasHorasFaltantes.Any())
                                        data.MensagemAtualizacaoHelper = string.Join("&#13;", diasHorasFaltantes.Select(i => string.Format("Data: {0:dd/MM/yyyy} - ({1})", i.Dia, i.HorasFaltantes)));
                                }
                            }							
						}
						else if (data.IsTensaoOrCorrente)
						{
							data.Resumo = _medicaoMensalReportService.LoadMedicoesMesTensaoCorrente(data.Ativos, data.TipoLeitura, parsedDate, forceReload);
							if (data.Resumo.MedicoesMesTensaoCorrente.Any())
							{
								var diasHorasFaltantes = data.Resumo.MedicoesMesTensaoCorrente.Where(i => i.HorasFaltantes != null);
								if (diasHorasFaltantes.Any())
									data.MensagemAtualizacaoHelper = string.Join("&#13;", diasHorasFaltantes.Select(i => string.Format("Data: {0:dd/MM/yyyy} - ({1})", i.Dia, i.HorasFaltantes)));
							}
						}

						data.GarantiaFisicaPotencia = _medicaoMensalReportService.GarantiaFisicaPotencia(data.Ativos, data.UnidadeMedidaGeracaoConsumo);

						if (forceReload)
						{
							foreach (var ativo in data.Ativos)
								_reportCacheItemLogService.Insert(
									_reportCacheLogService.GetByInserting(ativo.ID.Value, start, null, parsedDate),
									"Medição Mensal", "ForceReload");
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
			{
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
			}

			return AdminContent("MedicaoMensal/MedicaoMensalReport.aspx", data);
		}

		private bool TipoLeituraIsValid(String tipoLeitura)
		{
			foreach (var value in Enum.GetValues(typeof(Medicao.TiposLeitura)))
				if (value.ToString() == tipoLeitura)
					return true;
			return false;
		}

		public class ListViewModel
		{
			public Agente Agente;
			public List<Ativo> Ativos;
			public string TipoLeitura;
			public string UnidadeMedidaGeracaoConsumo;

			public MedicaoMensalResumoGeracaoDto ResumoGeracao;
			public MedicaoMensalResumoDto Resumo;

			public string MensagemAtualizacao;
			public string MensagemAtualizacaoHelper;
			public string MensagemMedidor;

			public bool IsTensaoOrCorrente;

			public double? GarantiaFisicaPotencia;
			public MedicaoMensalUnidadeMedidaDto UnidadeMedida;

			public List<DateTime> MedicoesDiasConsumo()
			{
				return Resumo.MedicoesMesConsumo.Select(i => i.Dia).ToList();
			}

			public List<DateTime> MedicoesDiasGeracao()
			{
				return ResumoGeracao.MedicoesMes.Select(i => i.Dia).ToList();
			}

			public List<DateTime> MedicoesDiasTensaoCorrente()
			{
				return Resumo.MedicoesMesTensaoCorrente.Select(i => i.Dia).ToList();
			}

			/* Gráfico Consumo - MWh */
			public string MedicoesDiaConsumoMWhMediaMensal()
			{
				return (Resumo.MWhTotal / Resumo.MedicoesMesConsumo.Count()).ToString("N3").Remove(".").Replace(",", ".");
			}

			public List<DateTime?> DemandasDataLeituraDiaConsumoMWhForaPontaCapacitivo()
			{
				return Resumo.MedicoesMesConsumo.Select(i => i.DateDemandaConsumoForaPonta).ToList();
			}

			public List<DateTime?> DemandasDataLeituraDiaConsumoMWhPonta()
			{
				return Resumo.MedicoesMesConsumo.Select(i => i.DateDemandaConsumoPonta).ToList();
			}

			public List<string> MedicoesDiaConsumoMwhForaPontaCapacitivo()
			{
				return Resumo.MedicoesMesConsumo.Select(i => (i.MWhCapacitivo + i.MWhForaPonta).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaConsumoMWhPonta()
			{
				return Resumo.MedicoesMesConsumo.Select(i => (i.MWhPonta).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaConsumoMWhGeradorDiesel()
			{
				return Resumo.MedicoesMesConsumo.Select(i => (i.MWhGeradorDiesel).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaConsumoMWhTotal()
			{
				return Resumo.MedicoesMesConsumo.Select(i => (i.MWhPonta + i.MWhCapacitivo + i.MWhForaPonta + i.MWhGeradorDiesel).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaDemandaMwhForaPontaCapacitivo()
			{
				return Resumo.MedicoesMesConsumo.Select(i => (Math.Max(i.DemandaForaPonta, i.DemandaCapacitivo)).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaDemandaMWhPonta()
			{
				return Resumo.MedicoesMesConsumo.Select(i => (i.DemandaPonta).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaDemandaMWhTotal()
			{
				return Resumo.MedicoesMesConsumo.Select(i => new[] { i.DemandaPonta, i.DemandaForaPonta, i.DemandaCapacitivo }.Max().ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public string EmpilhaConsumoMWhForaPontaCapacitivo()
			{
				return MedicoesDiaConsumoMwhForaPontaCapacitivo().Join(",");
			}

			public string EmpilhaConsumoMWhPonta()
			{
				return Resumo.MedicoesMesConsumo.Select(medicaoDia => (medicaoDia.MWhCapacitivo + medicaoDia.MWhForaPonta + medicaoDia.MWhPonta).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public string EmpilhaConsumoMWhGeradorDiesel()
			{
				return Resumo.MedicoesMesConsumo.Select(medicaoDia => (medicaoDia.MWhCapacitivo + medicaoDia.MWhForaPonta + medicaoDia.MWhPonta + medicaoDia.MWhGeradorDiesel).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public List<DemandaMedicaoDiaConsumoDto> DemandasConsumo;

			//public string EmpilhaConsumoMWhCapacitivo() {
			//	return Resumo.MedicoesMesConsumo.Select(m => m.MWhCapacitivo.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			//}

			//public string EmpilhaConsumoMWhForaPonta() {
			//	return Resumo.MedicoesMesConsumo.Select(medicaoDia => (medicaoDia.MWhCapacitivo + medicaoDia.MWhForaPonta).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			//}

			/* Gráfico Consumo - MWm */
			public string GetConsumoMWm()
			{
				return Resumo.MedicoesMesConsumo.Select(m => m.MWmTotal.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public List<string> MedicoesDiaConsumoMwm()
			{
				return Resumo.MedicoesMesConsumo.Select(i => (i.MWmTotal).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			/* Gráfico Geração - MWh */
			public string MedicoesDiaGeracaoMWhMediaMensal()
			{
				return (ResumoGeracao.MWhTotal / ResumoGeracao.MedicoesMes.Count()).ToString("N3").Remove(".").Replace(",", ".");
			}

			public List<DateTime?> DemandasDataLeituraDiaGeracaoMWhLeve()
			{
				return ResumoGeracao.MedicoesMes.Select(i => i.DateDemandaLeve).ToList();
			}

			public List<DateTime?> DemandasDataLeituraDiaGeracaoMWhMedio()
			{
				return ResumoGeracao.MedicoesMes.Select(i => i.DateDemandaMedio).ToList();
			}

			public List<DateTime?> DemandasDataLeituraDiaGeracaoMWhPesado()
			{
				return ResumoGeracao.MedicoesMes.Select(i => i.DateDemandaPesado).ToList();
			}

			public List<string> MedicoesDiaGeracaoMwhLeve()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhLeve).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaGeracaoMwhMedio()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhMedio).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaGeracaoMwhPesado()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhPesado).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaGeracaoMwhTotal()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhLeve + medicaoDia.MWhMedio + medicaoDia.MWhPesado).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaDemandaMwhLeve()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.DemandaLeve).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaDemandaMwhMedio()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.DemandaMedio).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesDiaDemandaMwhPesado()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.DemandaPesado).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public string EmpilhaGeracaoMWhLeve()
			{
				return ResumoGeracao.MedicoesMes.Select(m => m.MWhLeve.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public string EmpilhaGeracaoMWhMedio()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhLeve + medicaoDia.MWhMedio).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public string EmpilhaGeracaoMWhPesado()
			{
				return ResumoGeracao.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhLeve + medicaoDia.MWhMedio + medicaoDia.MWhPesado).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public List<DemandaMedicaoDiaGeracaoDto> DemandasGeracao;

			/* Gráfico Geração - MWm */
			public string GetGeracaoMWm()
			{
				return ResumoGeracao.MedicoesMes.Select(m => m.MWmTotal.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public List<string> MedicoesDiaGeracaoMwm()
			{
				return ResumoGeracao.MedicoesMes.Select(i => (i.MWmTotal).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			/* Gráfico Tensão e Corrente */
			public List<string> MedicoesTensaoCorrenteCanalC1()
			{
				return Resumo.MedicoesMesTensaoCorrente.Select(m => (m.CanalC1).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesTensaoCorrenteCanalC2()
			{
				return Resumo.MedicoesMesTensaoCorrente.Select(m => (m.CanalC2).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public List<string> MedicoesTensaoCorrenteCanalC3()
			{
				return Resumo.MedicoesMesTensaoCorrente.Select(m => (m.CanalC3).ToString("N3").Remove(".").Replace(",", ".")).ToList();
			}

			public string EmpilhaTensaoCorrenteCanalC1()
			{
				return Resumo.MedicoesMesTensaoCorrente.Select(m => (m.CanalC1 + m.CanalC2 + m.CanalC3).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public string EmpilhaTensaoCorrenteCanalC2()
			{
				return Resumo.MedicoesMesTensaoCorrente.Select(m => (m.CanalC2 + m.CanalC3).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}

			public string EmpilhaTensaoCorrenteCanalC3()
			{
				return Resumo.MedicoesMesTensaoCorrente.Select(m => m.CanalC3.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
			}
		}
	}
}
