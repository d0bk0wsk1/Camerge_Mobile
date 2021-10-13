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
    public class PrecoHorarioController : ControllerBase
	{
		private readonly ILoggerService _loggerService;
		private readonly IPrecoHorarioService _precoHorarioService;
        private readonly IContratoVigenciaService _contratoVigenciaService;
        
              


		public PrecoHorarioController(ILoggerService loggerService,
			IPrecoHorarioService precoHorarioService,
            IContratoVigenciaService contratoVigenciaService )
		{
			_loggerService = loggerService;
			_precoHorarioService = precoHorarioService;
            _contratoVigenciaService = contratoVigenciaService;    
		}

		//
		// GET: /Admin/Preco/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _precoHorarioService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Precos = paging.Items;
            
            var dataApuracao = DateTime.Now;
            if (Request["date"] != null)
                dataApuracao = Convert.ToDateTime(Request["date"]);
            else
                dataApuracao = _precoHorarioService.getLastDate();


            data.dataApuracao = dataApuracao;

            var precoHorarioListofTheDay = _precoHorarioService.GetbyDay(dataApuracao);   
            var grupoSubmercados = precoHorarioListofTheDay.Select(s => s.Submercado).GroupBy(g => g.ID);


            var submercadosListView = new List<Submercados> ();
            foreach(var submercado in grupoSubmercados)
            {
                var submercadoView = new Submercados();
                submercadoView.data = dataApuracao;
                submercadoView.descricao = submercado.First().Nome;
                submercadoView.sigla = _precoHorarioService.getSigla(submercadoView.descricao);
                submercadoView.color = _precoHorarioService.getColor(submercadoView.descricao); 

                submercadoView.valorMaximo = Convert.ToDouble(precoHorarioListofTheDay.Where(w => w.SubmercadoID == submercado.Key).Max(m => m.ValorPld));
                submercadoView.horaMaximo = Convert.ToDateTime(precoHorarioListofTheDay.Where(w => w.SubmercadoID == submercado.Key && w.ValorPld == submercadoView.valorMaximo).First().DataHora);

                submercadoView.valorMinimo = Convert.ToDouble(precoHorarioListofTheDay.Where(w => w.SubmercadoID == submercado.Key).Min(m => m.ValorPld));
                submercadoView.horaMinimo = Convert.ToDateTime(precoHorarioListofTheDay.Where(w => w.SubmercadoID == submercado.Key && w.ValorPld == submercadoView.valorMinimo).First().DataHora);

                submercadoView.mediaDiaria = Convert.ToDouble(precoHorarioListofTheDay.Where(w => w.SubmercadoID == submercado.Key).Average(m => m.ValorPld));
                var valoresList = new List<PrecoHorario>();
                valoresList.AddItems(precoHorarioListofTheDay.Where(w => w.SubmercadoID == submercado.First().ID));

                submercadoView.valores = valoresList;
                submercadosListView.Add(submercadoView);

            }
            data.submercados = submercadosListView;

            return AdminContent("PrecoHorario/PrecoHorarioList.aspx", data);
		}


        public ActionResult ImportPrecoHorario()
        {
            return AdminContent("PrecoHorario/PrecoHorarioImport.aspx");
        }

        public ActionResult ProcessImportPrecoHorario()
        {
            var data = new ImportPrecosHorarioModel();
            var AttIds = Request["AttachmentID"];
            var sobre = Request["SobrescreverExistentes"];


            //Gambiarra pra funcionar por causa dos services q dao problema na inicialização

            var mesesToUpdate = _precoHorarioService.ImportaPrecosHorarios(Request["AttachmentID"].ToInt(), Request["SobrescreverExistentes"].ToBoolean());
            //data.Resultado = processados;

            //return AdminContent("FaturaDistribuidora/FaturaDistribuidoraImportJSON.aspx", data);

            //Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
            var textoDisplay = "Importação Concluída";

            //atualiza contratos com spread+pld, teve que chamar daqui mesmo por estar dando problemas em instanciar os contratosVigenciaServices
            _contratoVigenciaService.updatePLDonSpread(mesesToUpdate);

            Web.SetMessage(textoDisplay);           

            var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

            if (Fmt.ConvertToBool(Request["ajax"]))
            {                
                var nextPage = Web.BaseUrl + "Admin/PrecoHorario/Index";                
                return Json(new { success = true, message = "Valores importadors", nextPage });                
            }

            var previousUrl = Web.AdminHistory.Previous;
            if (previousUrl != null)
                return Redirect(previousUrl);
            return RedirectToAction("Index");
        }

        public ActionResult GraficoGadget()
        {
            var data = new GraficoGadgetViewModel();
            return AdminContent("PrecoHorario/PrecoHorarioGraficoGadget.aspx", data);
        }

        public class GraficoGadgetViewModel
        {            
            public List<Submercados> submercados;
        }

        public class ListViewModel
        {
            public List<PrecoHorario> Precos;
            public long TotalRows;
            public long PageCount;
            public long PageNum;
            public List<Submercados> submercados;
            public DateTime dataApuracao;
        }

        public class ImportPrecosHorarioModel
        {
            public int registros;
        }

        public class Submercados
        {
            public string sigla;
            public string descricao;
            public string color;
            public DateTime data;
            public Double mediaDiaria;
            public Double valorMinimo;
            public DateTime horaMinimo;
            public Double valorMaximo;
            public DateTime horaMaximo;
            public List<PrecoHorario> valores;
        }      

    }
}
