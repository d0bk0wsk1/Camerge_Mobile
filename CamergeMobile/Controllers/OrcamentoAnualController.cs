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
	public class OrcamentoAnualController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ICalculoEconomiaService _calculoEconomiaService;
		private readonly IOpcaoImpostoService _opcaoImpostoService;

		public OrcamentoAnualController(IAtivoService ativoService,
			ICalculoEconomiaService calculoEconomiaService,
			IOpcaoImpostoService opcaoImpostoService)
		{
			_ativoService = ativoService;
			_calculoEconomiaService = calculoEconomiaService;
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
				var mes = Dates.GetFirstDayOfMonth(DateTime.Today);

				var includeIcms = Request["imposto"].Contains("icms");
				var includeImposto = Request["imposto"].Contains("imposto");
				var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
				var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");
				var precoEnergia = Request["preco"].ToDouble(0);
				var tipoEnergia = Request["tipoenergia"].ToDouble(0);

				var anoCivil = Request["anocivil"].ToInt(null);
				if (anoCivil != null)
					mes = Dates.GetFirstDayOfMonth(DateTime.Today); // new DateTime(anoCivil.Value, 12, 1);

				data.Ativos = ativos;
				data.Impostos = Request["imposto"];
				data.ImpostosCreditados = Request["creditaimp"];

				var dtos = _calculoEconomiaService.LoadCalculos(ativos, mes, precoEnergia, null, tipoEnergia, null, null, "rcnt", includeIcms, includeImposto, creditIcms, creditImposto, false, 12);
				if (dtos.Any())
				{
					var currentYear = DateTime.Today.Year;

					foreach (var dto in dtos)
					{
						if (anoCivil == null)
						{
							foreach (var calculo in dto.Calculos)
								calculo.Mes = new DateTime(((calculo.Mes.Year == currentYear) ? currentYear : (currentYear + 1)), calculo.Mes.Month, 1);

							dto.Calculos = dto.Calculos.OrderByDescending(i => i.Mes).ToList();
						}
						else
						{
							foreach (var calculo in dto.Calculos)
								calculo.Mes = new DateTime(anoCivil.Value, calculo.Mes.Month, 1);

							dto.Calculos = dto.Calculos.OrderBy(i => i.Mes).ToList();
						}
					}

					data.AtivosMes = dtos;
					data.AtivosDataReajusteTarifario = _ativoService.GetAtivosDataReajusteTarifario(data.Ativos);
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

			return AdminContent("OrcamentoAnual/OrcamentoAnualReport.aspx", data);
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public string TipoRelacao { get; set; }
			public string Impostos { get; set; }
			public string ImpostosCreditados { get; set; }
			public List<CalculoEconomiaAtivoDto> AtivosMes { get; set; }
			public List<AtivoDataAjusteTarifarioDto> AtivosDataReajusteTarifario { get; set; }
		}
	}
}
