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
	public class MedicaoErroController : ControllerBase
	{

		private readonly IMedicaoErroService _medicaoErroService;
		private readonly IAtivoService _ativoService;

		public MedicaoErroController(IMedicaoErroService medicaoErroService, IAtivoService ativoService) {
			_medicaoErroService = medicaoErroService;
			_ativoService = ativoService;
		}

		//
		// GET: /Admin/MedicaoErro/
		public ActionResult Index(Int32? Page) {

			var data = new ListViewModel();
			var paging = _medicaoErroService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.MedicaoErros = paging.Items;

			if (Request["ativo"].IsNotBlank()) {
				data.Ativo = _ativoService.FindByID(Request["ativo"].ToInt(0));
			}

			return AdminContent("MedicaoErro/MedicaoErroList.aspx", data);
		}

		//
		// GET: /Admin/GetMedicaoErros/
		public JsonResult GetMedicaoErros() {
			var medicaoErros = _medicaoErroService.GetAll().Select(o => new { o.ID, o.Mensagem });
			return Json(medicaoErros, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create() {
			var data = new FormViewModel();
			data.MedicaoErro = TempData["MedicaoErroModel"] as MedicaoErro;
			if (data.MedicaoErro == null) {
				data.MedicaoErro = _medicaoErroService.GetDefaultMedicaoErro();
				data.MedicaoErro.UpdateFromRequest();
			}
			return AdminContent("MedicaoErro/MedicaoErroEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id) {
			var data = new FormViewModel();
			data.MedicaoErro = TempData["MedicaoErroModel"] as MedicaoErro ?? _medicaoErroService.FindByID(id);
			if (data.MedicaoErro == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("MedicaoErro/MedicaoErroEdit.aspx", data);
		}

		public ActionResult Duplicate(Int32 id) {
			var medicaoErro = _medicaoErroService.FindByID(id);
			if (medicaoErro == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			medicaoErro.ID = null;
			TempData["MedicaoErroModel"] = medicaoErro;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id) {
			var medicaoErro = _medicaoErroService.FindByID(id);
			if (medicaoErro == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			} else {
				_medicaoErroService.Delete(medicaoErro);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoErro" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids) {

			_medicaoErroService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoErro" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save() {

			var medicaoErro = _medicaoErroService.GetDefaultMedicaoErro();
			var isEdit = Request["ID"].IsNotBlank();

			try {

				if (isEdit) {
					medicaoErro = _medicaoErroService.FindByID(Request["ID"].ToInt(0));
					if (medicaoErro == null) {
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				medicaoErro.UpdateFromRequest();
				_medicaoErroService.Save(medicaoErro);

				// medicaoErro.DeleteChildren();
				// medicaoErro.UpdateChildrenFromRequest();
				// medicaoErro.SaveChildren();

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"].IsBlank() || Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"])) {
					var nextPage = isSaveAndRefresh ? medicaoErro.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoErro";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh) {
					return RedirectToAction("Edit", new { medicaoErro.ID });
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
				TempData["MedicaoErroModel"] = medicaoErro;
				return isEdit && medicaoErro != null ? RedirectToAction("Edit", new { medicaoErro.ID }) : RedirectToAction("Create");
			}
		}

		private string HandleExceptionMessage(Exception ex) {
			string errorMessage;
			if (ex is RequiredFieldNullException) {
				var fieldName = ((RequiredFieldNullException) ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "NullException").Replace("XXX", friendlyFieldName);
			} else if (ex is FieldLengthException) {
				var fieldName = ((FieldLengthException) ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "LengthException").Replace("XXX", friendlyFieldName);
			} else {
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

		public class ListViewModel {
			public List<MedicaoErro> MedicaoErros;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
			public Ativo Ativo;
		}

		public class FormViewModel {
			public MedicaoErro MedicaoErro;
		}

	}
}
