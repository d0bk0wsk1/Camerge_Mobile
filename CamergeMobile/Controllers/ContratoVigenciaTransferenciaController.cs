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
	public class ContratoVigenciaTransferenciaController : ControllerBase
	{
		private readonly ICalculoContratoService _calculoContratoService;
		private readonly IContratoService _contratoService;
		private readonly IContratoVigenciaBalancoService _contratoVigenciaBalancoService;
		private readonly IContratoVigenciaTransferenciaService _contratoVigenciaTransferenciaService;
		private readonly IContratoVigenciaTransferenciaHasAttachmentService _contratoVigenciaTransferenciaHasAttachmentService;
		private readonly IPatamarService _patamarService;
		private readonly IPerfilAgenteService _perfilAgenteService;
		private readonly IPermissionActionService _permissionActionService;

		public ContratoVigenciaTransferenciaController(ICalculoContratoService calculoContratoService,
			IContratoService contratoService,
			IContratoVigenciaBalancoService contratoVigenciaBalancoService,
			IContratoVigenciaTransferenciaService contratoVigenciaTransferenciaService,
			IContratoVigenciaTransferenciaHasAttachmentService contratoVigenciaTransferenciaHasAttachmentService,
			IPatamarService patamarService,
			IPerfilAgenteService perfilAgenteService,
			IPermissionActionService permissionActionService)
		{
			_calculoContratoService = calculoContratoService;
			_contratoService = contratoService;
			_contratoVigenciaBalancoService = contratoVigenciaBalancoService;
			_contratoVigenciaTransferenciaService = contratoVigenciaTransferenciaService;
			_contratoVigenciaTransferenciaHasAttachmentService = contratoVigenciaTransferenciaHasAttachmentService;
			_patamarService = patamarService;
			_perfilAgenteService = perfilAgenteService;
			_permissionActionService = permissionActionService;
		}

		public ActionResult Index(int contrato, int vigencia, int balanco, Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _contratoVigenciaTransferenciaService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.ContratosVigenciaTransferencia = paging.Items;

			if (data.ContratosVigenciaTransferencia.Any())
			{
				data.CheckboxesAccess = _permissionActionService.GetActionsByController(UserSession.Person.RoleID.Value, "ContratoVigenciaBalanco");

				data.HasDiasUteisEnvioMedicao = data.ContratosVigenciaTransferencia.Any(i => i.ContratoVigenciaBalanco.ContratoVigencia.DiasUteisEnvioMedicao > 0);
				data.HasObservacoes = data.ContratosVigenciaTransferencia.Any(i => i.Observacao != null);

				var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(balanco);
				if (contratoVigenciaBalanco != null)
				{
					var patamarHorasMes = _patamarService.GetHorasMes(contratoVigenciaBalanco.Mes);
					if (patamarHorasMes != null)
						data.HorasMes = patamarHorasMes.Sum(i => i.Horas);
				}
			}

			return AdminContent("ContratoVigenciaTransferencia/ContratoVigenciaTransferenciaList.aspx", data);
		}

		public ActionResult Create(int contrato, int vigencia, int balanco)
		{
			var data = new FormViewModel();
			data.ContratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(balanco);
			data.ContratoVigenciaTransferencia = TempData["ContratoVigenciaTransferenciaModel"] as ContratoVigenciaTransferencia;
			if (data.ContratoVigenciaTransferencia == null)
			{
				var contratoVigenciaBalanco = data.ContratoVigenciaBalanco;

				data.ContratoVigenciaTransferencia = new ContratoVigenciaTransferencia()
				{
					ContratoVigenciaBalancoID = balanco,
					CliqCceeID = contratoVigenciaBalanco.CliqCceeID,
					MontanteMwmMes = contratoVigenciaBalanco.MontanteMwmMes ?? 0,
					IsInvalido = false,
					IsAjustado = false,
					IsAptoAjusteCliqCcee = false,
					IsAptoValidacao = false,
					IsValidado = false,
					IsFaturamento = false,
					IsMedicaoEnviada = false
				};

				data.MaxMontanteMwmMes = ((_calculoContratoService.GetMontanteApuracao(data.ContratoVigenciaBalanco) ?? 0));
				//- _calculoContratoService.GetTotalMontanteMwmMesTransferencias(data.ContratoVigenciaBalanco));

				data.PerfisAgenteList = _perfilAgenteService.GetByTiposRelacao(
					new[] { PerfilAgente.TiposRelacao.Cliente.ToString(), PerfilAgente.TiposRelacao.Terceiro.ToString() }
				);

				data.CheckboxesAccess = _permissionActionService.GetActionsByController(UserSession.Person.RoleID.Value, "ContratoVigenciaTransferencia");

				data.ContratoVigenciaTransferencia.UpdateFromRequest();
			}
			return AdminContent("ContratoVigenciaTransferencia/ContratoVigenciaTransferenciaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.ContratoVigenciaTransferencia = TempData["ContratoVigenciaTransferenciaModel"] as ContratoVigenciaTransferencia ?? _contratoVigenciaTransferenciaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.ContratoVigenciaTransferencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.ContratoVigenciaBalanco = data.ContratoVigenciaTransferencia.ContratoVigenciaBalanco;

			data.MaxMontanteMwmMes = (data.ContratoVigenciaBalanco.MontanteMwmMes ?? 0)
				- _calculoContratoService.GetTotalMontanteMwmMesTransferencias(data.ContratoVigenciaBalanco);

			data.PerfisAgenteList = _perfilAgenteService.GetByTiposRelacao(
				new[] { PerfilAgente.TiposRelacao.Cliente.ToString(), PerfilAgente.TiposRelacao.Terceiro.ToString() }
			);

			data.CheckboxesAccess = _permissionActionService.GetActionsByController(UserSession.Person.RoleID.Value, "ContratoVigenciaTransferencia");

			return AdminContent("ContratoVigenciaTransferencia/ContratoVigenciaTransferenciaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var contratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(id);
			if (contratoVigenciaTransferencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			contratoVigenciaTransferencia.ID = null;
			TempData["ContratoVigenciaTransferenciaModel"] = contratoVigenciaTransferencia;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigencia.ContratoID.Value, contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigenciaID.Value, contratoVigenciaTransferencia.ContratoVigenciaBalancoID.Value);
		}

		public ActionResult Del(Int32 id)
		{
			var contratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(id);
			if (contratoVigenciaTransferencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_contratoVigenciaTransferenciaService.Delete(contratoVigenciaTransferencia);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigenciaTransferencia/?contrato=" + contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigencia.ContratoID + "&vigencia=" + contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigenciaID + "&balanco=" + contratoVigenciaTransferencia.ContratoVigenciaBalancoID }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigenciaID, balanco = contratoVigenciaTransferencia.ContratoVigenciaBalancoID });
		}

		public ActionResult DelMultiple(String ids)
		{
			ContratoVigenciaBalanco contratoVigenciaBalanco = null;

			var contratosVigenciaTransferenciaID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosVigenciaTransferenciaID.Any())
			{
				var firstContratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(contratosVigenciaTransferenciaID.First());
				if (firstContratoVigenciaTransferencia != null)
				{
					contratoVigenciaBalanco = firstContratoVigenciaTransferencia.ContratoVigenciaBalanco;

					_contratoVigenciaTransferenciaService.DeleteMany(contratosVigenciaTransferenciaID);

					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}

			if (contratoVigenciaBalanco == null)
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/ContratoVigenciaTransferencia/?contrato=" + contratoVigenciaBalanco.ContratoVigencia.ContratoID + "&vigencia=" + contratoVigenciaBalanco.ContratoVigenciaID + "&balanco=" + contratoVigenciaBalanco.ID }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/
			return RedirectToAction("Index", new { contrato = contratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaBalanco.ContratoVigenciaID, balanco = contratoVigenciaBalanco.ID });
		}

		public FileResult ExportToXml(int contrato, int vigencia, int balanco, Int32? Page)
		{
			//var fullUrl = Web.AdminHistory.Previous;
			//var basicUrl = "".Substring(fullUrl.IndexOf("/Admin"));
			var basicUrl = "";

			var transferencias = _contratoVigenciaBalancoService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params, false);

			var zipPath = _contratoService.ExportToXml(transferencias.Items, true, UserSession.Person.ID, basicUrl);
			if (zipPath != null)
			{
				var fileName = string.Concat(transferencias.Items.First().ContratoVigencia.Contrato.NumeroInternoControle + "_", zipPath.RightFrom("\\\\"));
				Web.SetMessage("Contrato(s) exportado(s) com sucesso.", "SaveSuccess");
				return File(zipPath, "application/zip", fileName);
			}
			Web.SetMessage("Nenhum contrato exportado.", "error");
			return null;
		}

		public ActionResult ExportToXmlMultiple(String ids)
		{
			ContratoVigenciaBalanco contratoVigenciaBalanco = null;

			var contratosVigenciaTransferenciaID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosVigenciaTransferenciaID.Any())
			{
				var contratoVigenciaTransferencias = new List<ContratoVigenciaTransferencia>();

				foreach (var contratoVigenciaTransferenciaID in contratosVigenciaTransferenciaID)
				{
					var contratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(contratoVigenciaTransferenciaID);
					if (contratoVigenciaTransferencia != null)
						contratoVigenciaTransferencias.Add(contratoVigenciaTransferencia);
				}

				if (contratoVigenciaTransferencias.Any())
				{
					var fullUrl = Web.AdminHistory.Previous;
					var basicUrl = fullUrl.Substring(fullUrl.IndexOf("/Admin"));

					_contratoService.ExportToXml(contratoVigenciaTransferencias, UserSession.Person.ID, basicUrl);

					contratoVigenciaBalanco = contratoVigenciaTransferencias.First().ContratoVigenciaBalanco;
					Web.SetMessage("Exportado(s) com sucesso.");
				}
			}

			if (contratoVigenciaBalanco == null)
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.BaseUrl + "Admin/ContratoVigenciaTransferencia/?contrato=" + contratoVigenciaBalanco.ContratoVigencia.ContratoID + "&vigencia=" + contratoVigenciaBalanco.ContratoVigenciaID + "&balanco=" + contratoVigenciaBalanco.ID }, JsonRequestBehavior.AllowGet);
			return RedirectToAction("Index", new { contrato = contratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaBalanco.ContratoVigenciaID, balanco = contratoVigenciaBalanco.ID });
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
			return UpdateCheckboxes(id, nameof(ContratoVigenciaTransferencia.IsAjustado), value);
		}

		public ActionResult UpdateCheckboxIsAptoAjusteCliqCcee(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaTransferencia.IsAptoAjusteCliqCcee), value);
		}

		public ActionResult UpdateCheckboxIsAptoValidacao(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaTransferencia.IsAptoValidacao), value);
		}

		public ActionResult UpdateCheckboxIsFaturamento(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaTransferencia.IsFaturamento), value);
		}

		public ActionResult UpdateCheckboxIsInvalido(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaTransferencia.IsInvalido), value);
		}

		public ActionResult UpdateCheckboxIsMedicaoEnviada(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaTransferencia.IsMedicaoEnviada), value);
		}

		public ActionResult UpdateCheckboxIsValidado(int id, bool value)
		{
			return UpdateCheckboxes(id, nameof(ContratoVigenciaTransferencia.IsValidado), value);
		}

		public ActionResult UpdateCheckboxesByInvertedValue(string ids, string field)
		{
			ContratoVigenciaBalanco contratoVigenciaBalanco = null;

			var contratosVigenciaTransferenciaID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosVigenciaTransferenciaID.Any())
			{
				foreach (var contratoVigenciaTransferenciaID in contratosVigenciaTransferenciaID)
				{
					var contratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(contratoVigenciaTransferenciaID);
					if (contratoVigenciaTransferencia != null)
					{
						if (contratoVigenciaBalanco == null)
							contratoVigenciaBalanco = contratoVigenciaTransferencia.ContratoVigenciaBalanco;

						switch (field)
						{
							case "IsInvalido":
								contratoVigenciaTransferencia.IsInvalido = !contratoVigenciaTransferencia.IsInvalido;
								break;
							case "IsAjustado":
								contratoVigenciaTransferencia.IsAjustado = !contratoVigenciaTransferencia.IsAjustado;
								break;
							case "IsAptoAjusteCliqCcee":
								contratoVigenciaTransferencia.IsAptoAjusteCliqCcee = !contratoVigenciaTransferencia.IsAptoAjusteCliqCcee;
								break;
							case "IsAptoValidacao":
								contratoVigenciaTransferencia.IsAptoValidacao = !contratoVigenciaTransferencia.IsAptoValidacao;
								break;
							case "IsFaturamento":
								contratoVigenciaTransferencia.IsFaturamento = !contratoVigenciaTransferencia.IsFaturamento;
								break;
							case "IsMedicaoEnviada":
								contratoVigenciaTransferencia.IsMedicaoEnviada = !contratoVigenciaTransferencia.IsMedicaoEnviada;
								break;
							case "IsValidado":
								contratoVigenciaTransferencia.IsValidado = !contratoVigenciaTransferencia.IsValidado;
								break;
						}

						_contratoVigenciaTransferenciaService.Update(contratoVigenciaTransferencia);
					}
				}
			}

			if (contratoVigenciaBalanco == null)
				return Json(new { success = false, message = "", nextPage = Web.BaseUrl + "Admin/Contrato" }, JsonRequestBehavior.AllowGet);
			return Json(new { success = true, message = "", nextPage = Web.BaseUrl + "Admin/ContratoVigenciaTransferencia/?contrato=" + contratoVigenciaBalanco.ContratoVigencia.ContratoID + "&vigencia=" + contratoVigenciaBalanco.ContratoVigenciaID + "&balanco=" + contratoVigenciaBalanco.ID }, JsonRequestBehavior.AllowGet);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var contratoVigenciaTransferencia = new ContratoVigenciaTransferencia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					contratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(Request["ID"].ToInt(0));
					if (contratoVigenciaTransferencia == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				contratoVigenciaTransferencia.UpdateFromRequest();

				var contratoVigenciaTransferenciaExistente = _contratoVigenciaTransferenciaService.Get(contratoVigenciaTransferencia.ContratoVigenciaBalancoID.Value, contratoVigenciaTransferencia.PerfilAgenteID.Value);
				if (contratoVigenciaTransferenciaExistente != null)
				{
					if ((!isEdit) || ((isEdit) && (contratoVigenciaTransferencia.ID != contratoVigenciaTransferenciaExistente.ID)))
						throw new Exception("Perfil de agente já cadastrado.");
				}

				_contratoVigenciaTransferenciaHasAttachmentService.DeleteMany(contratoVigenciaTransferencia.ContratoVigenciaTransferenciaHasAttachmentList);

				contratoVigenciaTransferencia.UpdateChildrenFromRequest();

				_contratoVigenciaTransferenciaService.Save(contratoVigenciaTransferencia);

				_contratoVigenciaTransferenciaHasAttachmentService.InsertMany(contratoVigenciaTransferencia.ContratoVigenciaTransferenciaHasAttachmentList);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = /* isSaveAndRefresh ? contratoVigenciaTransferencia.GetAdminURL() : Web.AdminHistory.Previous ?? */ Web.BaseUrl + string.Format("Admin/ContratoVigenciaTransferencia/?contrato={0}&vigencia={1}&balanco={2}", contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigencia.ContratoID, contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigenciaID, contratoVigenciaTransferencia.ContratoVigenciaBalancoID);
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { contratoVigenciaTransferencia.ID });

				/*
				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				*/
				return RedirectToAction("Index", new { contrato = contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigenciaID, balanco = contratoVigenciaTransferencia.ContratoVigenciaBalancoID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["ContratoVigenciaTransferenciaModel"] = contratoVigenciaTransferencia;
				return isEdit && contratoVigenciaTransferencia != null ? RedirectToAction("Edit", new { contratoVigenciaTransferencia.ID }) : RedirectToAction("Create", new { contrato = contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigencia.ContratoID, vigencia = contratoVigenciaTransferencia.ContratoVigenciaBalanco.ContratoVigenciaID, balanco = contratoVigenciaTransferencia.ContratoVigenciaBalancoID });
			}
		}

		public JsonResult UpdateCheckboxes(int id, string field, bool value)
		{
			var contratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(id);
			if (contratoVigenciaTransferencia != null)
			{
				switch (field)
				{
					case "IsInvalido":
						contratoVigenciaTransferencia.IsInvalido = value;
						break;
					case "IsAjustado":
						contratoVigenciaTransferencia.IsAjustado = value;
						break;
					case "IsAptoAjusteCliqCcee":
						contratoVigenciaTransferencia.IsAptoAjusteCliqCcee = value;
						break;
					case "IsValidado":
						contratoVigenciaTransferencia.IsValidado = value;
						break;
					case "IsAptoValidacao":
						contratoVigenciaTransferencia.IsAptoValidacao = value;
						break;
					case "IsFaturamento":
						contratoVigenciaTransferencia.IsFaturamento = value;
						break;
					case "IsMedicaoEnviada":
						contratoVigenciaTransferencia.IsMedicaoEnviada = value;
						break;
				}

				_contratoVigenciaTransferenciaService.Update(contratoVigenciaTransferencia);

				return Json(new { success = true }, JsonRequestBehavior.AllowGet);
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
			public ContratoVigenciaTransferencia ContratoVigenciaTransferencia;
			public ContratoVigenciaBalanco ContratoVigenciaBalanco;
			public List<PerfilAgente> PerfisAgenteList;
			public List<string> CheckboxesAccess;
			public double MaxMontanteMwmMes;
			public bool ReadOnly;

		}

		public class ListViewModel
		{
			public List<ContratoVigenciaTransferencia> ContratosVigenciaTransferencia;
			public List<string> CheckboxesAccess;
			public int HorasMes;
			public bool HasDiasUteisEnvioMedicao;
			public bool HasObservacoes;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
