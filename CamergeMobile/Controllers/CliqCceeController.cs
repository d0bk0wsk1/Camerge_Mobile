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
	public class CliqCceeController : ControllerBase
	{
		private readonly ICliqCceeService _cliqCceeService;
		private readonly IContratoService _contratoService;
		private readonly IContratoTipoService _contratoTipoService;
		private readonly ILoggerService _loggerService;
		private readonly IPerfilAgenteService _perfilAgenteService;
		private readonly ISubmercadoService _submercadoService;

		public CliqCceeController(ICliqCceeService cliqCceeService,
			IContratoService contratoService,
			IContratoTipoService contratoTipoService,
			ILoggerService loggerService,
			IPerfilAgenteService perfilAgenteService,
			ISubmercadoService submercadoService)
		{
			_cliqCceeService = cliqCceeService;
			_contratoService = contratoService;
			_contratoTipoService = contratoTipoService;
			_loggerService = loggerService;
			_perfilAgenteService = perfilAgenteService;
			_submercadoService = submercadoService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _cliqCceeService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params,
				((UserSession.IsCliente) ? UserSession.Agentes.Select(i => i.ID.Value) : null));

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.CliqsCcee = paging.Items;

			if ((data.CliqsCcee.Any()) && (UserSession.IsCliente))
			{
				data.PerfisAgenteCompradorList = _perfilAgenteService.GetByIds(data.CliqsCcee.Select(i => i.PerfilAgenteCompradorID));
				data.PerfisAgenteVendedorList = _perfilAgenteService.GetByIds(data.CliqsCcee.Select(i => i.PerfilAgenteVendedorID));
			}
			else
			{
				var perfisAgente = _perfilAgenteService.GetByTiposRelacao(new[] { PerfilAgente.TiposRelacao.Cliente.ToString(), PerfilAgente.TiposRelacao.Terceiro.ToString() });

				data.PerfisAgenteCompradorList = perfisAgente;
				data.PerfisAgenteVendedorList = perfisAgente;
			}

			return AdminContent("CliqCcee/CliqCceeList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.CliqCcee = TempData["CliqCceeModel"] as CliqCcee;
			if (data.CliqCcee == null)
			{
				data.CliqCcee = new CliqCcee();

				var contratoTipo = _contratoTipoService.GetByNome("CCEAL");
				if (contratoTipo != null)
					data.CliqCcee.ContratoTipo = contratoTipo;

				var submercado = _submercadoService.GetByNome("Sul");
				if (submercado != null)
					data.CliqCcee.Submercado = submercado;

				data.CliqCcee.UpdateFromRequest();
			}

			data.PerfisAgenteList = _perfilAgenteService.GetByTiposRelacao(
				new[] { PerfilAgente.TiposRelacao.Cliente.ToString(), PerfilAgente.TiposRelacao.Terceiro.ToString() }
			);

			return AdminContent("CliqCcee/CliqCceeEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("CliqCcee/CliqCceeImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("cliq_ccee_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _cliqCceeService.ImportaCliqs(RawData, sobrescreverExistentes);
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
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/CliqCcee";
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
			data.CliqCcee = TempData["CliqCceeModel"] as CliqCcee ?? _cliqCceeService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.CliqCcee == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.PerfisAgenteList = _perfilAgenteService.GetByTiposRelacao(
				new[] { PerfilAgente.TiposRelacao.Cliente.ToString(), PerfilAgente.TiposRelacao.Terceiro.ToString() }
			);

			return AdminContent("CliqCcee/CliqCceeEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var medicao = _cliqCceeService.FindByID(id);
			if (medicao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			medicao.ID = null;
			TempData["CliqCceeModel"] = medicao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var cliqCcee = _cliqCceeService.FindByID(id);
			if (cliqCcee == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_cliqCceeService.Delete(cliqCcee);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/CliqCcee" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids)
		{
			_cliqCceeService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/CliqCcee" }, JsonRequestBehavior.AllowGet);
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
			var cliqCcee = new CliqCcee();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					cliqCcee = _cliqCceeService.FindByID(Request["ID"].ToInt(0));
					if (cliqCcee == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				cliqCcee.UpdateFromRequest();

				if (cliqCcee.PrazoInicio > cliqCcee.PrazoFim)
					throw new Exception("Prazo inicial não pode ser superior a prazo final.");

				var cliqCceeExistente = _cliqCceeService.GetByCodigo(cliqCcee.Codigo);
				if (cliqCceeExistente != null)
				{
					if ((!isEdit) || ((isEdit) && (cliqCcee.ID != cliqCceeExistente.ID)))
						throw new Exception("Código já cadastrado.");
				}

				cliqCcee.PrazoInicio = Dates.ToInitialHours(cliqCcee.PrazoInicio);
				cliqCcee.PrazoFim = Dates.ToFinalHours(cliqCcee.PrazoFim);
				_cliqCceeService.Save(cliqCcee);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? cliqCcee.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/CliqCcee";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { cliqCcee.ID });
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
				TempData["CliqCceeModel"] = cliqCcee;
				return isEdit && cliqCcee != null ? RedirectToAction("Edit", new { cliqCcee.ID }) : RedirectToAction("Create");
			}
		}

		public JsonResult GetCliqCcee(int id)
		{
			var cliqCcee = _cliqCceeService.FindByID(id);
			if (cliqCcee != null)
			{
				var viewModel = new CliqCceeViewModel()
				{
					ContratoTipo = cliqCcee.ContratoTipo.Nome,
					Submercado = cliqCcee.Submercado.Nome,
					Codigo = cliqCcee.Codigo,
					PrazoInicio = cliqCcee.PrazoInicio,
					PrazoFim = cliqCcee.PrazoFim,
					Observacao = cliqCcee.Observacao
				};

				return Json(viewModel, JsonRequestBehavior.AllowGet);
			}
			return Json(null);
		}

		public JsonResult GetCliqsCcee(int contrato, int submercado, int? perfil = null, String mes = null)
		{
			var modelContrato = _contratoService.FindByID(contrato);
			if (modelContrato != null)
			{
				var cliqsCcee = _cliqCceeService.GetByContrato(modelContrato, submercado, perfil);
                if (mes !=null)               
                    cliqsCcee = cliqsCcee.Where(w => w.PrazoInicio <= Convert.ToDateTime(mes) && w.PrazoFim >= Convert.ToDateTime(mes));
                
				if (cliqsCcee.Any())
					return Json(cliqsCcee.Select(i => new { i.ID, i.Codigo }), JsonRequestBehavior.AllowGet);
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

		public class FormViewModel
		{
			public CliqCcee CliqCcee;
			public List<PerfilAgente> PerfisAgenteList;
			public bool ReadOnly;
		}

		public class ListViewModel
		{
			public List<CliqCcee> CliqsCcee;
			public List<PerfilAgente> PerfisAgenteCompradorList;
			public List<PerfilAgente> PerfisAgenteVendedorList;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class CliqCceeViewModel
		{
			public string ContratoTipo { get; set; }
			public string Submercado { get; set; }
			public string Codigo { get; set; }
			public DateTime PrazoInicio { get; set; }
			public DateTime PrazoFim { get; set; }
			public string Observacao { get; set; }
			public string FormattedPrazoInicio
			{
				get
				{
					return PrazoInicio.ToString("dd/MM/yyyy HH:mm:ss");
				}
			}
			public string FormattedPrazoFim
			{
				get
				{
					return PrazoFim.ToString("dd/MM/yyyy HH:mm:ss");
				}
			}
		}
	}
}
