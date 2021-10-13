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
	public class AgenteController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly ILoggerService _loggerService;
		private readonly ILoginLogService _loginLogService;
		private readonly IPersonService _personService;
		private readonly IRoleService _roleService;

		public AgenteController(IAgenteService agenteService,
			ILoggerService loggerService,
			ILoginLogService loginLogService,
			IPersonService personService,
			IRoleService roleService)
		{
			_agenteService = agenteService;
			_loggerService = loggerService;
			_loginLogService = loginLogService;
			_personService = personService;
			_roleService = roleService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _agenteService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Agentes = paging.Items;

			return AdminContent("Agente/AgenteList.aspx", data);
		}

		public JsonResult GetAgente(int id)
		{
			var agente = _agenteService.FindByID(id);
			if (agente != null)
			{
				var obj = new { id = agente.ID, nome = agente.Nome, terceiro = agente.IsTerceiro };
				return Json(obj, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetAgentes()
		{
			var agentes = _agenteService.GetAll().Select(o => new { o.ID, o.Nome });
			return Json(agentes, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetTipoLeitura(string ids)
		{
			string tipoLeitura = null;

			var agentesID = ids.Split(',');
			if (agentesID.Count() == 1)
			{
				var agente = _agenteService.FindByID(agentesID[0].ToInt(0));
				if (agente != null)
				{
					if (agente.PerfilAgenteList.Any(i => i.IsConsumidor))
						tipoLeitura = Medicao.TiposLeitura.Consumo.ToString();
					else if (agente.PerfilAgenteList.Any(i => i.IsGerador))
						tipoLeitura = Medicao.TiposLeitura.Geracao.ToString();
				}
			}

			return Json(tipoLeitura, JsonRequestBehavior.AllowGet);
		}

		public JsonResult UpdateIsActive(int id, bool value)
		{
			var agente = _agenteService.FindByID(id);
			if (agente != null)
			{
				agente.IsActive = value;

				_agenteService.Update(agente);

				return Json(new { success = true }, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("Agente/AgenteImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("agente_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _agenteService.ImportaAgentes(RawData, sobrescreverExistentes); // n inseri unidade federativa aqui
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
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Agente";
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
			data.Agente = TempData["AgenteModel"] as Agente;
			if (data.Agente == null)
			{
				data.Agente = new Agente() { IsTerceiro = false, IsActive = true };
				data.Agente.UpdateFromRequest();
			}
			return AdminContent("Agente/AgenteEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Agente = TempData["AgenteModel"] as Agente ?? _agenteService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Agente == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Agente/AgenteEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var agente = _agenteService.FindByID(id);
			if (agente == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			agente.ID = null;
			TempData["AgenteModel"] = agente;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var agente = _agenteService.FindByID(id);
				if (agente == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_agenteService.Delete(agente);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Agente" }, JsonRequestBehavior.AllowGet);
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
				_agenteService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Agente" }, JsonRequestBehavior.AllowGet);
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
			var agente = new Agente();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					agente = _agenteService.FindByID(Request["ID"].ToInt(0));
					if (agente == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				agente.UpdateFromRequest();

				/*
				var roleDefault = _roleService.GetDefault();
				if (roleDefault != null)
				{
					if (!isEdit)
					{
						var person = new Person()
						{
							RoleID = roleDefault.ID,
							Name = agente.Nome,
							Password = BaseSecurity.HashPassword(Request["Password"]),
							IsActive = true
						};
						_personService.Save(person);
						agente.PersonID = person.ID;
					}
					else
					{
						agente.Person.Name = agente.Nome;
						if (Request["Password"].IsNotBlank())
						{
							agente.Person.Password = BaseSecurity.HashPassword(Request["Password"]);
							agente.Person.IsActive = true;
							_loginLogService.AddBasedOnController(agente.Person.ID.Value, Web.Request.ServerVariables["HTTP_USER_AGENT"]);
						}
						_personService.Save(agente.Person);
					}
				}
				*/

				_agenteService.Save(agente);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? agente.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Agente";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { agente.ID });

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
				TempData["AgenteModel"] = agente;
				return isEdit && agente != null ? RedirectToAction("Edit", new { agente.ID }) : RedirectToAction("Create");
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
			else if (ex.Message.Contains("perfil_agente_fk"))
			{
				errorMessage = "Este agente está relacionado a um perfil de agente e não pode ser deletado";
			}
			else
			{
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

		public class ListViewModel
		{
			public List<Agente> Agentes;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Agente Agente;
			public Boolean ReadOnly;
		}
	}
}
