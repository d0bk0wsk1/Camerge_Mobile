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
    public class ContratoPerfisController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IContratoPerfisReportService _contratoPerfisReportService;
        private readonly IDevecService _devecService;
        private readonly IContratoService _contratoService;

        public ContratoPerfisController(IAgenteService agenteService,
			IContratoPerfisReportService contratoPerfisReportService,
            IDevecService devecService,
            IContratoService contratoService)
		{
			_agenteService = agenteService;
			_contratoPerfisReportService = contratoPerfisReportService;
            _devecService = devecService;
            _contratoService = contratoService;
		}

		public ActionResult Index()
		{
            var data = new ListViewModel();            
            if (Request["date"] != null)
            {
                var ag = _agenteService.GetByConcatnatedIds(Request["agentes"]);                            
                data = getDataPerfis(Request["categoria"], DateTime.Parse(Request["date"]), ag, Request.QueryString["hstc"].ToBoolean());
            }                
            return AdminContent("ContratoPerfis/ContratoPerfisReport.aspx", data);
		}

        public ListViewModel getDataPerfis(string Categoria, DateTime date, List<Agente> agentes, bool hstc)
        {
            var data = new ListViewModel();           

            DateTime parsedDate = date;
            //if (DateTime.TryParse(date, out parsedDate))
            //{
            //var agentes = _agenteService.GetByConcatnatedIds(Request["agentes"]);
            var mes = Dates.GetFirstDayOfMonth(parsedDate);
            var isHistoric = hstc;
            data.Categoria = Categoria;

            if (data.Categoria == null)
            {
                if (agentes.Any(i => i.PerfilAgenteList.Any(x => x.IsConsumidor)))
                    data.Categoria = Medicao.TiposLeitura.Consumo.ToString();
                else
                    data.Categoria = Medicao.TiposLeitura.Geracao.ToString();
            }

            if (!agentes.Any() && UserSession.IsCliente)
                agentes = UserSession.Agentes.ToList();

            if (data.Categoria == Medicao.TiposLeitura.Consumo.ToString())
            {
                data.PerfisConsumo = _contratoPerfisReportService.GetPerfilConsumidor(mes, agentes, isHistoric);
                if (data.PerfisConsumo.Any())
                    data.PerfilConsumoConsolidado = _contratoPerfisReportService.GetPerfilConsumidorTotal(data.PerfisConsumo);
            }
            else if ((data.Categoria == Medicao.TiposLeitura.Geracao.ToString()) || (data.Categoria == "Comercializacao"))
            {
                data.PerfisGeracao = _contratoPerfisReportService.GetPerfilGerador(mes, agentes, isHistoric);
                if (data.PerfisGeracao.Any())
                    data.PerfilGeracaoConsolidado = _contratoPerfisReportService.GetPerfilGeradorTotal(data.PerfisGeracao);
            }
                
            if (data.PerfisConsumo !=null)
            {                
                foreach (var perfil in data.PerfisConsumo)
                {
                    var Devecs = new List<Devec>();
                    var devec = new Devec();
                    foreach (var ativo in perfil.PerfilAgente.AtivoList)
                    {
                        devec = _devecService.GetMostRecent(Convert.ToInt16(ativo.ID), parsedDate);
                        if (devec != null && devec.Mes == parsedDate)
                            Devecs.Add(devec);
                    }
                    perfil.Devecs = Devecs;
                    var listContratos = new List<ContratoReportDto>();
                    var agentes_tmp = new List<Agente>();
                    agentes_tmp.Add(perfil.PerfilAgente.Agente);
                    listContratos = _contratoService.GetReport(parsedDate, false, agentes_tmp);
                    perfil.contratosList = listContratos;
                    var notFaturado = perfil.contratosList.Where(w => w.ContratoVigenciaBalanco.IsFaturamento==false).Count();
                    var countChamadas = perfil.ChamadasNegociacao.Where(w=>w.Status=="EmAberto").Count();
                }
            }           
            return data;
        }


        public JsonResult GetEmailDevecPreview(string agente, string date, string email)
        {
            var ag = _agenteService.GetByConcatnatedIds(agente);
            var getBodyWithImages = _contratoPerfisReportService.GetBodyEmailDevec(DateTime.Parse(date), ag);
            return Json(getBodyWithImages, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SendEmailDevec(string agente, string date, string email)
        {               
            var data = new ListViewModel();           
            var ag = _agenteService.GetByConcatnatedIds(agente);
            data = getDataPerfis("Consumo", DateTime.Parse(date), ag, false);
            if (data != null)
            {
                _contratoPerfisReportService.SendEmailDevec(DateTime.Parse(date), ag, email);                
                //atualiza o banco da devec
                foreach (var perfil in data.PerfisConsumo)
                    foreach (var ativo in perfil.PerfilAgente.AtivoList)
                        if (perfil.Devecs.Where(s => s.AtivoID == ativo.ID && s.Mes == Convert.ToDateTime(date)).Count() > 0)
                        { 
                            foreach (var devec in perfil.Devecs.Where(s => s.AtivoID == ativo.ID && s.Mes == Convert.ToDateTime(date)))
                            {
                                devec.EmailEnviado = true;
                                _devecService.Update(devec);                                
                            }                                                     
                        }
                        else
                        {
                            //cria uma nova devec
                            var novaDevec = new Devec();
                            novaDevec.Ativo = ativo;
                            novaDevec.AtivoID = ativo.ID;
                            novaDevec.DateAdded = DateTime.Now;
                            novaDevec.EmailEnviado = true;
                            novaDevec.Mes = Convert.ToDateTime(date);
                            novaDevec.Preco = 0;
                            perfil.Devecs.Add(novaDevec);
                            _devecService.Insert(novaDevec);
                        }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {                
                return Json(null);
            }
               
        }

        public JsonResult RemoveEmailDevec(string agente, string date)
        {
            var data = new ListViewModel();
            var ag = _agenteService.GetByConcatnatedIds(agente);
            data = getDataPerfis("Consumo", DateTime.Parse(date), ag, false);
            if (data != null)
            {                
                //atualiza o banco da devec
                foreach (var perfil in data.PerfisConsumo)
                    foreach (var ativo in perfil.PerfilAgente.AtivoList)
                        if (perfil.Devecs.Where(s => s.AtivoID == ativo.ID && s.Mes == Convert.ToDateTime(date)).Count() > 0)
                        {
                            foreach (var devec in perfil.Devecs.Where(s => s.AtivoID == ativo.ID && s.Mes == Convert.ToDateTime(date)))
                            {
                                devec.EmailEnviado = false;
                                _devecService.Update(devec);
                            }
                        }                        
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(null);
        }

        public JsonResult CheckDevecStatus(string agente, string date)
        {
            var data = new ListViewModel();
            var ag = _agenteService.GetByConcatnatedIds(agente);
            data = getDataPerfis("Consumo", DateTime.Parse(date), ag, false);
            if (data != null)
            {
                //atualiza o banco da devec
                foreach (var perfil in data.PerfisConsumo)
                {
                    foreach (var ativo in perfil.PerfilAgente.AtivoList)
                    {
                        if (perfil.Devecs.Where(s => s.AtivoID == ativo.ID && s.Mes == Convert.ToDateTime(date)).Count() > 0)
                        {
                            foreach (var devec in perfil.Devecs.Where(s => s.AtivoID == ativo.ID && s.Mes == Convert.ToDateTime(date)))
                            {
                                if (devec.EmailEnviado == true)
                                    return Json(true, JsonRequestBehavior.AllowGet);
                                //else
                                 //   return Json(null);
                            }
                        }
                    }
                }
                return Json(null);
            }
            else
                return Json(null);
        }






        public class ListViewModel
		{
			public ContratoPerfilConsumoReportDto PerfilConsumoConsolidado { get; set; }
			public ContratoPerfilGeracaoReportDto PerfilGeracaoConsolidado { get; set; }
			public List<ContratoPerfilConsumoReportDto> PerfisConsumo { get; set; }
			public List<ContratoPerfilGeracaoReportDto> PerfisGeracao { get; set; }
			public string Categoria { get; set; }            
                    
        }
	}
}
