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
	public class ContratacaoEnergiaController : ControllerBase
	{
		private readonly IPerfilAgenteService _perfilAgenteService;
		private readonly IContratacaoEnergiaService _contratacaoEnergiaService;

		public ContratacaoEnergiaController(IPerfilAgenteService perfilAgenteService,
			IContratacaoEnergiaService contratacaoEnergiaService)
		{
			_perfilAgenteService = perfilAgenteService;
			_contratacaoEnergiaService = contratacaoEnergiaService;
		}

		public ActionResult Index()
		{
			string relacao = Request["relacao"] ?? PerfilAgente.TiposRelacao.Cliente.ToString();
			var data = new ReportViewModel()
			{
				Categoria = Request["categoria"],
				Relacao = relacao
			};

			if (Request["ano"] != null)
			{
				var anos = Fmt.ToIntArray(Request["ano"].Split(','));
				var perfisAgenteTela = new List<PerfilAgente>();

				/*
				var anoInicio = Dates.GetFirstDayOfMonth(parsedDate);
				anoInicio = new DateTime(anoInicio.Year, 1, 1);
				*/

				var allPerfisAgente = Request["allPerfilAgente"].ToBoolean();
				if (allPerfisAgente)
				{
					if (data.Categoria != null)
						perfisAgenteTela = _perfilAgenteService.GetByTipo(data.Categoria, true).Where(i => i.TipoRelacao == relacao).ToList();
				}
				else
				{
					perfisAgenteTela = _perfilAgenteService.GetByConcatnatedIds(Request["perfisAgente"]);
					if (!perfisAgenteTela.Any())
						perfisAgenteTela = _perfilAgenteService.GetByTipos(relacao).ToList();
				}

				if ((perfisAgenteTela != null) && (perfisAgenteTela.Any()))
				{
					perfisAgenteTela = perfisAgenteTela.Where(i => i.TipoRelacao == relacao).ToList();
					if (perfisAgenteTela.Any())
						data.Report = _contratacaoEnergiaService.GetReport(perfisAgenteTela, anos);
				}
			}
			return AdminContent("ContratacaoEnergia/ContratacaoEnergiaReport.aspx", data);
		}

		public class ReportViewModel
		{
			public List<ContratacaoEnergiaPerfilDto> Report;
			public string Relacao;
			public string Categoria;
			// isso aqui tem que entrar na tela public IEnumerable<Ferias> FeriasVigentes;
		}
	}
}
