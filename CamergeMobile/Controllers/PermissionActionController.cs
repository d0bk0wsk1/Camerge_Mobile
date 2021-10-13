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
	public class PermissionActionController : ControllerBase
	{
		private readonly IPermissionActionService _permissionActionService;
		private readonly IRoleService _roleService;

		public PermissionActionController(IPermissionActionService permissionActionService,
			IRoleService roleService)
		{
			_permissionActionService = permissionActionService;
			_roleService = roleService;
		}

		public ActionResult Index(int role, Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _permissionActionService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.PermissionActions = paging.Items;
			data.Role = _roleService.FindByID(role);

			return AdminContent("PermissionAction/PermissionActionList.aspx", data);
		}

		public JsonResult GetPermissionActions()
		{
			var permissionActions = _permissionActionService.GetAll().Select(o => new { o.ID, o.Controller, o.Action });
			return Json(permissionActions, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create(int role)
		{
			var data = new FormViewModel();
			data.PermissionAction = TempData["PermissionActionModel"] as PermissionAction;
			data.Role = _roleService.FindByID(role);
			if (data.PermissionAction == null)
			{
				data.PermissionAction = new PermissionAction();
				data.PermissionAction.RoleID = role;

				data.PermissionAction.UpdateFromRequest();
			}
			return AdminContent("PermissionAction/PermissionActionEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.PermissionAction = TempData["PermissionActionModel"] as PermissionAction ?? _permissionActionService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.PermissionAction == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.Role = data.PermissionAction.Role;

			return AdminContent("PermissionAction/PermissionActionEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var permissionAction = _permissionActionService.FindByID(id);
			if (permissionAction == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			permissionAction.ID = null;
			TempData["PermissionActionModel"] = permissionAction;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(permissionAction.RoleID.Value);
		}

		public ActionResult Del(Int32 id)
		{
			var permissionAction = _permissionActionService.FindByID(id);
			if (permissionAction == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_permissionActionService.Delete(permissionAction);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PermissionAction/?role=" + permissionAction.RoleID }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index", new { role = permissionAction.RoleID });
		}

		public ActionResult DelMultiple(String ids)
		{
			var permissionIds = ids.Split(',').Select(id => id.ToInt(0));
			var permissionAction = _permissionActionService.FindByID(permissionIds.First());

			try
			{
				_permissionActionService.DeleteMany(permissionIds);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PermissionAction/?role" + permissionAction.RoleID }, JsonRequestBehavior.AllowGet);
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
			var permissionAction = new PermissionAction();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					permissionAction = _permissionActionService.FindByID(Request["ID"].ToInt(0));
					if (permissionAction == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				permissionAction.UpdateFromRequest();

				var role = _roleService.FindByID(permissionAction.RoleID.Value);
				if (role == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				if ((!isEdit) && (_permissionActionService.CheckControllerExists(permissionAction.RoleID.Value, permissionAction.Controller)))
					throw new Exception("Já existe este controller cadastrado para esta role.");

				_permissionActionService.Save(permissionAction);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? permissionAction.GetAdminURL() : Web.BaseUrl + "Admin/PermissionAction/?role=" + permissionAction.RoleID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { permissionAction.ID });

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
				TempData["PermissionActionModel"] = permissionAction;
				return isEdit && permissionAction != null ? RedirectToAction("Edit", new { permissionAction.ID }) : RedirectToAction("Create", permissionAction.RoleID);
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
			public List<PermissionAction> PermissionActions;
			public Role Role;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public PermissionAction PermissionAction;
			public Role Role;
			public bool ReadOnly;
		}
	}
}
