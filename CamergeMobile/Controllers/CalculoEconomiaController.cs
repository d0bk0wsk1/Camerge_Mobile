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
	public class CalculoEconomiaController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IBandeiraCorService _bandeiraCorService;
		private readonly ICalculoEconomiaService _calculoEconomiaService;
		private readonly IMedicaoConsolidadoService _medicaoConsolidadoService;
		private readonly IOpcaoImpostoService _opcaoImpostoService;

		public CalculoEconomiaController(IAtivoService ativoService,
			IBandeiraCorService bandeiraCorService,
			ICalculoEconomiaService calculoEconomiaService,
			IMedicaoConsolidadoService medicaoConsolidadoService,
			IOpcaoImpostoService opcaoImpostoService)
		{
			_ativoService = ativoService;
			_bandeiraCorService = bandeiraCorService;
			_calculoEconomiaService = calculoEconomiaService;
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
					var corBandeira = _bandeiraCorService.GetCor(Request["bandeira"].ToInt(null));
					var includeIcms = Request["imposto"].Contains("icms");
					var includeImposto = Request["imposto"].Contains("imposto");
					var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
					var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");
					var precoEnergia = Request["preco"].ToDouble(0);
					var tipoEnergia = Request["tipoenergia"].ToDouble(0);
					var tipoVigencia = Request["vigencia"];

					var autoDate = Request["autodate"].ToBoolean();
					if (autoDate)
					{
						var mostRecentMonth = _medicaoConsolidadoService.GetRecentMonthWithMedicaoPotencial(ativos.First());
						if (mostRecentMonth != null)
							mes = mostRecentMonth.Value;
					}

					var dtos = _calculoEconomiaService.LoadCalculos(ativos, mes, precoEnergia, agenteConectadoId, tipoEnergia, corBandeira, null, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, true, 13);
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

			return AdminContent("CalculoEconomia/CalculoEconomiaReport.aspx", data);
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public string TipoRelacao { get; set; }
			public string Impostos { get; set; }
			public string ImpostosCreditados { get; set; }
			public List<CalculoEconomiaAtivoDto> AtivosMes { get; set; }
		}
	}
}
