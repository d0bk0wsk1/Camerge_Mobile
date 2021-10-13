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
	public class NavigationController : ControllerBase
	{
		[ChildActionOnly]
		public ViewResult UserNav()
		{
			return AdminView("Navigation/UserNav.ascx");
		}

		[ChildActionOnly]
		public ActionResult MainNav()
		{
			return AdminView("Navigation/MainNav.ascx");
		}

		[ChildActionOnly]
		public ActionResult RelatorioEnergiaShortcuts()
		{
			return AdminView("Navigation/RelatorioEnergiaShortcuts.ascx");
		}

		[ChildActionOnly]
		public ActionResult RelatorioFinanceiroShortcuts()
		{
			return AdminView("Navigation/RelatorioFinanceiroShortcuts.ascx");
		}

		[ChildActionOnly]
		public ActionResult RelatorioMigracaoAclShortcuts()
		{
			return AdminView("Navigation/RelatorioMigracaoAclShortcuts.ascx");
		}

		[ChildActionOnly]
		public ActionResult ComponenteFinanceiroShortcuts()
		{
			return AdminView("Navigation/ComponenteFinanceiroShortcuts.ascx");
		}

		[ChildActionOnly]
		public ActionResult ComercializacaoShortcuts()
		{
			return AdminView("Navigation/ComercializacaoShortcuts.ascx");
		}
	}
}
