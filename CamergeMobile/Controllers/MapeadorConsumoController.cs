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
	public class MapeadorConsumoController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IMapeadorCenarioService _mapeadorCenarioService;

		public MapeadorConsumoController(IAtivoService ativoService,
			IMapeadorCenarioService mapeadorCenarioService)
		{
			_ativoService = ativoService;
			_mapeadorCenarioService = mapeadorCenarioService;
		}

		public ActionResult Index()
		{
			var data = new ReportViewModel();

			var ativoMocked = _ativoService.FindByID(Request.QueryString["ativo"].ToInt());
			if (ativoMocked != null)
			{
				var currentMonth = Dates.GetLastDayOfMonth((Request.QueryString["date"].ConvertToDate(null) ?? DateTime.Today));
				var fromDate = Dates.GetFirstDayOfMonth(currentMonth.AddMonths(-11));

				data.FeriasVigentes = ativoMocked.FeriasList;
				if (data.FeriasVigentes.Any())
				{
					data.FeriasVigentes = data.FeriasVigentes.Where(i =>
						(i.DataInicio.Value.Year == fromDate.Year)
						|| (i.DataFim.Value.Year == currentMonth.Year));
				}

				data.MapeadorCenario = _mapeadorCenarioService.GetMocked(ativoMocked, fromDate, currentMonth, false);
				if (data.MapeadorCenario != null)
					data.Report = _mapeadorCenarioService.GetPrevisao(data.MapeadorCenario, null, true, false, false);
			}

			return AdminContent("MapeadorConsumo/MapeadorConsumoReport.aspx", data);
		}

		public class ReportViewModel
		{
			public MapeadorCenario MapeadorCenario;
			public MapeadorCenarioPrevisaoDto Report;
			public IEnumerable<Ferias> FeriasVigentes;
		}
	}
}
