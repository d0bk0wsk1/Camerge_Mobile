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
	public class TrammitItemController : ControllerBase
	{
		private readonly ITrammitService _trammitService;
		private readonly ITrammitItemService _trammitItemService;

		public TrammitItemController(ITrammitService trammitService,
				ITrammitItemService trammitItemService)
		{
			_trammitService = trammitService;
			_trammitItemService = trammitItemService;
		}

		//
		// GET: /Admin/TrammitItem/
		public ActionResult Index(int? trammitid, int? Page)
		{
			if (trammitid != null)
			{
				var data = new ListViewModel();

				var paging = _trammitItemService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.TrammitItems = paging.Items;
				data.Trammit = _trammitService.FindByID(trammitid.Value);

				return AdminContent("TrammitItem/TrammitItemList.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Create(int? trammitid)
		{
			if (trammitid != null)
			{
				var data = new FormViewModel();
				data.TrammitItem = TempData["TrammitItemModel"] as TrammitItem;
				data.Trammit = _trammitService.FindByID(trammitid.Value);
				if (data.TrammitItem == null)
				{
					data.TrammitItem = new TrammitItem();
					data.TrammitItem.TrammitID = trammitid.Value;

					data.TrammitItem.PositionOrder = (_trammitItemService.GetLastPositionOrder(trammitid.Value) ?? 0);
					data.TrammitItem.PositionOrder++;

					data.TrammitItem.UpdateFromRequest();
				}
				return AdminContent("TrammitItem/TrammitItemEdit.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.TrammitItem = TempData["TrammitItemModel"] as TrammitItem ?? _trammitItemService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.TrammitItem == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.Trammit = data.TrammitItem.Trammit;

			return AdminContent("TrammitItem/TrammitItemEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var trammitItem = _trammitItemService.FindByID(id);
			if (trammitItem == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			trammitItem.ID = null;
			TempData["TrammitItemModel"] = trammitItem;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(trammitItem.TrammitID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var trammitItem = _trammitItemService.FindByID(id);
				if (trammitItem == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_trammitItemService.Delete(trammitItem);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TrammitItem" }, JsonRequestBehavior.AllowGet);
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
				_trammitItemService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TrammitItem" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var trammitItem = new TrammitItem();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					trammitItem = _trammitItemService.FindByID(Request["ID"].ToInt(0));
					if (trammitItem == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}
				else
				{
					trammitItem.DateAdded = DateTime.Now;
				}

				trammitItem.UpdateFromRequest();

				var trammit = _trammitService.FindByID(trammitItem.TrammitID.Value);
				if (trammit == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				_trammitItemService.Save(trammitItem);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? trammitItem.GetAdminURL() : Web.BaseUrl + "Admin/TrammitItem/?trammitid=" + trammitItem.TrammitID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { trammitItem.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["TrammitItemModel"] = trammitItem;
				return isEdit && trammitItem != null ? RedirectToAction("Edit", new { trammitItem.ID }) : RedirectToAction("Create", trammitItem.TrammitID);
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
			public List<TrammitItem> TrammitItems;
			public Trammit Trammit;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public TrammitItem TrammitItem;
			public Trammit Trammit;
			public Boolean ReadOnly;
		}
	}
}
