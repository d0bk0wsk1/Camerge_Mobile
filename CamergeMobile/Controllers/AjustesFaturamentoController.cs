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
    public class AjustesFaturamentoController : ControllerBase
	{
		private readonly IAgenteService _agenteService;		
		private readonly IAjustesFaturamentoService _ajustesFaturamentoService;
        private readonly ILoggerService _loggerService;

        public AjustesFaturamentoController(IAgenteService agenteService,
            IAjustesFaturamentoService ajustesFaturamentoService,
            ILoggerService loggerService)			
		{
			_agenteService = agenteService;
            _ajustesFaturamentoService = ajustesFaturamentoService;
            _loggerService = loggerService;
        }

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _ajustesFaturamentoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.AjustesFaturamento = paging.Items;

			return AdminContent("AjustesFaturamento/AjustesFaturamentoList.aspx", data);
		}

        public ActionResult Create()
        {
            var data = new FormViewModel();
            data.AjustesFaturamento = TempData["AjustesFaturamentoModel"] as AjustesFaturamento;
            if (data.AjustesFaturamento == null)
            {
                data.AjustesFaturamento = new AjustesFaturamento();
                data.AjustesFaturamento.UpdateFromRequest();
            }
            return AdminContent("AjustesFaturamento/AjustesFaturamentoEdit.aspx", data);
        }

        public ActionResult Duplicate(Int32 id)
        {
            var AjustesFaturamento = _ajustesFaturamentoService.FindByID(id);
            if (AjustesFaturamento == null)
            {
                Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
                return RedirectToAction("Index");
            }
            AjustesFaturamento.ID = null;
            TempData["AjustesFaturamentoModel"] = AjustesFaturamento;
            Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
            return Create();
        }

        public ActionResult Del(Int32 id)
        {
            var medicao = _ajustesFaturamentoService.FindByID(id);
            if (medicao == null)
            {
                Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
            }
            else
            {
                _ajustesFaturamentoService.Delete(medicao);
                Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
            }

            if (Fmt.ConvertToBool(Request["ajax"]))
            {
                return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AjustesFaturamento" }, JsonRequestBehavior.AllowGet);
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
            _ajustesFaturamentoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

            Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

            if (Fmt.ConvertToBool(Request["ajax"]))
            {
                return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AjustesFaturamento" }, JsonRequestBehavior.AllowGet);
            }

            var previousUrl = Web.AdminHistory.Previous;
            if (previousUrl != null)
            {
                return Redirect(previousUrl);
            }

            return RedirectToAction("Index");
        }

        public ActionResult View(Int32 id)
        {
            return Edit(id, true);
        }


        public ActionResult Edit(Int32 id, Boolean readOnly = false)
        {
            var data = new FormViewModel();            
            data.AjustesFaturamento = TempData["AjusteModel"] as AjustesFaturamento ?? _ajustesFaturamentoService.FindByID(id);
            data.ReadOnly = readOnly;

            if (data.AjustesFaturamento == null)
            {
                Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
                return RedirectToAction("Index");
            }

            //data = GetDestinos(data);

            return AdminContent("AjustesFaturamento/AjustesFaturamentoEdit.aspx", data);
        }

        [ValidateInput(false)]
        public ActionResult Save()
        {
            var AjustesFaturamento = new AjustesFaturamento();
            var isEdit = Request["ID"].IsNotBlank();

            try
            {
                if (isEdit)
                {
                    AjustesFaturamento = _ajustesFaturamentoService.FindByID(Request["ID"].ToInt(0));
                    if (AjustesFaturamento == null)
                        throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
                }

                AjustesFaturamento.UpdateFromRequest();
                _ajustesFaturamentoService.Save(AjustesFaturamento);

                Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

                var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

                if (Fmt.ConvertToBool(Request["ajax"]))
                {
                    var nextPage = isSaveAndRefresh ? AjustesFaturamento.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AjustesFaturamento";
                    return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
                }

                if (isSaveAndRefresh)
                    return RedirectToAction("Edit", new { AjustesFaturamento.ID });

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
                TempData["AjustesFaturamentoModel"] = AjustesFaturamento;
                return isEdit && AjustesFaturamento != null ? RedirectToAction("Edit", new { AjustesFaturamento.ID }) : RedirectToAction("Create");
            }
        }

        [HttpGet]
        public ActionResult Import()
        {
            return AdminContent("AjustesFaturamento/AjustesFaturamentoImport.aspx");
        }

        [HttpPost]        
        public ActionResult Import(string RawData)
        {
            _loggerService.Setup("ajustesFaturamento_import");

            Exception exception = null;
            string friendlyErrorMessage = null;

            try
            {
                _loggerService.Log("Iniciando Importação", false);
                var processados = _ajustesFaturamentoService.ImportaAjustes(RawData);
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
                var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AjustesFaturamento";
                return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
            }

            var previousUrl = Web.AdminHistory.Previous;
            if (previousUrl != null)
            {
                return Redirect(previousUrl);
            }

            return RedirectToAction("Index");
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
            public AjustesFaturamento AjustesFaturamento;
            public Boolean ReadOnly;
        }


        public class ListViewModel
		{
			public List<AjustesFaturamento> AjustesFaturamento;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}		
	}
}
