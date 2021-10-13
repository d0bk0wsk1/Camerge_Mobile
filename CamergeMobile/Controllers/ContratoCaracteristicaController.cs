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
	public class ContratoCaracteristicaController : ControllerBase
	{
		private readonly IContratoCaracteristicaService _contratoCaracteristicaService;

		public ContratoCaracteristicaController(IContratoCaracteristicaService contratoCaracteristicaService)
		{
			_contratoCaracteristicaService = contratoCaracteristicaService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _contratoCaracteristicaService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.ContratoCaracteristicas = paging.Items;

			return AdminContent("ContratoCaracteristica/ContratoCaracteristicaList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.ContratoCaracteristica = TempData["ContratoCaracteristicaModel"] as ContratoCaracteristica;
			if (data.ContratoCaracteristica == null)
			{
				data.ContratoCaracteristica = new ContratoCaracteristica();
				data.ContratoCaracteristica.UpdateFromRequest();
			}
			return AdminContent("ContratoCaracteristica/ContratoCaracteristicaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.ContratoCaracteristica = TempData["ContratoCaracteristicaModel"] as ContratoCaracteristica ?? _contratoCaracteristicaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.ContratoCaracteristica == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("ContratoCaracteristica/ContratoCaracteristicaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var contratoCaracteristica = _contratoCaracteristicaService.FindByID(id);
			if (contratoCaracteristica == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			contratoCaracteristica.ID = null;
			TempData["ContratoCaracteristicaModel"] = contratoCaracteristica;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var contratoCaracteristica = _contratoCaracteristicaService.FindByID(id);
				if (contratoCaracteristica == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_contratoCaracteristicaService.Delete(contratoCaracteristica);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContratoCaracteristica" }, JsonRequestBehavior.AllowGet);
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
				_contratoCaracteristicaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContratoCaracteristica" }, JsonRequestBehavior.AllowGet);
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

			var contratoCaracteristica = new ContratoCaracteristica();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					contratoCaracteristica = _contratoCaracteristicaService.FindByID(Request["ID"].ToInt(0));
					if (contratoCaracteristica == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				contratoCaracteristica.UpdateFromRequest();
				_contratoCaracteristicaService.Save(contratoCaracteristica);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? contratoCaracteristica.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContratoCaracteristica";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { contratoCaracteristica.ID });
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
				TempData["ContratoCaracteristicaModel"] = contratoCaracteristica;
				return isEdit && contratoCaracteristica != null ? RedirectToAction("Edit", new { contratoCaracteristica.ID }) : RedirectToAction("Create");
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
			public List<ContratoCaracteristica> ContratoCaracteristicas;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public ContratoCaracteristica ContratoCaracteristica;
			public Boolean ReadOnly;
		}
	}
}
