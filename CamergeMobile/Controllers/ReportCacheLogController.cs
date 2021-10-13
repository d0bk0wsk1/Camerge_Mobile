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
	public class ReportCacheLogController : ControllerBase
	{
		private readonly IReportCacheLogService _reportCacheLogService;

		public ReportCacheLogController(IReportCacheLogService reportCacheLogService)
		{
			_reportCacheLogService = reportCacheLogService;
		}

		//
		// GET: /Admin/ReportCacheLog/
		public ActionResult Index(int? Page)
		{
			var data = new ListViewModel();

			var dia = Request["dia"].ConvertToDate(null);
			if (dia != null)
			{
				var ativo = Request["ativo"].ToInt(null);

				data.ReportCacheLogItems = _reportCacheLogService.GetOneDayWithPaging(
					Page ?? 1,
					Util.GetSettingInt("ItemsPerPage", 30),
					dia.Value, ativo
				);
			}

			return AdminContent("ReportCacheLog/ReportCacheLogReport.aspx", data);
		}

		public class ListViewModel
		{
			public Page<ReportCacheLog> ReportCacheLogItems;
		}
	}
}
