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
	public class ContabilizacaoCceeController : ControllerBase
	{
		private readonly IContabilizacaoCceeService _contabilizacaoCceeService;
		private readonly ILoggerService _loggerService;
		private readonly IPerfilAgenteService _perfilAgenteService;

		public ContabilizacaoCceeController(IContabilizacaoCceeService contabilizacaoCceeService,
			ILoggerService loggerService,
			IPerfilAgenteService perfilAgenteService)
		{
			_contabilizacaoCceeService = contabilizacaoCceeService;
			_loggerService = loggerService;
			_perfilAgenteService = perfilAgenteService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _contabilizacaoCceeService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Contabilizacoes = paging.Items;

			return AdminContent("ContabilizacaoCcee/ContabilizacaoCceeList.aspx", data);
		}

		public JsonResult GetContabilizacoes()
		{
			var contabilizacoes = _contabilizacaoCceeService.GetAll().Select(o => new { o.ID, o.PerfilAgenteID });
			return Json(contabilizacoes, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Contabilizacao = TempData["ContabilizacaoCceeModel"] as ContabilizacaoCcee;
			if (data.Contabilizacao == null)
			{
				data.Contabilizacao = new ContabilizacaoCcee();
				data.Contabilizacao.UpdateFromRequest();
			}
			return AdminContent("ContabilizacaoCcee/ContabilizacaoCceeEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("ContabilizacaoCcee/ContabilizacaoCceeImport.aspx");
		}

        [HttpGet]
        public ActionResult ImportPreContab()
        {
            return AdminContent("ContabilizacaoCcee/PreContabilizacaoCceeImport.aspx");
        }

        [HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("contabilizacao_ccee_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _contabilizacaoCceeService.ImportaContabilizacoes(RawData, sobrescreverExistentes);
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
				friendlyErrorMessage = string.Concat("Falha na importação. Verifique se os dados estão corretos e tente novamente.", " (", ex.Message, ")");
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
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContabilizacaoCcee";
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

        [HttpPost]
        public ActionResult ImportPreContab(string RawData)
        {
            _loggerService.Setup("pre_contabilizacao_ccee_import");

            Exception exception = null;
            string friendlyErrorMessage = null;

            try
            {
                _loggerService.Log("Iniciando Importação", false);

                var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

                var processados = _contabilizacaoCceeService.ImportaPreContabilizacoes(RawData, sobrescreverExistentes);
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
                friendlyErrorMessage = string.Concat("Falha na importação. Verifique se os dados estão corretos e tente novamente.", " (", ex.Message, ")");
            }

            if (exception != null)
            {
                _loggerService.Log("Exception: " + exception.Message, false);
                Web.SetMessage(friendlyErrorMessage, "error");

                if (Fmt.ConvertToBool(Request["ajax"]))
                {
                    return Json(new { success = false, message = Web.GetFlashMessageObject() });
                }
                return RedirectToAction("PreContabilizacaoCceeImport");
            }

            if (Fmt.ConvertToBool(Request["ajax"]))
            {
                var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContabilizacaoCcee";
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
			data.Contabilizacao = TempData["ContabilizacaoCceeModel"] as ContabilizacaoCcee ?? _contabilizacaoCceeService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Contabilizacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("ContabilizacaoCcee/ContabilizacaoCceeEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var contabilizacao = _contabilizacaoCceeService.FindByID(id);
			if (contabilizacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			contabilizacao.ID = null;
			TempData["ContabilizacaoCceeModel"] = contabilizacao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var contabilizacao = _contabilizacaoCceeService.FindByID(id);
			if (contabilizacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_contabilizacaoCceeService.Delete(contabilizacao);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContabilizacaoCcee" }, JsonRequestBehavior.AllowGet);
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
			_contabilizacaoCceeService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContabilizacaoCcee" }, JsonRequestBehavior.AllowGet);
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

			if (Request["perfilAgente"].IsNotBlank())
			{
				data.PerfilAgente = _perfilAgenteService.FindByID(Request["perfilAgente"].ToInt(0));
				if (data.PerfilAgente != null)
					data.Contabilizacoes = _contabilizacaoCceeService.GetReport(data.PerfilAgente.ID.Value, Request["monthYearRange"].ConvertToDate(null));
			}

			return AdminContent("ContabilizacaoCcee/ContabilizacaoCceeReport.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var contabilizacao = new ContabilizacaoCcee();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					contabilizacao = _contabilizacaoCceeService.FindByID(Request["ID"].ToInt(0));
					if (contabilizacao == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				contabilizacao.UpdateFromRequest();
				_contabilizacaoCceeService.Save(contabilizacao);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? contabilizacao.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContabilizacaoCcee";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { contabilizacao.ID });
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
				TempData["ContabilizacaoCceeModel"] = contabilizacao;
				return isEdit && contabilizacao != null ? RedirectToAction("Edit", new { contabilizacao.ID }) : RedirectToAction("Create");
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

		public ActionResult PopupHelp()
		{
			return View("~/Areas/Admin/Views/ContabilizacaoCcee/PopupHelp.aspx");
		}

		public class FormViewModel
		{
			public ContabilizacaoCcee Contabilizacao;
			public Boolean ReadOnly;
		}

		public class ListViewModel
		{
			public List<ContabilizacaoCcee> Contabilizacoes;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class ReportViewModel
		{
			public PerfilAgente PerfilAgente;
			public List<ContabilizacaoCcee> Contabilizacoes;
		}
	}
}
