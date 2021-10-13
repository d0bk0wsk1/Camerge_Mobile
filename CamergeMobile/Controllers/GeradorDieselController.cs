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
	public class GeradorDieselController : ControllerBase
	{
		private readonly IGeradorDieselService _geradorDieselService;

		public GeradorDieselController(IGeradorDieselService geradorDieselService)
		{
			_geradorDieselService = geradorDieselService;
		}

		//
		// GET: /Admin/GeradorDiesel/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _geradorDieselService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params,
				(UserSession.IsPerfilAgente || UserSession.IsPotencialAgente) ? UserSession.Agentes.Select(i => i.ID.Value) : null);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.GeradorDiesels = paging.Items;

			return AdminContent("GeradorDiesel/GeradorDieselList.aspx", data);
		}

		//
		// GET: /Admin/GetGeradorDiesels/
		public JsonResult GetGeradorDiesels()
		{
			var geradorDiesels = _geradorDieselService.GetAll().Select(o => new { o.ID, o.DataVigencia });
			return Json(geradorDiesels, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.GeradorDiesel = TempData["GeradorDieselModel"] as GeradorDiesel;
			if (data.GeradorDiesel == null)
			{
				data.GeradorDiesel = new GeradorDiesel();
				data.GeradorDiesel.UpdateFromRequest();
			}
			return AdminContent("GeradorDiesel/GeradorDieselEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.GeradorDiesel = TempData["GeradorDieselModel"] as GeradorDiesel ?? _geradorDieselService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.GeradorDiesel == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("GeradorDiesel/GeradorDieselEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var geradorDiesel = _geradorDieselService.FindByID(id);
			if (geradorDiesel == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			geradorDiesel.ID = null;
			TempData["GeradorDieselModel"] = geradorDiesel;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var geradorDiesel = _geradorDieselService.FindByID(id);
				if (geradorDiesel == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_geradorDieselService.Delete(geradorDiesel);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/GeradorDiesel" }, JsonRequestBehavior.AllowGet);
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
				_geradorDieselService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/GeradorDiesel" }, JsonRequestBehavior.AllowGet);
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

			var geradorDiesel = new GeradorDiesel();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					geradorDiesel = _geradorDieselService.FindByID(Request["ID"].ToInt(0));
					if (geradorDiesel == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				geradorDiesel.UpdateFromRequest();
				_geradorDieselService.Save(geradorDiesel);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? geradorDiesel.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/GeradorDiesel";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { geradorDiesel.ID });
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
				TempData["GeradorDieselModel"] = geradorDiesel;
				return isEdit && geradorDiesel != null ? RedirectToAction("Edit", new { geradorDiesel.ID }) : RedirectToAction("Create");
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
			public List<GeradorDiesel> GeradorDiesels;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public GeradorDiesel GeradorDiesel;
			public Boolean ReadOnly;
		}
	}
}
