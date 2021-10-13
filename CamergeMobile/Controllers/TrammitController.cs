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
	public class TrammitController : ControllerBase
	{
		private readonly ITrammitService _trammitService;
		private readonly ITrammitItemService _trammitItemService;

		public TrammitController(ITrammitService trammitService,
			ITrammitItemService trammitItemService)
		{
			_trammitService = trammitService;
			_trammitItemService = trammitItemService;
		}

		//
		// GET: /Admin/Trammit/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _trammitService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = (paging.Items.Count() / paging.ItemsPerPage); // paging.TotalPages;
			data.TotalRows = (paging.Items.Count()); // paging.TotalItems;
			data.Trammits = paging.Items;

			return AdminContent("Trammit/TrammitList.aspx", data);
		}

		public ActionResult Create(int? id = null)
		{
			var data = new FormViewModel();
			data.Trammit = TempData["TrammitModel"] as Trammit;
			if (data.Trammit == null)
			{
				data.Trammit = new Trammit();
				data.Trammit.UpdateFromRequest();
			}

			if (id != null)
				data.SourceTrammitID = id.Value;

			return AdminContent("Trammit/TrammitEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Trammit = TempData["TrammitModel"] as Trammit ?? _trammitService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Trammit == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Trammit/TrammitEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var Trammit = _trammitService.FindByID(id);
			if (Trammit == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			var sourceTrammitID = Trammit.ID.Value;

			Trammit.ID = null;

			TempData["TrammitModel"] = Trammit;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");

			return Create(sourceTrammitID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var Trammit = _trammitService.FindByID(id);
				if (Trammit == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_trammitService.Delete(Trammit);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Trammit" }, JsonRequestBehavior.AllowGet);
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
				var idsTrammit = ids.Split(',').Select(i => i.ToInt(0));
				if (idsTrammit.Any())
				{
					_trammitService.DeleteMany(idsTrammit);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Trammit" }, JsonRequestBehavior.AllowGet);
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
			var trammit = new Trammit();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					trammit = _trammitService.FindByID(Request["ID"].ToInt(0));
					if (trammit == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}
				else
				{
					trammit.DateAdded = DateTime.Now;
				}

				trammit.UpdateFromRequest();
				_trammitService.Save(trammit);

				// Duplicate Actions
				if (Request["SourceTrammitID"] != null)
				{
					var items = _trammitItemService.Get(Request["SourceTrammitID"].ToInt(0));
					if (items.Any())
					{
						foreach (var item in items)
						{
							_trammitItemService.Save(new TrammitItem()
							{
								TrammitID = trammit.ID,
								Nome = item.Nome,
								PositionOrder = item.PositionOrder,
								RequireAttachment = item.RequireAttachment,
								RequireComentario = item.RequireComentario,
								RequireDataPrazo = item.RequireDataPrazo,
								IsActive = item.IsActive,
								DateAdded = DateTime.Now
							});
						}
					}
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? trammit.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Trammit";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { trammit.ID });

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
				TempData["TrammitModel"] = trammit;
				return isEdit && trammit != null ? RedirectToAction("Edit", new { trammit.ID }) : RedirectToAction("Create");
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
			public List<Trammit> Trammits;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Trammit Trammit;
			public Boolean ReadOnly;
			public int? SourceTrammitID;
		}
	}
}
