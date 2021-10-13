using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CamergeMobile.Controllers
{
	public class SecurityController : ControllerBase
	{
		private readonly ILoginLogService _loginLogService;

		public SecurityController(ILoginLogService loginLogService)
		{
			_loginLogService = loginLogService;
		}

		public RedirectToRouteResult Index()
		{
			return RedirectToAction(controllerName: "Dashboard", actionName: "Index");
		}

		public ActionResult Login()
		{
			//if (Util.GetSettingBoolean("adminMaintenance", false, true)) {
			//	return RedirectToAction("Maintenance");
			//}

			if (Security.IsLoggedIn)
			{
				return RedirectToAction("Index");
			}
			return View();
		}

		public ActionResult Maintenance()
		{
			if (!Util.GetSettingBoolean("adminMaintenance", false, true))
			{
				return RedirectToAction("Index");
			}
			return View();
		}

		[HttpPost]
		public ActionResult Login(UserLogin login)
		{
			var email = login.UserName.Trim();

			var person = Person.LoadByEmail(email);
			if (person == null)
			{
				/*
				var agente = Agente.LoadBySigla(email);
				if (agente != null)
					person = agente.Person;
				*/
			}

			if (person == null)
			{
				Web.SetMessage("Usuário não localizado.", "error");
				return View(login);
			}

			Action authorise = () => FormsAuthentication.SetAuthCookie(Crypto.Encrypt(person.ID.ToString()), Fmt.ConvertToBool(login.RememberMe));

			var log = LogAccess(person);
			var authResponse = BaseSecurity.Auth(person, login.Password);

			if (authResponse.Success)
			{
				log.Status = BaseSecurity.LoginStatuses.Success.ToString();
				_loginLogService.Save(log);

				if (login.ReturnUrl.IsBlank())
				{
					authorise();
					return RedirectToAction("Index");
				}
				else
				{
					if (Url.IsReallyLocalUrl(login.ReturnUrl))
					{
						authorise();
						return Redirect(login.ReturnUrl);
					}
					else
					{
						Web.SetMessage(i18n.Get("Security", "InvalidRedirect"), "error");
						return RedirectToAction(controllerName: "Security", actionName: "Login");
					}
				}
			}
			else
			{
				if (authResponse.ErrorCode == BaseSecurity.ErrorCodes.TooManyTries.ToString())
				{
					var mailer = new Mailer();
                    mailer.From = new System.Net.Mail.MailAddress("medicao@camerge.com.br", "CAMERGE - Medição");
                    mailer.Subject = "[CAMERGE] Acesso Bloqueado: Too many tries.";
					mailer.Body = string.Format("Usuário: {0} / E-Mail: {1}", person.Name, person.Email);
					mailer.AddTo("medicao@camerge.com.br");
					mailer.Send(true);
				}

				Web.SetMessage(i18n.Get("Security", authResponse.ErrorCode), "error");
				return RedirectToAction(controllerName: "Security", actionName: "Login");
			}
		}

		private LoginLog LogAccess(Person person)
		{
			var log = new LoginLog();
			log.AccessDate = DateTime.Now;
			log.Ip = Web.UserIpAddress;
			log.HttpUserAgent = Web.Request.ServerVariables["HTTP_USER_AGENT"];
			log.PersonID = person != null ? person.ID : null;
			log.WindowSize = Request["windowSize"];
			log.IsMobileDevice = Web.IsMobileDevice;
			_loginLogService.Save(log);

			return log;
		}

		[HttpGet]
		public RedirectToRouteResult SignOut(bool? maintenance)
		{
			FormsAuthentication.SignOut();

			if (maintenance == true)
			{
				return RedirectToAction("Maintenance");
			}

			return RedirectToAction("Index");
		}
	}
}
