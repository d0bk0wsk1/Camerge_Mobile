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
	public class CalculoCativoController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IBandeiraCorService _bandeiraCorService;
		private readonly ICalculoCativoService _calculoCativoService;
		private readonly IMedicaoConsolidadoService _medicaoConsolidadoService;
		private readonly IOpcaoImpostoService _opcaoImpostoService;

		public CalculoCativoController(IAtivoService ativoService,
			IBandeiraCorService bandeiraCorService,
			ICalculoCativoService calculoCativoService,
			IMedicaoConsolidadoService medicaoConsolidadoService,
			IOpcaoImpostoService opcaoImpostoService)
		{
			_ativoService = ativoService;
			_bandeiraCorService = bandeiraCorService;
			_calculoCativoService = calculoCativoService;
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
					var includeIcms = Request["imposto"].Contains("icms");
					var includeImposto = Request["imposto"].Contains("imposto");
					var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
					var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");
					var corBandeira = _bandeiraCorService.GetCor(Request["bandeira"].ToInt(null));
					var includeUltrapassagem = (data.TipoRelacao != PerfilAgente.TiposRelacao.Potencial.ToString());

					var autoDate = Request["autodate"].ToBoolean();
					if (autoDate)
					{
						var mostRecentMonth = _medicaoConsolidadoService.GetRecentMonthWithMedicaoPotencial(ativos.First());
						if (mostRecentMonth != null)
							mes = mostRecentMonth.Value;
					}

					var dtos = _calculoCativoService.LoadCalculos(ativos, mes, agenteConectadoId, modalidade, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, includeUltrapassagem, corBandeira, 13);
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

			return AdminContent("CalculoCativo/CalculoCativoReport.aspx", data);
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public string TipoRelacao { get; set; }
			public string Impostos { get; set; }
			public string ImpostosCreditados { get; set; }
			public List<CalculoCativoAtivoDto> AtivosMes { get; set; }
		}
	}
}
