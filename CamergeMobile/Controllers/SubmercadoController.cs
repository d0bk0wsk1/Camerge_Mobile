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
	public class SubmercadoController : ControllerBase
	{

		private readonly ISubmercadoService _submercadoService;

		public SubmercadoController(ISubmercadoService submercadoService) {
			_submercadoService = submercadoService;
		}

		//
		// GET: /Admin/Submercado/
		public ActionResult Index(Int32? Page) {

			var data = new ListViewModel();
			var paging = _submercadoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Submercados = paging.Items;

			return AdminContent("Submercado/SubmercadoList.aspx", data);
		}

		//
		// GET: /Admin/GetSubmercados/
		public JsonResult GetSubmercados() {
			var submercados = _submercadoService.GetAll().Select(o => new { o.ID, o.Nome });
			return Json(submercados, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create() {
			var data = new FormViewModel();
			data.Submercado = TempData["SubmercadoModel"] as Submercado;
			if (data.Submercado == null) {
				data.Submercado = new Submercado();
				data.Submercado.UpdateFromRequest();
			}
			return AdminContent("Submercado/SubmercadoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false) {
			var data = new FormViewModel();
			data.Submercado = TempData["SubmercadoModel"] as Submercado ?? _submercadoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Submercado == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Submercado/SubmercadoEdit.aspx", data);
		}

		public ActionResult View(Int32 id) {
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id) {
			var submercado = _submercadoService.FindByID(id);
			if (submercado == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			submercado.ID = null;
			TempData["SubmercadoModel"] = submercado;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id) {
			var submercado = _submercadoService.FindByID(id);
			if (submercado == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			} else {
				_submercadoService.Delete(submercado);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Submercado" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids) {

			_submercadoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Submercado" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save() {

			var submercado = new Submercado();
			var isEdit = Request["ID"].IsNotBlank();

			try {

				if (isEdit) {
					submercado = _submercadoService.FindByID(Request["ID"].ToInt(0));
					if (submercado == null) {
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				submercado.UpdateFromRequest();
				_submercadoService.Save(submercado);

				// submercado.DeleteChildren();
				// submercado.UpdateChildrenFromRequest();
				// submercado.SaveChildren();

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"])) {
					var nextPage = isSaveAndRefresh ? submercado.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Submercado";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh) {
					return RedirectToAction("Edit", new { submercado.ID });
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
				TempData["SubmercadoModel"] = submercado;
				return isEdit && submercado != null ? RedirectToAction("Edit", new { submercado.ID }) : RedirectToAction("Create");
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
			public List<Submercado> Submercados;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel {
			public Submercado Submercado;
			public Boolean ReadOnly;
		}

	}
}
