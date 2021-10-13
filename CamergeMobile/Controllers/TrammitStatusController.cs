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
	public class TrammitStatusController : ControllerBase
	{
		private readonly ITrammitStatusService _trammitStatusService;

		public TrammitStatusController(ITrammitStatusService trammitStatusService)
		{
			_trammitStatusService = trammitStatusService;
		}

		//
		// GET: /Admin/TrammitStatus/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _trammitStatusService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = (paging.Items.Count() / paging.ItemsPerPage); // paging.TotalPages;
			data.TotalRows = (paging.Items.Count()); // paging.TotalItems;
			data.TrammitStatuss = paging.Items;

			return AdminContent("TrammitStatus/TrammitStatusList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.TrammitStatus = TempData["TrammitStatusModel"] as TrammitStatus;
			if (data.TrammitStatus == null)
			{
				data.TrammitStatus = new TrammitStatus();
				data.TrammitStatus.UpdateFromRequest();
			}
			return AdminContent("TrammitStatus/TrammitStatusEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.TrammitStatus = TempData["TrammitStatusModel"] as TrammitStatus ?? _trammitStatusService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.TrammitStatus == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("TrammitStatus/TrammitStatusEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var TrammitStatus = _trammitStatusService.FindByID(id);
			if (TrammitStatus == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			TrammitStatus.ID = null;
			TempData["TrammitStatusModel"] = TrammitStatus;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var TrammitStatus = _trammitStatusService.FindByID(id);
				if (TrammitStatus == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_trammitStatusService.Delete(TrammitStatus);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
				}
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TrammitStatus" }, JsonRequestBehavior.AllowGet);
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
			try
			{
				var idsTrammitStatus = ids.Split(',').Select(i => i.ToInt(0));
				if (idsTrammitStatus.Any())
				{
					_trammitStatusService.DeleteMany(idsTrammitStatus);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
				}
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TrammitStatus" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var trammitStatus = new TrammitStatus();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					trammitStatus = _trammitStatusService.FindByID(Request["ID"].ToInt(0));
					if (trammitStatus == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}
				else
				{
					trammitStatus.DateAdded = DateTime.Now;
				}

				trammitStatus.UpdateFromRequest();
				_trammitStatusService.Save(trammitStatus);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? trammitStatus.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TrammitStatus";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { trammitStatus.ID });
				}

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
				{
					return Redirect(previousUrl);
				}

				return RedirectToAction("Index");

			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				TempData["TrammitStatusModel"] = trammitStatus;
				return isEdit && trammitStatus != null ? RedirectToAction("Edit", new { trammitStatus.ID }) : RedirectToAction("Create");
			}
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

		public class ListViewModel
		{
			public List<TrammitStatus> TrammitStatuss;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public TrammitStatus TrammitStatus;
			public Boolean ReadOnly;
		}
	}
}
