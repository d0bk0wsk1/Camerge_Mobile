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
	public class AcessoRapidoController : ControllerBase
	{
		private readonly IAcessoRapidoService _acessoRapidoService;
		private readonly IAcessoRapidoItemService _acessoRapidoItemService;

		public AcessoRapidoController(IAcessoRapidoService acessoRapidoService,
			IAcessoRapidoItemService acessoRapidoItemService)
		{
			_acessoRapidoService = acessoRapidoService;
			_acessoRapidoItemService = acessoRapidoItemService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _acessoRapidoService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.AcessosRapido = paging.Items;

			return AdminContent("AcessoRapido/AcessoRapidoList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.AcessoRapido = TempData["AcessoRapidoModel"] as AcessoRapido;
			if (data.AcessoRapido == null)
			{
				data.AcessoRapido = new AcessoRapido();
				data.AcessoRapido.UpdateFromRequest();
			}
			return AdminContent("AcessoRapido/AcessoRapidoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.AcessoRapido = TempDataAttribute["AcessoRapidoModel"] as AcessoRapido ?? _acessoRapidoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.AcessoRapido == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("AcessoRapido/AcessoRapidoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var acessoRapido = _acessoRapidoService.FindByID(id);
			if (acessoRapido == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			acessoRapido.ID = null;
			TempData["AcessoRapidoModel"] = acessoRapido;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var acessoRapido = _acessoRapidoService.FindByID(id);
				if (acessoRapido == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_acessoRapidoItemService.DeleteByAcessoRapidoID(acessoRapido.ID.Value);
					_acessoRapidoService.Delete(acessoRapido);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AcessoRapido" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids)
		{
			try
			{
				var idsAcessoRapido = ids.Split(',').Select(i => i.ToInt(0));
				if (idsAcessoRapido.Any())
				{
					foreach (var idAcessoRapido in idsAcessoRapido)
						_acessoRapidoItemService.DeleteByAcessoRapidoID(idAcessoRapido);
					_acessoRapidoService.DeleteMany(idsAcessoRapido);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AcessoRapido" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var acessoRapido = new AcessoRapido();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					acessoRapido = _acessoRapidoService.FindByID(Request["ID"].ToInt(0));
					if (acessoRapido == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				acessoRapido.UpdateFromRequest();

				_acessoRapidoService.Save(acessoRapido);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? acessoRapido.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/AcessoRapido";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { acessoRapido.ID });

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
				TempData["AcessoRapidoModel"] = acessoRapido;
				return isEdit && acessoRapido != null ? RedirectToAction("Edit", new { acessoRapido.ID }) : RedirectToAction("Create");
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
			public List<AcessoRapido> AcessosRapido;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public AcessoRapido AcessoRapido;
			public Boolean ReadOnly;
		}
	}
}
