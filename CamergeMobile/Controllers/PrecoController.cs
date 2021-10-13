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
	public class PrecoController : ControllerBase
	{
		private readonly ILoggerService _loggerService;
		private readonly IPrecoService _precoService;

		public PrecoController(ILoggerService loggerService,
			IPrecoService precoService)
		{
			_loggerService = loggerService;
			_precoService = precoService;
		}

		//
		// GET: /Admin/Preco/
		public ActionResult Index(Int32? Page)
		{
			/*
			// Pré seleciona Sul como filtro da lista
			if (Request["submercado"] == null) {
				var sul = Submercado.LoadByNome("Sul");
				if (sul != null) {
					return Redirect(Web.FullUrl + (Web.FullUrl.Contains("?") ? "&" : "?") + "submercado=" + sul.ID);
				}
			}
			*/

			var data = new ListViewModel();
			var paging = _precoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Precos = paging.Items;

			return AdminContent("Preco/PrecoList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Preco = TempData["PrecoModel"] as Preco;

			if (data.Preco == null)
			{
				data.Preco = new Preco();
				data.Preco.UpdateFromRequest();
			}

			return AdminContent("Preco/PrecoEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("Preco/PrecoImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("preco_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _precoService.ImportaPrecos(RawData, sobrescreverExistentes);
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
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				return RedirectToAction("Import");
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Preco";
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
			data.Preco = TempData["PrecoModel"] as Preco ?? _precoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Preco == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Preco/PrecoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var preco = _precoService.FindByID(id);
			if (preco == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			preco.ID = null;
			TempData["PrecoModel"] = preco;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var preco = _precoService.FindByID(id);
			if (preco == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_precoService.Delete(preco);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Preco" }, JsonRequestBehavior.AllowGet);
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

			_precoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Preco" }, JsonRequestBehavior.AllowGet);
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
			var preco = new Preco();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					preco = _precoService.FindByID(Request["ID"].ToInt(0));
					if (preco == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				preco.UpdateFromRequest();

				if (!isEdit)
				{
					var checkExists = _precoService.Get(preco.SubmercadoID.Value, preco.InicioSemana.Value, preco.FimSemana.Value);
					if (checkExists != null)
						throw new Exception("Já existe preço cadastro com este submercado, este início e fim de semana.");
				}

				preco.InicioSemana = Dates.ToInitialHours(preco.InicioSemana.Value);
				preco.FimSemana = Dates.ToFinalHours(preco.FimSemana.Value);
                
                
                //atualiza os valores das vigencias por spread

				_precoService.Save(preco);

				// preco.DeleteChildren();
				// preco.UpdateChildrenFromRequest();
				// preco.SaveChildren();

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? preco.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Preco";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { preco.ID });
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
				TempData["PrecoModel"] = preco;
				return isEdit && preco != null ? RedirectToAction("Edit", new { preco.ID }) : RedirectToAction("Create");
			}
		}

		public ActionResult InformacaoMercado()
		{
			return AdminContent("Preco/InformacaoMercado.ascx");
		}

		public JsonResult GetPld(int submercado, DateTime mes, bool americanFormat = true)
		{
			// JS
			if (!americanFormat)
				mes = new DateTime(mes.Year, mes.Day, 1);

			double value = _precoService.CalcPldMes(submercado, mes);

			return Json(value, JsonRequestBehavior.AllowGet);
		}

		public JsonResult JsonInformacaoMercado()
		{
			var list = _precoService.GetInformacoesMercado();

			return Json(list, JsonRequestBehavior.AllowGet);
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
			public List<Preco> Precos;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Preco Preco;
			public Boolean ReadOnly;
		}

		/*
		public class LiquidacaoFinanceiraViewModel
		{
			public List<PrecoLiquidacaoDiferencaDto> Items { get; set; }
		}
		*/
	}
}
