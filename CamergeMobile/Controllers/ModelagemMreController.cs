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
	public class ModelagemMreController : ControllerBase
	{

		private readonly IModelagemMreService _modelagemMreService;

		public ModelagemMreController(IModelagemMreService modelagemMreService) {
			_modelagemMreService = modelagemMreService;
		}

		//
		// GET: /Admin/ModelagemMre/
		public ActionResult Index(Int32? Page) {
			var data = new ListViewModel();
			var paging = _modelagemMreService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.ModelagemMres = paging.Items;


			return AdminContent("ModelagemMre/ModelagemMreList.aspx", data);
		}

		public ActionResult Create() {
			var data = new FormViewModel();
			data.ModelagemMre = TempData["ModelagemMreModel"] as ModelagemMre;
			if (data.ModelagemMre == null) {
				data.ModelagemMre = new ModelagemMre();
				data.ModelagemMre.UpdateFromRequest();
			}
			return AdminContent("ModelagemMre/ModelagemMreEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false) {
			var data = new FormViewModel();
			data.ModelagemMre = TempData["ModelagemMreModel"] as ModelagemMre ?? _modelagemMreService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.ModelagemMre == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("ModelagemMre/ModelagemMreEdit.aspx", data);
		}

		public ActionResult View(Int32 id) {
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id) {
			var modelagemMre = _modelagemMreService.FindByID(id);
			if (modelagemMre == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			modelagemMre.ID = null;
			TempData["ModelagemMreModel"] = modelagemMre;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id) {
			var modelagemMre = _modelagemMreService.FindByID(id);
			if (modelagemMre == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			} else {
				_modelagemMreService.Delete(modelagemMre);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ModelagemMre" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids) {

			_modelagemMreService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ModelagemMre" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save() {

			var modelagemMre = new ModelagemMre();
			var isEdit = Request["ID"].IsNotBlank();

			try {

				if (isEdit) {
					modelagemMre = _modelagemMreService.FindByID(Request["ID"].ToInt(0));
					if (modelagemMre == null) {
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				modelagemMre.UpdateFromRequest();
				_modelagemMreService.Save(modelagemMre);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"].IsBlank() || Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"])) {
					var nextPage = isSaveAndRefresh ? modelagemMre.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ModelagemMre";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh) {
					return RedirectToAction("Edit", new { modelagemMre.ID });
				}

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null) {
					return Redirect(previousUrl);
				}

				return RedirectToAction("Index");

			} catch (Exception ex) {
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"])) {
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				TempData["ModelagemMreModel"] = modelagemMre;
				return isEdit && modelagemMre != null ? RedirectToAction("Edit", new { modelagemMre.ID }) : RedirectToAction("Create");
			}
		}

		private string HandleExceptionMessage(Exception ex) {
			string errorMessage;
			if (ex is RequiredFieldNullException) {
				var fieldName = ((RequiredFieldNullException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "NullException").Replace("XXX", friendlyFieldName);
			} else if (ex is FieldLengthException) {
				var fieldName = ((FieldLengthException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "LengthException").Replace("XXX", friendlyFieldName);
			} else {
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

		public class ListViewModel {
			public List<ModelagemMre> ModelagemMres;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel {
			public ModelagemMre ModelagemMre;
			public Boolean ReadOnly;
		}

	}
}
