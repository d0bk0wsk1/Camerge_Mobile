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
	public class IcmsVigenciaController : ControllerBase
	{
		private readonly IIcmsService _icmsService;
		private readonly IIcmsVigenciaService _icmsVigenciaService;
		private readonly IUnidadeFederativaService _unidadeFederativaService;

		public IcmsVigenciaController(IIcmsService icmsService,
			IIcmsVigenciaService icmsVigenciaService,
			IUnidadeFederativaService unidadeFederativaService)
		{
			_icmsService = icmsService;
			_icmsVigenciaService = icmsVigenciaService;
			_unidadeFederativaService = unidadeFederativaService;
		}

		//
		// GET: /Admin/IcmsVigencia/
		public ActionResult Index(Int32? unfeid, Int32? icmsid, Int32? Page)
		{
			var actionParams = Request.Params;

			if ((unfeid != null) && (icmsid == null))
			{
				icmsid = GetIdByAutoCreatedVigencia(unfeid);
				actionParams = Fmt.GetNewNameValueCollection(new { icmsid = icmsid, Page = Page }, Request.Params);
			}

			if (icmsid != null)
			{
				var data = new ListViewModel();

				var paging = _icmsVigenciaService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), actionParams);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.IcmsVigencias = paging.Items;
				data.Icms = _icmsService.FindByID(icmsid.Value);

				return AdminContent("IcmsVigencia/IcmsVigenciaList.aspx", data);
			}
			return HttpNotFound();
		}

		//
		// GET: /Admin/GetIcmsVigencias/
		public JsonResult GetIcmsVigencias()
		{
			var icmsVigencias = _icmsVigenciaService.GetAll().Select(o => new { o.ID, o.MesVigencia });
			return Json(icmsVigencias, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create(Int32? icmsid)
		{
			if (icmsid != null)
			{
				var data = new FormViewModel();
				data.IcmsVigencia = TempData["IcmsVigenciaModel"] as IcmsVigencia;
				data.Icms = _icmsService.FindByID(icmsid.Value);
				if (data.IcmsVigencia == null)
				{
					data.IcmsVigencia = new IcmsVigencia();
					data.IcmsVigencia.IcmsID = icmsid.Value;
					data.IcmsVigencia.MesVigencia = Dates.GetFirstDayOfMonth(DateTime.Today);

					data.IcmsVigencia.UpdateFromRequest();
				}
				return AdminContent("IcmsVigencia/IcmsVigenciaEdit.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.IcmsVigencia = TempData["IcmsVigenciaModel"] as IcmsVigencia ?? _icmsVigenciaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.IcmsVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.Icms = data.IcmsVigencia.Icms;

			return AdminContent("IcmsVigencia/IcmsVigenciaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var icmsVigencia = _icmsVigenciaService.FindByID(id);
			if (icmsVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			icmsVigencia.ID = null;
			TempData["IcmsVigenciaModel"] = icmsVigencia;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(icmsVigencia.IcmsID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var icmsVigencia = _icmsVigenciaService.FindByID(id);
				if (icmsVigencia == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_icmsVigenciaService.Delete(icmsVigencia);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/IcmsVigencia" }, JsonRequestBehavior.AllowGet);
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
				_icmsVigenciaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/IcmsVigencia" }, JsonRequestBehavior.AllowGet);
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
			var icmsVigencia = new IcmsVigencia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					icmsVigencia = _icmsVigenciaService.FindByID(Request["ID"].ToInt(0));
					if (icmsVigencia == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				icmsVigencia.UpdateFromRequest();

				var icms = _icmsService.FindByID(icmsVigencia.IcmsID);
				if (icms == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				var checkVigencia = _icmsVigenciaService.Get(icms.ID.Value, icmsVigencia.RamoAtividadeID, icmsVigencia.MesVigencia, (isEdit) ? icmsVigencia.ID : null);
				if (checkVigencia != null)
					throw new Exception("Já existe esta vigência e este ramo de atividade nesta unidade federativa.");

				_icmsVigenciaService.Save(icmsVigencia);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? icmsVigencia.GetAdminURL() : Web.BaseUrl + "Admin/IcmsVigencia/?icmsid=" + icmsVigencia.IcmsID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { icmsVigencia.ID });
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
				TempData["IcmsVigenciaModel"] = icmsVigencia;
				return isEdit && icmsVigencia != null ? RedirectToAction("Edit", new { icmsVigencia.ID }) : RedirectToAction("Create", icmsVigencia.IcmsID);
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

		public int? GetIdByAutoCreatedVigencia(int? unfeid = null)
		{
			if (unfeid != null)
			{
				var unidadeFederativa = _unidadeFederativaService.FindByID(unfeid.Value);
				if (unidadeFederativa != null)
				{
					var icms = _icmsService.Find(new Sql("WHERE unidade_federativa_id = @0;", unidadeFederativa.ID));
					if (icms == null)
					{
						var model = new Icms() { UnidadeFederativaID = unidadeFederativa.ID };

						_icmsService.Insert(model);

						return model.ID.Value;
					}
					return icms.ID.Value;
				}
			}
			return null;
		}

		public class ListViewModel
		{
			public List<IcmsVigencia> IcmsVigencias;
			public Icms Icms;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public IcmsVigencia IcmsVigencia;
			public Icms Icms;
			public Boolean ReadOnly;
		}
	}
}
