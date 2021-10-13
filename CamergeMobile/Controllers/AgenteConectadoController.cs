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
	public class AgenteConectadoController : ControllerBase
	{
		private readonly IAgenteConectadoService _agenteConectadoService;
		private readonly ILoggerService _loggerService;

		public AgenteConectadoController(IAgenteConectadoService agenteConectadoService,
			ILoggerService loggerService)
		{
			_agenteConectadoService = agenteConectadoService;
			_loggerService = loggerService;
		}

		//
		// GET: /Admin/AgenteConectado/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _agenteConectadoService.GetDetailedDtoPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30));

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.AgenteConectados = paging.Items;

			return AdminContent("AgenteConectado/AgenteConectadoList.aspx", data);
		}

		//
		// GET: /Admin/GetAgenteConectados/
		public JsonResult GetAgenteConectados()
		{
			var agenteConectados = _agenteConectadoService.GetAll().Select(o => new { o.ID, o.Nome });
			return Json(agenteConectados, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.AgenteConectado = TempData["AgenteConectadoModel"] as AgenteConectado;
			if (data.AgenteConectado == null)
			{
				data.AgenteConectado = new AgenteConectado();
				data.AgenteConectado.UpdateFromRequest();
			}
			return AdminContent("AgenteConectado/AgenteConectadoEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("AgenteConectado/AgenteConectadoImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("agente_conectado_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = true; // Request["SobrescreverExistentes"].ToBoolean();

				var processados = _agenteConectadoService.ImportaAgentesConectados(RawData, sobrescreverExistentes);
				if (processados == 0)
					Web.SetMessage("Nenhum dado foi importado", "info");
				else
					Web.SetMessage("Dados importados com sucesso");
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
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				return RedirectToAction("Import");
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AgenteConectado";
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.AgenteConectado = TempData["AgenteConectadoModel"] as AgenteConectado ?? _agenteConectadoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.AgenteConectado == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("AgenteConectado/AgenteConectadoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var agenteConectado = AgenteConectado.LoadByID(id);
			if (agenteConectado == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			agenteConectado.ID = null;
			TempData["AgenteConectadoModel"] = agenteConectado;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var agenteConectado = _agenteConectadoService.FindByID(id);
			if (agenteConectado == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_agenteConectadoService.Delete(agenteConectado);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AgenteConectado" }, JsonRequestBehavior.AllowGet);
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
			_agenteConectadoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AgenteConectado" }, JsonRequestBehavior.AllowGet);
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
			var agenteConectado = new AgenteConectado();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{

				if (isEdit)
				{
					agenteConectado = _agenteConectadoService.FindByID(Request["ID"].ToInt(0));
					if (agenteConectado == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				agenteConectado.UpdateFromRequest();
				_agenteConectadoService.Save(agenteConectado);

				// agenteConectado.DeleteChildren();
				// agenteConectado.UpdateChildrenFromRequest();
				// agenteConectado.SaveChildren();

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? agenteConectado.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AgenteConectado";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { agenteConectado.ID });
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
				TempData["AgenteConectadoModel"] = agenteConectado;
				return isEdit && agenteConectado != null ? RedirectToAction("Edit", new { agenteConectado.ID }) : RedirectToAction("Create");
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
			// public List<AgenteConectado> AgenteConectados;
			public List<AgenteConectadoDetailedDto> AgenteConectados;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public AgenteConectado AgenteConectado;
			public Boolean ReadOnly;
		}
	}
}
