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
	public class ExecutivoGeradorController : ControllerBase
	{
		private readonly IAtivoService _ativoService;

		public ExecutivoGeradorController(IAtivoService ativoService)
		{
			_ativoService = ativoService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel();

			DateTime parsedDate;

			if (((Request["ativo"].IsNotBlank())) && (DateTime.TryParse(Request["date"], out parsedDate)))
			{
				data.Ativo = _ativoService.FindByID(Request["ativo"].ToInt(0));
			}
			else
			{
				if (UserSession.Agentes != null)
					data.Ativo = _ativoService.GetByAgentes(UserSession.Agentes);
			}

			return AdminContent("ExecutivoGerador/ExecutivoGeradorReport.aspx", data);
		}

		public class ListViewModel
		{
			public Ativo Ativo;
		}
	}
}
