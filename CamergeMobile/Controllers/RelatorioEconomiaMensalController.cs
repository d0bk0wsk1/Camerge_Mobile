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
    public class RelatorioEconomiaMensalController : ControllerBase
	{
        private readonly IAtivoService _ativoService;
        private readonly IMedicaoResumoReportService _medicaoResumoReportService;
        private readonly IPerfilAgenteService _perfilAgenteService;
        private readonly ILoggerService _loggerService;
        private readonly IMedicaoMesRealService _medicaoMesRealService;
        private readonly IReportCacheLogService _reportCacheLogService;
        private readonly IReportCacheItemLogService _reportCacheItemLogService;
        private readonly IContratoService _contratoService;
        private readonly IAgenteService _agenteService;
        private readonly IMedicaoMensalReportService _medicaoMensalReportService;
        private readonly IMedicaoAnualReportService _medicaoAnualReportService;
        private readonly IContabilizacaoCceeService _contabilizacaoCceeService;
        private readonly ICalculoCativoService _calculoCativoService;
        private readonly ICalculoLivreService _calculoLivreService;
        private readonly ITarifaVigenciaValorService _tarifaVigenciaValorService;
        private readonly IDevecService _devecService;
        private readonly IIcmsVigenciaService _icmsVigenciaService;
        private readonly IBandeiraCorService _bandeiraCorService;
        private readonly IGeradorDieselService _geradorDieselService;
        private readonly IRelatorioEconomiaService _relatorioEconomiaService;
        private readonly IDemandaReportService _demandaReportService;

        
        public RelatorioEconomiaMensalController(IAtivoService ativoService,
            IMedicaoResumoReportService medicaoResumoReportService,
            IPerfilAgenteService perfilAgenteService,
            ILoggerService loggerService,
            IMedicaoMesRealService medicaoMesRealService,
            IReportCacheItemLogService reportCacheItemLogService,
            IContratoService contratoService, 
            IAgenteService agenteService,
            IMedicaoMensalReportService medicaoMensalReportService,
            IMedicaoAnualReportService medicaoAnualReportService,
            IContabilizacaoCceeService contabilizacaoCceeService,
            ICalculoCativoService calculoCativoService,
            ICalculoLivreService calculoLivreService,
            ITarifaVigenciaValorService tarifaVigenciaValorService,
            IDevecService devecService,
            IIcmsVigenciaService icmsVigenciaService,
            IBandeiraCorService bandeiraCorService,
            IGeradorDieselService geradorDieselService, 
            IRelatorioEconomiaService relatorioEconomiaService,
            IDemandaReportService demandaReportService)
        {
            _ativoService = ativoService;
            _medicaoResumoReportService = medicaoResumoReportService;
            _perfilAgenteService = perfilAgenteService;
            _loggerService = loggerService;
            _medicaoMesRealService = medicaoMesRealService;
            _reportCacheItemLogService = reportCacheItemLogService;
            _contratoService = contratoService;
            _agenteService = agenteService;
            _medicaoMensalReportService = medicaoMensalReportService;
            _medicaoAnualReportService = medicaoAnualReportService;
            _contabilizacaoCceeService = contabilizacaoCceeService;
            _calculoCativoService = calculoCativoService;
            _calculoLivreService = calculoLivreService;
            _tarifaVigenciaValorService = tarifaVigenciaValorService;
            _devecService = devecService;
            _icmsVigenciaService = icmsVigenciaService;
            _bandeiraCorService = bandeiraCorService;
            _geradorDieselService = geradorDieselService;
            _relatorioEconomiaService = relatorioEconomiaService;
            _demandaReportService = demandaReportService;
        }

		//
		// GET: /Admin/MedicaoResumo/
		public ActionResult Index()
		{
			var forceReload = Request["forceReload"].ToBoolean();

			var data = GetReport(Request["ativos"], Request["meses"], forceReload);            

            if (forceReload)
			return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
            return AdminContent("RelatorioEconomiaMensal/RelatorioEconomiaMensal.aspx",data);

        }
        [HttpGet]
        public ActionResult RelatorioEconomiaMensal()
        {
            return AdminContent("RelatorioEconomiaMensal/RelatorioEconomiaMensal.aspx");
        }

        private CalculoLivreDto GetCalculoLivreDtoBySimulacao(MedicaoConsolidadoConsumoMesDto medicaoConsolidadoConsumoMes, DateTime mes,
         int ativoID, int? agenteConectadoID, string modalidade, double? descontoCcee,
         bool includeIcms, bool includePisConfins, bool creditIcms, bool creditImposto)
        {
            var ativo = _ativoService.FindByID(ativoID);
            if (ativo != null)
            {
                var demandaContratada = new DemandaContratada()
                {
                    MesVigencia = mes,
                    Ponta = medicaoConsolidadoConsumoMes.DemandaPonta,
                    ForaPonta = Math.Max((medicaoConsolidadoConsumoMes.DemandaCapacitivo ?? 0), (medicaoConsolidadoConsumoMes.DemandaForaPonta ?? 0)),
                    Tipo = modalidade
                };

                var tarifaVigenciaValor = _tarifaVigenciaValorService.Get(agenteConectadoID ?? ativo.AgenteConectadoID.Value, mes, ativo.ClasseID.Value, modalidade);

                return _calculoLivreService.GetCalculoBySimplified(ativo, mes, medicaoConsolidadoConsumoMes, demandaContratada, tarifaVigenciaValor, agenteConectadoID ?? ativo.AgenteConectadoID.Value, null, descontoCcee, true, includeIcms, includePisConfins, creditIcms, creditImposto);
            }
            return new CalculoLivreDto();
        }


        public ListViewModel GetReport(string ativo, string date, bool forceReload)
        {
            var data = new ListViewModel();
            if (ativo != null)
            {
                data.Date = Convert.ToDateTime(date);
                data.Ativos = _ativoService.GetByConcatnatedIds(ativo);
                if (data.Ativos.Count > 0)
                {
                    data.TipoLeitura = ((data.Ativos.Any(i => i.IsGerador)) ? Medicao.TiposLeitura.Geracao.ToString() : Medicao.TiposLeitura.Consumo.ToString());
                    data.medicoesRealMes = _medicaoMesRealService.GetConsolidadoConsumoMesesDto(data.Ativos[0], data.Date.AddMonths(-36), data.Date, false).Where(w => w.Mes.Year >= 2018).ToList();
                    data.GeradorDiesel = _geradorDieselService.GetMostRecent(Convert.ToInt16(data.Ativos[0].ID), data.Date);
                    data.ResumoDiario = _medicaoMensalReportService.LoadMedicoesMesConsumo(data.Ativos, data.Date, forceReload);
                    data.MedicoesAno = _medicaoAnualReportService.LoadMedicoesAno(data.Ativos, data.TipoLeitura, null, forceReload);

                    var agentes_temp = new List<Agente>();
                    agentes_temp.Add(_agenteService.GetByAtivo(Convert.ToInt16(ativo)));
                    foreach (var medicao in data.medicoesRealMes)
                    {
                        data.Contratos = _contratoService.GetReport(medicao.Mes, false, agentes_temp);
                        medicao.MWhContrato = (data.Contratos.Sum(i => (i.MontanteApuracao == null) ? 0 : (agentes_temp.First().ID==(i.PerfilAgenteVendedor.AgenteID) ? (i.MontanteApuracao.MontanteApuracaoMWh ?? 0) * -1 : (i.MontanteApuracao.MontanteApuracaoMWh ?? 0))));
                        medicao.MWmContrato = (data.Contratos.Sum(i => (i.MontanteApuracao == null) ? 0 : (agentes_temp.First().ID == (i.PerfilAgenteVendedor.AgenteID) ? (i.MontanteApuracao.MontanteApuracao ?? 0) * -1 : (i.MontanteApuracao.MontanteApuracao ?? 0))));
                        medicao.PreçoAtualizadoContrato = data.Contratos.Sum(i => i.PrecoAtualizado);
                    }                 
                     data.Contabilizacoes = _contabilizacaoCceeService.GetReport(data.Ativos.First().PerfilAgenteID.Value).ToList();
                    data.calculosCativoLivre = _calculoCativoService.LoadCalculos(data.Ativos, data.Date, null, null, null, true, true, false, false, true, null, 13);
                    
                    // ajuste de valor de impostos no gerador diesel
                    // ajuste da contribuição associativa
                    foreach (var contrib in data.Contabilizacoes.OrderBy(o => o.DataContabilizacao))
                    {
                        //contrib.ContribuicaoAssociativa = data.Contabilizacoes.Where(w => w.DataContabilizacao == contrib.DataContabilizacao.AddMonths(1)).Select(s => s.ContribuicaoAssociativa).Sum();
                        if (contrib.DataContabilizacao == data.Ativos.First().DataInicioVigencia) contrib.ContribuicaoAssociativa = 0;
                    //
                    }
                    data.calculosLivre = _calculoLivreService.LoadCalculos(data.Ativos, data.Date, null, null, null, null, true, true, true, true, true, false, 13);
                    data.Economias = _relatorioEconomiaService.Get(data.Date.AddMonths(-12), data.Date, data.Ativos);
                    if (data.calculosLivre.Any())
                    {
                        var ativosMes = new List<AtivosMesViewModel>();
                        var precosEmpateMes = new List<precoEmpateMesViewModel>();
                        var custoLivre = new List<CustoLivre>();
                        foreach (var dto in data.calculosLivre)
                        {
                            var ativoMes = new AtivosMesViewModel() { Ativo = dto.Ativo };
                            var precoEmpateMes = new precoEmpateMesViewModel() { Ativo = dto.Ativo };
                            var corBandeira = _bandeiraCorService.GetCor(Request["bandeira"].ToInt(null));
                           // var custoLivreMes = new CustoLivre();
                            if (dto.Calculos.Any())
                            {
                                var calculoAtivoMes = new List<AtivoMesViewModel>();
                                var calculoprecoEmpateMes = new List<precoEmpateViewModel>();
                                //var calculoCativoMes = new CalculoCativo();
                                foreach (var calculo in dto.Calculos)
                                {
                                    var viewModel = GetAtivoMesViewModel(calculo, true);
                                    var custoLivreMes = new CustoLivre();
                                    //if ((viewModel.Observacao == null) && (calculo.CreditoImposto != null))
                                    //    CreditImposto(viewModel, calculo.CreditoImposto);
                                    var dtoCativo = calculo.Observacao != null ? null : _calculoCativoService.GetCalculo(calculo.Ativo, calculo.Mes, null, null, calculo.Vigencia, null, true, true, false, false, true, corBandeira);

                                    calculoprecoEmpateMes.Add(GetPreçoEmpateViewModel(calculo, dtoCativo, true));
                                    var contrato_tmp = _contratoService.GetReport(calculo.Mes, false, agentes_temp);
                                    var montante_contrato = contrato_tmp.Sum(i => (i.MontanteApuracao == null) ? 0 : ((agentes_temp.First().ID==i.PerfilAgenteVendedor.AgenteID.ToInt()) ? (i.MontanteApuracao.MontanteApuracao ?? 0) * -1 : (i.MontanteApuracao.MontanteApuracao ?? 0)));
                                    var preco_atualizado = (contrato_tmp.Sum(i => (i.PrecoAtualizado ?? 0) * ((i.MontanteApuracao == null) ? 0 : ((agentes_temp.First().ID == i.PerfilAgenteVendedor.AgenteID.ToInt()) ? (i.MontanteApuracao.MontanteApuracao ?? 0) * -1 : (i.MontanteApuracao.MontanteApuracao ?? 0)))))/montante_contrato;
                                    
                                    calculoAtivoMes.Add(viewModel);
                                    //ajustes para melhorar na view
                                    custoLivreMes.Mes = calculo.Mes;
                                    custoLivreMes.energiaContratada = Convert.ToDouble(data.medicoesRealMes.Where(w => w.Mes == calculo.Mes).Select(s => s.MWhContrato * preco_atualizado).Sum());
                                    //custoLivreMes.icmsST = viewModel.IcmsSt;
                                    CalcIcmsSt_on_custoLivreMes(custoLivreMes, calculo.Ativo, calculo.Mes, Convert.ToDouble(data.medicoesRealMes.Where(w => w.Mes == calculo.Mes).Select(s => s.MwhTotal).Sum()), true);
                                    custoLivreMes.DemandaPonta = calculo.DemandaPontaFaturada + calculo.DemandaPontaFaturadaDescEI + calculo.DemandaPontaMedida + calculo.DemandaPontaMedidaDescEI;
                                    custoLivreMes.DemandaForaPonta = calculo.DemandaForaPontaFaturada + calculo.DemandaForaPontaFaturadaDescEI + calculo.DemandaForaPontaMedida + calculo.DemandaForaPontaMedidaDescEI;
                                    custoLivreMes.TUSDPonta = viewModel.ConsumoPonta;
                                    custoLivreMes.TUSDForaPonta = viewModel.ConsumoForaPonta;
                                    custoLivreMes.CCEE = Convert.ToDouble(data.Contabilizacoes.Where(w => w.DataContabilizacao.AddMonths(1) == calculo.Mes).Select(s => ((s.Inadimplencia - s.ValorLiquidado) + s.EnergiaReserva)).Sum() + data.Contabilizacoes.Where(w => w.DataContabilizacao.AddMonths(1) == calculo.Mes).Select(s => s.ContribuicaoAssociativa).Sum());
                                    custoLivreMes.subvencaoTarifaria = calculo.SubvencaoTarifaria == null ? 0 : calculo.SubvencaoTarifaria.TotalSubvencaoTarifaria;
                                    custoLivreMes.ajusteDesconto = Convert.ToDouble(data.Economias.Where(w => w.Mes == calculo.Mes).Select(s => s.AjusteDesconto).Sum());
                                    custoLivreMes.MWhTotal = data.medicoesRealMes.Where(w => w.Mes == calculo.Mes).Select(s => s.MwhForaPonta + s.MwhPonta).Sum();

                                    if (calculo.DescontoCcee == 0 && calculo.Mes.AddMonths(-2) <= data.Ativos.First().DataInicioVigencia && calculo.Mes >= data.Ativos.First().DataInicioVigencia)
                                       {
                                        var calculotmp = _calculoLivreService.LoadCalculos(data.Ativos, calculo.Mes, null, 50, null, null, true, true, true, true, true, false, 1);
                                        
                                        if (calculo.Modalidade == "Verde")
                                        {
                                            custoLivreMes.DemandaForaPonta = (calculotmp.First().Calculos.First().DemandaForaPontaFaturada + calculotmp.First().Calculos.First().DemandaForaPontaFaturadaDescEI + calculotmp.First().Calculos.First().DemandaForaPontaMedida + calculotmp.First().Calculos.First().DemandaForaPontaMedidaDescEI);
                                            custoLivreMes.TUSDPonta = calculotmp.First().Calculos.First().ConsumoPontaAtivo;                                           

                                        }
                                        else
                                        {
                                            custoLivreMes.DemandaPonta = (calculotmp.First().Calculos.First().DemandaPontaFaturada + calculotmp.First().Calculos.First().DemandaPontaFaturadaDescEI + calculotmp.First().Calculos.First().DemandaPontaMedida + calculotmp.First().Calculos.First().DemandaPontaMedidaDescEI) * 0.5;
                                            custoLivreMes.DemandaForaPonta = (calculotmp.First().Calculos.First().DemandaForaPontaFaturada + calculotmp.First().Calculos.First().DemandaForaPontaFaturadaDescEI + calculotmp.First().Calculos.First().DemandaForaPontaMedida + calculotmp.First().Calculos.First().DemandaForaPontaMedidaDescEI) * 0.5;

                                        }
                                    }
                                    custoLivre.Add(custoLivreMes);
                                    ativoMes.AtivoMes = calculoAtivoMes;    
                                    precoEmpateMes.PrecoEmpateMes = calculoprecoEmpateMes;
                                }
                                ativosMes.Add(ativoMes);
                                precosEmpateMes.Add(precoEmpateMes);
                            }
                            data.AtivosMes = ativosMes;
                            data.precoEmpateMes = precosEmpateMes;
                            data.custoLivre = custoLivre;
                            data.GeradorDiesel = _geradorDieselService.GetMostRecent(Convert.ToInt16(data.Ativos[0].ID), data.Date);
             
                           

                            data.MedicoesDemandaAnoPonta = _demandaReportService.LoadMedicoesAno(data.Ativos, AgenteConectado.Tarifacoes.Ponta.ToString(), Medicao.TiposLeitura.Consumo, null, forceReload);
                            data.MedicoesDemandaAnoForaPonta = _demandaReportService.LoadMedicoesAno(data.Ativos, AgenteConectado.Tarifacoes.ForaPonta.ToString(), Medicao.TiposLeitura.Consumo, null, forceReload);

                            //mudancas para otimizar. remover os objetos obsoletos depois   

                        }
                    }
                }
            }
            return data;
        }        

        public JsonResult GetMesesRelatorioEconomiaMensalByAtivo(string ids)
        {
            var options = new Dictionary<string, string>();
            //options.Add("hstc", "Histórico");
            //options.Add("mmgg", "Segundo");
            var singleId = false;
            var id = Request["ids"];
            //var id = ids; 
            if (id.IsNotBlank())
            {
                singleId = (!id.Contains(','));

                int ativoID;
                if ((singleId) && (int.TryParse(id, out ativoID)))
                {
                    var medicoes = _medicaoMesRealService.GetByAtivo(ativoID);
                    var ativos = _ativoService.GetByConcatnatedIds(Convert.ToString(ativoID));
                    if (medicoes.Any())
                        foreach (var medicao in medicoes.Where(w => w.Mes > Convert.ToDateTime(ativos.First().DataInicioVigencia).AddMonths(-1)))
                            options.Add(medicao.Mes.ToString("dd/MM/yyyy"), System.Globalization.DateTimeFormatInfo.CurrentInfo.GetMonthName(medicao.Mes.Month)+"/"+ medicao.Mes.ToString("yyyy"));
                }
            }


            var ret = Json(options.Select(i => new { i.Key, i.Value }), JsonRequestBehavior.AllowGet);
            return ret;
        }

        private MedicaoResumoMedicaoMesDto GetConsolidado(IEnumerable<PerfilAgente> perfisAgente,
            List<MedicaoResumoMedicaoMesDto> resumos)
        {
            if (((resumos != null) && (resumos.Any())) && (perfisAgente.Any()))
            {
                // Consumo
                var consumoMWm = resumos.Sum(i => i.ConsumoMWm);
                var consumoMWmAnoAnterior = resumos.Where(i => i.ConsumoMWmAnoAnterior != null).Sum(i => i.ConsumoMWmAnoAnterior);
                var consumoMWmMesAnterior = resumos.Where(i => i.ConsumoMWmMesAnterior != null).Sum(i => i.ConsumoMWmMesAnterior);
                var ativosConsumoNotNull = resumos.Where(i => i.HorasConsumo != null);
                var horasConsumo = ativosConsumoNotNull.Sum(i => i.HorasConsumo) / ativosConsumoNotNull.Count();

                var consumoMWmAnoAnteriorWhenNull = resumos.Where(i => i.ConsumoMWmAnoAnterior == null).Sum(i => i.ConsumoMWm);
                if (consumoMWmAnoAnteriorWhenNull > 0)
                    consumoMWmAnoAnterior += consumoMWmAnoAnteriorWhenNull;

                var consumoMWmMesAnteriorWhenNull = resumos.Where(i => i.ConsumoMWmMesAnterior == null).Sum(i => i.ConsumoMWm);
                if (consumoMWmMesAnteriorWhenNull > 0)
                    consumoMWmMesAnterior += consumoMWmMesAnteriorWhenNull;

                // Geração
                var geracaoMWm = resumos.Sum(i => i.GeracaoMWm);
                var geracaoMWmAnoAnterior = (resumos.Any(i => i.GeracaoMWmAnoAnterior != null)) ? resumos.Sum(i => i.GeracaoMWmAnoAnterior) : null;
                var geracaoMWmMesAnterior = (resumos.Any(i => i.GeracaoMWmMesAnterior != null)) ? resumos.Sum(i => i.GeracaoMWmMesAnterior) : null;
                var ativosGeracaoNotNull = resumos.Where(i => i.HorasGeracao != null);
                var horasGeracao = ativosGeracaoNotNull.Sum(i => i.HorasGeracao) / ativosGeracaoNotNull.Count();

                return new MedicaoResumoMedicaoMesDto()
                {
                    ConsumoMWh = resumos.Sum(i => i.ConsumoMWh),
                    ConsumoMWm = consumoMWm,
                    ConsumoMWmAnoAnterior = consumoMWmAnoAnterior,
                    ConsumoMWmMesAnterior = consumoMWmMesAnterior,
                    HorasConsumo = horasConsumo,
                    PrevisaoConsumoMWh = resumos.Sum(i => i.PrevisaoConsumoMWh),
                    PrevisaoConsumoMWm = resumos.Sum(i => i.PrevisaoConsumoMWm),
                    TempoConsumo = Fmt.FormatTotalHoursInDetailedString(horasConsumo ?? 0),
                    // VariacaoAnoAnteriorConsumo = (consumoMWmAnoAnterior.HasValue) ? (double?)(((consumoMWm / consumoMWmAnoAnterior.Value) - 1) * 100) : null,
                    // VariacaoMesAnteriorConsumo = (consumoMWmMesAnterior.HasValue) ? (double?)(((consumoMWm / consumoMWmMesAnterior.Value) - 1) * 100) : null,
                    GeracaoMWh = resumos.Sum(i => i.GeracaoMWh),
                    GeracaoMWm = geracaoMWm,
                    GeracaoMWmAnoAnterior = geracaoMWmAnoAnterior,
                    GeracaoMWmMesAnterior = geracaoMWmMesAnterior,
                    HorasGeracao = horasGeracao,
                    PrevisaoGeracaoMWh = resumos.Sum(i => i.PrevisaoGeracaoMWh),
                    PrevisaoGeracaoMWm = resumos.Sum(i => i.PrevisaoGeracaoMWm),
                    TempoGeracao = Fmt.FormatTotalHoursInDetailedString(horasGeracao ?? 0),
                    // VariacaoAnoAnteriorGeracao = ((geracaoMWmAnoAnterior != null) && (geracaoMWmAnoAnterior > 0)) ? (double?)(((geracaoMWm / geracaoMWmAnoAnterior.Value) - 1) * 100) : 0,
                    // VariacaoMesAnteriorGeracao = ((geracaoMWmMesAnterior != null) && (geracaoMWmMesAnterior > 0)) ? (double?)(((geracaoMWm / geracaoMWmMesAnterior.Value) - 1) * 100) : 0,
                    UltimoDadoRecebido = resumos.Max(i => i.UltimoDadoRecebido)
                };
            }
            return null;
        }

        private void CalcIcmsSt_on_custoLivreMes(CustoLivre custoLivreMes, Ativo Ativo, DateTime Mes, double mwhTotal, bool includeIcms = false)
        {
            
            if (includeIcms)
            {
                var devec = _devecService.GetMostRecent(Ativo.ID.Value, Mes);
                if (devec != null)
                {
                    var icms = _icmsVigenciaService.GetByVigencia(Ativo, Mes);
                    if (icms != null)
                    {
                        //viewModel.Icms = icms.AliquotaEnergia;
                        custoLivreMes.icmsST = (mwhTotal * devec.Preco / (1 - icms.AliquotaEnergia) * icms.AliquotaEnergia);
                    }
                }
            }
        }

         

        public class AtivosMesViewModel
        {
            public Ativo Ativo { get; set; }
            public List<AtivoMesViewModel> AtivoMes { get; set; }     
        }

        public class precoEmpateMesViewModel
        {
            public Ativo Ativo { get; set; }            
            public List<precoEmpateViewModel> PrecoEmpateMes { get; set; }
        }

        public class AtivoMesViewModel
        {
            public Ativo Ativo { get; set; }
            public DateTime Mes { get; set; }
            public string ModalidadeTarifaria { get; set; }
            public double ConsumoPonta { get; set; }
            public double ConsumoForaPonta { get; set; }
            public double DemandaPonta { get; set; }
            public double DemandaForaPonta { get; set; }
            public double CustoGeradorTotal { get; set; }
            public double DescontoCcee { get; set; }
            public double Icms { get; set; }
            public double IcmsSt { get; set; }
            public double Total
            {
                get
                {
                    return ConsumoPonta + ConsumoForaPonta + DemandaPonta + DemandaForaPonta + IcmsSt;
                }
            }
            public bool HasDemandaPontaUltrapassagem { get; set; }
            public bool HasDemandaForaPontaUltrapassagem { get; set; }
            public string Observacao { get; set; }
        }

        private precoEmpateViewModel GetPreçoEmpateViewModel(CalculoLivreDto dtoLivre, CalculoCativoDto dtoCativo, bool includeIcms = false)
        {
            if (dtoLivre.Observacao == null)
            {
                if ((dtoCativo != null) && (dtoCativo.Observacao == null))
                {
                    var dtoLivre0Percent = _calculoLivreService.ApplyDescontoCcee(dtoLivre, 0, includeIcms, false);
                    var dtoLivre50Percent = _calculoLivreService.ApplyDescontoCcee(dtoLivre, 0.5, includeIcms, false);
                    var dtoLivre100Percent = _calculoLivreService.ApplyDescontoCcee(dtoLivre, 1, includeIcms, false);

                    var empate = 0; // (dtoLivre.MWhTotal == 0) ? 0 : ((dtoCativo.Total - dtoLivre.Total) / dtoLivre.MWhTotal);

                    var viewModel = new precoEmpateViewModel()
                    {
                        Ativo = dtoLivre.Ativo,
                        BandeiraCor = ((dtoCativo.Bandeira == null) ? null : dtoCativo.Bandeira.BandeiraCor.Nome),
                        Mes = dtoLivre.Mes,
                        MWhTotal = dtoLivre.MWhTotal,
                        Empate = empate,
                        Valor0Perc = ((dtoLivre0Percent == null) || (dtoLivre.MWhTotal == 0))
                            ? 0 : ((dtoCativo.Total - dtoLivre0Percent.Total) / dtoLivre.MWhTotal),
                        Valor50Perc = ((dtoLivre50Percent == null) || (dtoLivre.MWhTotal == 0))
                            ? 0 : ((dtoCativo.Total - dtoLivre50Percent.Total) / dtoLivre.MWhTotal),
                        Valor100Perc = ((dtoLivre100Percent == null) || (dtoLivre.MWhTotal == 0))
                            ? 0 : ((dtoCativo.Total - dtoLivre100Percent.Total) / dtoLivre.MWhTotal),
                    };

                    if (includeIcms)
                    {
                        var icms = _icmsVigenciaService.GetByVigencia(dtoLivre.Ativo, dtoLivre.Mes);
                        if (icms != null)
                        {
                            viewModel.Empate = viewModel.Empate * (1 - icms.AliquotaEnergia);
                            viewModel.Valor0Perc = viewModel.Valor0Perc * (1 - icms.AliquotaEnergia);
                            viewModel.Valor50Perc = viewModel.Valor50Perc * (1 - icms.AliquotaEnergia);
                            viewModel.Valor100Perc = viewModel.Valor100Perc * (1 - icms.AliquotaEnergia);
                        }
                    }

                    //var bandeira = _bandeiraService.Get(dtoLivre.Mes);
                    //if (bandeira != null)
                    //{
                    //	viewModel.BandeiraCor = bandeira.bandeiraCor.Nome;

                    //	return viewModel;
                    //}

                    // dtoLivre.Observacao = "Bandeira não localizada.";

                    return viewModel;
                }
                return new precoEmpateViewModel() { Mes = dtoCativo.Mes, Observacao = dtoCativo.Observacao };
            }
            return new precoEmpateViewModel() { Mes = dtoLivre.Mes, Observacao = dtoLivre.Observacao };
        }


        public class precoEmpateViewModel
        {
            public Ativo Ativo { get; set; }
            public DateTime Mes { get; set; }
            public string BandeiraCor { get; set; }
            public double MWhTotal { get; set; }
            public double Empate { get; set; }
            public double Valor0Perc { get; set; }
            public double Valor50Perc { get; set; }
            public double Valor100Perc { get; set; }
            public double Diferenca
            {
                get
                {
                    return Valor100Perc - Valor50Perc;
                }
            }
            public string Observacao { get; set; }
        }
            public class ListViewModel
        {
            //public IEnumerable<PerfilAgente> PerfisAgente = new List<PerfilAgente>();
            public GeradorDiesel GeradorDiesel;
            public List<Ativo> Ativos;
            public List<MedicaoResumoMedicaoMesDto> ResumosMedicaoMes;
            public MedicaoResumoMedicaoMesDto ConsolidadosMedicaoMes;
            public List<MedicaoConsolidadoConsumoMesDto> medicoesRealMes;
            public List<ContratoReportDto> Contratos;
            public MedicaoMensalResumoDto ResumoDiario;
            public List<MedicaoAnualMedicaoMesDto> MedicoesAno;
            public List<ContabilizacaoCcee> Contabilizacoes;
            public List<CalculoCativoAtivoDto> calculosCativoLivre { get; set; }
            public List<CalculoLivreAtivoDto> calculosLivre { get; set; }
            public IEnumerable<RelatorioEconomia> Economias = new List<RelatorioEconomia>();
            public List<DemandaMedicaoMesDto> MedicoesDemandaAnoPonta = new List<DemandaMedicaoMesDto>();
            public List<DemandaMedicaoMesDto> MedicoesDemandaAnoForaPonta = new List<DemandaMedicaoMesDto>();
            public List<CustoLivre> custoLivre = new List<CustoLivre>();

            public List<AtivosMesViewModel> AtivosMes { get; set; }
            public List<precoEmpateMesViewModel> precoEmpateMes { get; set; }
            public string GetMesesEmpate()
            {
                if (precoEmpateMes.Any())
                    return precoEmpateMes.First().PrecoEmpateMes.OrderBy(o => o.Mes).Select(i => i.Mes.ToString("MMMM/yy")).Join("','");
                return null;
            }

            public DateTime Date { get; set; }
            public string TipoLeitura { get; set; }
            // public IEnumerable<MedicaoMesReal>  =  new List<MedicaoMesReal>(); 
            public string GetValores(String unidadeMedida, List<MedicaoAnualMedicaoMesDto> medicoes)
            {
                var valores = new List<double>();

                var ultimoMes = medicoes.Any() ? medicoes.Max(m => m.Mes.Month) : 0;

                for (var i = 1; i <= ultimoMes; i++)
                    valores.Add(medicoes.Where(m => m.Mes.Month == i).Select(m => unidadeMedida == "MWh" ? m.MWh : m.MWm).FirstOrDefault());

                return valores.Select(m =>
                    m == 0.0
                    ? "null" // null will remove the point from the chart
                    : m.ToString("N3").Remove(".").Replace(",", ".")
                ).Join(",");
            }
            public String ValoresGF;
            public string EmpilhaConsumoMWhForaPontaCapacitivo()
            {
                return MedicoesDiaConsumoMwhForaPontaCapacitivo().Join(",");
            }
            public string EmpilhaConsumoMWhPonta()
            {
                return ResumoDiario.MedicoesMesConsumo.Select(medicaoDia => (medicaoDia.MWhCapacitivo + medicaoDia.MWhForaPonta + medicaoDia.MWhPonta).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
            }

            public string EmpilhaConsumoMWhGeradorDiesel()
            {
                return ResumoDiario.MedicoesMesConsumo.Select(medicaoDia => (medicaoDia.MWhCapacitivo + medicaoDia.MWhForaPonta + medicaoDia.MWhPonta + medicaoDia.MWhGeradorDiesel).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
            }
            public List<string> MedicoesDiaConsumoMwhForaPontaCapacitivo()
            {
                return ResumoDiario.MedicoesMesConsumo.Select(i => (i.MWhCapacitivo + i.MWhForaPonta).ToString("N3").Remove(".").Replace(",", ".")).ToList();
            }
        }        

        private AtivoMesViewModel GetAtivoMesViewModel(CalculoLivreDto dto, bool includeIcms = false)
        {
            if (dto.Observacao == null)
            {
                var viewModel = new AtivoMesViewModel()
                {
                    Ativo = dto.Ativo,
                    Mes = dto.Mes,
                    ModalidadeTarifaria = dto.Modalidade,
                    ConsumoPonta = dto.ConsumoPontaAtivo + dto.ConsumoPontaDescEI,
                    ConsumoForaPonta = dto.ConsumoForaPontaAtivo,
                    DemandaPonta = (dto.DemandaPontaMedida + dto.DemandaPontaMedidaDescEI + dto.DemandaPontaUltrapassagem
                            + dto.DemandaPontaFaturada + dto.DemandaPontaFaturadaDescEI),
                    DemandaForaPonta = (dto.DemandaForaPontaMedida + dto.DemandaForaPontaMedidaDescEI + dto.DemandaForaPontaUltrapassagem
                            + dto.DemandaForaPontaFaturada + dto.DemandaForaPontaFaturadaDescEI),
                    CustoGeradorTotal = dto.ConsumoPontaGerador,
                    DescontoCcee = dto.DescontoCcee,
                    HasDemandaPontaUltrapassagem = (dto.DemandaPontaUltrapassagem > 0),
                    HasDemandaForaPontaUltrapassagem = (dto.DemandaForaPontaUltrapassagem > 0)
                };

                //CalcIcmsSt(viewModel, dto.MWhTotal, includeIcms);
                return viewModel;
            }
            return new AtivoMesViewModel() { Mes = dto.Mes, Observacao = dto.Observacao };
        }

        public class CustoLivre
        {
            public DateTime Mes { get; set; }            
            public double energiaContratada {get; set; }
            public double icmsST{ get; set; }            
            public double DemandaPonta { get; set; }
            public double DemandaForaPonta { get; set; }
            public double TUSDPonta { get; set; }
            public double TUSDForaPonta { get; set; }
            public double CCEE { get; set; }
            public double subvencaoTarifaria { get; set; }
            public double ajusteDesconto { get; set; }
            public double MWhTotal { get; set; }

        }
    }

}
