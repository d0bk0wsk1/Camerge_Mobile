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
	public class MissingMedicaoLogController : ControllerBase
	{
		private readonly IMissingMedicaoLogService _missingMedicaoLogService;

		public MissingMedicaoLogController(IMissingMedicaoLogService missingMedicaoLogService)
		{
			_missingMedicaoLogService = missingMedicaoLogService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _missingMedicaoLogService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.MissingMedicaoLogs = paging.Items;

			return AdminContent("MissingMedicaoLog/MissingMedicaoLogList.aspx", data);
		}

		private string HandleExceptionMessage(Exception ex)
		{
			string errorMessage;
			if (ex is RequiredFieldNullException)
			{
				var fieldName = ((RequiredFieldNullException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "NullException").Replace("XXX", friendlyFieldName);
			}
			else if (ex is FieldLengthException)
			{
				var fieldName = ((FieldLengthException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "LengthException").Replace("XXX", friendlyFieldName);
			}
			else
			{
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

		public class FormViewModel
		{
			public MissingMedicaoLog MissingMedicaoLog;
			public Boolean ReadOnly;
		}

		public class ListViewModel
		{
			public List<MissingMedicaoLog> MissingMedicaoLogs;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
