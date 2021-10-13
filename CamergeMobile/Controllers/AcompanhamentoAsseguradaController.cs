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
	public class AcompanhamentoAsseguradaController : ControllerBase
	{
		private readonly IAcompanhamentoAsseguradaReportService _acompanhamentoAsseguradaReportService;
		private readonly IAtivoService _ativoService;
		private readonly IReportCacheLogService _reportCacheLogService;
		private readonly IReportCacheItemLogService _reportCacheItemLogService;

		public AcompanhamentoAsseguradaController(IAcompanhamentoAsseguradaReportService acompanhamentoAsseguradaReportService,
			IAtivoService ativoService,
			IReportCacheLogService reportCacheLogService,
			IReportCacheItemLogService reportCacheItemLogService)
		{
			_acompanhamentoAsseguradaReportService = acompanhamentoAsseguradaReportService;
			_ativoService = ativoService;
			_reportCacheLogService = reportCacheLogService;
			_reportCacheItemLogService = reportCacheItemLogService;
		}

		//
		// GET: /Admin/AcompanhamentoAssegurada/
		public ActionResult Index()
		{
			var data = new ListViewModel();
			var forceReload = Request["forceReload"].ToBoolean();

			if (Request["ativo"].IsNotBlank())
			{
				data.Ativo = _ativoService.FindByID(Request["ativo"].ToInt(0));

				if (data.Ativo != null)
				{
					if (UserSession.LoggedInUserCanSeeAtivo(data.Ativo))
					{
						var start = DateTime.Now;
						var monthYearRange = Request["monthYearRange"].ConvertToDate(null);

						var acompanhamentos = _acompanhamentoAsseguradaReportService.LoadAcompanhamentos(data.Ativo, monthYearRange, forceReload);

						data.AcompanhamentoAssegurada = new AcompanhamentoAsseguradaDto
						{
							Acompanhamentos = acompanhamentos,
							Resumos = _acompanhamentoAsseguradaReportService.LoadResumos(acompanhamentos)
						};

						if (forceReload)
						{
							_reportCacheItemLogService.Insert(
								_reportCacheLogService.GetByInserting(data.Ativo.ID.Value, start),
								"Acomp. Assegurada", "ForceReload");
						}
					}
					else
					{
						data.Ativo = null;
						Response.StatusCode = 403;
					}
				}
			}
			else
			{
				if (UserSession.Agentes != null)
					data.Ativo = _ativoService.GetByAgentes(UserSession.Agentes, PerfilAgente.TiposRelacao.Cliente.ToString());
			}

			if (forceReload)
			{
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
			}

			return AdminContent("AcompanhamentoAssegurada/AcompanhamentoAsseguradaReport.aspx", data);
		}

		public class ListViewModel
		{
			public Ativo Ativo;
			public AcompanhamentoAsseguradaDto AcompanhamentoAssegurada;
		}
	}
}
