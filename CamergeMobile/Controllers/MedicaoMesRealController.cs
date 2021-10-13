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
	public class MedicaoMesRealController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ILoggerService _loggerService;
		private readonly IMedicaoMesRealService _medicaoMesRealService;

		public MedicaoMesRealController(IAtivoService ativoService,
			ILoggerService loggerService,
			IMedicaoMesRealService medicaoMesRealService)
		{
			_ativoService = ativoService;
			_loggerService = loggerService;
			_medicaoMesRealService = medicaoMesRealService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _medicaoMesRealService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Medicoes = paging.Items;

			return AdminContent("MedicaoMesReal/MedicaoMesRealList.aspx", data);
		}

        

        public JsonResult GetMedicoes()
		{
			var medicoes = _medicaoMesRealService.GetAll().Select(o => new { o.ID, o.AtivoID });
			return Json(medicoes, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Medicao = TempData["MedicaoMesRealModel"] as MedicaoMesReal;
			if (data.Medicao == null)
			{
				data.Medicao = new MedicaoMesReal();
				data.Medicao.UpdateFromRequest();
			}
			return AdminContent("MedicaoMesReal/MedicaoMesRealEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("MedicaoMesReal/MedicaoMesRealImport.aspx");
		}

        
        public ActionResult ImportJSON()
        {
            
             return AdminContent("MedicaoMesReal/MedicaoMesRealJSON.aspx");
        }

        [HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("medicao_mes_real_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _medicaoMesRealService.ImportaMedicoes(RawData, sobrescreverExistentes);
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
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoMesReal";
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
			data.Medicao = TempData["MedicaoMesRealModel"] as MedicaoMesReal ?? _medicaoMesRealService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Medicao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("MedicaoMesReal/MedicaoMesRealEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var medicao = _medicaoMesRealService.FindByID(id);
			if (medicao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			medicao.ID = null;
			TempData["MedicaoMesRealModel"] = medicao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var medicao = _medicaoMesRealService.FindByID(id);
			if (medicao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_medicaoMesRealService.Delete(medicao);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoMesReal" }, JsonRequestBehavior.AllowGet);
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
			_medicaoMesRealService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoMesReal" }, JsonRequestBehavior.AllowGet);
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
			var medicao = new MedicaoMesReal();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					medicao = _medicaoMesRealService.FindByID(Request["ID"].ToInt(0));
					if (medicao == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				medicao.UpdateFromRequest();
				medicao.ConsumoTotal = (medicao.ConsumoPonta + medicao.ConsumoForaPonta);

				var medicaoExistente = _medicaoMesRealService.Get(medicao.AtivoID.Value, medicao.Mes, medicao.TipoLeitura);
				if ((medicaoExistente != null) && (medicaoExistente.ID != medicao.ID))
					throw new Exception("Já existe uma medição cadastrada neste ativo, neste mês e para este tipo de leitura.");

				_medicaoMesRealService.Save(medicao);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? medicao.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoMesReal";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { medicao.ID });
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
				TempData["MedicaoMesRealModel"] = medicao;
				return isEdit && medicao != null ? RedirectToAction("Edit", new { medicao.ID }) : RedirectToAction("Create");
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
			return View("~/Areas/Admin/Views/MedicaoMesReal/PopupHelp.aspx");
		}

		public class FormViewModel
		{
			public MedicaoMesReal Medicao;
			public Boolean ReadOnly;
		}

		public class ListViewModel
		{
			public List<MedicaoMesReal> Medicoes;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
