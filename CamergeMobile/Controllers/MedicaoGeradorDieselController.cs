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
	public class MedicaoGeradorDieselController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IMedicaoGeradorDieselService _medicaoGeradorDieselService;
		private readonly IMedicaoGeradorDieselValorService _medicaoGeradorDieselValorService;

		public MedicaoGeradorDieselController(IAtivoService ativoService,
			IMedicaoGeradorDieselService medicaoGeradorDieselService,
			IMedicaoGeradorDieselValorService medicaoGeradorDieselValorService)
		{
			_ativoService = ativoService;
			_medicaoGeradorDieselService = medicaoGeradorDieselService;
			_medicaoGeradorDieselValorService = medicaoGeradorDieselValorService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var ativoId = Request["ativo"].ToInt(0);

			var paging = _medicaoGeradorDieselService.GetAllWithPaging(
				ativoId,
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30));

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Ativo = _ativoService.FindByID(ativoId);
			data.MedicaoGeradorDiesels = paging.Items;

			return AdminContent("MedicaoGeradorDiesel/MedicaoGeradorDieselList.aspx", data);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var medicaoGeradorDiesel = _medicaoGeradorDieselService.FindByID(id);
				if (medicaoGeradorDiesel == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_medicaoGeradorDieselValorService.DeleteByMedicaoGeradorDiesel(medicaoGeradorDiesel.ID.Value);
					_medicaoGeradorDieselService.Delete(medicaoGeradorDiesel);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoGeradorDiesel" }, JsonRequestBehavior.AllowGet);
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
				var medicaoGeradorDieselIds = ids.Split(',').Select(id => id.ToInt(0));
				foreach (var id in medicaoGeradorDieselIds)
				{
					_medicaoGeradorDieselValorService.DeleteByMedicaoGeradorDiesel(id);
				}
				_medicaoGeradorDieselService.DeleteMany(medicaoGeradorDieselIds);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MedicaoGeradorDiesel" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

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
			public Ativo Ativo;
			public List<MedicaoGeradorDiesel> MedicaoGeradorDiesels;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public MedicaoGeradorDiesel MedicaoGeradorDiesel;
			public bool ReadOnly;
		}
	}
}
