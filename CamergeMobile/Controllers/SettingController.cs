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
	public class SettingController : ControllerBase
	{
		private readonly ISettingService _settingService;

		public SettingController(ISettingService settingService)
		{
			_settingService = settingService;
		}

		public ActionResult All()
		{
			var data = new FormViewModel();
			data.Settings = _settingService.GetAll();
			return AdminContent("Setting/SettingEdit.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var settings = _settingService.GetAll().ToArray();

			try
			{
				foreach (string key in Request.Form.Keys)
				{
					var setting = settings.FirstOrDefault(s => s.Key == key);
					if (setting != null)
					{
						setting.Value = Request[key];
						_settingService.Save(setting);
					}
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = Web.BaseUrl + "Admin/Setting/All";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
			}

			return RedirectToAction("All");
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
			public IEnumerable<Setting> Settings;
		}
	}
}
