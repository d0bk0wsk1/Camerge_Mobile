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
	public class DemandaContratadaController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IDemandaContratadaService _demandaContratadaService;
		private readonly IEventoService _eventoService;
		private readonly IEventoAgenteService _eventoAgenteService;

		public DemandaContratadaController(IAgenteService agenteService,
			IDemandaContratadaService demandaContratadaService,
			IEventoService eventoService,
			IEventoAgenteService eventoAgenteService)
		{
			_agenteService = agenteService;
			_demandaContratadaService = demandaContratadaService;
			_eventoService = eventoService;
			_eventoAgenteService = eventoAgenteService;
		}

		//
		// GET: /Admin/DemandaContratada/
		public ActionResult Index(Int32? Page, string relacao = null)
		{
			var data = new ListViewModel();

			var paging = _demandaContratadaService.GetAllWithPaging(
				(UserSession.IsCliente) ? UserSession.Agentes.Select(i => i.ID.Value) : null,
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.DemandaContratadas = paging.Items;
			data.TipoRelacao = relacao ?? PerfilAgente.TiposRelacao.Cliente.ToString();

			return AdminContent("DemandaContratada/DemandaContratadaList.aspx", data);
		}

		//
		// GET: /Admin/GetModalidades/
		public JsonResult GetModalidades(string ids)
		{
			var options = new Dictionary<string, string>();
			options.Add(string.Empty, "Histórico");

			var singleId = false;

			var id = Request["ids"];
			if (id.IsNotBlank())
			{
				var modalidades = Enum.GetValues(typeof(DemandaContratada.Modalidades)).Cast<DemandaContratada.Modalidades>().ToList();
				foreach (var modalidade in modalidades)
					options.Add(modalidade.ToString(), modalidade.ToString());

				singleId = (!id.Contains(','));

				int ativoID;
				if ((singleId) && (int.TryParse(id, out ativoID)))
				{
					var modalidade = _demandaContratadaService.GetMostRecent(ativoID);
					if (modalidade != null)
					{
						if (options.ContainsKey(modalidade.Tipo))
						{
							options[modalidade.Tipo] = string.Format("{0} (Atual)", options[modalidade.Tipo]);
						}
					}
				}
			}

			return Json(options.Select(i => new { i.Key, i.Value }), JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.DemandaContratada = TempData["DemandaContratadaModel"] as DemandaContratada;
			if (data.DemandaContratada == null)
			{
				data.DemandaContratada = new DemandaContratada()
				{
					MesVigencia = Dates.GetFirstDayOfMonth(DateTime.Today)
				};
				data.DemandaContratada.UpdateFromRequest();
			}

			data.TipoRelacao = Request["relacao"];
			if (data.TipoRelacao == null)
				data.TipoRelacao = data.DemandaContratada.Ativo.PerfilAgente.TipoRelacao;

			return AdminContent("DemandaContratada/DemandaContratadaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			// edit disabled
			//readOnly = false;

			var data = new FormViewModel();
			data.DemandaContratada = TempData["DemandaContratadaModel"] as DemandaContratada ?? _demandaContratadaService.FindByID(id);
			if (data.DemandaContratada == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.TipoRelacao = data.DemandaContratada.Ativo.PerfilAgente.TipoRelacao;
			data.ReadOnly = readOnly;

			return AdminContent("DemandaContratada/DemandaContratadaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var demandaContratada = _demandaContratadaService.FindByID(id);
			if (demandaContratada == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			demandaContratada.ID = null;
			TempData["DemandaContratadaModel"] = demandaContratada;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var demandaContratada = _demandaContratadaService.FindByID(id);
			if (demandaContratada == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_demandaContratadaService.Delete(demandaContratada);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/DemandaContratada" }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/

			return RedirectToAction("Index", new { relacao = demandaContratada.Ativo.PerfilAgente.TipoRelacao });
		}

		public ActionResult DelMultiple(String ids)
		{
			var demandasContratadasID = ids.Split(',').Select(id => id.ToInt(0));

			_demandaContratadaService.DeleteMany(demandasContratadasID);

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/DemandaContratada" }, JsonRequestBehavior.AllowGet);

			/*
			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			*/

			var relacao = PerfilAgente.TiposRelacao.Cliente.ToString();

			var demandaContratada = _demandaContratadaService.FindByID(demandasContratadasID.First());
			if (demandaContratada != null)
				relacao = demandaContratada.Ativo.PerfilAgente.TipoRelacao;

			return RedirectToAction("Index", new { relacao = relacao });
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var demandaContratada = new DemandaContratada();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					demandaContratada = _demandaContratadaService.FindByID(Request["ID"].ToInt(0));
					if (demandaContratada == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				demandaContratada.UpdateFromRequest();

				/*
				if (Request["Considera500InLivre"].ToBoolean())
					demandaContratada.ForaPonta = Math.Max(0.5, demandaContratada.ForaPonta.Value);
				*/

				if (demandaContratada.Tipo != DemandaContratada.Tipos.Azul.ToString())
					demandaContratada.Ponta = demandaContratada.ForaPonta;

				_demandaContratadaService.Save(demandaContratada);

				if (!isEdit)
					CreateAutoEvento(demandaContratada);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? demandaContratada.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/DemandaContratada/?relacao=" + Request["relacao"];
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { demandaContratada.ID });
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
				TempData["DemandaContratadaModel"] = demandaContratada;
				return isEdit && demandaContratada != null ? RedirectToAction("Edit", new { demandaContratada.ID }) : RedirectToAction("Create");
			}
		}

		public JsonResult GetHistoric(int ativoId)
		{
			var historic = _demandaContratadaService.Get(ativoId, null);
			if (historic.Any())
			{
				return Json(
					historic.Select(s => new
					{
						AtivoID = s.AtivoID,
						MesVigencia = Fmt.MonthYear(s.MesVigencia),
						Tipo = s.Tipo,
						Ponta = s.Ponta,
						ForaPonta = s.ForaPonta
					}),
					JsonRequestBehavior.AllowGet
				);
			}
			return Json(null);
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

		private void CreateAutoEvento(DemandaContratada demandaContratada)
		{
			var lastDemandaContratada = _demandaContratadaService.GetMostRecent(demandaContratada.AtivoID.Value, demandaContratada.ID, demandaContratada.MesVigencia);
			if (lastDemandaContratada != null)
			{
				var agente = _agenteService.GetByAtivo(demandaContratada.AtivoID.Value);
				if (agente != null)
				{
					if (((lastDemandaContratada.Tipo == DemandaContratada.Tipos.Verde.ToString())
						&& (demandaContratada.Tipo == DemandaContratada.Tipos.Azul.ToString()))
						|| (((demandaContratada.Ponta * 1.05) > lastDemandaContratada.Ponta)
						|| ((demandaContratada.ForaPonta * 1.05) > lastDemandaContratada.ForaPonta)))
					{
						var evento = new Evento()
						{
							Destino = "Agente",
							Titulo = "Ajuste Demanda Período de Teste",
							Descricao = string.Format("Prazo máximo para ajuste da demanda dentro do período de testes. Ativo: {0}", demandaContratada.Ativo.Nome),
							DateEvento = Dates.GetLastDayOfMonth(demandaContratada.MesVigencia.Value.AddMonths(2)),
							Mes = demandaContratada.MesVigencia
						};

						_eventoService.Insert(evento);

						var eventoAgente = new EventoAgente()
						{
							EventoID = evento.ID,
							AgenteID = agente.ID
						};

						_eventoAgenteService.Insert(eventoAgente);
					}
				}
			}
		}

		public class ListViewModel
		{
			public List<DemandaContratada> DemandaContratadas;
			public string TipoRelacao;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public DemandaContratada DemandaContratada;
			public string TipoRelacao;
			public bool ReadOnly;
		}
	}
}
