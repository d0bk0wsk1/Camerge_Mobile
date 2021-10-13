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
	public class FaturaController : ControllerBase
	{
		private readonly IFaturaService _faturaService;

		public FaturaController(IFaturaService faturaService)
		{
			_faturaService = faturaService;
		}

		public ActionResult Index(int? Page)
		{
			var data = new ListViewModel();
			var paging = _faturaService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Faturas = paging.Items;

			return AdminContent("Fatura/FaturaList.aspx", data);
		}

		public ActionResult Edit(int id, bool readOnly = false)
		{
			var data = new FormViewModel();
			data.Fatura = TempData["FaturaModel"] as Fatura ?? _faturaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Fatura == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Fatura/FaturaEdit.aspx", data);
		}

		public ActionResult View(int id)
		{
			return Edit(id, true);
		}

		public ActionResult Del(int id)
		{
			var contabilizacao = _faturaService.FindByID(id);
			if (contabilizacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_faturaService.Delete(contabilizacao);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Fatura" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(string ids)
		{
			_faturaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Fatura" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var fatura = new Fatura();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					fatura = _faturaService.FindByID(Request["ID"].ToInt(0));
					if (fatura == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				fatura.UpdateFromRequest();
				_faturaService.Save(fatura);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? fatura.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Fatura";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { fatura.ID });

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
				TempData["FaturaModel"] = fatura;
				return isEdit && fatura != null ? RedirectToAction("Edit", new { fatura.ID }) : RedirectToAction("Create");
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
			public Fatura Fatura;
			public bool ReadOnly;
		}

		public class ListViewModel
		{
			public List<Fatura> Faturas;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
