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
	public class ContratoVigenciaController : ControllerBase
	{
		private readonly IContratoService _contratoService;
		private readonly IContratoVigenciaService _contratoVigenciaService;
		private readonly IContratoVigenciaCompradorService _contratoVigenciaCompradorService;
		private readonly IContratoVigenciaVendedorService _contratoVigenciaVendedorService;
		private readonly IContratoVigenciaBalancoService _contratoVigenciaBalancoService;
		private readonly IContratoVigenciaHasAttachmentService _contratoVigenciaHasAttachmentService;
		private readonly IDescontoService _descontoService;
		private readonly ILoggerService _loggerService;
		private readonly ISubmercadoService _submercadoService;

		public ContratoVigenciaController(IContratoService contratoService,
			IContratoVigenciaService contratoVigenciaService,
			IContratoVigenciaCompradorService contratoVigenciaCompradorService,
			IContratoVigenciaVendedorService contratoVigenciaVendedorService,
			IContratoVigenciaBalancoService contratoVigenciaBalancoService,
			IContratoVigenciaHasAttachmentService contratoVigenciaHasAttachmentService,
			IDescontoService descontoService,
			ILoggerService loggerService,
			ISubmercadoService submercadoService)
		{
			_contratoService = contratoService;
			_contratoVigenciaService = contratoVigenciaService;
			_contratoVigenciaCompradorService = contratoVigenciaCompradorService;
			_contratoVigenciaVendedorService = contratoVigenciaVendedorService;
			_contratoVigenciaBalancoService = contratoVigenciaBalancoService;
			_contratoVigenciaHasAttachmentService = contratoVigenciaHasAttachmentService;
			_descontoService = descontoService;
			_loggerService = loggerService;
			_submercadoService = submercadoService;
		}

		public ActionResult Index(int contrato, Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _contratoVigenciaService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.ContratoVigencias = paging.Items;

			if (data.ContratoVigencias.Any())
			{
				data.MatchingMontantes = _contratoVigenciaService.GetMatchingMontantesBalancos(data.ContratoVigencias);
				data.HasAtivosCompradorAssociado = data.ContratoVigencias.Any(i => i.ContratoVigenciaCompradorList.Any());
				data.HasAtivosVendedorAssociado = data.ContratoVigencias.Any(i => i.ContratoVigenciaVendedorList.Any());
				data.HasConsumoLiquido = data.ContratoVigencias.Any(i => i.ConsumoLiquido > 0);
				data.HasPrecoAtualizadoOverridden = data.ContratoVigencias.Any(i => i.PrecoOverridden != null);
			}

			return AdminContent("ContratoVigencia/ContratoVigenciaList.aspx", data);
		}

		public ActionResult Create(int contrato)
		{
			var model = _contratoService.FindByID(contrato);

			var data = new FormViewModel();
			data.Contrato = _contratoService.FindByID(contrato);
			data.ContratoVigencia = TempData["ContratoVigenciaModel"] as ContratoVigencia;
			if (data.ContratoVigencia == null)
			{
				var submercado = _submercadoService.GetByNome("Sul");

				data.ContratoVigencia = new ContratoVigencia()
				{
					ContratoID = contrato,
					DescontoID = data.Contrato.PerfilAgenteVendedor.DescontoID,
					SubmercadoID = (submercado == null) ? null : submercado.ID,
					PrazoInicio = model.PrazoInicio,
					PrazoFim = model.PrazoFim,
					SazonalizacaoPositiva = 0,
					SazonalizacaoNegativa = 0,
					FlexibilidadePositiva = 0,
					FlexibilidadeNegativa = 0,
					Modulacao = ContratoVigencia.Modulacoes.Flat.ToString(),
					Retusd = 35,
					IsMontantePreDefinido = false,
					PerdasContrato = 0 // 2.5
				};

				data.ContratoVigencia.UpdateFromRequest();
			}

			data.IsSameMonth = IsSameMonth(model);
			if (!data.IsSameMonth)
				data.IsProinfa = IsProinfa(model);

			if (data.IsSameMonth)
			{
				data.ContratoVigencia.ApuracaoMontante = ContratoVigencia.ApuracoesMontante.Fixo.ToString();
				data.ContratoVigencia.PerdasContrato = 0;
			}
			else if (data.IsProinfa)
			{
				data.ContratoVigencia.ApuracaoMontante = ContratoVigencia.ApuracoesMontante.Fixo.ToString();
				data.ContratoVigencia.Preco = 0;
				data.ContratoVigencia.Retusd = 0;
				data.ContratoVigencia.SazonalizacaoNegativa = 100;
				data.ContratoVigencia.SazonalizacaoPositiva = 100;
				data.ContratoVigencia.TipoEnergia = 0;
			}

			return AdminContent("ContratoVigencia/ContratoVigenciaEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import(int contrato)
		{
			return AdminContent("ContratoVigencia/ContratoVigenciaImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("contrato_vigencia_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			Contrato contrato = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				contrato = _contratoService.FindByID(Request["ContratoID"].ToInt(0));
				if (contrato != null)
				{
					var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

					var processados = _contratoVigenciaService.ImportaContratosVigencia(contrato, RawData, sobrescreverExistentes);
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
				return RedirectToAction("Import", new { contrato = contrato.ID });
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigencia/?contrato=" + contrato.ID;
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contrato.ID });
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.ContratoVigencia = TempData["ContratoVigenciaModel"] as ContratoVigencia ?? _contratoVigenciaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.ContratoVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.Contrato = data.ContratoVigencia.Contrato;
			data.IsSameMonth = IsSameMonth(data.ContratoVigencia.Contrato);
			if (!data.IsSameMonth)
				data.IsProinfa = IsProinfa(data.ContratoVigencia.Contrato);

			return AdminContent("ContratoVigencia/ContratoVigenciaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var contratoVigencia = _contratoVigenciaService.FindByID(id);
			if (contratoVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			contratoVigencia.ContratoVigenciaCompradorList = _contratoVigenciaCompradorService.GetByContratoVigencia(contratoVigencia.ID.Value).ToList();
			contratoVigencia.ContratoVigenciaVendedorList = _contratoVigenciaVendedorService.GetByContratoVigencia(contratoVigencia.ID.Value).ToList();
			contratoVigencia.ID = null;
			TempData["ContratoVigenciaModel"] = contratoVigencia;

			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(contratoVigencia.ContratoID.Value);
		}

		public ActionResult Del(Int32 id)
		{
			var contratoVigencia = _contratoVigenciaService.FindByID(id);
			if (contratoVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_contratoVigenciaBalancoService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);
				_contratoVigenciaVendedorService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);
				_contratoVigenciaCompradorService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);
				_contratoVigenciaService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);

				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigencia/?contrato=" + contratoVigencia.ContratoID }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contratoVigencia.ContratoID });
		}

		public ActionResult DelMultiple(String ids)
		{
			Contrato contrato = null;

			var contratosVigenciaID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosVigenciaID.Any())
			{
				contrato = _contratoService.FindByID(contratosVigenciaID.First());

				foreach (var contratoVigenciaID in contratosVigenciaID)
				{
					_contratoVigenciaBalancoService.DeleteByContratoVigenciaID(contratoVigenciaID);
					_contratoVigenciaVendedorService.DeleteByContratoVigenciaID(contratoVigenciaID);
					_contratoVigenciaCompradorService.DeleteByContratoVigenciaID(contratoVigenciaID);
					_contratoVigenciaService.DeleteByContratoVigenciaID(contratoVigenciaID);
				}

				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (contrato == null)
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigencia/?contrato=" + contrato.ID }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contrato.ID });
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var contratoVigencia = new ContratoVigencia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					contratoVigencia = _contratoVigenciaService.FindByID(Request["ID"].ToInt(0));
					if (contratoVigencia == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				contratoVigencia.UpdateFromRequest();

				if (IsSameMonth(contratoVigencia.Contrato))
					contratoVigencia.PrazoFim = contratoVigencia.PrazoInicio;

				/*
				if (IsCurtoPrazo(contratoVigencia.Contrato) && ((contratoVigencia.PrazoFim - contratoVigencia.PrazoInicio).TotalDays > 31))
					throw new Exception("A diferença entre fim e início da vigência não pode ser superior a 31 dias.");
				*/

				if ((contratoVigencia.PrazoInicio < contratoVigencia.Contrato.PrazoInicio)
					|| (contratoVigencia.PrazoInicio > contratoVigencia.Contrato.PrazoFim)
					|| (contratoVigencia.PrazoFim < contratoVigencia.Contrato.PrazoInicio)
					|| (contratoVigencia.PrazoFim > contratoVigencia.Contrato.PrazoFim))
					throw new Exception("O(s) prazo(s) da vigência deve(m) estar dentro do range do prazo do contrato.");

				if (contratoVigencia.PrazoInicio > contratoVigencia.PrazoFim)
					throw new Exception("Prazo inicial não pode ser superior a prazo final.");

				/*
				if ((contratoVigencia.ApuracaoMontante.Contains("Consumo")) && (contratoVigencia.ConsumoLiquido == null))
					throw new Exception("Campo 'Consumo Líquido' precisa estar preenchido para este tipo de apuração de montante.");
				*/

				contratoVigencia.PrazoInicio = Dates.GetFirstDayOfMonth(contratoVigencia.PrazoInicio);
				contratoVigencia.PrazoFim = Dates.GetLastDayOfMonth(contratoVigencia.PrazoFim);

				contratoVigencia.SazonalizacaoPositiva = Fmt.ToDouble(contratoVigencia.SazonalizacaoPositiva, false, true);
				contratoVigencia.SazonalizacaoNegativa = Fmt.ToDouble(contratoVigencia.SazonalizacaoNegativa, false, true);
				contratoVigencia.FlexibilidadePositiva = Fmt.ToDouble(contratoVigencia.FlexibilidadePositiva, false, true);
				contratoVigencia.FlexibilidadeNegativa = Fmt.ToDouble(contratoVigencia.FlexibilidadeNegativa, false, true);
				contratoVigencia.PerdasContrato = Fmt.ToDouble(contratoVigencia.PerdasContrato, false, true);
				contratoVigencia.ConsumoLiquido = Fmt.ToDouble(contratoVigencia.ConsumoLiquido, false, true);

				if (contratoVigencia.MontanteMwm != null)
					contratoVigencia.MontanteMwm = Math.Round(contratoVigencia.MontanteMwm.Value, 6);

				if (contratoVigencia.ProporcaoGeracao != null)
					contratoVigencia.ProporcaoGeracao = Fmt.ToDouble(contratoVigencia.ProporcaoGeracao, false, true);

				if (isEdit)
				{
					_contratoVigenciaCompradorService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);
					_contratoVigenciaVendedorService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);
					_contratoVigenciaBalancoService.UpdateIsInvalidoByContratoVigenciaID(contratoVigencia.ID.Value, true);
				}

				var compradoresId = Fmt.ToListIds(Request.Form["ContratoVigenciaCompradorIDList"]);
				var vendedoresId = Fmt.ToListIds(Request.Form["ContratoVigenciaVendedorIDList"]);

				if (!compradoresId.Any())
				{
					if ((contratoVigencia.CliqCcee != null)
						&& (contratoVigencia.CliqCcee.ContratoTipo.Nome.Equals("Proinfa", StringComparison.InvariantCultureIgnoreCase)))
						throw new Exception("Obrigatório o preenchimento do ativo comprador para este Cliq CCEE do tipo PROINFA.");

					//if ((contratoVigencia.ApuracaoMontante == ContratoVigencia.ApuracoesMontante.Consumo.ToString())
						//|| (contratoVigencia.ApuracaoMontante == ContratoVigencia.ApuracoesMontante.ConsumoPerdas.ToString())
					//	|| (contratoVigencia.ApuracaoMontante == ContratoVigencia.ApuracoesMontante.ConsumoPerdasProinfa.ToString()))
					//	throw new Exception("Obrigatório o preenchimento de pelo menos um ativo comprador para esta apuração montante.");
				}
				if (!vendedoresId.Any())
				{
					if (contratoVigencia.ApuracaoMontante == ContratoVigencia.ApuracoesMontante.Geracao.ToString())
						throw new Exception("Obrigatório o preenchimento de pelo menos um ativo vendedor.");
				}

				_contratoVigenciaService.Save(contratoVigencia);

				_contratoVigenciaHasAttachmentService.DeleteMany(contratoVigencia.ContratoVigenciaHasAttachmentList);

				contratoVigencia.UpdateChildrenFromRequest();

				_contratoVigenciaHasAttachmentService.InsertMany(contratoVigencia.ContratoVigenciaHasAttachmentList);

				if (compradoresId.Any())
				{
					if ((IsProinfa(contratoVigencia.Contrato)) && (compradoresId.Count() > 1))
						throw new Exception("Apenas 1 ativo comprador é permitido para um contrato do tipo 'Proinfa'.");

					_contratoVigenciaCompradorService.InsertMany(contratoVigencia.ID.Value, compradoresId);
				}
				if (vendedoresId.Any())
				{
					_contratoVigenciaVendedorService.InsertMany(contratoVigencia.ID.Value, vendedoresId);
				}

				if (Request["autobalancos"].ToBoolean())
				{
					_contratoVigenciaBalancoService.GenerateFromContratoVigencia(contratoVigencia, true);
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? contratoVigencia.GetAdminURL() : /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + string.Format("Admin/ContratoVigencia/?contrato={0}", contratoVigencia.ContratoID);
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { contratoVigencia.ID });

				/*
				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				*/
				return RedirectToAction("Index", new { contrato = contratoVigencia.ContratoID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["ContratoVigenciaModel"] = contratoVigencia;
				return isEdit && contratoVigencia != null ? RedirectToAction("Edit", new { contratoVigencia.ID }) : RedirectToAction("Create", new { contrato = contratoVigencia.ContratoID });
			}
		}

		public JsonResult GetRetusd(int desconto)
		{
			var descontoRetusd = _descontoService.FindByID(desconto);
			if (descontoRetusd != null)
				return Json(((descontoRetusd.Valor / 100) * 70), JsonRequestBehavior.AllowGet);
			return Json(0, JsonRequestBehavior.AllowGet);
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

		private bool IsSameMonth(Contrato contrato)
		{
			// return (contrato.ContratoCaracteristica.Nome.Equals("Curto Prazo", StringComparison.InvariantCultureIgnoreCase));

			return (Dates.IsSameMonthYear(contrato.PrazoInicio, contrato.PrazoFim));
		}

		private bool IsProinfa(Contrato contrato)
		{
			return (contrato.ContratoCaracteristica.Nome.Equals("Proinfa", StringComparison.InvariantCultureIgnoreCase));
		}

		public class FormViewModel
		{
			public ContratoVigencia ContratoVigencia;
			public Contrato Contrato;
			public bool IsSameMonth;
			public bool IsProinfa;
			public bool ReadOnly;
		}

		public class ListViewModel
		{
			public List<ContratoVigencia> ContratoVigencias;
			public Dictionary<int, bool> MatchingMontantes;
			public bool HasAtivosCompradorAssociado;
			public bool HasAtivosVendedorAssociado;
			public bool HasConsumoLiquido;
			public bool HasPrecoAtualizadoOverridden;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
			public List<DateTime> Prazos
			{
				get
				{
					if (ContratoVigencias.Any())
						return Dates.GetByIntervalInMonths(ContratoVigencias.Min(i => i.PrazoInicio), ContratoVigencias.Max(i => i.PrazoFim));
					return new List<DateTime>();
				}
			}
		}
	}
}
