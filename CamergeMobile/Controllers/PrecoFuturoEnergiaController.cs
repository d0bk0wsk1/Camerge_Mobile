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
	public class PrecoFuturoEnergiaController : ControllerBase
	{
		private readonly IDescontoService _descontoService;
		private readonly IPrecoFuturoEnergiaService _precoFuturoEnergiaService;

		public PrecoFuturoEnergiaController(IDescontoService descontoService,
			IPrecoFuturoEnergiaService precoFuturoEnergiaService)
		{
			_descontoService = descontoService;
			_precoFuturoEnergiaService = precoFuturoEnergiaService;
		}

		//
		// GET: /Admin/PrecoFuturoEnergia/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _precoFuturoEnergiaService.GetDetailedDtoPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = (paging.Items.Count() / paging.ItemsPerPage); // paging.TotalPages;
			data.TotalRows = (paging.Items.Count()); // paging.TotalItems;
			data.PrecosFuturoEnergia = paging.Items;

			return AdminContent("PrecoFuturoEnergia/PrecoFuturoEnergiaList.aspx", data);
		}

		//
		// GET: /Admin/GetPrecoFuturoEnergias/
		public JsonResult GetPrecoFuturoEnergias()
		{
			var precoFuturoEnergias = _precoFuturoEnergiaService.GetAll().Select(o => new { o.ID, o.Ano });
			return Json(precoFuturoEnergias, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.PrecoFuturoEnergia = TempData["PrecoFuturoEnergiaModel"] as PrecoFuturoEnergia;
			if (data.PrecoFuturoEnergia == null)
			{
				data.PrecoFuturoEnergia = new PrecoFuturoEnergia();
				data.PrecoFuturoEnergia.UpdateFromRequest();
			}
			return AdminContent("PrecoFuturoEnergia/PrecoFuturoEnergiaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.PrecoFuturoEnergia = TempData["PrecoFuturoEnergiaModel"] as PrecoFuturoEnergia ?? _precoFuturoEnergiaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.PrecoFuturoEnergia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("PrecoFuturoEnergia/PrecoFuturoEnergiaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var precoFuturoEnergia = _precoFuturoEnergiaService.FindByID(id);
			if (precoFuturoEnergia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			precoFuturoEnergia.ID = null;
			TempData["PrecoFuturoEnergiaModel"] = precoFuturoEnergia;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var precoFuturoEnergia = _precoFuturoEnergiaService.FindByID(id);
				if (precoFuturoEnergia == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_precoFuturoEnergiaService.Delete(precoFuturoEnergia);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PrecoFuturoEnergia" }, JsonRequestBehavior.AllowGet);
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
				_precoFuturoEnergiaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PrecoFuturoEnergia" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult Report()
		{
			var data = new ReportViewModel();

			if (Request["desconto"] != null)
			{
				data.DescontoID = Request["desconto"].ToInt();

				var desconto = _descontoService.FindByID(data.DescontoID);
				if (desconto != null)
					data.Anos = _precoFuturoEnergiaService.GetAnosByDesconto(desconto);
			}

			return AdminContent("PrecoFuturoEnergia/PrecoFuturoEnergiaReport.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var precoFuturoEnergia = new PrecoFuturoEnergia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					precoFuturoEnergia = _precoFuturoEnergiaService.FindByID(Request["ID"].ToInt(0));
					if (precoFuturoEnergia == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				precoFuturoEnergia.UpdateFromRequest();

				if (!isEdit)
				{
					var checkPreco = _precoFuturoEnergiaService.Find(new Sql("WHERE ano = @0", precoFuturoEnergia.Ano));
					if (checkPreco != null)
						throw new Exception("Ano já cadastrado.");
				}

				_precoFuturoEnergiaService.Save(precoFuturoEnergia);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? precoFuturoEnergia.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PrecoFuturoEnergia";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { precoFuturoEnergia.ID });
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
				TempData["PrecoFuturoEnergiaModel"] = precoFuturoEnergia;
				return isEdit && precoFuturoEnergia != null ? RedirectToAction("Edit", new { precoFuturoEnergia.ID }) : RedirectToAction("Create");
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
			public List<PrecoFuturoEnergiaDetailedDto> PrecosFuturoEnergia;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public PrecoFuturoEnergia PrecoFuturoEnergia;
			public Boolean ReadOnly;
		}

		public class ReportViewModel
		{
			public List<PrecoFuturoEnergiaAnoDto> Anos { get; set; }
			public int DescontoID { get; set; }
		}
	}
}
