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
	public class DescontoController : Controller
	{

		private readonly IDescontoService _descontoService;

		public DescontoController(IDescontoService descontoService) {
			_descontoService = descontoService;
		}

		//
		// GET: /Admin/Desconto/
		public ActionResult Index(Int32? Page) {

			var data = new ListViewModel();
			var paging = _descontoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Descontos = paging.Items;

			return AdminContent("Desconto/DescontoList.aspx", data);
		}

		//
		// GET: /Admin/GetDescontos/
		public JsonResult GetDescontos() {
			var descontos = _descontoService.GetAll().Select(o => new { o.ID, o.Descricao });
			return Json(descontos, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create() {
			var data = new FormViewModel();
			data.Desconto = TempData["DescontoModel"] as Desconto;
			if (data.Desconto == null) {
				data.Desconto = new Desconto();
				data.Desconto.UpdateFromRequest();
			}
			return AdminContent("Desconto/DescontoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false) {
			var data = new FormViewModel();
			data.Desconto = TempData["DescontoModel"] as Desconto ?? _descontoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Desconto == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Desconto/DescontoEdit.aspx", data);
		}

		public ActionResult View(Int32 id) {
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id) {
			var desconto = _descontoService.FindByID(id);
			if (desconto == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			desconto.ID = null;
			TempData["DescontoModel"] = desconto;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id) {
			var desconto = _descontoService.FindByID(id);
			if (desconto == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			} else {
				_descontoService.Delete(desconto);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Desconto" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids) {

			_descontoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Desconto" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save() {

			var desconto = new Desconto();
			var isEdit = Request["ID"].IsNotBlank();

			try {

				if (isEdit) {
					desconto = _descontoService.FindByID(Request["ID"].ToInt(0));
					if (desconto == null) {
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				desconto.UpdateFromRequest();
				_descontoService.Save(desconto);

				// desconto.DeleteChildren();
				// desconto.UpdateChildrenFromRequest();
				// desconto.SaveChildren();

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"])) {
					var nextPage = isSaveAndRefresh ? desconto.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Desconto";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh) {
					return RedirectToAction("Edit", new { desconto.ID });
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
				TempData["DescontoModel"] = desconto;
				return isEdit && desconto != null ? RedirectToAction("Edit", new { desconto.ID }) : RedirectToAction("Create");
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
			public List<Desconto> Descontos;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel {
			public Desconto Desconto;
			public Boolean ReadOnly;
		}

	}
}
