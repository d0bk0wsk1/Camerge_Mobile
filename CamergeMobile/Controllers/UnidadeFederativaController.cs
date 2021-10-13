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
	public class UnidadeFederativaController : ControllerBase
	{
		private readonly IUnidadeFederativaService _unidadeFederativaService;

		public UnidadeFederativaController(IUnidadeFederativaService unidadeFederativaService)
		{
			_unidadeFederativaService = unidadeFederativaService;
		}

		//
		// GET: /Admin/UnidadeFederativa/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _unidadeFederativaService.GetSummaryDtoPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30));

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.UnidadeFederativas = paging.Items;

			return AdminContent("UnidadeFederativa/UnidadeFederativaList.aspx", data);
		}

		//
		// GET: /Admin/GetUnidadeFederativas/
		public JsonResult GetUnidadeFederativas()
		{
			var unidadeFederativas = _unidadeFederativaService.GetAll().Select(o => new { o.ID, o.Nome });
			return Json(unidadeFederativas, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.UnidadeFederativa = TempData["UnidadeFederativaModel"] as UnidadeFederativa;
			if (data.UnidadeFederativa == null)
			{
				data.UnidadeFederativa = new UnidadeFederativa();
				data.UnidadeFederativa.UpdateFromRequest();
			}
			return AdminContent("UnidadeFederativa/UnidadeFederativaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.UnidadeFederativa = TempData["UnidadeFederativaModel"] as UnidadeFederativa ?? _unidadeFederativaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.UnidadeFederativa == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("UnidadeFederativa/UnidadeFederativaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var unidadeFederativa = _unidadeFederativaService.FindByID(id);
			if (unidadeFederativa == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			unidadeFederativa.ID = null;
			TempData["UnidadeFederativaModel"] = unidadeFederativa;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var unidadeFederativa = _unidadeFederativaService.FindByID(id);
				if (unidadeFederativa == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_unidadeFederativaService.Delete(unidadeFederativa);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/UnidadeFederativa" }, JsonRequestBehavior.AllowGet);
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
				_unidadeFederativaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/UnidadeFederativa" }, JsonRequestBehavior.AllowGet);
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

			var unidadeFederativa = new UnidadeFederativa();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					unidadeFederativa = _unidadeFederativaService.FindByID(Request["ID"].ToInt(0));
					if (unidadeFederativa == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				unidadeFederativa.UpdateFromRequest();
				_unidadeFederativaService.Save(unidadeFederativa);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? unidadeFederativa.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/UnidadeFederativa";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { unidadeFederativa.ID });
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
				TempData["UnidadeFederativaModel"] = unidadeFederativa;
				return isEdit && unidadeFederativa != null ? RedirectToAction("Edit", new { unidadeFederativa.ID }) : RedirectToAction("Create");
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
			public List<UnidadeFederativaSummaryDto> UnidadeFederativas;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public UnidadeFederativa UnidadeFederativa;
			public Boolean ReadOnly;
		}
	}
}
