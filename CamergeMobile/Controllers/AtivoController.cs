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
	public class AtivoController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IAtivoHasAttachmentService _ativoHasAttachmentService;
		private readonly IFeriasService _feriasService;
		private readonly IGarantiaFisicaService _garantiaFisicaService;
		private readonly IRiscoHidrologicoService _riscoHidrologicoService;
		private readonly IGeradorLeituraService _geradorLeituraService;
		private readonly IGeradorLeituraQueueService _geradorLeituraQueueService;
		private readonly IMapeadorMedicaoCacheQueueService _mapeadorMedicaoCacheQueueService;
		private readonly IMedicaoErroService _medicaoErroService;
		private readonly IRelatorioQueueService _relatorioQueueService;
		private readonly ISazonalizacaoService _sazonalizacaoService;
		private readonly IUnidadeGeradoraService _unidadeGeradoraService;

		public AtivoController(IAtivoService ativoService,
			IAtivoHasAttachmentService ativoHasAttachmentService,
			IFeriasService feriasService,
			IGarantiaFisicaService garantiaFisicaService,
			IRiscoHidrologicoService riscoHidrologicoService,
			IGeradorLeituraService geradorLeituraService,
			IGeradorLeituraQueueService geradorLeituraQueueService,
			IMapeadorMedicaoCacheQueueService mapeadorMedicaoCacheQueueService,
			IMedicaoErroService medicaoErroService,
			IRelatorioQueueService relatorioQueueService,
			ISazonalizacaoService sazonalizacaoService,
			IUnidadeGeradoraService unidadeGeradoraService)
		{
			_ativoService = ativoService;
			_ativoHasAttachmentService = ativoHasAttachmentService;
			_feriasService = feriasService;
			_garantiaFisicaService = garantiaFisicaService;
			_riscoHidrologicoService = riscoHidrologicoService;
			_geradorLeituraService = geradorLeituraService;
			_geradorLeituraQueueService = geradorLeituraQueueService;
			_mapeadorMedicaoCacheQueueService = mapeadorMedicaoCacheQueueService;
			_medicaoErroService = medicaoErroService;
			_relatorioQueueService = relatorioQueueService;
			_sazonalizacaoService = sazonalizacaoService;
			_unidadeGeradoraService = unidadeGeradoraService;
		}

		//
		// GET: /Admin/Ativo/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel()
			{
				TipoRelacao = Request["relacao"] ?? PerfilAgente.TiposRelacao.Cliente.ToString()
			};

			var paging = _ativoService.GetAllWithPaging(
					(UserSession.IsPerfilAgente || UserSession.IsPotencialAgente || UserSession.IsComercializadora) ? UserSession.Agentes.Select(i => i.ID.Value) : null,
					data.TipoRelacao,
					Page ?? 1,
					Util.GetSettingInt("ItemsPerPage", 30),
					Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Ativos = paging.Items;

			return AdminContent("Ativo/AtivoList.aspx", data);
		}

		//
		// GET: /Admin/GetAtivos/
		public JsonResult GetAtivos()
		{
			Object ativos;

			if (Request["perfilAgente"].IsNotBlank())
				ativos = AtivoList.LoadByPerfilAgenteID(Request["perfilAgente"].ToInt(0)).Select(o => new { o.ID, o.Nome });
			else if (Request["agente"].IsNotBlank())
				ativos = _ativoService.GetByAgente(Request["agente"].ToInt(0)).Select(o => new { o.ID, o.Nome });
			else
				ativos = _ativoService.GetAll().Select(o => new { o.ID, o.Nome });

			return Json(ativos, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetTipoLeitura(string ids)
		{
			string tipoLeitura = null;

			var ativosID = ids.Split(',');
			if (ativosID.Count() == 1)
			{
				var ativo = _ativoService.FindByID(ativosID[0].ToInt(0));
				if (ativo != null)
				{
					if (ativo.PerfilAgente.IsConsumidor)
						tipoLeitura = Medicao.TiposLeitura.Consumo.ToString();
					else if (ativo.PerfilAgente.IsGerador || ativo.PerfilAgente.IsGeradorGD)
						tipoLeitura = Medicao.TiposLeitura.Geracao.ToString();
				}
			}

			return Json(tipoLeitura, JsonRequestBehavior.AllowGet);
		}

		public JsonResult UpdateIsActive(int id, bool value)
		{
			var ativo = _ativoService.FindByID(id);
			if (ativo != null)
			{
				ativo.IsActive = value;

				_ativoService.Update(ativo);

				return Json(new { success = true }, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();

			data.Ativo = TempData["AtivoModel"] as Ativo;
			if (data.Ativo == null)
			{
				data.Ativo = _ativoService.GetDefaultAtivo();
				data.Ativo.UpdateFromRequest();

				data.TipoRelacao = Request["relacao"] ?? PerfilAgente.TiposRelacao.Cliente.ToString();
			}
			else
			{
				data.TipoRelacao = data.Ativo.PerfilAgente.TipoRelacao;
			}

			return AdminContent("Ativo/AtivoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Ativo = TempData["AtivoModel"] as Ativo ?? _ativoService.FindByID(id);
			if (data.Ativo == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.TipoRelacao = data.Ativo.PerfilAgente.TipoRelacao;
			data.ReadOnly = readOnly;

			return AdminContent("Ativo/AtivoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var ativo = _ativoService.FindByID(id);
			if (ativo == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			ativo.ID = null;
			TempData["AtivoModel"] = ativo;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var ativo = _ativoService.FindByID(id);
				if (ativo == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					if (_ativoService.AtivoHasMedicoes(id))
					{
						throw new Exception("Ativo possui medições e não pode ser deletado");
					}
					_ativoService.Delete(ativo);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Ativo" }, JsonRequestBehavior.AllowGet);
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
				var ativoIds = ids.Split(',').Select(id => id.ToInt(0));
				foreach (var ativoId in ativoIds)
				{
					if (_ativoService.AtivoHasMedicoes(ativoId))
					{
						throw new Exception("Ativo possui medições e não pode ser deletado");
					}
				}
				_ativoService.DeleteMany(ativoIds);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Ativo" }, JsonRequestBehavior.AllowGet);
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
			var ativo = _ativoService.GetDefaultAtivo();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					ativo = _ativoService.FindByID(Request["ID"].ToInt(0));
					if (ativo == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				var hasGeradorLeituraQueue = (_geradorLeituraQueueService.GetAll().Any());

				ativo.UpdateFromRequest();

				var checkCodigoPontoMedicao = _ativoService.GetByCodigoPontoMedicao(ativo.CodigoPontoMedicao);
				if ((!isEdit) && (checkCodigoPontoMedicao != null))
					throw new Exception("Já existe ativo cadastro com este Código Ponto Medição.");

				var oldEntriesFerias = ativo.FeriasList.ToList();
				var oldEntriesGerador = ativo.GeradorLeituraList.ToList();
				var oldEntriesSazonalizacao = ativo.SazonalizacaoList.ToList();

				_ativoService.Save(ativo);

				_feriasService.DeleteMany(ativo.FeriasList);
				_garantiaFisicaService.DeleteMany(ativo.GarantiaFisicaList);
				_riscoHidrologicoService.DeleteMany(ativo.RiscoHidrologicoList);
				_sazonalizacaoService.DeleteMany(ativo.SazonalizacaoList);
				_ativoHasAttachmentService.DeleteMany(ativo.AtivoHasAttachmentList);
				_unidadeGeradoraService.DeleteMany(ativo.UnidadeGeradoraList);

				if (!hasGeradorLeituraQueue)
					_geradorLeituraService.DeleteMany(oldEntriesGerador);

				ativo.UpdateChildrenFromRequest();

				/*
				foreach (var ferias in ativo.FeriasList)
				{
					ferias.DataInicio = Fmt.ToInitialHours(ferias.DataInicio.Value);
					ferias.DataFim = Fmt.ToFinalHours(ferias.DataFim.Value);
					if (ferias.DataInicio.Value > ferias.DataFim.Value)
						ferias.DataFim = ferias.DataInicio;
					if ((ferias.DataFim.Value - ferias.DataInicio.Value).TotalDays > 31)
						ferias.DataFim = ferias.DataInicio.Value.AddDays(30);
				}
				*/

				foreach (var sazonalizacao in ativo.SazonalizacaoList)
				{
					sazonalizacao.MesInicioVigencia = Fmt.ToInitialHours(sazonalizacao.MesInicioVigencia.Value);
					sazonalizacao.MesFimVigencia = Dates.GetLastDayOfMonth(Fmt.ToFinalHours(sazonalizacao.MesFimVigencia.Value));
				}

				_feriasService.InsertMany(ativo.FeriasList);
				_garantiaFisicaService.InsertMany(ativo.GarantiaFisicaList);
				_riscoHidrologicoService.InsertMany(ativo.RiscoHidrologicoList);
				_sazonalizacaoService.InsertMany(ativo.SazonalizacaoList);
				_ativoHasAttachmentService.InsertMany(ativo.AtivoHasAttachmentList);
				_unidadeGeradoraService.InsertMany(ativo.UnidadeGeradoraList);

				AddReportsInQueueByFerias(ativo, oldEntriesFerias);
				/// AddReportsInQueueBySazonalizacao(ativo, oldEntriesSazonalizacao);

				if (!hasGeradorLeituraQueue)
				{
					foreach (var geradorLeitura in ativo.GeradorLeituraList)
					{
						geradorLeitura.DataInicioVigencia = Fmt.ToInitialHours(geradorLeitura.DataInicioVigencia.Value);
						geradorLeitura.DataFimVigencia = Fmt.ToFinalHours(geradorLeitura.DataFimVigencia.Value);
						_geradorLeituraService.Insert(geradorLeitura);
					}

					AddReportsInQueueByGeradorLeitura(ativo, oldEntriesGerador);
				}

				var medicaoErro = _medicaoErroService.FindByID(Request["MedicaoErroID"].ToInt(0));
				if (medicaoErro != null && !medicaoErro.Resolvido.Value)
				{
					medicaoErro.Resolvido = true;
					_medicaoErroService.Save(medicaoErro);
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? ativo.GetAdminURL() : /* Web.AdminHistory.Previous ?? */ string.Concat(Web.BaseUrl, "Admin/Ativo/?relacao=", ativo.PerfilAgente.TipoRelacao);
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { ativo.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				TempData["AtivoModel"] = ativo;
				return isEdit && ativo != null ? RedirectToAction("Edit", new { ativo.ID }) : RedirectToAction("Create");
			}
		}

		private void AddReportsInQueueByFerias(Ativo ativo, List<Ferias> oldEntriesFerias)
		{
			if (ativo.PerfilAgente.IsConsumidor)
			{
				var items = new List<MapeadorMedicaoCacheQueue>();

				var tipoLeitura = Medicao.TiposLeitura.Consumo.ToString();

				if (oldEntriesFerias.Any())
				{
					var diffEntries = ativo.FeriasList.Where(i => !oldEntriesFerias.Contains(i)).ToList();
					diffEntries.AddRange(oldEntriesFerias.Where(i => !ativo.FeriasList.Contains(i)));

					if (diffEntries.Any())
						_mapeadorMedicaoCacheQueueService.AddToList(items, diffEntries, tipoLeitura);
				}
				else
				{
					_mapeadorMedicaoCacheQueueService.AddToList(items, ativo.FeriasList, tipoLeitura);
				}

				if (items.Any())
					foreach (var item in items.Where(i => i.Mes < DateTime.Now))
						if (_mapeadorMedicaoCacheQueueService.Get(item.AtivoID.Value, item.Mes, item.TipoLeitura) == null)
							_mapeadorMedicaoCacheQueueService.Insert(item);
			}
		}

		private void AddReportsInQueueByGeradorLeitura(Ativo ativo, List<GeradorLeitura> oldEntriesGerador)
		{
			if (oldEntriesGerador.Any())
			{
				var diffEntries = ativo.GeradorLeituraList.Where(i => !oldEntriesGerador.Contains(i)).ToList();
				if (diffEntries.Any())
				{
					foreach (var item in diffEntries)
					{
						for (DateTime dia = item.DataInicioVigencia.Value; dia <= item.DataFimVigencia.Value; dia = dia.AddDays(1))
							_geradorLeituraQueueService.Insert(new GeradorLeituraQueue() { AtivoID = item.AtivoID.Value, GeradorLeituraID = item.ID, DataLeitura = dia, Origem = "Ativo" });

						_mapeadorMedicaoCacheQueueService.Insert(new MapeadorMedicaoCacheQueue() { AtivoID = item.AtivoID.Value, Mes = Dates.GetFirstDayOfMonth(item.DataInicioVigencia.Value), TipoLeitura = Medicao.TiposLeitura.Consumo.ToString() });

						if (item.DataInicioVigencia.Value.Month != item.DataFimVigencia.Value.Month)
						{
							for (DateTime dia = item.DataInicioVigencia.Value; dia <= item.DataFimVigencia.Value; dia = dia.AddDays(1))
								_geradorLeituraQueueService.Insert(new GeradorLeituraQueue() { AtivoID = item.AtivoID.Value, GeradorLeituraID = item.ID, DataLeitura = Dates.GetFirstDayOfMonth(item.DataFimVigencia.Value), Origem = "Ativo" });
							_mapeadorMedicaoCacheQueueService.Insert(new MapeadorMedicaoCacheQueue() { AtivoID = item.AtivoID.Value, Mes = Dates.GetFirstDayOfMonth(item.DataFimVigencia.Value), TipoLeitura = Medicao.TiposLeitura.Consumo.ToString() });
						}
					}
				}
			}
			else
			{
				foreach (var item in ativo.GeradorLeituraList)
				{
					var tipoLeitura = Medicao.TiposLeitura.Consumo.ToString();

					var ativoID = item.Ativo.ID.Value;
					var mes = Dates.GetFirstDayOfMonth(item.DataInicioVigencia.Value);

					/// _geradorLeituraQueueService.Insert(new GeradorLeituraQueue() { AtivoID = ativoID, GeradorLeituraID = item.ID, DataLeitura = mes, Origem = "Ativo" });

					for (DateTime dia = item.DataInicioVigencia.Value; dia <= item.DataFimVigencia.Value; dia = dia.AddDays(1))
						_geradorLeituraQueueService.Insert(new GeradorLeituraQueue() { AtivoID = item.AtivoID.Value, GeradorLeituraID = item.ID, DataLeitura = dia, Origem = "Ativo" });

					_mapeadorMedicaoCacheQueueService.Insert(new MapeadorMedicaoCacheQueue() { AtivoID = ativoID, Mes = mes, TipoLeitura = tipoLeitura });

					/*
					if (item.DataInicioVigencia.Value.Month != item.DataFimVigencia.Value.Month)
					{
						mes = Dates.GetFirstDayOfMonth(item.DataFimVigencia.Value);

						_geradorLeituraQueueService.Insert(new GeradorLeituraQueue() { AtivoID = ativoID, GeradorLeituraID = item.ID, DataLeitura = mes, Origem = "Ativo" });
						_mapeadorMedicaoCacheQueueService.Insert(new MapeadorMedicaoCacheQueue() { AtivoID = ativoID, Mes = mes, TipoLeitura = tipoLeitura });
					}
					*/
				}
			}
		}

		private void AddReportsInQueueBySazonalizacao(Ativo ativo, List<Sazonalizacao> oldEntriesSazonalizacao)
		{
			if (oldEntriesSazonalizacao.Any())
			{
				var diffEntries = ativo.SazonalizacaoList.Where(i => !oldEntriesSazonalizacao.Contains(i)).ToList();
				if (diffEntries.Any())
				{
					var relatoriosToQueue = new List<RelatorioQueue>();

					foreach (var item in diffEntries)
					{
						var ativoID = item.Ativo.ID.Value;

						_relatorioQueueService.AddToList(relatoriosToQueue, ativo.ID.Value, Dates.GetFirstDayOfMonth(item.MesInicioVigencia.Value));

						if (item.MesInicioVigencia.Value.Month != item.MesFimVigencia.Value.Month)
							_relatorioQueueService.AddToList(relatoriosToQueue, ativo.ID.Value, Dates.GetFirstDayOfMonth(item.MesFimVigencia.Value));
					}

					if (relatoriosToQueue.Any())
						_relatorioQueueService.InsertMany(relatoriosToQueue);
				}
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

		public class ListViewModel
		{
			public List<Ativo> Ativos;
			public string TipoRelacao;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Ativo Ativo;
			public string TipoRelacao;
			public bool ReadOnly;
		}
	}
}
