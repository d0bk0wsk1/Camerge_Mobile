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
	public class PrecoFuturoEnergiaValorController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IPrecoFuturoEnergiaService _precoFuturoEnergiaService;
		private readonly IPrecoFuturoEnergiaValorService _precoFuturoEnergiaValorService;

		public PrecoFuturoEnergiaValorController(IAtivoService ativoService,
			IPrecoFuturoEnergiaService precoFuturoEnergiaService,
			IPrecoFuturoEnergiaValorService precoFuturoEnergiaValorService)
		{
			_ativoService = ativoService;
			_precoFuturoEnergiaService = precoFuturoEnergiaService;
			_precoFuturoEnergiaValorService = precoFuturoEnergiaValorService;
		}

		//
		// GET: /Admin/PrecoFuturoEnergiaValor/
		public ActionResult Index(Int32 prfuenid, Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _precoFuturoEnergiaValorService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.PrecoFuturoEnergiaValores = paging.Items;
			data.PrecoFuturoEnergia = _precoFuturoEnergiaService.FindByID(prfuenid);

			return AdminContent("PrecoFuturoEnergiaValor/PrecoFuturoEnergiaValorList.aspx", data);
		}

		//
		// GET: /Admin/GetPrecoFuturoEnergiaValors/
		public JsonResult GetPrecoFuturoEnergiaValors()
		{
			var PrecoFuturoEnergiaValors = _precoFuturoEnergiaValorService.GetAll().Select(o => new { o.ID, o.TipoEnergia });
			return Json(PrecoFuturoEnergiaValors, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create(int prfuenid)
		{
			var data = new FormViewModel();
			data.PrecoFuturoEnergiaValor = TempData["PrecoFuturoEnergiaValorModel"] as PrecoFuturoEnergiaValor;
			data.PrecoFuturoEnergia = _precoFuturoEnergiaService.FindByID(prfuenid);
			if (data.PrecoFuturoEnergiaValor == null)
			{
				data.PrecoFuturoEnergiaValor = new PrecoFuturoEnergiaValor();
				data.PrecoFuturoEnergiaValor.PrecoFuturoEnergiaID = prfuenid;

				data.PrecoFuturoEnergiaValor.UpdateFromRequest();
			}
			return AdminContent("PrecoFuturoEnergiaValor/PrecoFuturoEnergiaValorEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.PrecoFuturoEnergiaValor = TempData["PrecoFuturoEnergiaValorModel"] as PrecoFuturoEnergiaValor ?? _precoFuturoEnergiaValorService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.PrecoFuturoEnergiaValor == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.PrecoFuturoEnergia = data.PrecoFuturoEnergiaValor.PrecoFuturoEnergia;

			return AdminContent("PrecoFuturoEnergiaValor/PrecoFuturoEnergiaValorEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var PrecoFuturoEnergiaValor = _precoFuturoEnergiaValorService.FindByID(id);
			if (PrecoFuturoEnergiaValor == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			PrecoFuturoEnergiaValor.ID = null;
			TempData["PrecoFuturoEnergiaValorModel"] = PrecoFuturoEnergiaValor;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(PrecoFuturoEnergiaValor.PrecoFuturoEnergia.ID.Value);
		}

		public ActionResult Del(Int32 id)
		{
			int? precoFuturoEnergiaID = null;

			try
			{
				var precoFuturoEnergiaValor = _precoFuturoEnergiaValorService.FindByID(id);
				if (precoFuturoEnergiaValor == null)
				{
					precoFuturoEnergiaID = precoFuturoEnergiaValor.PrecoFuturoEnergiaID;

					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_precoFuturoEnergiaValorService.Delete(precoFuturoEnergiaValor);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + string.Format("Admin/PrecoFuturoEnergiaValor/?prfuenid={0}", precoFuturoEnergiaID) }, JsonRequestBehavior.AllowGet);
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
			int? precoFuturoEnergiaID = null;

			try
			{
				var precoFuturoEnergiaValorIDs = ids.Split(',').Select(id => id.ToInt(0));
				if (!precoFuturoEnergiaValorIDs.Any())
					throw new Exception();

				var precoFuturoEnergiaValor = _precoFuturoEnergiaValorService.FindByID(precoFuturoEnergiaValorIDs.First());
				if (precoFuturoEnergiaValor != null)
					precoFuturoEnergiaID = precoFuturoEnergiaValor.PrecoFuturoEnergiaID;

				_precoFuturoEnergiaValorService.DeleteMany(precoFuturoEnergiaValorIDs);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + string.Format("Admin/PrecoFuturoEnergiaValor/?prfuenid={0}", precoFuturoEnergiaID) }, JsonRequestBehavior.AllowGet);
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
			var precoFuturoEnergiaValor = new PrecoFuturoEnergiaValor();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					precoFuturoEnergiaValor = _precoFuturoEnergiaValorService.FindByID(Request["ID"].ToInt(0));
					if (precoFuturoEnergiaValor == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				precoFuturoEnergiaValor.UpdateFromRequest();

				var precoFuturoEnergia = _precoFuturoEnergiaService.FindByID(precoFuturoEnergiaValor.PrecoFuturoEnergiaID);
				if (precoFuturoEnergia == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				_precoFuturoEnergiaValorService.Save(precoFuturoEnergiaValor);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? precoFuturoEnergiaValor.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + string.Format("Admin/PrecoFuturoEnergiaValor/?prfuenid={0}", precoFuturoEnergia.ID);
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { precoFuturoEnergiaValor.ID });
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
				TempData["PrecoFuturoEnergiaValorModel"] = precoFuturoEnergiaValor;
				return isEdit && precoFuturoEnergiaValor != null ? RedirectToAction("Edit", new { precoFuturoEnergiaValor.ID }) : RedirectToAction("Create", new { prfuenid = precoFuturoEnergiaValor.PrecoFuturoEnergia.ID });
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
			public List<PrecoFuturoEnergiaValor> PrecoFuturoEnergiaValores;
			public PrecoFuturoEnergia PrecoFuturoEnergia;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public PrecoFuturoEnergiaValor PrecoFuturoEnergiaValor;
			public PrecoFuturoEnergia PrecoFuturoEnergia;
			public Boolean ReadOnly;
		}
	}
}
