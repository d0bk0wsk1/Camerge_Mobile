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
	public class ContratoVigenciaBalancoController : ControllerBase
	{
		private readonly ICalculoContratoService _calculoContratoService;
		private readonly IContratoService _contratoService;
		private readonly IContratoVigenciaService _contratoVigenciaService;
		private readonly IContratoVigenciaBalancoService _contratoVigenciaBalancoService;
		private readonly IContratoVigenciaBalancoDeclaradoService _contratoVigenciaBalancoDeclaradoService;
		private readonly IContratoVigenciaBalancoHasAttachmentService _contratoVigenciaBalancoHasAttachmentService;
		private readonly IContratoVigenciaTransferenciaService _contratoVigenciaTransferenciaService;
		private readonly ILoggerService _loggerService;
		private readonly IPatamarService _patamarService;
		private readonly IPermissionActionService _permissionActionService;
        private readonly IPerfilAgenteService _perfilAgenteService;

        public ContratoVigenciaBalancoController(ICalculoContratoService calculoContratoService,
			IContratoService contratoService,
			IContratoVigenciaService contratoVigenciaService,
			IContratoVigenciaBalancoService contratoVigenciaBalancoService,
			IContratoVigenciaBalancoDeclaradoService contratoVigenciaBalancoDeclaradoService,
			IContratoVigenciaBalancoHasAttachmentService contratoVigenciaBalancoHasAttachmentService,
			IContratoVigenciaTransferenciaService contratoVigenciaTransferenciaService,
			ILoggerService loggerService,
			IPatamarService patamarService,
			IPermissionActionService permissionActionService,
            IPerfilAgenteService perfilAgenteService)
		{
			_calculoContratoService = calculoContratoService;
			_contratoService = contratoService;
			_contratoVigenciaService = contratoVigenciaService;
			_contratoVigenciaBalancoService = contratoVigenciaBalancoService;
			_contratoVigenciaBalancoDeclaradoService = contratoVigenciaBalancoDeclaradoService;
			_contratoVigenciaBalancoHasAttachmentService = contratoVigenciaBalancoHasAttachmentService;
			_contratoVigenciaTransferenciaService = contratoVigenciaTransferenciaService;
			_loggerService = loggerService;
			_patamarService = patamarService;
			_permissionActionService = permissionActionService;
            _perfilAgenteService = perfilAgenteService;
        }

		public ActionResult Index(int contrato, int vigencia, Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _contratoVigenciaBalancoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params,
				false);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.ContratosVigenciaBalanco = paging.Items;

			if (data.ContratosVigenciaBalanco.Any())
			{
				var fromDate = data.ContratosVigenciaBalanco.Min(i => i.Mes);
				var toDate = Dates.GetLastDayOfMonth(data.ContratosVigenciaBalanco.Max(i => i.Mes));

				data.CheckboxesAccess = _permissionActionService.GetActionsByController(UserSession.Person.RoleID.Value, "ContratoVigenciaBalanco");

				data.HorasMeses = _patamarService.GetHorasMeses(fromDate, toDate);
				data.MontantesApuracao = _calculoContratoService.GetMontantesApuracaoDto(data.ContratosVigenciaBalanco, true);
				data.PrecosAtualizados = _calculoContratoService.GetPrecosAtualizadoDto(data.ContratosVigenciaBalanco);
				data.NotasFiscais = _contratoService.GetContratosNfDto(data.ContratosVigenciaBalanco);
				data.HasFlexibilidadeUtilizada = data.MontantesApuracao.Any(i => i.FlexibilidadeUtilizada != null && i.FlexibilidadeUtilizada != 0);
				data.HasModulacaoDeclarada = data.ContratosVigenciaBalanco.Any(i => i.ContratoVigenciaBalancoDeclaradoList.Any());
				data.HasPerdas = data.ContratosVigenciaBalanco.Any(i => i.PerdasMes > 0);
				data.HasTransferencias = data.ContratosVigenciaBalanco.Any(i => i.ContratoVigenciaTransferenciaList.Any());
				data.HasDiasUteisEnvioMedicao = data.ContratosVigenciaBalanco.Any(i => i.ContratoVigencia.DiasUteisEnvioMedicao > 0);
				data.HasObservacoes = data.ContratosVigenciaBalanco.Any(i => i.Observacao != null);                
            }
            //teste = data.montanteApuracaoMes.MontanteApuracao.Value)/ contratoVigenciaBalanco.MontanteMwmMes.Value)-1)
			return AdminContent("ContratoVigenciaBalanco/ContratoVigenciaBalancoList.aspx", data);
		}

		public ActionResult Create(int contrato, int vigencia)
		{
			var data = new FormViewModel();
			data.ContratoVigencia = _contratoVigenciaService.FindByID(vigencia);
			data.ContratoVigenciaBalanco = TempData["ContratoVigenciaBalancoModel"] as ContratoVigenciaBalanco;
			if (data.ContratoVigenciaBalanco == null)
			{
				var contratoVigencia = data.ContratoVigencia;

                data.ContratoVigenciaBalanco = new ContratoVigenciaBalanco()
                {
                    ContratoVigenciaID = vigencia,
                    CliqCceeID = contratoVigencia.CliqCceeID,
                    MontanteMwmMes = contratoVigencia.MontanteMwm ?? 0,
                    PerdasMes = ((contratoVigencia.PerdasContrato ?? 0) * 100),
                    IsInvalido = false,
                    IsAjustado = false,
                    IsAptoAjusteCliqCcee = false,
                    IsAptoValidacao = false,
                    IsValidado = false,
                    IsFaturamento = false,
                    IsMedicaoEnviada = false,
                    Mes = DateTime.Now
				};

				data.CheckboxesAccess = _permissionActionService.GetActionsByController(UserSession.Person.RoleID.Value, "ContratoVigenciaBalanco");

				data.ContratoVigenciaBalanco.UpdateFromRequest();
			}
			return AdminContent("ContratoVigenciaBalanco/ContratoVigenciaBalancoEdit.aspx", data);
		}

		[HttpGet]
		public ActionResult Import(int vigencia)
		{
			return AdminContent("ContratoVigenciaBalanco/ContratoVigenciaBalancoImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("contrato_vigencia_balanco_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			ContratoVigencia contratoVigencia = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				contratoVigencia = _contratoVigenciaService.FindByID(Request["ContratoVigenciaID"].ToInt(0));
				if (contratoVigencia != null)
				{
					var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

					var processados = _contratoVigenciaBalancoService.ImportaContratosVigenciaBalanco(contratoVigencia, RawData, sobrescreverExistentes);
					if (processados == 0)
						Web.SetMessage("Nenhum dado foi importado", "info");
					else
						Web.SetMessage("Dados importados com sucesso");
				}
				else
				{
					throw new Exception("Vigência de contrato não localizada.");
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
				return RedirectToAction("Import", new { vigencia = contratoVigencia.ID });
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigenciaBalanco/?contrato=" + contratoVigencia.ContratoID + "&vigencia=" + contratoVigencia.ID;
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contratoVigencia.ContratoID, vigencia = contratoVigencia.ID });
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.ContratoVigenciaBalanco = TempData["ContratoVigenciaBalancoModel"] as ContratoVigenciaBalanco ?? _contratoVigenciaBalancoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.ContratoVigenciaBalanco == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.ContratoVigencia = data.ContratoVigenciaBalanco.ContratoVigencia;
			data.ContratoVigenciaBalanco.MontanteCliqCcee = _calculoContratoService.GetMontanteApuracao(data.ContratoVigenciaBalanco);
			data.ContratoVigenciaBalanco.PrecoAtualizado = _calculoContratoService.GetPrecoAtualizado(data.ContratoVigenciaBalanco);
			data.ContratoVigenciaBalanco.ContratoVigenciaBalancoDeclaradoList = _contratoVigenciaBalancoDeclaradoService.GetByContratoVigenciaBalanco(data.ContratoVigenciaBalanco);

			data.Modulacoes = _calculoContratoService.GetModulacoes(data.ContratoVigenciaBalanco);

			data.CheckboxesAccess = _permissionActionService.GetActionsByController(UserSession.Person.RoleID.Value, "ContratoVigenciaBalanco");

            //data.MaxMontanteMwmMes = (data.ContratoVigenciaBalanco.MontanteMwmMes ?? 0)-_calculoContratoService.GetTotalMontanteMwmMesTransferencias(data.ContratoVigenciaBalanco);
            data.MaxMontanteMwmMes = 0;
            data.PerfisAgenteList = _perfilAgenteService.GetByTiposRelacao(
                new[] { PerfilAgente.TiposRelacao.Cliente.ToString(), PerfilAgente.TiposRelacao.Terceiro.ToString() }
            );


            return AdminContent("ContratoVigenciaBalanco/ContratoVigenciaBalancoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(id);
			if (contratoVigenciaBalanco == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			contratoVigenciaBalanco.ID = null;
			TempData["ContratoVigenciaBalancoModel"] = contratoVigenciaBalanco;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(contratoVigenciaBalanco.ContratoVigencia.ContratoID.Value, contratoVigenciaBalanco.ContratoVigenciaID.Value);
		}

		public ActionResult Del(Int32 id)
		{
			var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(id);
			if (contratoVigenciaBalanco == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_contratoVigenciaBalancoDeclaradoService.DeleteByContratoVigenciaBalancoID(contratoVigenciaBalanco.ID.Value);
				_contratoVigenciaBalancoService.Delete(contratoVigenciaBalanco);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigenciaBalanco/?contrato=" + contratoVigenciaBalanco.ContratoVigencia.ContratoID + "&vigencia=" + contratoVigenciaBalanco.ContratoVigenciaID }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaBalanco.ContratoVigenciaID });
		}

		public ActionResult DelMultiple(String ids)
		{
			ContratoVigencia contratoVigencia = null;

			var contratosVigenciaBalancoID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosVigenciaBalancoID.Any())
			{
				var firstContratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(contratosVigenciaBalancoID.First());
				if (firstContratoVigenciaBalanco != null)
				{
					contratoVigencia = firstContratoVigenciaBalanco.ContratoVigencia;

					foreach (var contratoVigenciaBalancoID in contratosVigenciaBalancoID)
						_contratoVigenciaBalancoDeclaradoService.DeleteByContratoVigenciaBalancoID(contratoVigenciaBalancoID);

					_contratoVigenciaBalancoService.DeleteMany(contratosVigenciaBalancoID);

					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}

			if (contratoVigencia == null)
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigenciaBalanco/?contrato=" + contratoVigencia.ContratoID + "&vigencia=" + contratoVigencia.ID }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contratoVigencia.ContratoID, vigencia = contratoVigencia.ID });
		}

		public FileResult ExportToXml(int contrato, int vigencia, Int32? Page)
		{
			//var fullUrl = Web.AdminHistory.Previous;
			//var basicUrl = "".Substring(fullUrl.IndexOf("/Admin"));
			var basicUrl = "";

			var balancos = _contratoVigenciaBalancoService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params, false);

			var zipPath = _contratoService.ExportToXml(balancos.Items, true, UserSession.Person.ID, basicUrl);
			if (zipPath != null)
			{
				var fileName = string.Concat(balancos.Items.First().ContratoVigencia.Contrato.NumeroInternoControle + "_", zipPath.RightFrom("\\\\"));
				Web.SetMessage("Contrato(s) exportado(s) com sucesso.", "SaveSuccess");
				return File(zipPath, "application/zip", fileName);
			}
			Web.SetMessage("Nenhum contrato exportado.", "error");
			return null;
		}

		public ActionResult ExportToXmlMultiple(String ids)
		{
			ContratoVigencia contratoVigencia = null;

			var contratosVigenciaBalancoID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosVigenciaBalancoID.Any())
			{
				var contratoVigenciaBalancos = new List<ContratoVigenciaBalanco>();

				foreach (var contratoVigenciaBalancoID in contratosVigenciaBalancoID)
				{
					var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(contratoVigenciaBalancoID);
					if (contratoVigenciaBalanco != null)
						contratoVigenciaBalancos.Add(contratoVigenciaBalanco);
				}

				if (contratoVigenciaBalancos.Any())
				{
					var fullUrl = Web.AdminHistory.Previous;
					var basicUrl = fullUrl.Substring(fullUrl.IndexOf("/Admin"));

					_contratoService.ExportToXml(contratoVigenciaBalancos, true, UserSession.Person.ID, basicUrl);

					contratoVigencia = contratoVigenciaBalancos.First().ContratoVigencia;
					Web.SetMessage("Exportado(s) com sucesso.");
				}
			}

			if (contratoVigencia == null)
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.BaseUrl + "Admin/ContratoVigenciaBalanco/?contrato=" + contratoVigencia.ContratoID + "&vigencia=" + contratoVigencia.ID }, JsonRequestBehavior.AllowGet);
			return RedirectToAction("Index", new { contrato = contratoVigencia.ContratoID, vigencia = contratoVigencia.ID });
		}

		public ActionResult GenerateMonths(int contrato, int vigencia, bool rewrite = false)
		{
			var contratoVigencia = _contratoVigenciaService.FindByID(vigencia);
			if (contratoVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				var balancos = _contratoVigenciaBalancoService.GenerateFromContratoVigencia(contratoVigencia, rewrite);
				if (balancos.Any())
					Web.SetMessage("Meses criados com suesso.");
				else
					return Json(new { success = false, message = "Falha ao criar o meses automaticamente." }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ContratoVigenciaBalanco/?contrato=" + contrato + "&vigencia=" + vigencia }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index", new { mapceid = contratoVigencia.ID });
		}

		public ActionResult UpdateIsAjustadoMultiple(string ids)
		{
			return UpdateCheckboxesByInvertedValue(ids, "IsAjustado");
		}

		public ActionResult UpdateIsAptoAjusteCliqCceeMultiple(string ids)
		{
			return UpdateCheckboxesByInvertedValue(ids, "IsAptoAjusteCliqCcee");
		}

		public ActionResult UpdateIsAptoValidacaoMultiple(string ids)
		{
			return UpdateCheckboxesByInvertedValue(ids, "IsAptoValidacao");
		}

		public ActionResult UpdateIsFaturamentoMultiple(string ids)
		{
			return UpdateCheckboxesByInvertedValue(ids, "IsFaturamento");
		}

		public ActionResult UpdateIsInvalidoMultiple(string ids)
		{
			return UpdateCheckboxesByInvertedValue(ids, "IsInvalido");
		}

		public ActionResult UpdateIsMedicaoEnviadaMultiple(string ids)
		{
			return UpdateCheckboxesByInvertedValue(ids, "IsMedicaoEnviada");
		}

		public ActionResult UpdateIsValidadoMultiple(string ids)
		{
			return UpdateCheckboxesByInvertedValue(ids, "IsValidado");
		}

		public ActionResult UpdateCheckboxIsAjustado(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaBalanco.IsAjustado), value);
		}

		public ActionResult UpdateCheckboxIsAptoAjusteCliqCcee(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaBalanco.IsAptoAjusteCliqCcee), value);
		}

		public ActionResult UpdateCheckboxIsAptoValidacao(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaBalanco.IsAptoValidacao), value);
		}

		public ActionResult UpdateCheckboxIsFaturamento(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaBalanco.IsFaturamento), value);
		}

		public ActionResult UpdateCheckboxIsInvalido(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaBalanco.IsInvalido), value);
		}

		public ActionResult UpdateCheckboxIsMedicaoEnviada(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaBalanco.IsMedicaoEnviada), value);
		}

		public ActionResult UpdateCheckboxIsValidado(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaBalanco.IsValidado), value);
		}

		public ActionResult UpdateCheckboxesByInvertedValue(string ids, string field)
		{
			ContratoVigencia contratoVigencia = null;

			var contratosVigenciaBalancoID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosVigenciaBalancoID.Any())
			{
				foreach (var contratoVigenciaBalancoID in contratosVigenciaBalancoID)
				{
					var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(contratoVigenciaBalancoID);
					if (contratoVigenciaBalanco != null)
					{
						if (contratoVigencia == null)
							contratoVigencia = contratoVigenciaBalanco.ContratoVigencia;

						switch (field)
						{
							case "IsInvalido":
								contratoVigenciaBalanco.IsInvalido = !contratoVigenciaBalanco.IsInvalido;
								break;
							case "IsAjustado":
								contratoVigenciaBalanco.IsAjustado = !contratoVigenciaBalanco.IsAjustado;
								break;
							case "IsAptoAjusteCliqCcee":
								contratoVigenciaBalanco.IsAptoAjusteCliqCcee = !contratoVigenciaBalanco.IsAptoAjusteCliqCcee;
								break;
							case "IsAptoValidacao":
								contratoVigenciaBalanco.IsAptoValidacao = !contratoVigenciaBalanco.IsAptoValidacao;
								break;
							case "IsFaturamento":
								contratoVigenciaBalanco.IsFaturamento = !contratoVigenciaBalanco.IsFaturamento;
								SetValoresFaturados(contratoVigenciaBalanco);
								break;
							case "IsMedicaoEnviada":
								contratoVigenciaBalanco.IsMedicaoEnviada = !contratoVigenciaBalanco.IsMedicaoEnviada;
								break;
							case "IsValidado":
								contratoVigenciaBalanco.IsValidado = !contratoVigenciaBalanco.IsValidado;
								break;
						}

						_contratoVigenciaBalancoService.Update(contratoVigenciaBalanco);
					}
				}
			}

			if (contratoVigencia == null)
				return Json(new { success = false, message = "", nextPage = Web.BaseUrl + "Admin/Contrato" }, JsonRequestBehavior.AllowGet);
			return Json(new { success = true, message = "", nextPage = Web.BaseUrl + "Admin/ContratoVigenciaBalanco/?contrato=" + contratoVigencia.ContratoID + "&vigencia=" + contratoVigencia.ID }, JsonRequestBehavior.AllowGet);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var contratoVigenciaBalanco = new ContratoVigenciaBalanco();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(Request["ID"].ToInt(0));
					if (contratoVigenciaBalanco == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				contratoVigenciaBalanco.UpdateFromRequest();

				if ((contratoVigenciaBalanco.Mes < contratoVigenciaBalanco.ContratoVigencia.PrazoInicio) || (contratoVigenciaBalanco.Mes > contratoVigenciaBalanco.ContratoVigencia.PrazoFim))
					throw new Exception("O(s) prazo(s) do balanço deve(m) estar dentro do range do prazo da vigência.");

				var contratoVigenciaBalancoExistente = _contratoVigenciaBalancoService.Get(contratoVigenciaBalanco.ContratoVigenciaID.Value, contratoVigenciaBalanco.Mes);
				if (contratoVigenciaBalancoExistente != null)
				{
					if ((!isEdit) || ((isEdit) && (contratoVigenciaBalanco.ID != contratoVigenciaBalancoExistente.ID)))
						throw new Exception("Mês já cadastrado.");
				}

				_contratoVigenciaBalancoHasAttachmentService.DeleteMany(contratoVigenciaBalanco.ContratoVigenciaBalancoHasAttachmentList);
				_contratoVigenciaBalancoDeclaradoService.DeleteMany(contratoVigenciaBalanco.ContratoVigenciaBalancoDeclaradoList);

				contratoVigenciaBalanco.UpdateChildrenFromRequest();

				if (contratoVigenciaBalanco.ContratoVigenciaBalancoDeclaradoList.Any())
				{
					var patamarHorasSemana = _patamarService.GetHorasMes(contratoVigenciaBalanco.Mes);
					if (patamarHorasSemana.Any())
					{
						if ((contratoVigenciaBalanco.MontanteMwmMes * patamarHorasSemana.Sum(i => i.Horas)) != contratoVigenciaBalanco.ContratoVigenciaBalancoDeclaradoList.Sum(i => i.MwhLeve + i.MwhMedio + i.MwhPesado))
							throw new Exception("O MWh do montante mês deve ser igual ao montante total do declarado.");
					}
				}

				if (!isEdit)
					contratoVigenciaBalanco.DateAdded = DateTime.Now;

				// contratoVigenciaBalanco.MontanteCliqCcee = null;
				// contratoVigenciaBalanco.PrecoAtualizado = null;
				// contratoVigenciaBalanco.FlexibilidadeUtilizada = Fmt.ToDouble(contratoVigenciaBalanco.FlexibilidadeUtilizada, false, true);
				contratoVigenciaBalanco.PerdasMes = Fmt.ToDouble(contratoVigenciaBalanco.PerdasMes, false, true);

				if (contratoVigenciaBalanco.MontanteMwmMes != null)
					contratoVigenciaBalanco.MontanteMwmMes = Math.Round(contratoVigenciaBalanco.MontanteMwmMes.Value, 6);

				// Salva os valores faturados
				SetValoresFaturados(contratoVigenciaBalanco);

				_contratoVigenciaBalancoService.Save(contratoVigenciaBalanco);

				_contratoVigenciaBalancoDeclaradoService.InsertMany(contratoVigenciaBalanco.ContratoVigenciaBalancoDeclaradoList);
				_contratoVigenciaBalancoHasAttachmentService.InsertMany(contratoVigenciaBalanco.ContratoVigenciaBalancoHasAttachmentList);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? contratoVigenciaBalanco.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + string.Format("Admin/ContratoVigenciaBalanco/?contrato={0}&vigencia={1}", contratoVigenciaBalanco.ContratoVigencia.ContratoID, contratoVigenciaBalanco.ContratoVigenciaID);
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { contratoVigenciaBalanco.ID });

				/*
				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				*/
				return RedirectToAction("Index", new { contrato = contratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaBalanco.ContratoVigenciaID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["ContratoVigenciaBalancoModel"] = contratoVigenciaBalanco;
				return isEdit && contratoVigenciaBalanco != null ? RedirectToAction("Edit", new { contratoVigenciaBalanco.ID }) : RedirectToAction("Create", new { contrato = contratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaBalanco.ContratoVigenciaID });
			}
		}

		public JsonResult GetModulacao(int vigencia, DateTime date, double montante, string unidade = "mwh")
		{
			var contratoVigencia = _contratoVigenciaService.FindByID(vigencia);
			if (contratoVigencia != null)
			{
				var modulacao = _calculoContratoService.GetModulacoes(contratoVigencia, date, montante, unidade);
				if (modulacao.Any())
					return Json(modulacao, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetModulacaoDeclarado(DateTime date, int? id = null)
		{
			if (id != null)
			{
				var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(id.Value);
				if ((contratoVigenciaBalanco != null) && (contratoVigenciaBalanco.Mes != date))
					id = null;
			}

			var declarados = _contratoVigenciaBalancoDeclaradoService.GetByContratoVigenciaBalanco(date, id);

			var obj = declarados.Select(i => new { i.ID, InicioSemana = i.InicioSemana.Value.ToString("dd/MM/yyyy"), FimSemana = i.FimSemana.Value.ToString("dd/MM/yyyy"), i.MwhLeve, i.MwhMedio, i.MwhPesado });
			return Json(obj, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetMontanteCliqCcee(int vigencia, DateTime date, double montante, double perdas)
		{
			CalculoContratoMontanteApuracaoDto montanteCliqCcee = null;

			var contratoVigencia = _contratoVigenciaService.FindByID(vigencia);
			if (contratoVigencia != null)
				montanteCliqCcee = _calculoContratoService.GetMontanteApuracaoDto(contratoVigencia, date, montante, perdas);

			return Json(montanteCliqCcee, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetPrecoAtualizado(int vigencia, DateTime date)
		{
			double? value = null;

			var contratoVigencia = _contratoVigenciaService.FindByID(vigencia);
			if (contratoVigencia != null)
				value = _calculoContratoService.GetPrecoAtualizado(contratoVigencia, date);

			return Json(value, JsonRequestBehavior.AllowGet);
		}

		public JsonResult UpdateCheckboxes(int id, string field, bool value)
		{
			var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(id);
			if (contratoVigenciaBalanco != null)
			{
				switch (field)
				{
					case "IsInvalido":
						contratoVigenciaBalanco.IsInvalido = value;
						break;
					case "IsAjustado":
						contratoVigenciaBalanco.IsAjustado = value;
						break;
					case "IsAptoAjusteCliqCcee":
						contratoVigenciaBalanco.IsAptoAjusteCliqCcee = value;
						break;
					case "IsValidado":
						contratoVigenciaBalanco.IsValidado = value;
						break;
					case "IsAptoValidacao":
						contratoVigenciaBalanco.IsAptoValidacao = value;
						break;
					case "IsFaturamento":
						contratoVigenciaBalanco.IsFaturamento = value;
						SetValoresFaturados(contratoVigenciaBalanco);
						break;
					case "IsMedicaoEnviada":
						contratoVigenciaBalanco.IsMedicaoEnviada = value;
						break;
				}

				_contratoVigenciaBalancoService.Update(contratoVigenciaBalanco);

				return Json(new { success = true }, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		private void SetValoresFaturados(ContratoVigenciaBalanco contratoVigenciaBalanco)
		{
			if (contratoVigenciaBalanco.IsFaturamento)
			{
				if (contratoVigenciaBalanco.MontanteCliqCceeFaturado == null)
					contratoVigenciaBalanco.MontanteCliqCceeFaturado = _calculoContratoService.GetMontanteApuracao(contratoVigenciaBalanco);
				if (contratoVigenciaBalanco.PrecoAtualizadoFaturado == null)
					contratoVigenciaBalanco.PrecoAtualizadoFaturado = _calculoContratoService.GetPrecoAtualizado(contratoVigenciaBalanco);
			}
			else
			{
				contratoVigenciaBalanco.MontanteCliqCceeFaturado = null;
				contratoVigenciaBalanco.PrecoAtualizadoFaturado = null;
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

		public class FormViewModel
		{
			public ContratoVigenciaBalanco ContratoVigenciaBalanco;
			public ContratoVigencia ContratoVigencia;
			public List<ContratoVigenciaModulacaoDto> Modulacoes;
			public List<string> CheckboxesAccess;
			public bool ReadOnly;
            public List<PerfilAgente> PerfisAgenteList;
            public double MaxMontanteMwmMes;

        }

		public class ListViewModel
		{
			public List<ContratoVigenciaBalanco> ContratosVigenciaBalanco;
			public List<CalculoContratoMontanteApuracaoDto> MontantesApuracao;
			public List<CalculoContratoPrecoAtualizadoDto> PrecosAtualizados;
			public List<ContratoNfDto> NotasFiscais;
			public IEnumerable<PatamarHorasMesDto> HorasMeses;
			public List<string> CheckboxesAccess;
			public bool HasFlexibilidadeUtilizada;
			public bool HasModulacaoDeclarada;
			public bool HasPerdas;
			public bool HasMontanteCliqCceeOverridden;
			public bool HasPrecoAtualizadoOverridden;
			public bool HasTransferencias;
			public bool HasDiasUteisEnvioMedicao;
			public bool HasObservacoes;
			public long TotalRows;
			public long PageCount;
			public long PageNum;            
        }
	}
}
