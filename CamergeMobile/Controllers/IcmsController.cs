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
	public class IcmsController : ControllerBase
	{
		private readonly IIcmsService _icmsService;
		private readonly IIcmsVigenciaService _icmsVigenciaService;

		public IcmsController(IIcmsService icmsService,
			IIcmsVigenciaService icmsVigenciaService)
		{
			_icmsService = icmsService;
			_icmsVigenciaService = icmsVigenciaService;
		}

		//
		// GET: /Admin/Icms/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _icmsService.GetDetailedDtoPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = (paging.Items.Count() / paging.ItemsPerPage); // paging.TotalPages;
			data.TotalRows = (paging.Items.Count()); // paging.TotalItems;
			data.Icmss = paging.Items;

			return AdminContent("Icms/IcmsList.aspx", data);
		}

		//
		// GET: /Admin/GetIcmss/
		public JsonResult GetIcmss()
		{
			var icms = _icmsService.GetAll().Select(o => new { o.ID, o.UnidadeFederativa.Nome });
			return Json(icms, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Icms = TempData["IcmsModel"] as Icms;
			if (data.Icms == null)
			{
				data.Icms = new Icms();
				data.Icms.UpdateFromRequest();
			}
			return AdminContent("Icms/IcmsEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Icms = TempData["IcmsModel"] as Icms ?? _icmsService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Icms == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Icms/IcmsEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var icms = _icmsService.FindByID(id);
			if (icms == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			icms.ID = null;
			TempData["IcmsModel"] = icms;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var icms = _icmsService.FindByID(id);
				if (icms == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_icmsVigenciaService.DeleteByIcmsID(icms.ID.Value);
					_icmsService.Delete(icms);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Icms" }, JsonRequestBehavior.AllowGet);
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
				var idsIcms = ids.Split(',').Select(i => i.ToInt(0));
				if (idsIcms.Any())
				{
					foreach (var idIcms in idsIcms)
						_icmsVigenciaService.DeleteByIcmsID(idIcms);
					_icmsService.DeleteMany(idsIcms);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Icms" }, JsonRequestBehavior.AllowGet);
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
			var icms = new Icms();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					icms = _icmsService.FindByID(Request["ID"].ToInt(0));
					if (icms == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				icms.UpdateFromRequest();

				if (!isEdit)
				{
					var checkUnidadeFederativa = Icms.LoadByUnidadeFederativaID(icms.UnidadeFederativaID);
					if (checkUnidadeFederativa != null)
						throw new Exception("UF já possui ICMS cadastrado.");
				}

				_icmsService.Save(icms);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? icms.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Icms";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { icms.ID });
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
				TempData["IcmsModel"] = icms;
				return isEdit && icms != null ? RedirectToAction("Edit", new { icms.ID }) : RedirectToAction("Create");
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
			public List<IcmsDetailedDto> Icmss;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Icms Icms;
			public Boolean ReadOnly;
		}
	}
}
