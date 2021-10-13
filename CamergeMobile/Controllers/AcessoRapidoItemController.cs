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
	public class AcessoRapidoItemController : ControllerBase
	{
		private readonly IAcessoRapidoService _acessoRapidoService;
		private readonly IAcessoRapidoItemService _acessoRapidoItemService;

		public AcessoRapidoItemController(IAcessoRapidoService acessoRapidoService,
			IAcessoRapidoItemService acessoRapidoItemService)
		{
			_acessoRapidoService = acessoRapidoService;
			_acessoRapidoItemService = acessoRapidoItemService;
		}

		public ActionResult Index(int acessoRapido, Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _acessoRapidoItemService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.AcessoRapidoItems = paging.Items;
			data.AcessoRapido = _acessoRapidoService.FindByID(acessoRapido);

			return AdminContent("AcessoRapidoItem/AcessoRapidoItemList.aspx", data);
		}

		public JsonResult GetAcessoRapidoItems()
		{
			var acessoRapidoItems = _acessoRapidoItemService.GetAll().Select(o => new { o.ID, o.Controller, o.Action });
			return Json(acessoRapidoItems, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create(int acessoRapido)
		{
			var data = new FormViewModel();
			data.AcessoRapidoItem = TempData["AcessoRapidoItemModel"] as AcessoRapidoItem;
			data.AcessoRapido = _acessoRapidoService.FindByID(acessoRapido);
			if (data.AcessoRapidoItem == null)
			{
				data.AcessoRapidoItem = new AcessoRapidoItem();
				data.AcessoRapidoItem.AcessoRapidoID = acessoRapido;

				data.AcessoRapidoItem.UpdateFromRequest();
			}
			return AdminContent("AcessoRapidoItem/AcessoRapidoItemEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.AcessoRapidoItem = TempData["AcessoRapidoItemModel"] as AcessoRapidoItem ?? _acessoRapidoItemService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.AcessoRapidoItem == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.AcessoRapido = data.AcessoRapidoItem.AcessoRapido;

			return AdminContent("AcessoRapidoItem/AcessoRapidoItemEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var acessoRapidoItem = _acessoRapidoItemService.FindByID(id);
			if (acessoRapidoItem == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			acessoRapidoItem.ID = null;
			TempData["AcessoRapidoItemModel"] = acessoRapidoItem;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(acessoRapidoItem.AcessoRapidoID.Value);
		}

		public ActionResult Del(Int32 id)
		{
			var acessoRapidoItem = _acessoRapidoItemService.FindByID(id);
			if (acessoRapidoItem == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_acessoRapidoItemService.Delete(acessoRapidoItem);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AcessoRapidoItem/?acessoRapido" + acessoRapidoItem.AcessoRapidoID }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index", new { acessoRapido = acessoRapidoItem.AcessoRapidoID });
		}

		public ActionResult DelMultiple(String ids)
		{
			var acessoRapidoIds = ids.Split(',').Select(id => id.ToInt(0));
			var acessoRapidoItem = _acessoRapidoItemService.FindByID(acessoRapidoIds.First());

			try
			{
				_acessoRapidoItemService.DeleteMany(acessoRapidoIds);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AcessoRapidoItem/?acessoRapido" + acessoRapidoItem.AcessoRapidoID }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var acessoRapidoItem = new AcessoRapidoItem();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					acessoRapidoItem = _acessoRapidoItemService.FindByID(Request["ID"].ToInt(0));
					if (acessoRapidoItem == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				acessoRapidoItem.UpdateFromRequest();

				var acessoRapido = _acessoRapidoService.FindByID(acessoRapidoItem.AcessoRapidoID.Value);
				if (acessoRapido == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				if (string.IsNullOrEmpty(acessoRapidoItem.Action))
					acessoRapidoItem.Action = "Index";

				_acessoRapidoItemService.Save(acessoRapidoItem);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? acessoRapidoItem.GetAdminURL() : Web.BaseUrl + "Admin/AcessoRapidoItem/?acessoRapido=" + acessoRapidoItem.AcessoRapidoID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { acessoRapidoItem.ID });

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
				TempData["AcessoRapidoItemModel"] = acessoRapidoItem;
				return isEdit && acessoRapidoItem != null ? RedirectToAction("Edit", new { acessoRapidoItem.ID }) : RedirectToAction("Create", acessoRapidoItem.AcessoRapidoID);
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
			public List<AcessoRapidoItem> AcessoRapidoItems;
			public AcessoRapido AcessoRapido;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public AcessoRapidoItem AcessoRapidoItem;
			public AcessoRapido AcessoRapido;
			public Boolean ReadOnly;
		}
	}
}
