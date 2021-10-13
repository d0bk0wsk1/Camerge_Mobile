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
	public class PersonController : ControllerBase
	{
		private readonly IPersonService _personService;
		private readonly ILoginLogService _loginLogService;

		public PersonController(IPersonService personService,
			ILoginLogService loginLogService)
		{
			_personService = personService;
			_loginLogService = loginLogService;
		}

		//
		// GET: /Admin/Person/
		public ActionResult Index(Int32? Page)
		{

			var data = new ListViewModel();
			var paging = _personService.GetAllWithPaging(
				UserSession.IsDeveloper ? null : BaseSecurity.BaseRoles.Administrator.ToString(),
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.People = paging.Items;

			return AdminContent("Person/PersonList.aspx", data);
		}

		//
		// GET: /Admin/GetPeople/
		public JsonResult GetPeople()
		{
			var people = _personService.GetAll().Select(o => new { o.ID, o.Name });
			return Json(people, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Person = TempData["PersonModel"] as Person;
			if (data.Person == null)
			{
				data.Person = new Person();
				data.Person.UpdateFromRequest();
			}
			return AdminContent("Person/PersonEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Person = TempData["PersonModel"] as Person ?? _personService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Person == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			/*
			if (data.Person.Roles.IsNotBlank()
					&& data.Person.Roles.Split(',').Contains(BaseSecurity.BaseRoles.Developer.ToString())
					&& !UserSession.IsDeveloper)
			*/

			if ((data.Person.Role.Tipo == Role.Tipos.Developer.ToString()) && (!UserSession.IsDeveloper))
			{
				Response.StatusCode = 403;
				return Index(null);
			}

			data.Person.Password = null;
			return AdminContent("Person/PersonEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult ChangePassword()
		{
			var data = new FormViewModel();
			data.Person = _personService.FindByID(UserSession.Person.ID ?? 0);
			return AdminContent("Person/PasswordEdit.aspx", data);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var person = _personService.FindByID(id);
			if (person == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			person.ID = null;
			person.Password = null;
			TempData["PersonModel"] = person;
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var person = _personService.FindByID(id);
			if (person == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_personService.Delete(person);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Person" }, JsonRequestBehavior.AllowGet);
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

			_personService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Person" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult Historic(int person)
		{
			var query = new PetaPoco.Sql("WHERE person_id = @0 ORDER BY access_date DESC LIMIT 20;", person);

			var viewModel = new HistoricViewModel()
			{
				LoginLogs = _loginLogService.GetAll(query)
			};

			return AdminContent("Person/PersonHistoric.aspx", viewModel);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var person = new Person();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{

				if (isEdit)
				{
					person = _personService.FindByID(Request["ID"].ToInt(0));
					if (person == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				var currentIsActive = Fmt.ConvertToBool(person.IsActive);
				var currentPassword = person.Password;

				person.UpdateFromRequest();
				person.Password = person.Password.IsBlank() ? currentPassword : BaseSecurity.HashPassword(person.Password);

				if ((!isEdit) && (_personService.GetByName(person.Name) != null))
					throw new Exception("Nome já cadastrado.");

				_personService.Save(person);

				if (isEdit && !currentIsActive && Fmt.ConvertToBool(person.IsActive))
					_loginLogService.AddBasedOnController(person.ID.Value, Web.Request.ServerVariables["HTTP_USER_AGENT"]);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"].IsBlank() || Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? person.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Person";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { person.ID });
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
				Web.SetMessage(ex.Message, "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				TempData["PersonModel"] = person;
				return isEdit && person != null ? RedirectToAction("Edit", new { person.ID }) : RedirectToAction("Create");
			}
		}

		[ValidateInput(false)]
		public ActionResult SavePassword()
		{

			try
			{

				var person = UserSession.Person;

				Boolean success = true;

				var currentPassword = BaseSecurity.HashPassword(Request["CurrentPassword"]);

				if (person.Password == currentPassword)
				{
					var newPassword = Request["NewPassword"];
					if (newPassword.IsNotBlank() && newPassword == Request["NewPasswordAgain"])
					{
						newPassword = BaseSecurity.HashPassword(newPassword);
						person.Password = newPassword;
						_personService.Save(person);
						Web.SetMessage(i18n.Get("Security", "PasswordUpdated"));
					}
					else
					{
						Web.SetMessage(i18n.Get("Security", "PasswordsAreNotTheSame"), "error");
						success = false;
					}
				}
				else
				{
					Web.SetMessage(i18n.Get("Security", "CurrentPasswordWrong"), "error");
					success = false;
				}

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = Web.BaseUrl + "Admin/";
					return Json(new { success, message = Web.GetFlashMessageObject(), nextPage });
				}

				return Redirect("~/Admin");

			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				return RedirectToAction("ChangePassword");
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
			public List<Person> People;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Person Person;
			public Boolean ReadOnly;
		}

		public class HistoricViewModel
		{
			public IEnumerable<LoginLog> LoginLogs;
		}
	}
}
