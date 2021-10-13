using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CamergeMobile.Controllers
{

	public class ErrorController : ControllerBase
	{
		//
		// GET: /Admin/Error/
		public ActionResult Index(Int32 status) {
			if (!Security.IsLoggedIn) {
				return Redirect("~/Error/?status=" + status + "&n=1");
			}
			var data = new ViewModel();
			data.ErrorStatus = status;
			data.Message = Get("HttpErrors", status.ToString()) ?? Get("HttpErrors", "Generic");
			return AdminContent("Error/Generic.aspx", data);
		}

		public class ViewModel {
			public Int32 ErrorStatus;
			public String Message;
		}

	}
}
