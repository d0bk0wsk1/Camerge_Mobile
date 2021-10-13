using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CamergeMobile.Controllers
{
	public class ControllerBase : Controller
	{
		protected override IAsyncResult BeginExecute(RequestContext requestContext, AsyncCallback callback, object state)
		{
			var accept = (requestContext.HttpContext.Request.Headers["Accept"] + "").ToLower();
			if (requestContext.HttpContext.Request.HttpMethod == "GET" && accept.DoesntContain("application/json"))
				Web.AdminHistory.Add(Web.FullUrl);
			return base.BeginExecute(requestContext, callback, state);
		}

		protected override void OnResultExecuted(ResultExecutedContext filterContext)
		{
			var accept = (filterContext.HttpContext.Request.Headers["Accept"] + "").ToLower();
			var isAjax = accept.Contains("application/json") || Fmt.ConvertToBool(filterContext.HttpContext.Request["ajax"]);

			var statusCode = filterContext.HttpContext.Response.StatusCode;

			if (statusCode != 200 && statusCode != 302 && !Web.HasMessage && !isAjax)
			{
				Web.SetMessage(i18n.Gaia.Get("HttpErrors", filterContext.HttpContext.Response.StatusCode.ToString()), "error");
			}

			//if (filterContext.RouteData.Values["controller"].ToString() != "Security" && Util.GetSettingBoolean("adminMaintenance", false)) {
			//	Web.Redirect(Web.BaseUrl + "Admin/Security/SignOut?maintenance=true");
			//	return;
			//}

			base.OnResultExecuted(filterContext);
		}

		protected override void OnException(ExceptionContext filterContext)
		{
			Error.Gather(filterContext.Exception);
			var errorTitle = Error.ErrorMessage;
			var errorBody = Error.ToHtml();

			var isDeveloperAccess = false;
			try
			{
				if (Security.IsLoggedIn && UserSession.IsDeveloper)
				{
					isDeveloperAccess = true;
				}
			}
			catch { }

			if (isDeveloperAccess || Util.IsDevEnvironment)
			{
				Web.Write(errorBody);
			}
			else
			{
				Error.NotifyAsync(errorTitle, errorBody);
				if (!Web.Response.IsRequestBeingRedirected)
				{
					Web.Redirect("~/Admin/Error?status=500");
				}
			}
			Web.End();
		}

		protected String GetViewAsString(Controller thisController, string viewName, object model)
		{
			thisController.ViewData.Model = model;
			using (var sw = new StringWriter())
			{
				var controllerContext = thisController.ControllerContext ?? new ControllerContext(Web.Request.RequestContext, thisController);
				var viewResult = ViewEngines.Engines.FindPartialView(controllerContext, viewName);
				if (viewResult.View == null)
				{
					throw new Exception("View [" + viewName + "] not found");
				}
				var viewContext = new ViewContext(controllerContext, viewResult.View, thisController.ViewData, thisController.TempData, sw);
				viewResult.View.Render(viewContext, sw);
				return sw.ToString();
			}
		}

		protected ActionResult AdminContent(String view)
		{
			return AdminContent(view, null);
		}

		protected ActionResult AdminContent(String view, Object data)
		{
			view = "~/Areas/Admin/Views/" + view;
			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var html = GetViewAsString(this, view, data);
				var contentHtml = "<title>" + html.ExtractTextBetween("<title>", "</title>").Trim() + "</title>";
				contentHtml += html.ExtractTextBetween("<!-- Content -->", "<!-- /Content -->");
				contentHtml += html.ExtractTextBetween("<!-- Message -->", "<!-- /Message -->");
				return Content(contentHtml);
			}
			return View(view, data);
		}

		protected ViewResult AdminView(String view)
		{
			return AdminView(view, null);
		}

		protected ViewResult AdminView(String view, Object data)
		{
			view = "~/Areas/Admin/Views/" + view;
			return View(view, data);
		}
	}
}
