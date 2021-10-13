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
	public class RelatorioQueueController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IMapeadorMedicaoCacheQueueService _mapeadorMedicaoCacheQueueService;
		private readonly IRelatorioQueueService _relatorioQueueService;

		public RelatorioQueueController(IAtivoService ativoService,
			IMapeadorMedicaoCacheQueueService mapeadorMedicaoCacheQueueService,
			IRelatorioQueueService relatorioQueueService)
		{
			_ativoService = ativoService;
			_mapeadorMedicaoCacheQueueService = mapeadorMedicaoCacheQueueService;
			_relatorioQueueService = relatorioQueueService;
		}

		public ActionResult Create()
		{
			var data = new FormViewModel()
			{
				RelatorioQueue = new RelatorioQueue()
			};

			return AdminContent("RelatorioQueue/RelatorioQueueEdit.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var relatorioQueue = new RelatorioQueue();

			try
			{
				var ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);
				if (!ativos.Any())
					throw new Exception("Não foi possível localizar o(s) ativo(s) ou campo é inválido.");

				var dataInicio = Request["DataInicio"].ConvertToDate(null);
				var dataFim = Request["DataFim"].ConvertToDate(null);
				var hasMapeador = Request["hasMapeador"].ToBoolean();

				if ((dataInicio == null) || (dataFim == null))
					throw new Exception("Campos meses não podem ser nulos.");
				if (dataInicio > dataFim)
					throw new Exception("Mês inicial não pode ser mais que mês final.");

				var fromDate = Dates.GetFirstDayOfMonth(dataInicio.Value);
				var toDate = Dates.GetLastDayOfMonth(dataFim.Value);

				for (var date = fromDate; date <= toDate; date = date.AddMonths(1))
				{
					foreach (var ativo in ativos)
					{
						if (((ativo.DataInicioVigencia == null) || (date >= ativo.DataInicioVigencia.Value))
							&& ((ativo.DataFimVigencia == null) || (date <= ativo.DataFimVigencia.Value)))
						{
							// _relatorioQueueService.Insert(new RelatorioQueue() { AtivoID = ativo.ID });
							_relatorioQueueService.Insert(new RelatorioQueue() { AtivoID = ativo.ID, Date = date });

							if ((hasMapeador) && (ativo.PerfilAgente.IsConsumidor))
							{
								_mapeadorMedicaoCacheQueueService.Insert(
									new MapeadorMedicaoCacheQueue() { AtivoID = ativo.ID, Mes = date, TipoLeitura = Medicao.TiposLeitura.Consumo.ToString(), DateAdded = DateTime.Now }
								);
							}
						}
					}
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ScheduledTaskMonitor";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index", "ScheduledTaskMonitor");
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });

				TempData["RelatorioQueueModel"] = relatorioQueue;
				return RedirectToAction("Create");
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
			public RelatorioQueue RelatorioQueue;
		}
	}
}
