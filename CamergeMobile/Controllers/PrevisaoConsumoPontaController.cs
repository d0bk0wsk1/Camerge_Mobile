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
	public class PrevisaoConsumoPontaController : ControllerBase
	{
		private readonly IPrevisaoConsumoPontaService _previsaoConsumoPontaService;

		public PrevisaoConsumoPontaController(IPrevisaoConsumoPontaService previsaoConsumoPontaService)
		{
			_previsaoConsumoPontaService = previsaoConsumoPontaService;
		}

		//
		// GET: /Admin/PrevisaoConsumoPonta/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _previsaoConsumoPontaService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.PrevisaoConsumoPontas = paging.Items;

			return AdminContent("PrevisaoConsumoPonta/PrevisaoConsumoPontaList.aspx", data);
		}

		//
		// GET: /Admin/GetPrevisaoConsumoPontas/
		public JsonResult GetPrevisaoConsumoPontas()
		{
			var estimativaConsumoPontas = _previsaoConsumoPontaService.GetAll().Select(o => new { o.ID, o.DataVigencia });
			return Json(estimativaConsumoPontas, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.PrevisaoConsumoPonta = TempData["PrevisaoConsumoPontaModel"] as PrevisaoConsumoPonta;
			if (data.PrevisaoConsumoPonta == null)
			{
				data.PrevisaoConsumoPonta = new PrevisaoConsumoPonta();
				data.PrevisaoConsumoPonta.UpdateFromRequest();
			}
			return AdminContent("PrevisaoConsumoPonta/PrevisaoConsumoPontaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.PrevisaoConsumoPonta = TempData["PrevisaoConsumoPontaModel"] as PrevisaoConsumoPonta ?? _previsaoConsumoPontaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.PrevisaoConsumoPonta == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("PrevisaoConsumoPonta/PrevisaoConsumoPontaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var estimativaConsumoPonta = _previsaoConsumoPontaService.FindByID(id);
			if (estimativaConsumoPonta == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			estimativaConsumoPonta.ID = null;
			TempData["PrevisaoConsumoPontaModel"] = estimativaConsumoPonta;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var estimativaConsumoPonta = _previsaoConsumoPontaService.FindByID(id);
				if (estimativaConsumoPonta == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_previsaoConsumoPontaService.Delete(estimativaConsumoPonta);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PrevisaoConsumoPonta" }, JsonRequestBehavior.AllowGet);
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
				_previsaoConsumoPontaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PrevisaoConsumoPonta" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var estimativaConsumoPonta = new PrevisaoConsumoPonta();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					estimativaConsumoPonta = _previsaoConsumoPontaService.FindByID(Request["ID"].ToInt(0));
					if (estimativaConsumoPonta == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				estimativaConsumoPonta.UpdateFromRequest();

				if (estimativaConsumoPonta.PrevisaoConsumoPontaGerador > 1)
					estimativaConsumoPonta.PrevisaoConsumoPontaGerador = (estimativaConsumoPonta.PrevisaoConsumoPontaGerador / 100);
				if (estimativaConsumoPonta.EstimativaConsumoForaPonta > 1)
					estimativaConsumoPonta.EstimativaConsumoForaPonta = (estimativaConsumoPonta.EstimativaConsumoForaPonta / 100);
				if (estimativaConsumoPonta.TipoCusto == null)
					estimativaConsumoPonta.Custo = 0;

				if ((estimativaConsumoPonta.EstimativaConsumoForaPonta + estimativaConsumoPonta.PrevisaoConsumoPontaGerador) > 1)
					throw new Exception("Campos 'Fora Ponta' e 'Ponta' não devem totalizar mais de 100% (1).");

				_previsaoConsumoPontaService.Save(estimativaConsumoPonta);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? estimativaConsumoPonta.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PrevisaoConsumoPonta";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { estimativaConsumoPonta.ID });
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
				TempData["PrevisaoConsumoPontaModel"] = estimativaConsumoPonta;
				return isEdit && estimativaConsumoPonta != null ? RedirectToAction("Edit", new { estimativaConsumoPonta.ID }) : RedirectToAction("Create");
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
			public List<PrevisaoConsumoPonta> PrevisaoConsumoPontas;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public PrevisaoConsumoPonta PrevisaoConsumoPonta;
			public Boolean ReadOnly;
		}
	}
}
