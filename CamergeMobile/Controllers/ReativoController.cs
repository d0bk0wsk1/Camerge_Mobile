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
	public class ReativoController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IDemandaContratadaService _demandaContratadaService;
		private readonly IReativoReportService _reativoReportService;

		public ReativoController(IAtivoService ativoService,
			IDemandaContratadaService demandaContratadaService,
			IReativoReportService reativoReportService)
		{
			_ativoService = ativoService;
			_demandaContratadaService = demandaContratadaService;
			_reativoReportService = reativoReportService;
		}

		//
		// GET: /Admin/Reativo/
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
						data.DemandaContratadaEmVigencia = _demandaContratadaService.GetDemandaContratadaEmVigencia(data.Ativo);
						data.MedicoesAno = _reativoReportService.LoadMedicoesAno(data.Ativo, data.DemandaContratadaEmVigencia, forceReload);
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
					data.Ativo = _ativoService.GetByAgentes(UserSession.Agentes);
			}

			if (forceReload)
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
			return AdminContent("Reativo/ReativoReport.aspx", data);
		}

        public ActionResult Mensal()
        {
            var data = new ReportMensalModel();
            var forceReload = Request["forceReload"].ToBoolean();

            if (Request["ativo"].IsNotBlank())
            {
                data.Ativo = _ativoService.FindByID(Request["ativo"].ToInt(0));
                data.Mes = Convert.ToDateTime(Request["date"]);

                if (data.Ativo != null)
                {
                    data.ReativoMensalList = _reativoReportService.LoadMonthData(data.Ativo, data.Mes);
                }  
            }            

            if (forceReload)
                return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
            return AdminContent("Reativo/ReativoMonthReport.aspx", data);
        }

        //public At

        public class ReportMensalModel
        {
            public Ativo Ativo;
            public DateTime Mes;
            public double ReativoTotal
            {
                get
                {
                    if (ReativoMensalList.Count() > 0)                    
                        return ReativoMensalList.Sum(s => s.ReativoExcedente) / 4 / 1000;                    
                    else
                        return 0;
                }
            }
            public List<ReativoDto> ReativoMensalList;
        }
                    

        public class ListViewModel
		{
			public Ativo Ativo;
			public DemandaContratada DemandaContratadaEmVigencia;
			public List<ReativoMedicaoMesDto> MedicoesAno;
			public string GetValores(List<ReativoMedicaoMesDto> medicoes)
			{
				var valores = new List<Double>();

				var ultimoMes = medicoes.Any() ? medicoes.Max(m => m.Mes.Month) : 0;

				for (var i = 1; i <= ultimoMes; i++)
				{
					valores.Add(medicoes.Where(m => m.Mes.Month == i).Select(m => (m.Ponta + m.ForaPonta + m.Capacitivo)).FirstOrDefault());
				}
				return valores.Select(m =>
					/*
					m == 0.0
					? "null" // null will remove the point from the chart
					: m.ToString("N3").Remove(".").Replace(",", ".")
					*/
					m.ToString("N3").Remove(".").Replace(",", ".")
				).Join(",");
			}
		}

	}
}
