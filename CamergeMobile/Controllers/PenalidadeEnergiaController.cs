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
	public class PenalidadeEnergiaController : ControllerBase
	{
		private readonly ILoggerService _loggerService;
		private readonly IPenalidadeEnergiaService _penalidadeEnergiaService;

		public PenalidadeEnergiaController(ILoggerService loggerService,
			IPenalidadeEnergiaService penalidadeEnergiaService)
		{
			_loggerService = loggerService;
			_penalidadeEnergiaService = penalidadeEnergiaService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _penalidadeEnergiaService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.PenalidadesEnergia = paging.Items;

			return AdminContent("PenalidadeEnergia/PenalidadeEnergiaList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.PenalidadeEnergia = TempData["PenalidadeEnergiaModel"] as PenalidadeEnergia;
			if (data.PenalidadeEnergia == null)
			{
				data.PenalidadeEnergia = new PenalidadeEnergia();
				data.PenalidadeEnergia.UpdateFromRequest();
			}
			return AdminContent("PenalidadeEnergia/PenalidadeEnergiaEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("PenalidadeEnergia/PenalidadeEnergiaImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("penalidadeEnergia_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _penalidadeEnergiaService.ImportaPenalidadesEnergia(RawData, sobrescreverExistentes);
				if (processados == 0)
				{
					Web.SetMessage("Nenhum dado foi importado", "info");
				}
				else
				{
					Web.SetMessage("Dados importados com sucesso");
				}
			}
			catch (GenericImportException ex)
			{
				exception = ex;
				friendlyErrorMessage = string.Format("Falha na importação. {0}", ex.Message);
			}
			catch (Exception ex)
			{
				exception = ex;
				friendlyErrorMessage = "Falha na importação. Verifique se os dados estão corretos e tente novamente";
			}

			if (exception != null)
			{
				_loggerService.Log("Exception: " + exception.Message, false);
				Web.SetMessage(friendlyErrorMessage, "error");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				return RedirectToAction("Import");
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PenalidadeEnergia";
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.PenalidadeEnergia = TempData["PenalidadeEnergiaModel"] as PenalidadeEnergia ?? _penalidadeEnergiaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.PenalidadeEnergia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("PenalidadeEnergia/PenalidadeEnergiaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var medicao = _penalidadeEnergiaService.FindByID(id);
			if (medicao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			medicao.ID = null;
			TempData["PenalidadeEnergiaModel"] = medicao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var medicao = _penalidadeEnergiaService.FindByID(id);
			if (medicao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_penalidadeEnergiaService.Delete(medicao);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PenalidadeEnergia" }, JsonRequestBehavior.AllowGet);
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
			_penalidadeEnergiaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PenalidadeEnergia" }, JsonRequestBehavior.AllowGet);
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
			var penalidadeEnergia = new PenalidadeEnergia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					penalidadeEnergia = _penalidadeEnergiaService.FindByID(Request["ID"].ToInt(0));
					if (penalidadeEnergia == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				penalidadeEnergia.UpdateFromRequest();
				_penalidadeEnergiaService.Save(penalidadeEnergia);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? penalidadeEnergia.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PenalidadeEnergia";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { penalidadeEnergia.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["PenalidadeEnergiaModel"] = penalidadeEnergia;
				return isEdit && penalidadeEnergia != null ? RedirectToAction("Edit", new { penalidadeEnergia.ID }) : RedirectToAction("Create");
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

		public class FormViewModel
		{
			public PenalidadeEnergia PenalidadeEnergia;
			public Boolean ReadOnly;
		}

		public class ListViewModel
		{
			public List<PenalidadeEnergia> PenalidadesEnergia;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
