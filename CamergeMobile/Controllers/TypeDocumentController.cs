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
	public class TypeDocumentController : ControllerBase
	{
		private readonly ITypeDocumentService _typeDocumentService;

		public TypeDocumentController(ITypeDocumentService typeDocumentService)
		{
			_typeDocumentService = typeDocumentService;
		}

		public ActionResult Index(int? Page)
		{
			var data = new ListViewModel();

			var paging = _typeDocumentService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = (paging.Items.Count() / paging.ItemsPerPage);
			data.TotalRows = (paging.Items.Count());
			data.TypeDocuments = paging.Items;

			return AdminContent("TypeDocument/TypeDocumentList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.TypeDocument = TempData["TypeDocumentModel"] as TypeDocument;

			if (data.TypeDocument == null)
			{
				data.TypeDocument = new TypeDocument()
				{
					IsActive = true
				};
				data.TypeDocument.UpdateFromRequest();
			}

			return AdminContent("TypeDocument/TypeDocumentEdit.aspx", data);
		}

		public ActionResult Edit(int id, bool readOnly = false)
		{
			var data = new FormViewModel();
			data.TypeDocument = TempData["TypeDocumentModel"] as TypeDocument ?? _typeDocumentService.FindByID(id);
			data.ReadOnly = readOnly;

			if (data.TypeDocument == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("TypeDocument/TypeDocumentEdit.aspx", data);
		}

		public ActionResult View(int id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(int id)
		{
			var typeDocument = _typeDocumentService.FindByID(id);
			if (typeDocument == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			typeDocument.ID = null;
			TempData["TypeDocumentModel"] = typeDocument;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");

			return Create();
		}

		public ActionResult Del(int id)
		{
			try
			{
				var typeDocument = _typeDocumentService.FindByID(id);
				if (typeDocument == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_typeDocumentService.Delete(typeDocument);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TypeDocument" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(string ids)
		{
			try
			{
				var idsTypeDocument = ids.Split(',').Select(i => i.ToInt(0));
				if (idsTypeDocument.Any())
				{
					_typeDocumentService.DeleteMany(idsTypeDocument);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TypeDocument" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var typeDocument = new TypeDocument();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					typeDocument = _typeDocumentService.FindByID(Request["ID"].ToInt(0));
					if (typeDocument == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}
				else
				{
					typeDocument.DateAdded = DateTime.Now;
				}

				typeDocument.UpdateFromRequest();
				_typeDocumentService.Save(typeDocument);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? typeDocument.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TypeDocument";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { typeDocument.ID });

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

				TempData["TypeDocumentModel"] = typeDocument;
				return isEdit && typeDocument != null ? RedirectToAction("Edit", new { typeDocument.ID }) : RedirectToAction("Create");
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
			public List<TypeDocument> TypeDocuments;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public TypeDocument TypeDocument;
			public bool ReadOnly;
		}
	}
}
