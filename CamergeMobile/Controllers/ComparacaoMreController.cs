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
	public class ComparacaoMreController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IComparacaoMreService _comparacaoMreService;

		public ComparacaoMreController(IAtivoService ativoService,
			IComparacaoMreService comparacaoMreService)
		{
			_ativoService = ativoService;
			_comparacaoMreService = comparacaoMreService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel();

			if (Request["ativo"].IsNotBlank())
			{
				var ativo = _ativoService.FindByID(Request["ativo"].ToInt(0));
				if (ativo != null)
				{
					DateTime mesInicio;
					DateTime mesFim;

					if ((DateTime.TryParse(Request["dtini"], out mesInicio)) && (DateTime.TryParse(Request["dtfim"], out mesFim)))
					{
						data.Ativo = ativo;
						data.MesInicio = Dates.GetFirstDayOfMonth(mesInicio);
						data.MesFim = Dates.GetLastDayOfMonth(mesFim);

						data.ComparacaoMeses = _comparacaoMreService.GetComparacaoMeses(data.Ativo, data.MesInicio, data.MesFim);
					}
				}
			}
			else
			{
				if (UserSession.Agentes != null)
					data.Ativo = _ativoService.GetByAgentes(UserSession.Agentes);
			}

			return AdminContent("ComparacaoMre/ComparacaoMreReport.aspx", data);
		}

		public class ListViewModel
		{
			public Ativo Ativo { get; set; }
			public DateTime MesInicio { get; set; }
			public DateTime MesFim { get; set; }
			public List<ComparacaoMreDto> ComparacaoMeses { get; set; }
		}
	}
}
