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
	public class CalculoLivreController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ICalculoLivreService _calculoLivreService;
		private readonly IMapeadorCenarioService _mapeadorCenarioService;
		private readonly IMedicaoConsolidadoService _medicaoConsolidadoService;
		private readonly IOpcaoImpostoService _opcaoImpostoService;

		public CalculoLivreController(IAtivoService ativoService,
			ICalculoLivreService calculoLivreService,
			IMapeadorCenarioService mapeadorCenarioService,
			IMedicaoConsolidadoService medicaoConsolidadoService,
			IOpcaoImpostoService opcaoImpostoService)
		{
			_ativoService = ativoService;
			_calculoLivreService = calculoLivreService;
			_mapeadorCenarioService = mapeadorCenarioService;
			_medicaoConsolidadoService = medicaoConsolidadoService;
			_opcaoImpostoService = opcaoImpostoService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel()
			{
				TipoRelacao = Request["relacao"] ?? PerfilAgente.TiposRelacao.Cliente.ToString()
			};

			if (Request["ativos"].IsNotBlank())
			{
				var ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);

				data.Ativos = ativos;
				data.Impostos = Request["imposto"];
				data.ImpostosCreditados = Request["creditaimp"];

				DateTime parsedDate;
				if (DateTime.TryParse(Request["date"], out parsedDate))
				{
					var mes = Dates.GetFirstDayOfMonth(parsedDate);

					var agenteConectadoId = Request["agentecon"].ToInt(null);
					var modalidade = Fmt.ToString(Request["modalidade"], true);
					var tipoVigencia = Request["vigencia"];
					var tipoEnergia = Request["tipoenergia"].ToDouble(null);
					var includeIcms = Request["imposto"].Contains("icms");
					var includeImposto = Request["imposto"].Contains("imposto");
					var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
					var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");
					var includeUltrapassagem = (data.TipoRelacao != PerfilAgente.TiposRelacao.Potencial.ToString());
					var mapeadorCenarioId = Request["mapceid"].ToInt(null);

					var autoDate = Request["autodate"].ToBoolean();
					if (autoDate)
					{
						var mostRecentMonth = _medicaoConsolidadoService.GetRecentMonthWithMedicaoPotencial(ativos.First());
						if (mostRecentMonth != null)
							mes = mostRecentMonth.Value;
					}

					var dtos = new List<CalculoLivreAtivoDto>();

					//if (tipoEnergia != null)
					//	tipoEnergia /= 100;

					if (mapeadorCenarioId == null)
					{
						dtos = _calculoLivreService.LoadCalculos(ativos, mes, agenteConectadoId, tipoEnergia, modalidade, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, includeUltrapassagem, false, 13);
					}
					else
					{
						data.IsMapeador = true;

						var mapeadorCenario = _mapeadorCenarioService.FindByID(mapeadorCenarioId.Value);
						if (mapeadorCenario != null)
							dtos = _calculoLivreService.LoadCalculos(mapeadorCenario, agenteConectadoId, tipoEnergia, modalidade, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, includeUltrapassagem, false, 13);
					}

					if (dtos.Any())
						data.AtivosMes = dtos;
				}
			}
			else
			{
				if (UserSession.Agentes != null)
				{
					var ativo = _ativoService.GetByAgentes(UserSession.Agentes);
					if (ativo != null)
					{
						data.Ativos = new List<Ativo>() { ativo };

						var opcaoImposto = _opcaoImpostoService.GetMostRecent(ativo.ID.Value);
						if (opcaoImposto != null)
						{
							data.Impostos = opcaoImposto.TipoImposto;
							data.ImpostosCreditados = opcaoImposto.TipoCredito;
						}
					}
				}
			}

			return AdminContent("CalculoLivre/CalculoLivreReport.aspx", data);
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public string TipoRelacao { get; set; }
			public string Impostos { get; set; }
			public string ImpostosCreditados { get; set; }
			public bool IsMapeador{ get; set; }
			public List<CalculoLivreAtivoDto> AtivosMes { get; set; }
		}
	}
}
