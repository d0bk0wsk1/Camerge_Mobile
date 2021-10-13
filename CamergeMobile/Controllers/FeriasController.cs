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
	public class FeriasController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IFeriasService _feriasService;

		public FeriasController(IAtivoService ativoService,
			IFeriasService feriasService)
		{
			_ativoService = ativoService;
			_feriasService = feriasService;
		}

		//
		// GET: /Admin/Ferias/
		public ActionResult Index(int ativo, Int32? Page)
		{
			var data = new ListViewModel();

			data.Ativo = _ativoService.FindByID(ativo);
			if (data.Ativo != null)
			{
				var paging = _feriasService.GetAllWithPaging(
					ativo,
					Page ?? 1,
					Util.GetSettingInt("ItemsPerPage", 30),
					Request.Params);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.Ferias = paging.Items;

				return AdminContent("Ferias/FeriasList.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Create(int ativo)
		{
			var data = new FormViewModel();
			data.Ferias = TempData["FeriasModel"] as Ferias;
			if (data.Ferias == null)
			{
				data.Ferias = new Ferias()
				{
					AtivoID = ativo
				};
				data.Ferias.UpdateFromRequest();
			}
			return AdminContent("Ferias/FeriasEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Ferias = TempData["FeriasModel"] as Ferias ?? _feriasService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Ferias == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Ferias/FeriasEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var ferias = _feriasService.FindByID(id);
			if (ferias == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			ferias.ID = null;
			TempData["FeriasModel"] = ferias;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(ferias.AtivoID.Value);
		}

		public ActionResult Del(Int32 id)
		{
			var ferias = _feriasService.FindByID(id);
			if (ferias == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_feriasService.Delete(ferias);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Ferias" }, JsonRequestBehavior.AllowGet);
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

			_feriasService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Ferias" }, JsonRequestBehavior.AllowGet);
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
			var ferias = new Ferias();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					ferias = _feriasService.FindByID(Request["ID"].ToInt(0));
					if (ferias == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				ferias.UpdateFromRequest();

				if ((ferias.DataFim.Value - ferias.DataInicio.Value).TotalDays > 31)
					throw new Exception("A diferença de dias entre a data fim e a data início não pode ser superior a 31 dias.");

				_feriasService.Save(ferias);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? ferias.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Ferias?ativo=" + ferias.AtivoID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { ferias.ID });

				/*
				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				*/
				return RedirectToAction("Index", new { ativo = ferias.AtivoID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["FeriasModel"] = ferias;
				return isEdit && ferias != null ? RedirectToAction("Edit", new { ferias.ID }) : RedirectToAction("Create", new { ativo = ferias.AtivoID });
			}
		}

		[ChildActionOnly]
		public ActionResult MinList()
		{
			return AdminView("Ferias/FeriasMinList.ascx");
		}

		public JsonResult GetFeriasByDate(int ativo, DateTime date)
		{
			var dtIni = Dates.ToInitialHours(date);
			var dtFim = Dates.ToFinalHours(date);

			return GetFeriasByRange(ativo, dtIni, dtFim);
		}

		public JsonResult GetFeriasByMonthYear(int ativo, DateTime date)
		{
			var dtIni = Dates.GetFirstDayOfMonth(date);
			var dtFim = Dates.GetLastDayOfMonth(date);

			return GetFeriasByRange(ativo, dtIni, dtFim);
		}

		public JsonResult GetFeriasByRange(int ativo, DateTime dtini, DateTime dtfim)
		{
			var ativoFerias = _ativoService.FindByID(ativo);
			if (ativoFerias != null)
			{
				if (ativoFerias.FeriasList.Any())
				{
					var feriasVigentes = ativoFerias.FeriasList.Where(i => ((dtini >= i.DataInicio && dtini <= i.DataFim) || (dtfim >= i.DataInicio && dtfim <= i.DataFim)));
					if (feriasVigentes.Any())
					{
						return Json(
							feriasVigentes.Select(s => new
							{
								AtivoID = s.AtivoID.Value,
								DataInicio = s.DataInicio.Value.FmtDate(),
								DataFim = s.DataFim.Value.FmtDate()
							}),
							JsonRequestBehavior.AllowGet
						);
					}
				}
			}

			return Json(null, JsonRequestBehavior.AllowGet);
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
			public List<Ferias> Ferias;
			public Ativo Ativo;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Ferias Ferias;
			public Boolean ReadOnly;
		}
	}
}
