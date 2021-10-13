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
	public class TrammitTarefaController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ITrammitService _trammitService;
		private readonly ITrammitItemService _trammitItemService;
		private readonly ITrammitProcessoService _trammitProcessoService;
		private readonly ITrammitTarefaService _trammitTarefaService;
		private readonly ITrammitTarefaHasAttachmentService _trammitTarefaHasAttachmentService;
		private readonly ITrammitTarefaStatusService _trammitTarefaStatusService;
		private readonly ITrammitStatusService _trammitStatusService;

		public TrammitTarefaController(IAtivoService ativoService,
			ITrammitService trammitService,
			ITrammitItemService trammitItemService,
			ITrammitProcessoService trammitProcessoService,
			ITrammitTarefaService trammitTarefaService,
			ITrammitTarefaHasAttachmentService trammitTarefaHasAttachmentService,
			ITrammitTarefaStatusService trammitTarefaStatusService,
			ITrammitStatusService trammitStatusService)
		{
			_ativoService = ativoService;
			_trammitService = trammitService;
			_trammitItemService = trammitItemService;
			_trammitProcessoService = trammitProcessoService;
			_trammitTarefaService = trammitTarefaService;
			_trammitTarefaHasAttachmentService = trammitTarefaHasAttachmentService;
			_trammitTarefaStatusService = trammitTarefaStatusService;
			_trammitStatusService = trammitStatusService;
		}

		public ActionResult Index(int trammitprid)
		{
			var data = new ListViewModel();

			var trammitProcesso = _trammitProcessoService.FindByID(trammitprid);
			if (trammitProcesso != null)
			{
				var list = _trammitTarefaService.GetLastStatus(trammitProcesso.ID.Value);

				data.TrammitTarefas = list.OrderBy(i => i.Positionorder);
				data.Trammit = _trammitService.FindByID(trammitProcesso.TrammitID.Value);
				data.Ativo = _ativoService.FindByID(trammitProcesso.AtivoID.Value);
                data.TrammitProcesso = trammitProcesso;

				data.BulkTrammitStatus = _trammitStatusService.GetAll(true); // _trammitStatusService.GetAllExceptSucccess();
			}

			return AdminContent("TrammitTarefa/TrammitTarefaList.aspx", data);
		}

		public ActionResult Create(int trammitprid, int trammititid, int? trammitstid = null)
		{
			var data = new FormViewModel();
			data.TrammitTarefa = TempData["TrammitTarefaModel"] as TrammitTarefa;

			data.TrammitProcesso = _trammitProcessoService.FindByID(trammitprid);
			data.TrammitItem = _trammitItemService.FindByID(trammititid);

			if (trammitstid != null)
				data.TrammitStatus = _trammitStatusService.FindByID(trammitstid.Value);

			data.TrammitTarefaStatus = _trammitTarefaStatusService.Get(data.TrammitItem.ID.Value, data.TrammitProcesso.ID.Value);

			if (data.TrammitTarefa == null)
			{
				data.TrammitTarefa = new TrammitTarefa()
				{
					TrammitProcessoID = trammitprid,
					TrammitItemID = trammititid,
					PersonID = UserSession.Person.ID
				};

				data.TrammitTarefa.UpdateFromRequest();
			}
			return AdminContent("TrammitTarefa/TrammitTarefaEdit.aspx", data);
		}

		public ActionResult Edit(int id, bool readOnly = false)
		{
			var data = new FormViewModel();
			data.TrammitTarefa = TempData["TrammitTarefaModel"] as TrammitTarefa ?? _trammitTarefaService.FindByID(id);
			data.TrammitProcesso = data.TrammitTarefa.TrammitProcesso;
			data.TrammitItem = data.TrammitTarefa.TrammitItem;

			if (data.TrammitTarefa == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index", new { trammitprid = data.TrammitProcesso.ID });
			}

			data.ReadOnly = readOnly;
			if (!data.ReadOnly)
			{
				var lastStatus = _trammitTarefaStatusService.Get(data.TrammitItem.ID.Value, data.TrammitProcesso.ID.Value);
				if ((lastStatus.Any()) && (lastStatus.Last().TrammitStatus.IsPending == false))
				{
					Web.SetMessage("Item já finalizado.", "error");
					return RedirectToAction("Index", new { trammitprid = data.TrammitProcesso.ID });
				}
			}

			data.TrammitTarefaStatus = _trammitTarefaStatusService.Get(data.TrammitItem.ID.Value, data.TrammitProcesso.ID.Value);
			data.Ativo = _ativoService.FindByID(data.TrammitProcesso.AtivoID.Value);

			return AdminContent("TrammitTarefa/TrammitTarefaEdit.aspx", data);
		}

		public ActionResult View(int id)
		{
			return Edit(id, true);
		}

		public ActionResult UpdateStatus(int id, string extra)
		{
			int idTrammitProcesso = 0;
			int idTrammitStatus = 0;

			try
			{
				var splittedExtra = extra.Split(',');
				idTrammitProcesso = splittedExtra[0].ToInt();
				idTrammitStatus = splittedExtra[1].ToInt();

				var trammitItem = _trammitItemService.FindByID(id);
				if (trammitItem == null)
					throw new Exception("Item não localizado.");
				var trammitProcesso = _trammitProcessoService.FindByID(idTrammitProcesso);
				if (trammitProcesso == null)
					throw new Exception("Processo não localizado.");
				var trammitStatus = _trammitStatusService.FindByID(idTrammitStatus);
				if (trammitStatus == null)
					throw new Exception("Status não localizado.");

				if ((trammitItem == null) || (trammitProcesso == null) || (trammitStatus == null))
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_trammitTarefaStatusService.Insert(trammitItem.ID.Value, trammitProcesso.ID.Value, trammitStatus.ID.Value, UserSession.Person.ID.Value);
					Web.SetMessage("Status alterado com sucesso");
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.BaseUrl + "Admin/TrammitTarefa?trammitprid=" + idTrammitProcesso }, JsonRequestBehavior.AllowGet);
			return RedirectToAction("Index", new { trammitprid = idTrammitProcesso });
		}

		public ActionResult UpdateStatusMultiple(string ids, string extra)
		{
			int idTrammitProcesso = 0;
			int idTrammitStatus = 0;

			try
			{
				var splittedExtra = extra.Split(',');
				idTrammitProcesso = splittedExtra[0].ToInt();
				idTrammitStatus = splittedExtra[1].ToInt();

				var idsTrammitItem = ids.Split(',').Select(i => i.ToInt(0));
				if (idsTrammitItem.Any())
				{
					var trammitProcesso = _trammitProcessoService.FindByID(idTrammitProcesso);
					if (trammitProcesso == null)
						throw new Exception("Processo não localizado.");
					var trammitStatus = _trammitStatusService.FindByID(idTrammitStatus);
					if (trammitStatus == null)
						throw new Exception("Status não localizado.");

					foreach (var idTrammitItem in idsTrammitItem)
						_trammitTarefaStatusService.Insert(idTrammitItem, trammitProcesso.ID.Value, trammitStatus.ID.Value, UserSession.Person.ID.Value);

					Web.SetMessage("Status alterado com sucesso");
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.BaseUrl + "Admin/TrammitTarefa?trammitprid=" + idTrammitProcesso }, JsonRequestBehavior.AllowGet);
			return RedirectToAction("Index", new { trammitprid = idTrammitProcesso });
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var trammitTarefa = new TrammitTarefa();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					trammitTarefa = _trammitTarefaService.FindByID(Request["ID"].ToInt(0));
					if (trammitTarefa == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}
				else
				{
					trammitTarefa.DateAdded = DateTime.Now;
				}

				trammitTarefa.UpdateFromRequest();

				var trammitProcesso = _trammitProcessoService.FindByID(trammitTarefa.TrammitProcessoID.Value);
				if (trammitProcesso == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				_trammitTarefaService.Save(trammitTarefa);
				_trammitTarefaHasAttachmentService.DeleteMany(trammitTarefa.TrammitTarefaHasAttachmentList);

				trammitTarefa.UpdateChildrenFromRequest();

				_trammitTarefaHasAttachmentService.InsertMany(trammitTarefa.TrammitTarefaHasAttachmentList);

				if (!isEdit)
				{
					var trammitStatus = _trammitStatusService.FindByID(Request["TrammitStatusID"].ToInt());
					if (trammitStatus == null)
						throw new Exception("Status não localizado.");

					_trammitTarefaStatusService.Insert(trammitTarefa.TrammitItemID.Value, trammitTarefa.TrammitProcessoID.Value, trammitStatus.ID.Value, UserSession.Person.ID.Value);
                    
                }
                else
                {
                    if (Request["TrammitStatusID"]!=null)
                    {
                        var extra = trammitTarefa.TrammitProcessoID.Value + "," + Request["TrammitStatusID"].ToInt();
                        UpdateStatus(trammitTarefa.TrammitItemID.Value, extra);
                    }                    
                }

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

                

                if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? trammitTarefa.GetAdminURL() : Web.BaseUrl + "Admin/TrammitTarefa/?trammitprid=" + trammitTarefa.TrammitProcessoID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage }, JsonRequestBehavior.AllowGet);
				}

				return RedirectToAction("Index", new { trammitprid = trammitTarefa.TrammitProcessoID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
                if (Fmt.ConvertToBool(Request["ajax"]))
                    return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);

                //if (Fmt.ConvertToBool(Request["ajax"]))
				//	return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["TrammitTarefaModel"] = trammitTarefa;
				return RedirectToAction("Index", "Dashboard");
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
			public IEnumerable<TrammitTarefaLastStatusDto> TrammitTarefas;
			public IEnumerable<TrammitStatus> BulkTrammitStatus;
			public Ativo Ativo;
			public Trammit Trammit;
            public TrammitProcesso TrammitProcesso;
            public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Ativo Ativo;
			public TrammitStatus TrammitStatus;
			public TrammitProcesso TrammitProcesso;
			public TrammitTarefa TrammitTarefa;
			public TrammitItem TrammitItem;
			public IEnumerable<TrammitTarefaStatus> TrammitTarefaStatus;
			public bool ReadOnly;
		}
	}
}
