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
	public class MedicaoHorarioController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IAtivoService _ativoService;
		private readonly IDemandaReportService _demandaReportService;
		private readonly IGarantiaFisicaService _garantiaFisicaService;
		private readonly IMedidorService _medidorService;
		private readonly IMedicaoHorarioReportService _medicaoDiarioDetailedReportService;
		private readonly IMedicaoUltimoDadoService _medicaoUltimoDadoService;

		public MedicaoHorarioController(IAgenteService agenteService,
			IAtivoService ativoService,
			IDemandaReportService demandaReportService,
			IGarantiaFisicaService garantiaFisicaService,
			IMedidorService medidorService,
			IMedicaoHorarioReportService medicaoDiarioDetailedReportService,
			IMedicaoUltimoDadoService medicaoUltimoDadoService)
		{
			_agenteService = agenteService;
			_ativoService = ativoService;
			_demandaReportService = demandaReportService;
			_garantiaFisicaService = garantiaFisicaService;
			_medidorService = medidorService;
			_medicaoDiarioDetailedReportService = medicaoDiarioDetailedReportService;
			_medicaoUltimoDadoService = medicaoUltimoDadoService;
		}

		//
		// GET: /Admin/MedicaoDiario/
		public ActionResult Index()
		{
			var data = new ListViewModel();

			if (Request["tipoleitura"] == null && UserSession.IsPerfilAgente)
			{
				data.TipoLeitura = _agenteService.AgentesHasGerador(UserSession.Agentes) ? "Geracao" : "Consumo";
			}

			if (Request["ativos"].IsNotBlank())
			{
				data.Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));
				data.TipoLeitura = Request["tipoleitura"];

				DateTime parsedDate;

				if (data.Ativos.Any() && DateTime.TryParse(Request["date"], out parsedDate))
				{
					var isAllowed = true;
					if (data.Ativos.Any(ativo => !UserSession.LoggedInUserCanSeeAtivo(ativo)))
					{
						data.Ativos = new List<Ativo>();
						Response.StatusCode = 403;
						isAllowed = false;
					}

					var ativosIDComMedidor = _medidorService.GetAtivosIDComMedidorByAtivosID(data.Ativos.Select(i => i.ID.Value).ToList());
					if (ativosIDComMedidor.Any())
					{
						var ativosSemMedidor = data.Ativos.Where(i => !ativosIDComMedidor.Contains(i.ID.Value));

						data.Ativos = data.Ativos.Where(i => ativosIDComMedidor.Contains(i.ID.Value)).ToList();
						data.MensagemMedidor = string.Format("Medidor não associado para o(s) ativo(s): {0}.", ativosSemMedidor.Select(i => i.Nome).Join(" / "));
					}
					else
					{
						data.MensagemMedidor = "Este relatório só pode ser gerado para ativo(s) com módulo(s) de medição remoto.";
						isAllowed = false;
					}

					if (data.Ativos.Count() == 1)
						data.MensagemAtualizacao = _medicaoUltimoDadoService.GetMensagemAtualizacao(data.Ativos.First(), parsedDate);

					if (isAllowed)
					{
						data.Resumo = _medicaoDiarioDetailedReportService.LoadMedicoesDia(data.Ativos, parsedDate, data.TipoLeitura);

						if (data.Ativos.Count() == 1)
						{
							var gf = _garantiaFisicaService.GetGarantiaFisicaEmVigencia(data.Ativos.First());
							if (gf != null)
								data.GarantiaFisica = gf.Potencia;
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

			return AdminContent("MedicaoHorario/MedicaoHorarioReport.aspx", data);
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos = new List<Ativo>();
			public MedicaoHorarioResumoDto Resumo;
			public double? GarantiaFisica;
			public string TipoLeitura;
			public string MensagemAtualizacao;
			public string MensagemMedidor;
		}
	}
}
