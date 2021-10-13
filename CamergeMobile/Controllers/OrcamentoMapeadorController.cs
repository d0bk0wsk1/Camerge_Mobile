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
	public class OrcamentoMapeadorController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ICalculoEconomiaService _calculoEconomiaService;
		private readonly IMapeadorCenarioService _mapeadorCenarioService;
		private readonly IMedicaoConsolidadoService _medicaoConsolidadoService;

		public OrcamentoMapeadorController(IAtivoService ativoService,
			ICalculoEconomiaService calculoEconomiaService,
			IMapeadorCenarioService mapeadorCenarioService,
			IMedicaoConsolidadoService medicaoConsolidadoService)
		{
			_ativoService = ativoService;
			_calculoEconomiaService = calculoEconomiaService;
			_mapeadorCenarioService = mapeadorCenarioService;
			_medicaoConsolidadoService = medicaoConsolidadoService;
		}

		public ActionResult Index(int mapceid)
		{
			var data = new ListViewModel()
			{
				TipoRelacao = Request["relacao"] ?? PerfilAgente.TiposRelacao.Cliente.ToString()
			};

			var list = new List<CalculoEconomiaAtivoDto>();

			var mapeadorCenario = _mapeadorCenarioService.FindByID(mapceid);
			if (mapeadorCenario != null)
			{
				var ativo = mapeadorCenario.Ativo;
				var mes = Dates.GetFirstDayOfMonth(DateTime.Today);

				var includeIcms = Fmt.ContainsWithNull(Request["imposto"], "icms");
				var includeImposto = Fmt.ContainsWithNull(Request["imposto"], "imposto");
				var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
				var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");
				var precoEnergia = Request["preco"].ToDouble(0);
				var tipoEnergia = Request["tipoenergia"].ToDouble(0);

				var medicoes = _medicaoConsolidadoService.GetMedicaoMesesByMapeadorCenario(mapeadorCenario);
				if (medicoes.Any())
					list = _calculoEconomiaService.GetCalculoEconomiaAtivo(medicoes, mapeadorCenario, precoEnergia, ativo.AgenteConectadoID, tipoEnergia, includeIcms, includeImposto, creditIcms, creditImposto);
			}

			data.AtivosMes = list;

			return AdminContent("OrcamentoMapeador/OrcamentoMapeadorReport.aspx", data);
		}

		public class ListViewModel
		{
			public string TipoRelacao { get; set; }
			public List<CalculoEconomiaAtivoDto> AtivosMes { get; set; }
		}
	}
}
