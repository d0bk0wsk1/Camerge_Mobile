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
	public class MedicaoImportacaoController : ControllerBase
	{
		private readonly IGeradorLeituraQueueService _geradorLeituraQueueService;
		private readonly IMapeadorMedicaoCacheQueueService _mapeadorMedicaoCacheQueueService;
		private readonly IMedicaoAjustaCanaisQueueService _medicaoAjustaCanaisQueueService;
		private readonly IMedicaoImportacaoService _medicaoImportacaoService;
		private readonly IRelatorioQueueService _relatorioQueueService;

		public MedicaoImportacaoController(IGeradorLeituraQueueService geradorLeituraQueueService,
			IMapeadorMedicaoCacheQueueService mapeadorMedicaoCacheQueueService,
			IMedicaoAjustaCanaisQueueService medicaoAjustaCanaisQueueService,
			IMedicaoImportacaoService medicaoImportacaoService,
			IRelatorioQueueService relatorioQueueService)
		{
			_geradorLeituraQueueService = geradorLeituraQueueService;
			_mapeadorMedicaoCacheQueueService = mapeadorMedicaoCacheQueueService;
			_medicaoAjustaCanaisQueueService = medicaoAjustaCanaisQueueService;
			_medicaoImportacaoService = medicaoImportacaoService;
			_relatorioQueueService = relatorioQueueService;
		}

		public ActionResult Index(Int32? Page, bool refreshable = true)
		{
			var data = new ListViewModel();
			var paging = _medicaoImportacaoService.GetAllWithPaging(
				Page ?? 1,
				 200,
				Request.Params);

            //var paging = _medicaoImportacaoService.GetAllWithPaging(
              //  Page ?? 1,
                //Util.GetSettingInt("ItemsPerPage", 200),
               // Request.Params);

            data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.IsRefreshable = refreshable;
			data.FilaImportacao = paging.Items;

			if (data.IsRefreshable)
			{
				data.CountAjustaCanaisQueue = _medicaoAjustaCanaisQueueService.CountAll();
				data.CountGeradorDieselQueue = _geradorLeituraQueueService.CountAll();
				data.CountMapeadorMedicaoCacheQueue = _mapeadorMedicaoCacheQueueService.CountAll();
				data.CountRelatorioQueue = _relatorioQueueService.CountAll();
			}

			if (Request["Retry"].ToInt(0) != 0)
			{
				_medicaoImportacaoService.ReagendarImportacao(Request["Retry"].ToInt(0));
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "Retry"));
			}

			return AdminContent("MedicaoImportacao/MedicaoImportacaoList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.MedicaoImportacao = TempData["MedicaoImportacaoModel"] as MedicaoImportacao;
			if (data.MedicaoImportacao == null)
			{
				data.MedicaoImportacao = new MedicaoImportacao();
				data.MedicaoImportacao.UpdateFromRequest();
			}

			data.MedicaoImportacao.IgnorarPrimeiraLinha = true;
			data.MedicaoImportacao.QtdeLinhasIgnoradas = 3;

			return AdminContent("MedicaoImportacao/MedicaoImport.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var medicaoImportacao = new MedicaoImportacao();

			try
			{
				medicaoImportacao.UpdateFromRequest();

				medicaoImportacao.Status = MedicaoImportacao.ImportacaoStatus.Fila.ToString();
				medicaoImportacao.RowsProcessed = 0;
				medicaoImportacao.IsProcessado = false;
				medicaoImportacao.TotalRows = _medicaoImportacaoService.ValidateAndGetTotalRows(medicaoImportacao.Attachment, medicaoImportacao.IgnorarPrimeiraLinha ?? false, medicaoImportacao.Delimitador);

				_medicaoImportacaoService.Save(medicaoImportacao);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? medicaoImportacao.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoImportacao";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

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
				TempData["MedicaoImportacaoModel"] = medicaoImportacao;
				return RedirectToAction("Create");
			}
		}

		public ActionResult PopupHelp()
		{
			return View("~/Areas/Admin/Views/MedicaoImportacao/PopupHelp.aspx");
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
			public List<MedicaoImportacao> FilaImportacao;
			public bool IsRefreshable { get; set; }
			public int CountAjustaCanaisQueue { get; set; }
			public int CountGeradorDieselQueue { get; set; }
			public int CountMapeadorMedicaoCacheQueue { get; set; }
			public int CountRelatorioQueue { get; set; }
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public MedicaoImportacao MedicaoImportacao;
		}
	}
}
