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
	public class HorarioVeraoController : ControllerBase
	{
		private readonly IHorarioVeraoService _horarioVeraoService;

		public HorarioVeraoController(IHorarioVeraoService horarioVeraoService)
		{
			_horarioVeraoService = horarioVeraoService;
		}

		//
		// GET: /Admin/HorarioVerao/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _horarioVeraoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.HorarioVeraos = paging.Items;

			return AdminContent("HorarioVerao/HorarioVeraoList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.HorarioVerao = TempData["HorarioVeraoModel"] as HorarioVerao;
			if (data.HorarioVerao == null)
			{
				data.HorarioVerao = new HorarioVerao();
				data.HorarioVerao.UpdateFromRequest();
			}
			return AdminContent("HorarioVerao/HorarioVeraoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.HorarioVerao = TempData["HorarioVeraoModel"] as HorarioVerao ?? _horarioVeraoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.HorarioVerao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("HorarioVerao/HorarioVeraoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var horarioVerao = _horarioVeraoService.FindByID(id);
			if (horarioVerao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			horarioVerao.ID = null;
			TempData["HorarioVeraoModel"] = horarioVerao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var horarioVerao = _horarioVeraoService.FindByID(id);
			if (horarioVerao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_horarioVeraoService.Delete(horarioVerao);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/HorarioVerao" }, JsonRequestBehavior.AllowGet);
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

			_horarioVeraoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/HorarioVerao" }, JsonRequestBehavior.AllowGet);
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

			var horarioVerao = new HorarioVerao();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{

				if (isEdit)
				{
					horarioVerao = _horarioVeraoService.FindByID(Request["ID"].ToInt(0));
					if (horarioVerao == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				horarioVerao.UpdateFromRequest();
				_horarioVeraoService.Save(horarioVerao);

				// horarioVerao.DeleteChildren();
				// horarioVerao.UpdateChildrenFromRequest();
				// horarioVerao.SaveChildren();

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? horarioVerao.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/HorarioVerao";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { horarioVerao.ID });
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
				TempData["HorarioVeraoModel"] = horarioVerao;
				return isEdit && horarioVerao != null ? RedirectToAction("Edit", new { horarioVerao.ID }) : RedirectToAction("Create");
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
			public List<HorarioVerao> HorarioVeraos;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public HorarioVerao HorarioVerao;
			public Boolean ReadOnly;
		}
	}
}
