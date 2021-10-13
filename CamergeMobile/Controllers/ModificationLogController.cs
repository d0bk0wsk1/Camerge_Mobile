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
	public class ModificationLogController : ControllerBase
	{

		private readonly IModificationLogService _modificationLogService;

		public ModificationLogController(IModificationLogService modificationLogService) {
			_modificationLogService = modificationLogService;
		}

		//
		// GET: /Admin/ModificationLog/
		public ActionResult Index(Int32? Page) {

			var data = new ListViewModel();
			var paging = _modificationLogService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.ModificationLogs = paging.Items;

			return AdminContent("ModificationLog/ModificationLogList.aspx", data);
		}

		//private void AddFilters(ref SqlQuery sql) {
		//	if (Request["text"].IsNotBlank()) {
		//		sql.Add("AND model_name ILIKE").AddParameter("%"+Request["text"]+"%");
		//	}
		//}

		//private void AddOrder(ref SqlQuery sql) {
		//	if (Request["sort"].IsNotBlank()) {
		//		String field = null;
		//		switch (Request["sort"]) {
		//			case "modelName": field = "model_name"; break;

		//		}
		//		if (field != null) {
		//			var direction = Request["dir"] == "desc" ? "DESC" : "ASC";
		//			sql.Add("ORDER BY").Add(field).Add(direction);
		//		}
		//	}
		//}

		//
		// GET: /Admin/GetModificationLogs/
		//public JsonResult GetModificationLogs() {
		//	var modificationLogs = ModificationLogList.LoadAll().Select(o => new { o.ID, o.ModelName });
		//	return Json(modificationLogs, JsonRequestBehavior.AllowGet);
		//}

		//public ActionResult Create() {
		//	var data = new FormViewModel();
		//	data.ModificationLog = TempData["ModificationLogModel"] as ModificationLog;
		//	if (data.ModificationLog == null) {
		//		data.ModificationLog = new ModificationLog();
		//		data.ModificationLog.UpdateFromRequest();
		//	}
		//	return AdminContent("ModificationLog/ModificationLogEdit.aspx", data);
		//}

		//public ActionResult Edit(Int32 id, Boolean readOnly = false) {
		//	var data = new FormViewModel();
		//	data.ModificationLog = TempData["ModificationLogModel"] as ModificationLog ?? ModificationLog.LoadByID(id);
		//	data.ReadOnly = readOnly;
		//	if (data.ModificationLog == null) {
		//		Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
		//		return RedirectToAction("Index");
		//	}
		//	return AdminContent("ModificationLog/ModificationLogEdit.aspx", data);
		//}

		//public ActionResult View(Int32 id) {
		//	return Edit(id, true);
		//}

		//public ActionResult Duplicate(Int32 id) {
		//	var modificationLog = ModificationLog.LoadByID(id);
		//	if (modificationLog == null) {
		//		Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
		//		return RedirectToAction("Index");
		//	}
		//	modificationLog.ID = null;
		//	TempData["ModificationLogModel"] = modificationLog;
		//	Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
		//	return Create();
		//}

		//public ActionResult Del(Int32 id) {
		//	var modificationLog = ModificationLog.LoadByID(id);
		//	Boolean success = false;
		//	if (modificationLog == null) {
		//		Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
		//	} else {
		//		modificationLog.Delete();
		//		success = true;
		//		Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
		//	}

		//	if (Fmt.ConvertToBool(Request["ajax"])) {
		//		return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ModificationLog" }, JsonRequestBehavior.AllowGet);
		//	}

		//	var previousUrl = Web.AdminHistory.Previous;
		//	if (previousUrl != null) {
		//		return Redirect(previousUrl);
		//	}

		//	return RedirectToAction("Index");
		//}

		//public ActionResult DelMultiple(String ids) {

		//	new SqlQuery("DELETE FROM modification_log WHERE id IN (").AddParameter(ids, SqlQuery.SqlParameterType.IntList).Add(")").Execute();
		//	Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

		//	if (Fmt.ConvertToBool(Request["ajax"])) {
		//		return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ModificationLog" }, JsonRequestBehavior.AllowGet);
		//	}

		//	var previousUrl = Web.AdminHistory.Previous;
		//	if (previousUrl != null) {
		//		return Redirect(previousUrl);
		//	}

		//	return RedirectToAction("Index");
		//}

		//[ValidateInput(false)]
		//public ActionResult Save() {

		//	var modificationLog = new ModificationLog();
		//	var isEdit = Request["ID"].IsNotBlank();

		//	try {

		//		if (isEdit) {
		//			modificationLog = ModificationLog.LoadByID(Request["ID"].ToInt(0));
		//			if (modificationLog == null) {
		//				throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
		//			}
		//		}

		//		modificationLog.UpdateFromRequest();
		//		modificationLog.Save();



		//		Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

		//		var isSaveAndRefresh = Request["SubmitValue"].IsBlank() || Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

		//		if (Fmt.ConvertToBool(Request["ajax"])) {
		//			var nextPage = isSaveAndRefresh ? modificationLog.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ModificationLog";
		//			return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
		//		}

		//		if (isSaveAndRefresh) {
		//			return RedirectToAction("Edit", new { modificationLog.ID });
		//		}

		//		var previousUrl = Web.AdminHistory.Previous;
		//		if (previousUrl != null) {
		//			return Redirect(previousUrl);
		//		}

		//		return RedirectToAction("Index");

		//	} catch (Exception ex) {
		//		Web.SetMessage(HandleExceptionMessage(ex), "error");
		//		if (Fmt.ConvertToBool(Request["ajax"])) {
		//			return Json(new { success = false, message = Web.GetFlashMessageObject() });
		//		}
		//		TempData["ModificationLogModel"] = modificationLog;
		//		return isEdit && modificationLog != null ? RedirectToAction("Edit", new { modificationLog.ID }) : RedirectToAction("Create");
		//	}
		//}

		//private string HandleExceptionMessage(Exception ex) {
		//	string errorMessage;
		//	if (ex is RequiredFieldNullException) {
		//		var fieldName = ((RequiredFieldNullException) ex).FieldName;
		//		var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
		//		errorMessage = i18n.Gaia.Get("FormValidation", "NullException").Replace("XXX", friendlyFieldName);
		//	} else if (ex is FieldLengthException) {
		//		var fieldName = ((FieldLengthException) ex).FieldName;
		//		var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
		//		errorMessage = i18n.Gaia.Get("FormValidation", "LengthException").Replace("XXX", friendlyFieldName);
		//	} else {
		//		errorMessage = ex.Message;
		//	}

		//	return errorMessage;
		//}

		public class ListViewModel {
			public List<ModificationLog> ModificationLogs;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		//public class FormViewModel {
		//	public ModificationLog ModificationLog;
		//	public Boolean ReadOnly;
		//}

	}
}
