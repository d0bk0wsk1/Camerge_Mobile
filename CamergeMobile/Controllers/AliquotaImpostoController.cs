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
	public class AliquotaImpostoController : ControllerBase
	{
		private readonly IAliquotaImpostoService _aliquotaImpostoService;

		public AliquotaImpostoController(IAliquotaImpostoService aliquotaImpostoService)
		{
			_aliquotaImpostoService = aliquotaImpostoService;
		}

		//
		// GET: /Admin/AliquotaImposto/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _aliquotaImpostoService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.AliquotaImpostos = paging.Items;

			return AdminContent("AliquotaImposto/AliquotaImpostoList.aspx", data);
		}

		//
		// GET: /Admin/GetAliquotaImpostos/
		public JsonResult GetAliquotaImpostos()
		{
			var aliquotaImpostos = _aliquotaImpostoService.GetAll().Select(o => new { o.ID, o.MesVigencia });
			return Json(aliquotaImpostos, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.AliquotaImposto = TempData["AliquotaImpostoModel"] as AliquotaImposto;
			if (data.AliquotaImposto == null)
			{
				data.AliquotaImposto = new AliquotaImposto();
				data.AliquotaImposto.UpdateFromRequest();
			}
			return AdminContent("AliquotaImposto/AliquotaImpostoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.AliquotaImposto = TempData["AliquotaImpostoModel"] as AliquotaImposto ?? _aliquotaImpostoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.AliquotaImposto == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("AliquotaImposto/AliquotaImpostoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var aliquotaImposto = _aliquotaImpostoService.FindByID(id);
			if (aliquotaImposto == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			aliquotaImposto.ID = null;
			TempData["AliquotaImpostoModel"] = aliquotaImposto;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var aliquotaImposto = _aliquotaImpostoService.FindByID(id);
				if (aliquotaImposto == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_aliquotaImpostoService.Delete(aliquotaImposto);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AliquotaImposto" }, JsonRequestBehavior.AllowGet);
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
				_aliquotaImpostoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AliquotaImposto" }, JsonRequestBehavior.AllowGet);
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

			var aliquotaImposto = new AliquotaImposto();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					aliquotaImposto = _aliquotaImpostoService.FindByID(Request["ID"].ToInt(0));
					if (aliquotaImposto == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				aliquotaImposto.UpdateFromRequest();
				_aliquotaImpostoService.Save(aliquotaImposto);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? aliquotaImposto.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AliquotaImposto";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { aliquotaImposto.ID });
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
				TempData["AliquotaImpostoModel"] = aliquotaImposto;
				return isEdit && aliquotaImposto != null ? RedirectToAction("Edit", new { aliquotaImposto.ID }) : RedirectToAction("Create");
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
			public List<AliquotaImposto> AliquotaImpostos;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public AliquotaImposto AliquotaImposto;
			public Boolean ReadOnly;
		}
	}
}
