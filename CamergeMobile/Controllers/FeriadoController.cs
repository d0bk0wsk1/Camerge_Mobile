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
	public class FeriadoController : ControllerBase
	{

		private readonly IFeriadoService _feriadoService;

		public FeriadoController(IFeriadoService feriadoService) {
			_feriadoService = feriadoService;
		}

		//
		// GET: /Admin/Feriado/
		public ActionResult Index(Int32? Page) {

			var data = new ListViewModel();
			var paging = _feriadoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Feriados = paging.Items;

			return AdminContent("Feriado/FeriadoList.aspx", data);
		}

		public ActionResult Create() {
			var data = new FormViewModel();
			data.Feriado = TempData["FeriadoModel"] as Feriado;
			if (data.Feriado == null) {
				data.Feriado = new Feriado();
				data.Feriado.UpdateFromRequest();
			}
			return AdminContent("Feriado/FeriadoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false) {
			var data = new FormViewModel();
			data.Feriado = TempData["FeriadoModel"] as Feriado ?? _feriadoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Feriado == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Feriado/FeriadoEdit.aspx", data);
		}

		public ActionResult View(Int32 id) {
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id) {
			var feriado = _feriadoService.FindByID(id);
			if (feriado == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			feriado.ID = null;
			TempData["FeriadoModel"] = feriado;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id) {
			var feriado = _feriadoService.FindByID(id);
			if (feriado == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			} else {
				_feriadoService.Delete(feriado);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Feriado" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids) {

			_feriadoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Feriado" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save() {

			var feriado = new Feriado();
			var isEdit = Request["ID"].IsNotBlank();

			try {

				if (isEdit) {
					feriado = _feriadoService.FindByID(Request["ID"].ToInt(0));
					if (feriado == null) {
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				feriado.UpdateFromRequest();
				_feriadoService.Save(feriado);

				// feriado.DeleteChildren();
				// feriado.UpdateChildrenFromRequest();
				// feriado.SaveChildren();

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"])) {
					var nextPage = isSaveAndRefresh ? feriado.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Feriado";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh) {
					return RedirectToAction("Edit", new { feriado.ID });
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
				TempData["FeriadoModel"] = feriado;
				return isEdit && feriado != null ? RedirectToAction("Edit", new { feriado.ID }) : RedirectToAction("Create");
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
			public List<Feriado> Feriados;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel {
			public Feriado Feriado;
			public Boolean ReadOnly;
		}

	}
}
