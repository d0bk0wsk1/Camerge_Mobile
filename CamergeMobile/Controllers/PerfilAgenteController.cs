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
	public class PerfilAgenteController : ControllerBase
	{
		private readonly ILoggerService _loggerService;
		private readonly IPerfilAgenteService _perfilAgenteService;

		public PerfilAgenteController(ILoggerService loggerService,
			IPerfilAgenteService perfilAgenteService)
		{
			_loggerService = loggerService;
			_perfilAgenteService = perfilAgenteService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _perfilAgenteService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.PerfilAgentes = paging.Items;

			return AdminContent("PerfilAgente/PerfilAgenteList.aspx", data);
		}

		public JsonResult IsPerfilConsumidor(int id)
		{
			var perfilAgente = _perfilAgenteService.FindByID(id);
			if (perfilAgente != null)
				return Json(perfilAgente.IsConsumidor, JsonRequestBehavior.AllowGet);
			return Json(false, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetPerfilAgentes()
		{
			var perfilAgentes = _perfilAgenteService.GetAll().Select(o => new { o.ID, o.Sigla });
			return Json(perfilAgentes, JsonRequestBehavior.AllowGet);
		}

		public JsonResult UpdateIsActive(int id, bool value)
		{
			var perfilAgente = _perfilAgenteService.FindByID(id);
			if (perfilAgente != null)
			{
				perfilAgente.IsActive = value;

				_perfilAgenteService.Update(perfilAgente);

				return Json(new { success = true }, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("PerfilAgente/PerfilAgenteImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("perfil_agente_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _perfilAgenteService.ImportaPerfisAgente(RawData, sobrescreverExistentes);
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
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PerfilAgente";
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.PerfilAgente = TempData["PerfilAgenteModel"] as PerfilAgente;
			if (data.PerfilAgente == null)
			{
				data.PerfilAgente = new PerfilAgente()
				{
					IsActive = true
				};

				data.PerfilAgente.UpdateFromRequest();
			}
			return AdminContent("PerfilAgente/PerfilAgenteEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.PerfilAgente = TempData["PerfilAgenteModel"] as PerfilAgente ?? _perfilAgenteService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.PerfilAgente == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("PerfilAgente/PerfilAgenteEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var perfilAgente = _perfilAgenteService.FindByID(id);
			if (perfilAgente == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			perfilAgente.ID = null;
			TempData["PerfilAgenteModel"] = perfilAgente;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var perfilAgente = _perfilAgenteService.FindByID(id);
				if (perfilAgente == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_perfilAgenteService.Delete(perfilAgente);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PerfilAgente" }, JsonRequestBehavior.AllowGet);
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
				_perfilAgenteService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PerfilAgente" }, JsonRequestBehavior.AllowGet);
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
			var perfilAgente = new PerfilAgente();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					perfilAgente = _perfilAgenteService.FindByID(Request["ID"].ToInt(0));
					if (perfilAgente == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				perfilAgente.UpdateFromRequest();

				var existente = _perfilAgenteService.FindByCodigo(perfilAgente.Codigo);
				if (existente != null && perfilAgente.ID != existente.ID)
					throw new ArgumentException(string.Format("Já existe um perfil de agente cadastrado com este código ({0}).", perfilAgente.Codigo));

				existente = _perfilAgenteService.FindBySigla(perfilAgente.Sigla);
				if (existente != null && perfilAgente.ID != existente.ID)
					throw new ArgumentException(string.Format("Já existe um perfil de agente cadastrado com esta sigla ({0}).", perfilAgente.Sigla));

				perfilAgente.Sigla = perfilAgente.Sigla.ToUpper();

				_perfilAgenteService.Save(perfilAgente);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? perfilAgente.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PerfilAgente";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { perfilAgente.ID });
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
				TempData["PerfilAgenteModel"] = perfilAgente;
				return isEdit && perfilAgente != null ? RedirectToAction("Edit", new { perfilAgente.ID }) : RedirectToAction("Create");
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
			else if (ex.Message.Contains("ativo_fk"))
			{
				errorMessage = "Este perfil de agente está relacionado a um ativo e não pode ser deletado";
			}
			else
			{
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

		public class ListViewModel
		{
			public List<PerfilAgente> PerfilAgentes;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public PerfilAgente PerfilAgente;
			public Boolean ReadOnly;
		}
	}
}
