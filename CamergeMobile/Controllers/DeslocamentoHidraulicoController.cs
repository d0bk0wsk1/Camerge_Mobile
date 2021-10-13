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
	public class DeslocamentoHidraulicoController : ControllerBase
	{
		private readonly IDeslocamentoHidraulicoService _deslocamentoHidraulicoService;

		public DeslocamentoHidraulicoController(IDeslocamentoHidraulicoService deslocamentoHidraulicoService)
		{
            _deslocamentoHidraulicoService = deslocamentoHidraulicoService;
		}

		//
		// GET: /Admin/AliquotaImposto/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _deslocamentoHidraulicoService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.DeslocamentoHidraulico = paging.Items;

			return AdminContent("DeslocamentoHidraulico/DeslocamentoHidraulicoList.aspx", data);
		}

		
		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.DeslocamentoHidraulico = TempData["DeslocamentoHidraulicoModel"] as DeslocamentoHidraulico;
			if (data.DeslocamentoHidraulico == null)
			{
				data.DeslocamentoHidraulico = new DeslocamentoHidraulico();
				data.DeslocamentoHidraulico.UpdateFromRequest();
			}
			return AdminContent("DeslocamentoHidraulico/DeslocamentoHidraulicoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.DeslocamentoHidraulico = TempData["DeslocamentoHidraulicoModel"] as DeslocamentoHidraulico ?? _deslocamentoHidraulicoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.DeslocamentoHidraulico == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("DeslocamentoHidraulico/DeslocamentoHidraulicoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var deslocamentoHidraulico = _deslocamentoHidraulicoService.FindByID(id);
			if (deslocamentoHidraulico == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
            deslocamentoHidraulico.ID = null;
			TempData["DeslocamentoHidraulicoModel"] = deslocamentoHidraulico;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var deslocamentoHidraulico = _deslocamentoHidraulicoService.FindByID(id);
				if (deslocamentoHidraulico == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
                    _deslocamentoHidraulicoService.Delete(deslocamentoHidraulico);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/DeslocamentoHidraulico" }, JsonRequestBehavior.AllowGet);
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
                _deslocamentoHidraulicoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/DeslocamentoHidraulico" }, JsonRequestBehavior.AllowGet);
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

			var deslocamentoHidraulico = new DeslocamentoHidraulico();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
                    deslocamentoHidraulico = _deslocamentoHidraulicoService.FindByID(Request["ID"].ToInt(0));
					if (deslocamentoHidraulico == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

                deslocamentoHidraulico.UpdateFromRequest();
                _deslocamentoHidraulicoService.Save(deslocamentoHidraulico);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? deslocamentoHidraulico.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/DeslocamentoHidraulico";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { deslocamentoHidraulico.ID });
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
				TempData["deslocamentoHidraulicoModel"] = deslocamentoHidraulico;
				return isEdit && deslocamentoHidraulico != null ? RedirectToAction("Edit", new { deslocamentoHidraulico.ID }) : RedirectToAction("Create");
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
			public List<DeslocamentoHidraulico> DeslocamentoHidraulico;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public DeslocamentoHidraulico DeslocamentoHidraulico;
			public Boolean ReadOnly;
		}
	}
}
