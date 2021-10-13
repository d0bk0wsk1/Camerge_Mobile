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
	public class LoginLogController : ControllerBase
	{
		private readonly ILoginLogService _loginLogService;

		public LoginLogController(ILoginLogService loginLogService)
		{
			_loginLogService = loginLogService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _loginLogService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.LoginLogs = paging.Items;

			return AdminContent("LoginLog/LoginLogList.aspx", data);
		}

		public ActionResult Summary(Int32? Page, int? agente = null, bool full = false)
		{
			var data = new ListViewModel();
			var paging = _loginLogService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				agente, full);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.LoginLogs = paging.Items;

			return AdminContent("LoginLog/LoginLogSummary.aspx", data);
		}

		public ActionResult Del(Int32 id)
		{
			var loginLog = _loginLogService.FindByID(id);
			if (loginLog == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_loginLogService.Delete(loginLog);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/LoginLog" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids)
		{

			_loginLogService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/LoginLog" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public class ListViewModel
		{
			public List<LoginLog> LoginLogs;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
