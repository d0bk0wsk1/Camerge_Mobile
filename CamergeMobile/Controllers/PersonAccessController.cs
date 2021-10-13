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
	public class PersonAccessController : ControllerBase
	{
		private readonly IPersonService _personService;
		private readonly IPersonAccessService _personAccessService;

		public PersonAccessController(IPersonService personService,
			IPersonAccessService personAccessService)
		{
			_personService = personService;
			_personAccessService = personAccessService;
		}

		public ActionResult Index(int person, Int32? Page)
		{
			var actionParams = Request.Params;
			actionParams = Fmt.GetNewNameValueCollection(new { peid = person, Page = Page }, Request.Params);

			var data = new ListViewModel();
			var paging = _personAccessService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				actionParams);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.PersonAccesses = paging.Items;

			data.Person = _personService.FindByID(person);

			return AdminContent("PersonAccess/PersonAccessList.aspx", data);
		}

		public ActionResult Create(int person)
		{
			var data = new FormViewModel();
			data.PersonAccess = TempData["PersonAccessModel"] as PersonAccess;
			if (data.PersonAccess == null)
			{
				data.PersonAccess = new PersonAccess();

				var model = _personService.FindByID(person);
				if (model != null)
				{
					data.PersonAccess.Person = model;
					data.PersonAccess.PersonID = model.ID;
				}

				data.PersonAccess.UpdateFromRequest();
			}
			return AdminContent("PersonAccess/PersonAccessEdit.aspx", data);
		}

		public ActionResult Del(Int32 id)
		{
			var personAccess = _personAccessService.FindByID(id);
			if (personAccess == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_personAccessService.Delete(personAccess);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PersonAccess/?person=" + personAccess.PersonID }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index", new { person = personAccess.PersonID });
		}

		public ActionResult DelMultiple(String ids)
		{
			_personAccessService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PersonAccess" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var personAccess = new PersonAccess();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					personAccess = _personAccessService.FindByID(Request["ID"].ToInt(0));
					if (personAccess == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				personAccess.UpdateFromRequest();
				_personAccessService.Save(personAccess);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? personAccess.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/PersonAccess/?person=" + personAccess.PersonID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { personAccess.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index", new { person = personAccess.PersonID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["PersonAccessModel"] = personAccess;
				return isEdit && personAccess != null ? RedirectToAction("Edit", new { personAccess.ID }) : RedirectToAction("Create");
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
			public PersonAccess PersonAccess;
			public Boolean ReadOnly;
		}

		public class ListViewModel
		{
			public Person Person;
			public List<PersonAccess> PersonAccesses;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
