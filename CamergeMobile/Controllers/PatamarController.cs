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
	public class PatamarController : ControllerBase {

		private readonly IPatamarService _patamarService;
		private readonly IMedicaoErroService _medicaoErroService;

		public PatamarController(IPatamarService patamarService, IMedicaoErroService medicaoErroService) {
			_patamarService = patamarService;
			_medicaoErroService = medicaoErroService;
		}

		//
		// GET: /Admin/Patamar/
		public ActionResult Index(Int32? Page) {

			var data = new ListViewModel();
			var paging = _patamarService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Patamares = paging.Items;

			return AdminContent("Patamar/PatamarList.aspx", data);
		}

		//
		// GET: /Admin/GetPatamares/
		public JsonResult GetPatamares() {
			var patamares = _patamarService.GetAll().Select(o => new { o.ID, o.Nome });
			return Json(patamares, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create() {
			var data = new FormViewModel();
			data.Patamar = TempData["PatamarModel"] as Patamar;
			if (data.Patamar == null) {
				data.Patamar = new Patamar();
				data.Patamar.UpdateFromRequest();
			}
			return AdminContent("Patamar/PatamarEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import() {
			return AdminContent("Patamar/PatamarImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(String RawData) {
			try {

				var processados = _patamarService.ImportaPatamares(RawData);

				if (processados == 0) {
					Web.SetMessage("Nenhum dado foi importado", "info");
				} else {
					Web.SetMessage("Dados importados com sucesso");
				}

			} catch (Exception ex) {
				Web.SetMessage("Falha na importação. Verifique se os dados estão corretos e tente novamente", "error");
				if (Fmt.ConvertToBool(Request["ajax"])) {
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				return RedirectToAction("Import");
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Patamar";
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");

		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false) {
			var data = new FormViewModel();
			data.Patamar = TempData["PatamarModel"] as Patamar ?? _patamarService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Patamar == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Patamar/PatamarEdit.aspx", data);
		}

		public ActionResult View(Int32 id) {
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id) {
			var patamar = _patamarService.FindByID(id);
			if (patamar == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			patamar.ID = null;
			TempData["PatamarModel"] = patamar;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id) {
			var patamar = _patamarService.FindByID(id);
			if (patamar == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			} else {
				_patamarService.Delete(patamar);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Patamar" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids) {

			_patamarService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Patamar" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save() {

			var patamar = new Patamar();
			var isEdit = Request["ID"].IsNotBlank();

			try {

				if (isEdit) {
					patamar = _patamarService.FindByID(Request["ID"].ToInt(0));
					if (patamar == null) {
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				patamar.UpdateFromRequest();
				_patamarService.Save(patamar);

				var medicaoErro = _medicaoErroService.FindByID(Request["MedicaoErroID"].ToInt(0));
				if (medicaoErro != null && !medicaoErro.Resolvido.Value) {
					medicaoErro.Resolvido = true;
					_medicaoErroService.Save(medicaoErro);
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"])) {
					var nextPage = isSaveAndRefresh ? patamar.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Patamar";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh) {
					return RedirectToAction("Edit", new { patamar.ID });
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
				TempData["PatamarModel"] = patamar;
				return isEdit && patamar != null ? RedirectToAction("Edit", new { patamar.ID }) : RedirectToAction("Create");
			}
		}

		public ActionResult PopupHelp() {
			return View("~/Areas/Admin/Views/Patamar/PopupHelp.aspx");
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
			public List<Patamar> Patamares;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel {
			public Patamar Patamar;
			public Boolean ReadOnly;
		}

	}
}
