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
	public class ScheduledTaskMonitorController : ControllerBase
	{
		private readonly IRelatorioQueueService _relatorioQueueService;

		public ScheduledTaskMonitorController(IRelatorioQueueService relatorioQueueService)
		{
			_relatorioQueueService = relatorioQueueService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel();
			data.RelatorioQueueItems = _relatorioQueueService
					.GetAll()
					.OrderByDescending(x => x.DateAssigned)
					.ThenBy(x => x.ID)
							.ToList();

			data.ReportControllers = new Dictionary<DateTime, string>();
			var ctrlDates =
					data.RelatorioQueueItems.Where(x => x.DateAssigned.HasValue)
							.GroupBy(x => x.DateAssigned.Value)
							.Select(x => x.Key);

			var ctrlNumber = 1;
			foreach (var ctrlDate in ctrlDates)
			{
				data.ReportControllers[ctrlDate] = string.Format("Controlador {0}", ctrlNumber);
				ctrlNumber++;
			}

			return AdminContent("ScheduledTaskMonitor/ScheduledTaskMonitorList.aspx", data);
		}

		public ActionResult Del(int id)
		{
			try
			{
				var relatorio = _relatorioQueueService.FindByID(id);
				if (relatorio == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_relatorioQueueService.Delete(relatorio);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ScheduledTaskMonitor" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(string ids)
		{
			try
			{
				_relatorioQueueService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ScheduledTaskMonitor" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult Relist(int id)
		{
			try
			{
				var relatorio = _relatorioQueueService.FindByID(id);
				if (relatorio == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					relatorio.DateAssigned = null;
					_relatorioQueueService.Update(relatorio);

					Web.SetMessage("Item relistado com sucesso.");
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ScheduledTaskMonitor" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult RelistMultiple(string ids)
		{
			try
			{
				var relatoriosIDs = ids.Split(',').Select(id => id.ToInt(0));
				if (relatoriosIDs.Any())
				{
					foreach (var relatorioID in relatoriosIDs)
					{
						var relatorio = _relatorioQueueService.FindByID(relatorioID);
						if (relatorio != null)
						{
							relatorio.DateAssigned = null;
							_relatorioQueueService.Update(relatorio);
						}
					}
				}
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ScheduledTaskMonitor" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
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
			public List<RelatorioQueue> RelatorioQueueItems;
			public Dictionary<DateTime, string> ReportControllers;
		}
	}
}
