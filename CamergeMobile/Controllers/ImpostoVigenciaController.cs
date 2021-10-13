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
	public class ImpostoVigenciaController : ControllerBase
	{
		private readonly IAgenteConectadoService _agenteConectadoService;
		private readonly IAtivoService _ativoService;
		private readonly IImpostoService _impostoService;
		private readonly IImpostoVigenciaService _impostoVigenciaService;
		private readonly ILoggerService _loggerService;

		public ImpostoVigenciaController(IAgenteConectadoService agenteConectadoService,
			IAtivoService ativoService,
			IImpostoService impostoService,
			IImpostoVigenciaService impostoVigenciaService,
			ILoggerService loggerService)
		{
			_agenteConectadoService = agenteConectadoService;
			_ativoService = ativoService;
			_impostoService = impostoService;
			_impostoVigenciaService = impostoVigenciaService;
			_loggerService = loggerService;
		}

		//
		// GET: /Admin/ImpostoVigencia/
		public ActionResult Index(Int32? agcoid, Int32? impid, Int32? Page)
		{
			var actionParams = Request.Params;

			if ((agcoid != null) && (impid == null))
			{
				if (UserSession.IsPerfilAgente)
				{
					if (!UserSession.Agentes.Any(ag => ag.PerfilAgenteList.Any(peag => peag.AtivoList.Any(at => at.AgenteConectadoID == agcoid.Value))))
						throw new Exception("Este perfil de agente não possui acesso para visualizar esta página.");
				}

				impid = GetIdByAutoCreatedVigencia(agcoid);
				actionParams = Fmt.GetNewNameValueCollection(new { impid = impid, Page = Page }, Request.Params);
			}

			if (impid != null)
			{
				var data = new ListViewModel();

				var paging = _impostoVigenciaService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), actionParams);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.ImpostoVigencias = paging.Items;
				data.Imposto = _impostoService.FindByID(impid.Value);

				return AdminContent("ImpostoVigencia/ImpostoVigenciaList.aspx", data);
			}
			return HttpNotFound();
		}

		//
		// GET: /Admin/GetImpostoVigencias/
		public JsonResult GetImpostoVigencias()
		{
			var impostoVigencias = _impostoVigenciaService.GetAll().Select(o => new { o.ID, o.MesVigencia });
			return Json(impostoVigencias, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create(Int32? impid)
		{
			if (impid != null)
			{
				var data = new FormViewModel();
				data.ImpostoVigencia = TempData["ImpostoVigenciaModel"] as ImpostoVigencia;
				data.Imposto = _impostoService.FindByID(impid.Value);
				if (data.ImpostoVigencia == null)
				{
					data.ImpostoVigencia = new ImpostoVigencia();
					data.ImpostoVigencia.ImpostoID = impid.Value;
					data.ImpostoVigencia.MesVigencia = Dates.GetFirstDayOfMonth(DateTime.Today);

					data.ImpostoVigencia.UpdateFromRequest();
				}
				return AdminContent("ImpostoVigencia/ImpostoVigenciaEdit.aspx", data);
			}
			return HttpNotFound();
		}

		[HttpGet]
		public ActionResult Import(int imposto)
		{
			return AdminContent("ImpostoVigencia/ImpostoVigenciaImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("Imposto_vigencia_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			Imposto imposto = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				imposto = _impostoService.FindByID(Request["ImpostoID"].ToInt(0));
				if (imposto != null)
				{
					var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

					var processados = _impostoVigenciaService.ImportaImpostoVigencias(imposto, RawData, sobrescreverExistentes);
					if (processados == 0)
						Web.SetMessage("Nenhum dado foi importado", "info");
					else
						Web.SetMessage("Dados importados com sucesso");
				}
				else
				{
					throw new Exception("Contrato não localizado.");
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
				return RedirectToAction("Import", new { contrato = imposto.ID });
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ImpostoVigencia/?impid=" + imposto.ID;
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { impid = imposto.ID });
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.ImpostoVigencia = TempData["ImpostoVigenciaModel"] as ImpostoVigencia ?? _impostoVigenciaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.ImpostoVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.Imposto = data.ImpostoVigencia.Imposto;

			return AdminContent("ImpostoVigencia/ImpostoVigenciaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var impostoVigencia = _impostoVigenciaService.FindByID(id);
			if (impostoVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			impostoVigencia.ID = null;
			TempData["ImpostoVigenciaModel"] = impostoVigencia;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(impostoVigencia.ImpostoID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var impostoVigencia = _impostoVigenciaService.FindByID(id);
				if (impostoVigencia == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_impostoVigenciaService.Delete(impostoVigencia);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ImpostoVigencia" }, JsonRequestBehavior.AllowGet);
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
				_impostoVigenciaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ImpostoVigencia" }, JsonRequestBehavior.AllowGet);
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
			var impostoVigencia = new ImpostoVigencia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					impostoVigencia = _impostoVigenciaService.FindByID(Request["ID"].ToInt(0));
					if (impostoVigencia == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				impostoVigencia.UpdateFromRequest();

				var imposto = _impostoService.FindByID(impostoVigencia.ImpostoID);
				if (imposto == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				var checkVigencia = _impostoVigenciaService.Get(imposto.ID.Value, impostoVigencia.MesVigencia, (isEdit) ? impostoVigencia.ID : null);
				if (checkVigencia != null)
					throw new Exception("Já existe esta vigência neste agente conectado.");

				_impostoVigenciaService.Save(impostoVigencia);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? impostoVigencia.GetAdminURL() : Web.BaseUrl + "Admin/ImpostoVigencia/?impid=" + impostoVigencia.ImpostoID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { impostoVigencia.ID });
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
				TempData["ImpostoVigenciaModel"] = impostoVigencia;
				return isEdit && impostoVigencia != null ? RedirectToAction("Edit", new { impostoVigencia.ID }) : RedirectToAction("Create", impostoVigencia.ImpostoID);
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

		public int? GetIdByAutoCreatedVigencia(int? agcoid = null)
		{
			if (agcoid != null)
			{
				var agenteConectado = _agenteConectadoService.FindByID(agcoid.Value);
				if (agenteConectado != null)
				{
					var imposto = _impostoService.Find(new Sql("WHERE agente_conectado_id = @0;", agenteConectado.ID));
					if (imposto == null)
					{
						var model = new Imposto() { AgenteConectadoID = agenteConectado.ID };

						_impostoService.Insert(model);

						return model.ID.Value;
					}
					return imposto.ID.Value;
				}
			}
			return null;
		}

		public class ListViewModel
		{
			public List<ImpostoVigencia> ImpostoVigencias;
			public Imposto Imposto;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public ImpostoVigencia ImpostoVigencia;
			public Imposto Imposto;
			public Boolean ReadOnly;
		}
	}
}
