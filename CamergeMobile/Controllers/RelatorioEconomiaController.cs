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
	public class RelatorioEconomiaController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IRelatorioEconomiaService _relatorioEconomiaService;
        private readonly ICalculoCativoService _calculoCativoService;
        private readonly ICalculoLivreService _calculoLivreService;
        private readonly IContratoService _contratoService;
        private readonly IAgenteService _agenteService;
        private readonly IContabilizacaoCceeService _contabilizacaoCceeService;
        private readonly IMedicaoCacheDiaTarifacaoService _medicaoCacheDiaTarifacaoService;
        private readonly IMedicaoAnualReportService _medicaoAnualReportService;
        private readonly IPDFService _PDFService;
        private readonly IGanhoSwapService _ganhoSwapService;
        private readonly IContratacaoEnergiaService _contratacaoEnergiaService;
        private readonly IRelatorioMensalCacheService _relatorioMensalCacheService;


        public RelatorioEconomiaController(IAtivoService ativoService,
			IRelatorioEconomiaService relatorioEconomiaService,
            ICalculoCativoService calculoCativoService,
            ICalculoLivreService calculoLivreService,
            IContratoService contratoService,
            IAgenteService agenteService,
            IContabilizacaoCceeService contabilizacaoCceeService,
            IMedicaoCacheDiaTarifacaoService medicaoCacheDiaTarifacaoService,
            IMedicaoAnualReportService medicaoAnualReportService,
            IPDFService PDFService,
            IGanhoSwapService ganhoSwapService,
            IContratacaoEnergiaService contratacaoEnergiaService,
            IRelatorioMensalCacheService relatorioMensalCacheService)
        {
            _ativoService = ativoService;
			_relatorioEconomiaService = relatorioEconomiaService;
            _calculoCativoService = calculoCativoService;
            _calculoLivreService = calculoLivreService;
            _contratoService = contratoService;
            _agenteService = agenteService;
            _contabilizacaoCceeService = contabilizacaoCceeService;
            _medicaoCacheDiaTarifacaoService = medicaoCacheDiaTarifacaoService;
            _medicaoAnualReportService = medicaoAnualReportService;
            _PDFService = PDFService;
            _ganhoSwapService = ganhoSwapService;
            _contratacaoEnergiaService = contratacaoEnergiaService;
            _relatorioMensalCacheService = relatorioMensalCacheService;
        }

		public ActionResult Index(int? Page)
		{
			var data = new ListViewModel();
			var paging = _relatorioEconomiaService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Economias = paging.Items;

			return AdminContent("RelatorioEconomia/RelatorioEconomiaList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.RelatorioEconomia = TempData["RelatorioEconomiaModel"] as RelatorioEconomia;
			if (data.RelatorioEconomia == null)
			{
				data.RelatorioEconomia = new RelatorioEconomia();
				data.RelatorioEconomia.UpdateFromRequest();
			}
			return AdminContent("RelatorioEconomia/RelatorioEconomiaEdit.aspx", data);
		}

		public ActionResult Edit(int id, bool readOnly = false)
		{
			var data = new FormViewModel();
			data.RelatorioEconomia = TempData["RelatorioEconomiaModel"] as RelatorioEconomia ?? _relatorioEconomiaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.RelatorioEconomia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("RelatorioEconomia/RelatorioEconomiaEdit.aspx", data);
		}

		public ActionResult Del(int id)
		{
			var RelatorioEconomia = _relatorioEconomiaService.FindByID(id);
			if (RelatorioEconomia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_relatorioEconomiaService.Delete(RelatorioEconomia);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/RelatorioEconomia" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(string ids)
		{
			_relatorioEconomiaService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/RelatorioEconomia" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult Duplicate(int id)
		{
			var RelatorioEconomia = _relatorioEconomiaService.FindByID(id);
			if (RelatorioEconomia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			RelatorioEconomia.ID = null;
			TempData["RelatorioEconomiaModel"] = RelatorioEconomia;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult View(int id)
		{
			return Edit(id, true);
		}

		public ActionResult Report()
		{
			var data = new ReportViewModel();
			var forceReload = Request["forceReload"].ToBoolean();
            var EconomiaCativoLivreList = new List<EconomiaCativoLivreDto>();

            if (Request["ativos"].IsNotBlank())
			{
                //data.Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));

                //DateTime parsedDate;
                //if ((data.Ativos.Any()) && (DateTime.TryParse(Request["date"], out parsedDate)))
                //{
                //	parsedDate = Dates.GetFirstDayOfMonth(parsedDate);

                //	if (data.Ativos.Any(ativo => UserSession.LoggedInUserCanSeeAtivo(ativo)))
                //	{
                //		data.RelatoriosEconomia = _relatorioEconomiaService.Get(parsedDate, data.Ativos);
                //	}
                //	else
                //	{
                //		data.Ativos = new List<Ativo>();
                //		Response.StatusCode = 403;
                //	}
                //}
                // Nova Implementacao - 17-09-20 - Hilario

                var Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));

                EconomiaCativoLivreList = _relatorioEconomiaService.GetDadosEconomiaCativoLivre(Ativos, Convert.ToDateTime("01/" + DateTime.Today.ToString("MM/yyyy")));

            }
			else
			{
				//if (UserSession.Agentes != null)
				//{
				//	var ativo = _ativoService.GetByAgentes(UserSession.Agentes, PerfilAgente.TiposRelacao.Cliente.ToString());
				//	if (ativo != null)
				//		data.Ativos = new List<Ativo>() { ativo };
				//}
			}

			if (forceReload)
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));

            if (Request["chkdata"].ToBoolean())
            {
                EconomiaCativoLivreList = EconomiaCativoLivreList.Where(w => w.Mes == Convert.ToDateTime(Request["date"])).ToList();
            }

            data.EconomiaCativoLivreList = EconomiaCativoLivreList;
             
            return AdminContent("RelatorioEconomia/RelatorioEconomiaReport.aspx", data);
		}

        public ActionResult ReportGeneratorView()
        {
            var forceReload = Request["forceReload"].ToBoolean();
            var data = new ReportGeneratorViewModel();
            if (Request["ativos"].IsNotBlank())
            {              
                var Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));
                data.mes = Convert.ToDateTime(Request["date"]);
                data.Ativos = Ativos;            

                var report = _relatorioEconomiaService.reportGenerator(data.Ativos.First(), data.mes);

                data.DashboardResumo = report.DashboardResumo;
                data.EconomiaCativoLivreList = report.EconomiaCativoLivreList;
                data.medicaoDiario = report.medicaoDiario;
                data.medicaoMensal = report.medicaoMensal;
                data.DadosContratacao = report.DadosContratacao;
                data.PerfilConsumo = report.PerfilConsumo;
                data.HistoricoDemanda = report.HistoricoDemanda;
                data.MemorialCalculoCativo = report.MemorialCalculoCativo;
                data.MemorialCalculoLivre = report.MemorialCalculoLivre;
                data.CustosML = report.CustosML;
                data.CustosMC = report.CustosMC;
                data.ContabilizacaoCCEE = report.ContabilizacaoCCEE;
                data.PrecoEmpateMeses = report.PrecoEmpateMeses;
            }            

            if (forceReload)
                return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));                  
            
            return AdminContent("RelatorioEconomia/RelatorioEconomiaReportGeneratorView.aspx", data);
		}

		public ActionResult ReportGeneratorViewGerador()
		{
			var forceReload = Request["forceReload"].ToBoolean();
			var data = new ReportGeneratorViewGeradorModel();
			if (Request["ativos"].IsNotBlank())
			{
				var Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));
				data.mes = Convert.ToDateTime(Request["date"]);
				data.Ativos = Ativos;

				var report = _relatorioEconomiaService.reportGenerator(data.Ativos.First(), data.mes);

				data.DashboardResumo = report.DashboardResumo;
				data.EconomiaCativoLivreList = report.EconomiaCativoLivreList;
				data.medicaoDiario = report.medicaoDiario;
				data.medicaoMensal = report.medicaoMensal;
				data.DadosContratacao = report.DadosContratacao;
				data.PerfilConsumo = report.PerfilConsumo;
				data.HistoricoDemanda = report.HistoricoDemanda;
				data.MemorialCalculoCativo = report.MemorialCalculoCativo;
				data.MemorialCalculoLivre = report.MemorialCalculoLivre;
				data.CustosML = report.CustosML;
				data.CustosMC = report.CustosMC;
				data.ContabilizacaoCCEE = report.ContabilizacaoCCEE;
				data.PrecoEmpateMeses = report.PrecoEmpateMeses;
			}

			if (forceReload)
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));

			return AdminContent("RelatorioEconomia/RelatorioEconomiaReportGeneratorViewGerador.aspx", data);
		}

		public ActionResult Historico(int? Page, bool isactive = true)
        {
            var data = new HistoricoListViewModel();
            var paging = new Page<RelatorioMensalCache>();


            if (UserSession.IsPerfilAgente || UserSession.IsPotencialAgente || UserSession.IsComercializadora)
                paging = _relatorioMensalCacheService.GetAllWithPaging(UserSession.Agentes.Select(i => i.ID.Value), Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);
            else
                paging = _relatorioMensalCacheService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);


            data.PageNum = paging.CurrentPage;
            data.PageCount = paging.TotalPages;
            data.TotalRows = paging.TotalItems;
            data.RelatorioMensalCache = paging.Items;

            return AdminContent("RelatorioEconomia/RelatorioEconomiaHistorico.aspx", data);
        }

        public ActionResult ApprovalList(int? Page, bool isactive = true)
        {
            var data = new ApprovalListViewModel();
            var paging = new Page<RelatorioMensalCache>();


            if (UserSession.IsPerfilAgente || UserSession.IsPotencialAgente || UserSession.IsComercializadora)
                paging = _relatorioMensalCacheService.GetAllWithPaging(UserSession.Agentes.Select(i => i.ID.Value), Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);
            else
                paging = _relatorioMensalCacheService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);


            data.PageNum = paging.CurrentPage;
            data.PageCount = paging.TotalPages;
            data.TotalRows = paging.TotalItems;
            data.RelatorioMensalCache = paging.Items;

            var statusList = new List<ReportGenerateStatusDto>();
            //pega status se pode 
            foreach (var relatorio in data.RelatorioMensalCache)            
                statusList.AddItems(_relatorioEconomiaService.checkReportGeneratorData(relatorio.Ativo, Convert.ToDateTime(relatorio.Mes)));

            data.ReportGenerateStatusList = statusList;
            return AdminContent("RelatorioEconomia/RelatorioEconomiaReportGeneratorApprovalList.aspx", data);
        }

        public ActionResult ReportGeneratorApproval()
        {
            var data = new ReportGeneratorViewModelTeste();
            var forceReload = Request["forceReload"].ToBoolean();
            
            if (Request["ativos"].IsNotBlank())
            {
                var Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));
                data.mes = Convert.ToDateTime(Request["date"]);
                data.Ativos = Ativos;
            }
            return AdminContent("RelatorioEconomia/RelatorioEconomiaReportGeneratorApproval.aspx", data);
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult generatePDFandEmail(string Html)
        {
            return Json(_PDFService.generatePDFandEmail(Html));
        }


        [HttpPost]
        [ValidateInput(false)]
        public JsonResult saveReport(int ativo_id, string mes)
        {
            var ativo = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(ativo_id, SqlQuery.SqlParameterType.IntList).Add(")")).First();
            //var DashboardResumo = new ReportDashboardResumoDto();
            //DashboardResumo = model;
            
            return Json(_relatorioEconomiaService.saveReport(ativo, Convert.ToDateTime(mes), UserSession.Person, true));
        }


        [HttpPost]
        [ValidateInput(false)]
        public JsonResult generateCacheReport(int ativo_id)
        {
            var ativo = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(ativo_id, SqlQuery.SqlParameterType.IntList).Add(")")).First();
            //var DashboardResumo = new ReportDashboardResumoDto();
            //DashboardResumo = model;
            return Json(_relatorioEconomiaService.generateReportCachebyAtivo(ativo));
        }

        
        public class HistoricoListViewModel
        {
            public List<RelatorioMensalCache> RelatorioMensalCache;
            public long TotalRows;
            public long PageCount;
            public long PageNum;
        }

        public class ApprovalListViewModel
        {
            public List<RelatorioMensalCache> RelatorioMensalCache;
            public long TotalRows;
            public long PageCount;
            public long PageNum;
            public List<ReportGenerateStatusDto> ReportGenerateStatusList;
        }

        public ActionResult Gadget()
		{
            var data = new GadgetViewModel();            
            var EconomiaCativoLivreList = new List<EconomiaCativoLivreDto>();
            var Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(1329, SqlQuery.SqlParameterType.IntList).Add(")"));
            //Ativos.AddItems(AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(1969, SqlQuery.SqlParameterType.IntList).Add(")")));
            //Ativos.AddItems(AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(65, SqlQuery.SqlParameterType.IntList).Add(")")));
            //Ativos.AddItems(AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(11, SqlQuery.SqlParameterType.IntList).Add(")")));


            var ativo_list = new List<Ativo>();

            if (UserSession.IsCliente)
            {
                ativo_list = UserSession.Agentes.First().PerfilAgenteList.First().AtivoList;
            }
            else
            {
                ativo_list = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(1329, SqlQuery.SqlParameterType.IntList).Add(")"));
            }

           



            EconomiaCativoLivreList = _relatorioEconomiaService.GetDadosEconomiaCativoLivre(Ativos, Convert.ToDateTime("01/" + DateTime.Today.ToString("MM/yyyy")));
            data.EconomiaCativoLivreList = EconomiaCativoLivreList.Where(w => w.Status != "Invalido").ToList();          


            var textoteste = "";

            var ativos_series = data.EconomiaCativoLivreList.GroupBy(g => g.Ativo.Nome).Select(s => s.Key);

            foreach (var ativo_series in ativos_series)
            {
                textoteste += ativo_series + " - ";
                foreach (var mes in data.EconomiaCativoLivreList.Where(w=>w.Ativo.Nome == ativo_series))
                {
                    textoteste += mes.Mes.ToString("MM/yyyy") + "-" + mes.EconomiaBruta.ToString("N2") + " ";
                }
            }

            return AdminContent("RelatorioEconomia/RelatorioEconomiaGadget.aspx", data);
		}

		[HttpGet]
		public ActionResult Import()
		{
			return AdminContent("RelatorioEconomia/RelatorioEconomiaImport.aspx");
		}

		[HttpGet]
		public ActionResult RelatorioEconomiaMensal()
		{
			return AdminContent("RelatorioEconomia/RelatorioEconomiaMensal.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _relatorioEconomiaService.ImportaRows(RawData, sobrescreverExistentes);
				if (processados == 0)
				{
					Web.SetMessage("Nenhum dado foi importado", "info");
				}
				else
				{
					Web.SetMessage("Dados importados com sucesso");
				}
			}
			catch (GenericImportException ex)
			{
				exception = ex;
				friendlyErrorMessage = string.Format("Falha na importação. {0}", ex.Message);
			}
			catch (Exception ex)
			{
				exception = ex;
				friendlyErrorMessage = string.Concat("Falha na importação. Verifique se os dados estão corretos e tente novamente.", " (", ex.Message, ")");
			}

			if (exception != null)
			{
				Web.SetMessage(friendlyErrorMessage, "error");

				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				return RedirectToAction("Import");
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/RelatorioEconomia";
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			//return RedirectToAction("Import");
			return Redirect(Web.BaseUrl + "Admin/RelatorioEconomia");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var RelatorioEconomia = new RelatorioEconomia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					RelatorioEconomia = _relatorioEconomiaService.FindByID(Request["ID"].ToInt(0));
					if (RelatorioEconomia == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				RelatorioEconomia.UpdateFromRequest();

				var relatorioEconomiaExistente = _relatorioEconomiaService.GetSingle(RelatorioEconomia.AtivoID.Value, RelatorioEconomia.Mes.Value);
				if ((relatorioEconomiaExistente != null) && (!isEdit))
					throw new Exception("Dado já existente para este ativo neste mês.");

				_relatorioEconomiaService.Save(RelatorioEconomia);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? RelatorioEconomia.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/RelatorioEconomia";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { RelatorioEconomia.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["RelatorioEconomiaModel"] = RelatorioEconomia;
				return isEdit && RelatorioEconomia != null ? RedirectToAction("Edit", new { RelatorioEconomia.ID }) : RedirectToAction("Create");
			}
		}

		private string HandleExceptionMessage(Exception ex)
		{
			string errorMessage;
			if (ex is RequiredFieldNullException)
			{
				var fieldName = ((RequiredFieldNullException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "NullException").Replace("XXX", friendlyFieldName);
			}
			else if (ex is FieldLengthException)
			{
				var fieldName = ((FieldLengthException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "LengthException").Replace("XXX", friendlyFieldName);
			}
			else
			{
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

        public IEnumerable<RelatorioEconomiaMinifiedDto> GetRelatorioEconomiaMinifiedData(List<Ativo> ativos = null)
        { 
            var dataRetorno = new List<RelatorioEconomiaMinifiedDto>();
            var calculosCativo = _calculoCativoService.LoadCalculos(ativos, Convert.ToDateTime("01/07/2020"), null, null, null, true, true, false, false, true, null, 13);
            var calculosLivre = _calculoLivreService.LoadCalculos(ativos, Convert.ToDateTime("01/07/2020"), null, null, null, null, true, true, true, true, true, false, 13);
           
            //agentes_temp.Add(_agenteService.GetByAtivo(Convert.ToInt16(ativos)));
            //var contrato_tmp = _contratoService.GetReport(Convert.ToDateTime("01/07/2020"), false, ativos.First().Agente);

            var EconomiaCativoLivreList = new List<EconomiaCativoLivreDto>();

            foreach (var calculoCativo in calculosCativo)
            {
                foreach(var calculo in calculoCativo.Calculos)
                {
                    var EconomiaCativoLivre = new EconomiaCativoLivreDto();
                    EconomiaCativoLivre.Ativo = calculo.Ativo;
                    EconomiaCativoLivre.Mes = calculo.Mes;
                    EconomiaCativoLivre.CativoTotalBruto = calculo.Total;
                    EconomiaCativoLivre.CativoTotalLiquido = calculo.TotalLiquido;

                    var DtoCalculosLivre = calculosLivre.Where(w => w.Ativo == calculo.Ativo).First();
                    var calculoLivre = DtoCalculosLivre.Calculos.Where(w => w.Mes == calculo.Mes).First();

                    EconomiaCativoLivre.LivreTotalDistribuidoraBruto = calculoLivre.Total;
                    EconomiaCativoLivre.LivreTotalDistribuidoraLiquido = calculoLivre.TotalLiquido;
                    var agentes_temp = new List<Agente>();
                    
                    agentes_temp.Add(_agenteService.GetByAtivo(Convert.ToInt16(calculo.Ativo.ID)));
                    var contrato_tmp = _contratoService.GetReport(calculo.Mes, false, agentes_temp);
                    foreach (var contrato in contrato_tmp)
                    {
                       // EconomiaCativoLivre.LivreTotalEnergiaBruto += contrato.NotaFiscal.Montante;
                       // EconomiaCativoLivre.LivreTotalEnergiaLiquido += contrato.NotaFiscal.Montante;
                    }

                    var Contabilizacoes = _contabilizacaoCceeService.GetReport(calculo.Ativo.PerfilAgenteID.Value).ToList();
                    //EconomiaCativoLivre.LivreCCEE = Convert.ToDouble(Contabilizacoes.Where(w => w.DataContabilizacao.AddMonths(1) == calculo.Mes).Select(s => ((s.Inadimplencia - s.ValorLiquidado) + s.EnergiaReserva)).Sum() + Contabilizacoes.Where(w => w.DataContabilizacao.AddMonths(1) == calculo.Mes).Select(s => s.ContribuicaoAssociativa).Sum());
                    EconomiaCativoLivreList.Add(EconomiaCativoLivre);
                }

            }
            return dataRetorno;
        }


        public class FormViewModel
		{
			public RelatorioEconomia RelatorioEconomia;
			public bool ReadOnly;
		}

		public class ListViewModel
		{
			public List<RelatorioEconomia> Economias;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class GadgetViewModel
		{
			//public List<Ativo> Ativos = new List<Ativo>();
            public List<EconomiaCativoLivreDto> EconomiaCativoLivreList;
            /*
            public IEnumerable<RelatorioEconomiaMinifiedDto> RelatoriosEconomia { get; set; }
			public double EconomiaTotal { get; set; }
			public string GetValores(string ativo, IEnumerable<RelatorioEconomiaMinifiedDto> dados)
			{
				var valores = new List<double>();

				var finalDate = dados.Max(i => i.Mes);
				var initialDate = finalDate.AddMonths(-11);

				if (initialDate < dados.Min(i => i.Mes)) initialDate = dados.Min(i => i.Mes);


				if (dados.Where(m => m.Ativo == ativo).Min(i => i.Mes) != initialDate)
				{
					while (initialDate < dados.Where(m => m.Ativo == ativo).Min(i => i.Mes))
					{
						valores.Add(0);
						initialDate = initialDate.AddMonths(1);
					}
				}

				foreach (var dadosMes in dados.Where(m => m.Ativo == ativo))
					valores.Add(dadosMes.Economia);


				return valores.Select(m =>
					m == 0.0
					? "null" // null will remove the point from the chart                    
					: m.ToString("N2").Remove(".").Replace(",", ".")
				).Join(",");
			}
			public string GetText()
			{
				double economiaMensal = 0;
				double economiaAnual = 0;

				var mesRef = RelatoriosEconomia.Max(i => i.Mes);

				if (RelatoriosEconomia.Any())
				{
					var lastEconomia = RelatoriosEconomia.Last();

					economiaMensal = lastEconomia.Economia;
					mesRef = lastEconomia.Mes;

					economiaAnual = RelatoriosEconomia.Sum(i => i.Economia);
				}

				/* Alterei Aqui - Hilario 31-07-19
				if ((economiaMensal > 0) && (economiaAnual > 0))
					return string.Format("Em {0:MMMM/yy}, a economia em relação ao Mercado Cativo foi de {1:C2}. Com a gestão da CAMERGE, a {4} já economizou {2:C2} nos últimos 12 meses e {3:C2} desde a migração para o mercado livre.",
						mesRef, economiaMensal, economiaAnual, EconomiaTotal, UserSession.Person.Name);
				else if ((economiaMensal < 0) && (economiaAnual > 0))
					return string.Format("Em {0:MMMM/yy}, não houve economia em relação ao Mercado Cativo. No entanto, até o momento, a {3} já economizou {1:C2} nos últimos 12 meses no Mercado Livre e {2:C2} desde a migração.",
						mesRef, economiaAnual, EconomiaTotal, UserSession.Person.Name);
				else if ((economiaMensal > 0) && (economiaAnual < 0))
					return string.Format("Em {0:MMMM/yy}, a economia em relação ao Mercado Cativo foi de {1:C2} e {2:C2} desde a migração.",
						mesRef, economiaMensal, EconomiaTotal);
				else if ((economiaMensal < 0) && (economiaAnual < 0))
					return string.Format("Em {0:MMMM/yy}, não houve economia em relação ao Mercado Cativo. No entanto economizou e {1:C2} desde a migração",
						mesRef, EconomiaTotal);
				else
					return null;
				*/

                /*
				var contaAgentes = this.Ativos.GroupBy(g => g.PerfilAgente.Sigla).Count();
				if (contaAgentes > 1)
				{
					if ((economiaMensal > 0) && (EconomiaTotal > 0))
						return string.Format("Em {0:MMMM/yy}, a economia em relação ao Mercado Cativo foi de {1:C2}. Com a gestão da CAMERGE, a economia foi de {3:C2} desde a migração para o Mercado Livre.",
								mesRef, economiaMensal, economiaAnual, EconomiaTotal);
					else if ((economiaMensal < 0) && (EconomiaTotal > 0))
						return string.Format("Em {0:MMMM/yy}, não houve economia em relação ao Mercado Cativo. No entanto, até o momento, a economia no Mercado Livre foi de {2:C2}.",
								mesRef, economiaAnual, EconomiaTotal);
					else if ((economiaMensal > 0) && (EconomiaTotal < 0))
						return string.Format("Em {0:MMMM/yy}, a economia em relação ao Mercado Cativo foi de {1:C2} e {2:C2} no Mercado Livre.",
								mesRef, economiaMensal, EconomiaTotal);
					else if ((economiaMensal < 0) && (EconomiaTotal < 0))
						return string.Format("Em {0:MMMM/yy}, não houve economia em relação ao Mercado Cativo. No entanto economizou e {1:C2} no Mercado Livre.",
								mesRef, EconomiaTotal);
					else
						return null;
				}

				if ((economiaMensal > 0) && (EconomiaTotal > 0))
					return string.Format("Em {0:MMMM/yy}, a economia em relação ao Mercado Cativo foi de {1:C2}. Com a gestão da CAMERGE, a {4} já economizou {3:C2} desde a migração para o Mercado Livre.",
							mesRef, economiaMensal, economiaAnual, EconomiaTotal, this.Ativos[0].PerfilAgente.Sigla);
				else if ((economiaMensal < 0) && (EconomiaTotal > 0))
					return string.Format("Em {0:MMMM/yy}, não houve economia em relação ao Mercado Cativo. No entanto, até o momento, a economia no Mercado Livre foi de {2:C2}.",
							mesRef, economiaAnual, EconomiaTotal, this.Ativos[0].PerfilAgente.Sigla);
				else if ((economiaMensal > 0) && (EconomiaTotal < 0))
					return string.Format("Em {0:MMMM/yy}, a economia em relação ao Mercado Cativo foi de {1:C2} e {2:C2} no Mercado Livre.",
							mesRef, economiaMensal, EconomiaTotal);
				else if ((economiaMensal < 0) && (EconomiaTotal < 0))
					return string.Format("Em {0:MMMM/yy}, não houve economia em relação ao Mercado Cativo. No entanto economizou e {1:C2} no Mercado Livre.",
							mesRef, EconomiaTotal);
				else
					return null;
			}
            */
		}

		public class ReportViewModel
		{
			public List<Ativo> Ativos = new List<Ativo>();
			public IEnumerable<RelatorioEconomia> RelatoriosEconomia;
            public List<EconomiaCativoLivreDto> EconomiaCativoLivreList;
        }

        public class ReportGeneratorViewModelTeste
        {
            public List<Ativo> Ativos = new List<Ativo>();
            public DateTime mes;
        }

            public class ReportGeneratorViewModel
        {
            public List<Ativo> Ativos = new List<Ativo>();
            public DateTime mes;
            public ReportDashboardResumoDto DashboardResumo;
            public List<EconomiaCativoLivreDto> EconomiaCativoLivreList;
            public ReportGeneratorDadosContratacao DadosContratacao;
            public List<MedicaoCacheDiaTarifacao> medicaoDiario;
            public List<MedicaoAnualMedicaoMesDto> medicaoMensal;
            public ReportGeneratorPerfilConsumo PerfilConsumo;
            public ReportGeneratorHistoricoDemanda HistoricoDemanda;
            public MemorialCalculoCativoDto MemorialCalculoCativo;
            public MemorialCalculoLivreDto MemorialCalculoLivre;
            public ReportGeneratorCustosML CustosML;
            public ReportGeneratorCustosMC CustosMC;
            public ReportGeneratorContabilizacaoCCEE ContabilizacaoCCEE;
            public List<ReportGeneratorPrecoEmpateMes> PrecoEmpateMeses;
        }

		public class ReportGeneratorViewGeradorModel
		{
			public List<Ativo> Ativos = new List<Ativo>();
			public DateTime mes;
			public ReportDashboardResumoDto DashboardResumo;
			public List<EconomiaCativoLivreDto> EconomiaCativoLivreList;
			public ReportGeneratorDadosContratacao DadosContratacao;
			public List<MedicaoCacheDiaTarifacao> medicaoDiario;
			public List<MedicaoAnualMedicaoMesDto> medicaoMensal;
			public ReportGeneratorPerfilConsumo PerfilConsumo;
			public ReportGeneratorHistoricoDemanda HistoricoDemanda;
			public MemorialCalculoCativoDto MemorialCalculoCativo;
			public MemorialCalculoLivreDto MemorialCalculoLivre;
			public ReportGeneratorCustosML CustosML;
			public ReportGeneratorCustosMC CustosMC;
			public ReportGeneratorContabilizacaoCCEE ContabilizacaoCCEE;
			public List<ReportGeneratorPrecoEmpateMes> PrecoEmpateMeses;
		}

	}
}
