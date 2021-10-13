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
	public class TrammitProcessoController : ControllerBase
	{
		private readonly ITrammitService _trammitService;
		private readonly ITrammitProcessoService _trammitProcessoService;
        private readonly ITrammitTarefaService _trammitTarefaService;

        public TrammitProcessoController(ITrammitService trammitService,
				ITrammitProcessoService trammitProcessoService,
                ITrammitTarefaService trammitTarefaService)
		{
			_trammitService = trammitService;
            _trammitTarefaService = trammitTarefaService;
            _trammitProcessoService = trammitProcessoService;
		}

		//
		// GET: /Admin/TrammitProcesso/
		public ActionResult Index(int? trammitid, int? Page)
		{
		    var data = new ListViewModel();
            var paging = _trammitProcessoService.GetDtoAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

            data.PageNum = paging.CurrentPage;
            data.PageCount = paging.TotalPages;
            data.TotalRows = paging.TotalItems;
            data.TrammitProcessos = paging.Items;
            if (trammitid!=null)
                data.Trammit = _trammitService.FindByID(trammitid.Value);

            foreach (var processo in data.TrammitProcessos)
            {
               var list = _trammitTarefaService.GetLastStatus(processo.ID);
               processo.TrammitTarefas = list.OrderBy(i => i.Positionorder);
            }
            
            if (Request["finalizados"]== "onlyFinished")
               data.TrammitProcessos = data.TrammitProcessos.Where(w => w.TrammitTarefas.Count()>0 && w.TrammitTarefas.Count(c => c.IsFinalizado) == w.TrammitTarefas.Count()).ToList();

            if (Request["finalizados"] == "onlyNotFinished")
                data.TrammitProcessos = data.TrammitProcessos.Where(w => w.TrammitTarefas.Count(c => c.IsFinalizado) != w.TrammitTarefas.Count()).ToList();


            return AdminContent("TrammitProcesso/TrammitProcessoList.aspx", data);

            
			//return HttpNotFound();
		}

		public ActionResult Create(int? trammitid)
		{
			if (trammitid != null)
			{
				var data = new FormViewModel();
				data.TrammitProcesso = TempData["TrammitProcessoModel"] as TrammitProcesso;
				data.Trammit = _trammitService.FindByID(trammitid.Value);
				if (data.TrammitProcesso == null)
				{
					data.TrammitProcesso = new TrammitProcesso();
					data.TrammitProcesso.TrammitID = trammitid.Value;

					data.TrammitProcesso.UpdateFromRequest();
				}
				return AdminContent("TrammitProcesso/TrammitProcessoEdit.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.TrammitProcesso = TempData["TrammitProcessoModel"] as TrammitProcesso ?? _trammitProcessoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.TrammitProcesso == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.Trammit = data.TrammitProcesso.Trammit;

			return AdminContent("TrammitProcesso/TrammitProcessoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var TrammitProcesso = _trammitProcessoService.FindByID(id);
			if (TrammitProcesso == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			TrammitProcesso.ID = null;
			TempData["TrammitProcessoModel"] = TrammitProcesso;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(TrammitProcesso.TrammitID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var TrammitProcesso = _trammitProcessoService.FindByID(id);
				if (TrammitProcesso == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_trammitProcessoService.Delete(TrammitProcesso);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TrammitProcesso" }, JsonRequestBehavior.AllowGet);
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
				_trammitProcessoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TrammitProcesso" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var trammitProcesso = new TrammitProcesso();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					trammitProcesso = _trammitProcessoService.FindByID(Request["ID"].ToInt(0));
					if (trammitProcesso == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}
				else
				{
					trammitProcesso.DateAdded = DateTime.Now;
				}

				trammitProcesso.UpdateFromRequest();

				var trammit = _trammitService.FindByID(trammitProcesso.TrammitID.Value);
				if (trammit == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				_trammitProcessoService.Save(trammitProcesso);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? trammitProcesso.GetAdminURL() : Web.BaseUrl + "Admin/TrammitProcesso/?trammitid=" + trammitProcesso.TrammitID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { trammitProcesso.ID });

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
				TempData["TrammitProcessoModel"] = trammitProcesso;
				return isEdit && trammitProcesso != null ? RedirectToAction("Edit", new { trammitProcesso.ID }) : RedirectToAction("Create", trammitProcesso.TrammitID);
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
			public List<TrammitProcessoDto> TrammitProcessos;
			public Trammit Trammit;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public TrammitProcesso TrammitProcesso;
			public Trammit Trammit;
			public Boolean ReadOnly;
		}
	}
}
