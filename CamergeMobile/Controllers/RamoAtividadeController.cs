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
	public class RamoAtividadeController : ControllerBase
	{
		private readonly IRamoAtividadeService _ramoAtividadeService;

		public RamoAtividadeController(IRamoAtividadeService ramoAtividadeService)
		{
			_ramoAtividadeService = ramoAtividadeService;
		}

		//
		// GET: /Admin/RamoAtividade/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _ramoAtividadeService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.RamoAtividades = paging.Items;

			return AdminContent("RamoAtividade/RamoAtividadeList.aspx", data);
		}

		//
		// GET: /Admin/GetRamoAtividades/
		public JsonResult GetRamoAtividades()
		{
			var ramoAtividades = _ramoAtividadeService.GetAll().Select(o => new { o.ID, o.Nome });
			return Json(ramoAtividades, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.RamoAtividade = TempData["RamoAtividadeModel"] as RamoAtividade;
			if (data.RamoAtividade == null)
			{
				data.RamoAtividade = new RamoAtividade();
				data.RamoAtividade.UpdateFromRequest();
			}
			return AdminContent("RamoAtividade/RamoAtividadeEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.RamoAtividade = TempData["RamoAtividadeModel"] as RamoAtividade ?? _ramoAtividadeService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.RamoAtividade == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("RamoAtividade/RamoAtividadeEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var ramoAtividade = _ramoAtividadeService.FindByID(id);
			if (ramoAtividade == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			ramoAtividade.ID = null;
			TempData["RamoAtividadeModel"] = ramoAtividade;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var ramoAtividade = _ramoAtividadeService.FindByID(id);
				if (ramoAtividade == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_ramoAtividadeService.Delete(ramoAtividade);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/RamoAtividade" }, JsonRequestBehavior.AllowGet);
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
				_ramoAtividadeService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/RamoAtividade" }, JsonRequestBehavior.AllowGet);
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

			var ramoAtividade = new RamoAtividade();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					ramoAtividade = _ramoAtividadeService.FindByID(Request["ID"].ToInt(0));
					if (ramoAtividade == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				ramoAtividade.UpdateFromRequest();
				_ramoAtividadeService.Save(ramoAtividade);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? ramoAtividade.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/RamoAtividade";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { ramoAtividade.ID });
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
				TempData["RamoAtividadeModel"] = ramoAtividade;
				return isEdit && ramoAtividade != null ? RedirectToAction("Edit", new { ramoAtividade.ID }) : RedirectToAction("Create");
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
			public List<RamoAtividade> RamoAtividades;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public RamoAtividade RamoAtividade;
			public Boolean ReadOnly;
		}
	}
}
