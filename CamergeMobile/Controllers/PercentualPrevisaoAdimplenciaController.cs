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
	public class PercentualPrevisaoAdimplenciaController : ControllerBase
	{
		private readonly IPercentualPrevisaoAdimplenciaService _percentualPrevisaoAdimplenciaService;

		public PercentualPrevisaoAdimplenciaController(IPercentualPrevisaoAdimplenciaService percentualPrevisaoAdimplenciaService)
		{
            _percentualPrevisaoAdimplenciaService = percentualPrevisaoAdimplenciaService;
		}

		//
		// GET: /Admin/AliquotaImposto/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _percentualPrevisaoAdimplenciaService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.PercentualPrevisaoAdimplencia = paging.Items;

			return AdminContent("PercentualPrevisaoAdimplencia/PercentualPrevisaoAdimplenciaList.aspx", data);
		}

		
		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.PercentualPrevisaoAdimplencia = TempData["PercentualPrevisaoAdimplenciaModel"] as PercentualPrevisaoAdimplencia;
			if (data.PercentualPrevisaoAdimplencia == null)
			{
				data.PercentualPrevisaoAdimplencia = new PercentualPrevisaoAdimplencia();
				data.PercentualPrevisaoAdimplencia.UpdateFromRequest();
			}
			return AdminContent("PercentualPrevisaoAdimplencia/PercentualPrevisaoAdimplenciaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.PercentualPrevisaoAdimplencia = TempData["PercentualPrevisaoAdimplenciaModel"] as PercentualPrevisaoAdimplencia ?? _percentualPrevisaoAdimplenciaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.PercentualPrevisaoAdimplencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("PercentualPrevisaoAdimplencia/PercentualPrevisaoAdimplenciaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var percentualPrevisaoAdimplencia = _percentualPrevisaoAdimplenciaService.FindByID(id);
			if (percentualPrevisaoAdimplencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
            percentualPrevisaoAdimplencia.ID = null;
			TempData["PercentualPrevisaoAdimplenciaModel"] = percentualPrevisaoAdimplencia;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var percentualPrevisaoAdimplencia = _percentualPrevisaoAdimplenciaService.FindByID(id);
				if (percentualPrevisaoAdimplencia == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
                    _percentualPrevisaoAdimplenciaService.Delete(percentualPrevisaoAdimplencia);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PercentualPrevisaoAdimplencia" }, JsonRequestBehavior.AllowGet);
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
                _percentualPrevisaoAdimplenciaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PercentualPrevisaoAdimplencia" }, JsonRequestBehavior.AllowGet);
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

			var percentualPrevisaoAdimplencia = new PercentualPrevisaoAdimplencia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
                    percentualPrevisaoAdimplencia = _percentualPrevisaoAdimplenciaService.FindByID(Request["ID"].ToInt(0));
					if (percentualPrevisaoAdimplencia == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

                percentualPrevisaoAdimplencia.UpdateFromRequest();
                _percentualPrevisaoAdimplenciaService.Save(percentualPrevisaoAdimplencia);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? percentualPrevisaoAdimplencia.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PercentualPrevisaoAdimplencia";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { percentualPrevisaoAdimplencia.ID });
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
				TempData["PercentualPrevisaoAdimplenciaModel"] = percentualPrevisaoAdimplencia;
				return isEdit && percentualPrevisaoAdimplencia != null ? RedirectToAction("Edit", new { percentualPrevisaoAdimplencia.ID }) : RedirectToAction("Create");
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
			public List<PercentualPrevisaoAdimplencia> PercentualPrevisaoAdimplencia;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public PercentualPrevisaoAdimplencia PercentualPrevisaoAdimplencia;
			public Boolean ReadOnly;
		}
	}
}
