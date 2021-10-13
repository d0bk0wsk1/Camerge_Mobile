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
	public class MedicaoDiarioController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IAtivoService _ativoService;
		private readonly IMedidorService _medidorService;
		private readonly IMedicaoDiarioReportService _medicaoDiarioReportService;
		private readonly IMedicaoUltimoDadoService _medicaoUltimoDadoService;

		public MedicaoDiarioController(IAgenteService agenteService,
			IAtivoService ativoService,
			IMedidorService medidorService,
			IMedicaoDiarioReportService medicaoDiarioReportService,
			IMedicaoUltimoDadoService medicaoUltimoDadoService)
		{
			_agenteService = agenteService;
			_ativoService = ativoService;
			_medidorService = medidorService;
			_medicaoDiarioReportService = medicaoDiarioReportService;
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

					/*
					var ativosIDComMedidor = _medidorService.GetAtivosIDComMedidorByAtivosID(data.Ativos.Select(i => i.ID.Value).ToList());
					if (ativosIDComMedidor.Any())
					{
						var ativosSemMedidor = data.Ativos.Where(i => !ativosIDComMedidor.Contains(i.ID.Value));
						data.MensagemMedidor = string.Format("Dados de demanda e consumo por posto tarifário, os montantes medidos podem diferir daqueles registrados pela distribuidora: {0}.", ativosSemMedidor.Select(i => i.Nome).Join(" / "));
					}
					else
					{
						data.MensagemMedidor = "Medidor não associado para o(s) ativo(s) selecionado(s).";
					}
					*/

					data.MensagemMedidor = _medidorService.GetMensagemIfAtivosHaveMedidor(data.Ativos);

					if (data.Ativos.Count() == 1)
						data.MensagemAtualizacao = _medicaoUltimoDadoService.GetMensagemAtualizacao(data.Ativos.First(), parsedDate);

					if (isAllowed)
					{
						data.Resumo = _medicaoDiarioReportService.LoadMedicoesDiaList(data.Ativos, parsedDate, data.TipoLeitura);
						// data.DemandasConsumo = _demandaReportService.GetDemandasConsumoDiaPerMonth(data.Ativos.Select(i => i.ID.Value).ToList(), parsedDate);
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

			return AdminContent("MedicaoDiario/MedicaoDiarioReport.aspx", data);
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos = new List<Ativo>();
			public List<MedicaoDiarioResumoDto> Resumo;
			public string TipoLeitura;
			public string MensagemAtualizacao;
			public string MensagemMedidor;
			// public List<DemandaMedicaoDiaConsumoDto> DemandasConsumo;
		}
	}
}
