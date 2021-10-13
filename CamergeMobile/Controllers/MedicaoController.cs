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
	public class MedicaoController : ControllerBase
	{

		private readonly IMedicaoService _medicaoService;
		private readonly IReportCacheService _reportCacheService;
		private readonly IRelatorioQueueService _relatorioQueueService;

		public MedicaoController(
			IMedicaoService medicaoService,
			IReportCacheService reportCacheService,
			IRelatorioQueueService relatorioQueueService) {
			_medicaoService = medicaoService;
			_reportCacheService = reportCacheService;
			_relatorioQueueService = relatorioQueueService;
		}

		//
		// GET: /Admin/Medicao/
		//public ActionResult Index(Int32? Page) {

		//	var data = new ListViewModel();
		//	var sql = new SqlQuery("SELECT * FROM medicao WHERE 1=1");

		//	Page = Page ?? 1;
		//	sql.Paging(Util.GetSettingInt("ItemsPerPage", 30), Page.Value);
		//	data.PageNum = Page.Value;
		//	AddFilters(ref sql);
		//	AddOrder(ref sql);
		//	sql.FetchPageCount();
		//	data.PageCount = sql.PageCount;
		//	data.TotalRows = sql.TotalRowCount;

		//	data.Medicaos = MedicaoList.Load(sql);

		//	return AdminContent("Medicao/MedicaoList.aspx", data);
		//}

		//private void AddFilters(ref Sql sql) {
		//	if (Request["text"].IsNotBlank()) {
		//		sql.Add("AND nome_patamar ILIKE").AddParameter("%"+Request["text"]+"%");
		//	}
		//}

		//private void AddOrder(ref Sql sql) {
		//	if (Request["sort"].IsNotBlank()) {
		//		String field = null;
		//		switch (Request["sort"]) {
		//			case "nomePatamar": field = "nome_patamar"; break;
		//			case "tipoLeitura": field = "tipo_leitura"; break;
		//			case "tarifacao": field = "tarifacao"; break;

		//		}
		//		if (field != null) {
		//			var direction = Request["dir"] == "desc" ? "DESC" : "ASC";
		//			sql.Add("ORDER BY").Add(field).Add(direction);
		//		}
		//	}
		//}

		//
		// GET: /Admin/GetMedicaos/
		//public JsonResult GetMedicaos() {
		//	var medicaos = MedicaoList.LoadAll().Select(o => new { o.ID, o.NomePatamar });
		//	return Json(medicaos, JsonRequestBehavior.AllowGet);
		//}

		public ActionResult Create() {
			var data = new FormViewModel();
			data.Medicao = TempData["MedicaoModel"] as Medicao;
			if (data.Medicao == null) {
				data.Medicao = new Medicao();
				data.Medicao.UpdateFromRequest();
			}
			return AdminContent("Medicao/MedicaoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id) {
			var data = new FormViewModel();
			data.Medicao = TempData["MedicaoModel"] as Medicao ?? _medicaoService.FindByID(id);
			if (data.Medicao == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				//return RedirectToAction("Index");
				return Redirect("~/Admin/MedicaoErro/");
			}
			return AdminContent("Medicao/MedicaoEdit.aspx", data);
		}

		public ActionResult Duplicate(Int32 id) {
			var medicao = _medicaoService.FindByID(id);
			if (medicao == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				//return RedirectToAction("Index");
				return Redirect("~/Admin/MedicaoErro/");
			}
			medicao.ID = null;
			TempData["MedicaoModel"] = medicao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id) {
			var medicao = _medicaoService.FindByID(id);
			if (medicao == null) {
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			} else {
				medicao.Delete();
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Medicao" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			//return RedirectToAction("Index");
			return Redirect("~/Admin/MedicaoErro/");
		}

		public ActionResult DelMultiple(String ids) {

			_medicaoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"])) {
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Medicao" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null) {
				return Redirect(previousUrl);
			}

			//return RedirectToAction("Index");
			return Redirect("~/Admin/MedicaoErro/");
		}

		[ValidateInput(false)]
		public ActionResult Save() {

			var medicao = new Medicao();
			var isEdit = Request["ID"].IsNotBlank();

			try {

				if (isEdit) {
					medicao = _medicaoService.FindByID(Request["ID"].ToInt(0));
					if (medicao == null) {
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				medicao.UpdateFromRequest();

				var m = medicao.DataLeituraFim.Value.Minute;
				if (m != 0 && m != 15 && m != 30 && m != 45) {
					throw new Exception("Data de leitura inválida. Pacotes são agrupados em 15 minutos.");
				}

				medicao.DataLeituraInicio = medicao.DataLeituraFim.Value.AddMinutes(-15);
				medicao.Origem = "MANUAL";

				var medicaoExistente = Medicao.Load(new SqlQuery("WHERE data_leitura_inicio = ").AddParameter(medicao.DataLeituraInicio.Value).Add("AND tipo_leitura = ").AddParameter(medicao.TipoLeitura).Add("AND ativo_id = ").AddParameter(medicao.AtivoID).Add("LIMIT 1"));
				if (medicaoExistente != null) {
					throw new Exception("Medição de " + medicao.TipoLeitura + " para " + medicao.DataLeituraFim.Value + " já existe");
				}

				_medicaoService.Save(medicao);

				var medicaoErro = MedicaoErro.LoadByID(Request["MedicaoErroID"].ToInt(0));
				if (medicaoErro != null && !medicaoErro.Resolvido.Value) {
					medicaoErro.Resolvido = true;
					medicaoErro.Save();
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"].IsBlank() || Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"])) {
					var nextPage = isSaveAndRefresh ? medicao.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Medicao";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh) {
					return RedirectToAction("Edit", new { medicao.ID });
				}

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null) {
					return Redirect(previousUrl);
				}

				_relatorioQueueService.Insert(new RelatorioQueue {AtivoID = medicaoErro.AtivoID.Value, Date = medicaoErro.DataLeitura.Value.Date});

				//return RedirectToAction("Index");
				return Redirect("~/Admin/MedicaoErro/");

			} catch (Exception ex) {
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"])) {
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				TempData["MedicaoModel"] = medicao;
				return isEdit && medicao != null ? RedirectToAction("Edit", new { medicao.ID }) : RedirectToAction("Create");
			}
		}

		private string HandleExceptionMessage(Exception ex) {
			string errorMessage;
			if (ex is RequiredFieldNullException) {
				var fieldName = ((RequiredFieldNullException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "NullException").Replace("XXX", friendlyFieldName);
			} else if (ex is FieldLengthException) {
				var fieldName = ((FieldLengthException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "LengthException").Replace("XXX", friendlyFieldName);
			} else {
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

		public class ListViewModel {
			public List<Medicao> Medicaos;
			public Int32 TotalRows;
			public Int32 PageCount;
			public Int32 PageNum;
		}

		public class FormViewModel {
			public Medicao Medicao;
		}
	}
}
