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
	public class EventoController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IContabilizacaoCceeService _contabilizacaoCceeService;
		private readonly IEventoService _eventoService;
		private readonly IEventoAgenteService _eventoAgenteService;
		private readonly IEventoCategoriaService _eventoCategoriaService;

		public EventoController(IAgenteService agenteService,
			IContabilizacaoCceeService contabilizacaoCceeService,
			IEventoService eventoService,
			IEventoAgenteService eventoAgenteService,
			IEventoCategoriaService eventoCategoriaService)
		{
			_agenteService = agenteService;
			_contabilizacaoCceeService = contabilizacaoCceeService;
			_eventoService = eventoService;
			_eventoAgenteService = eventoAgenteService;
			_eventoCategoriaService = eventoCategoriaService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _eventoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Eventos = paging.Items;

			return AdminContent("Evento/EventoList.aspx", data);
		}

		public JsonResult GetEventos()
		{
			var eventos = _eventoService.GetAll().Select(o => new { o.ID, o.Destino });
			return Json(eventos, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Evento = TempData["EventoModel"] as Evento;
			data.CceeFields = _contabilizacaoCceeService.GetAllFields();

			if (data.Evento == null)
			{
				data.Evento = new Evento();
				data.Evento.UpdateFromRequest();
			}
			else
			{
				data = GetDestinos(data);
				data.Evento.ID = null;
			}

			return AdminContent("Evento/EventoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.CceeFields = _contabilizacaoCceeService.GetAllFields();
			data.Evento = TempData["EventoModel"] as Evento ?? _eventoService.FindByID(id);
			data.ReadOnly = readOnly;

			if (data.Evento == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data = GetDestinos(data);

			return AdminContent("Evento/EventoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var evento = _eventoService.FindByID(id);
			if (evento == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			// evento.ID = null;

			TempData["EventoModel"] = evento;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var evento = _eventoService.FindByID(id);
			if (evento == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				if (evento.Destino == Evento.Destinos.Agente.ToString())
					_eventoAgenteService.DeleteByEventoId(evento.ID.Value);
				else if (evento.Destino == Evento.Destinos.Categoria.ToString())
					_eventoCategoriaService.DeleteByEventoId(evento.ID.Value);

				_eventoService.Delete(evento);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Evento" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids)
		{
			foreach (var id in ids.Split(',').Select(id => id.ToInt(0)))
			{
				var evento = _eventoService.FindByID(id);
				if (evento != null)
				{
					if (evento.Destino == Evento.Destinos.Agente.ToString())
						_eventoAgenteService.DeleteByEventoId(evento.ID.Value);
					else if (evento.Destino == Evento.Destinos.Categoria.ToString())
						_eventoCategoriaService.DeleteByEventoId(evento.ID.Value);

					_eventoService.Delete(evento);
				}
			}

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Evento" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult Report()
		{
			var data = new ReportViewModel();

			if ((Request["agente"].IsNotBlank()) && (Request["date"].IsNotBlank()))
			{
				data.Agente = _agenteService.FindByID(Request["agente"].ToInt(0));
				if (data.Agente != null)
				{
					var dtIni = Request["date"].ConvertToDate(null);
					if (dtIni != null)
					{
						var eventos = new List<EventoDto>();

						dtIni = Dates.GetFirstDayOfMonth(dtIni.Value);
						var dtFim = Dates.GetLastDayOfMonth(dtIni.Value);

						var eventosCcee = _eventoService.GetEventosDto(data.Agente, dtIni.Value, dtFim, true, true);
						if (eventosCcee.Any())
							eventos.AddRange(eventosCcee);
						var eventosContrato = _eventoService.GetDtoByContrato(data.Agente.ID.Value, dtIni.Value, dtFim);
						if (eventosContrato.Any())
							eventos.AddRange(eventosContrato);
						var eventosVigenciaContratual = _eventoService.GetDtoByVigenciaContratualDistribuidora(data.Agente.ID.Value, dtIni.Value);
						if (eventosVigenciaContratual.Any())
							eventos.AddRange(eventosVigenciaContratual);

						if (eventos.Any())
							eventos = eventos.OrderBy(i => i.DateEvento).ToList();

						data.Eventos = eventos;
					}
				}
			}

			return AdminContent("Evento/EventoReport.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var evento = new Evento();
			var isEdit = Request["ID"].IsNotBlank();

			if ((Request["Destino"] == null) || ((Request["agentes"] == null) && (Request["categorias"] == null)))
				throw new Exception("Campos 'Destino' devem ser preenchidos.");

			try
			{
				if (isEdit)
				{
					evento = _eventoService.FindByID(Request["ID"].ToInt(0));
					if (evento == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}
				else
				{
					evento.DateAdded = DateTime.Now;
				}

				evento.DateModified = DateTime.Now;

				evento.UpdateFromRequest();
				_eventoService.Save(evento);

				if (evento.Destino == "Agente")
				{
					if (isEdit)
						_eventoCategoriaService.DeleteByEventoId(evento.ID.Value);
					_eventoAgenteService.AddAndSave(evento.ID.Value, Request["agentes"], true);
				}
				else if (evento.Destino == "Categoria")
				{
					if (isEdit)
						_eventoAgenteService.DeleteByEventoId(evento.ID.Value);
					_eventoCategoriaService.AddAndSave(evento.ID.Value, Request["categorias"], true);
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? evento.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Evento";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { evento.ID });
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
				TempData["EventoModel"] = evento;
				return isEdit && evento != null ? RedirectToAction("Edit", new { evento.ID }) : RedirectToAction("Create");
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

		private FormViewModel GetDestinos(FormViewModel viewModel)
		{
			if (viewModel.Evento.Destino == "Agente")
			{
				var agentes = _eventoAgenteService.GetAll(new Sql("WHERE evento_id = @0", viewModel.Evento.ID));
				if ((agentes != null) && (agentes.Any()))
				{
					viewModel.Agentes = string.Join(",", agentes.ToList().Select(i => i.AgenteID.Value));
				}
			}
			else if (viewModel.Evento.Destino == "Categoria")
			{
				var categorias = _eventoCategoriaService.GetAll(new Sql("WHERE evento_id = @0", viewModel.Evento.ID));
				if ((categorias != null) && (categorias.Any()))
				{
					viewModel.Categorias = string.Join(",", categorias.ToList().Select(i => i.Categoria));
				}
			}

			return viewModel;
		}

		public class FormViewModel
		{
			public Dictionary<string, string> CceeFields;
			public Evento Evento;
			public string Categorias;
			public string Agentes;
			public bool ReadOnly;
		}

		public class ListViewModel
		{
			public List<Evento> Eventos;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class ReportViewModel
		{
			public Agente Agente;
			public List<EventoDto> Eventos;
		}
	}
}
