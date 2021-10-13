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
    public class SimulacaoEconomia2Controller : ControllerBase
    {
        private readonly IAliquotaImpostoService _aliquotaImpostoService;
        private readonly IAtivoService _ativoService;
        private readonly IBandeiraService _bandeiraService;
        private readonly IBandeiraCorService _bandeiraCorService;
        private readonly ICalculoEconomiaService _calculoEconomiaService;
        private readonly IIcmsVigenciaService _icmsVigenciaService;
        private readonly IMapeadorCenarioService _mapeadorCenarioService;
        private readonly IMapeadorPotencialCenarioService _mapeadorPotencialCenarioService;
        private readonly IMedicaoConsolidadoService _medicaoConsolidadoService;
        private readonly IOpcaoImpostoService _opcaoImpostoService;
        private readonly IPrecoFuturoEnergiaService _precoFuturoEnergiaService;
        private readonly IPrecoFuturoEnergiaValorService _precoFuturoEnergiaValorService;
        private readonly IMedicaoClientePotencialService _medicaoClientePotencialService;
        private const int custoCcee = 5;

        public SimulacaoEconomia2Controller(IAliquotaImpostoService aliquotaImpostoService,
            IAtivoService ativoService,
            IBandeiraService bandeiraService,
            IBandeiraCorService bandeiraCorService,
            ICalculoEconomiaService calculoEconomiaService,
            IIcmsVigenciaService icmsVigenciaService,
            IMapeadorCenarioService mapeadorCenarioService,
            IMapeadorPotencialCenarioService mapeadorPotencialCenarioService,
            IMedicaoConsolidadoService medicaoConsolidadoService,
            IOpcaoImpostoService opcaoImpostoService,
            IPrecoFuturoEnergiaService precoFuturoEnergiaService,
            IPrecoFuturoEnergiaValorService precoFuturoEnergiaValorService,
            IMedicaoClientePotencialService medicaoClientePotencialService)
        {
            _aliquotaImpostoService = aliquotaImpostoService;
            _ativoService = ativoService;
            _bandeiraService = bandeiraService;
            _bandeiraCorService = bandeiraCorService;
            _calculoEconomiaService = calculoEconomiaService;
            _icmsVigenciaService = icmsVigenciaService;
            _mapeadorCenarioService = mapeadorCenarioService;
            _mapeadorPotencialCenarioService = mapeadorPotencialCenarioService;
            _medicaoConsolidadoService = medicaoConsolidadoService;
            _opcaoImpostoService = opcaoImpostoService;
            _precoFuturoEnergiaService = precoFuturoEnergiaService;
            _precoFuturoEnergiaValorService = precoFuturoEnergiaValorService;
            _medicaoClientePotencialService = medicaoClientePotencialService;

        }

        [HttpPost]
        [ValidateInput(false)]
        //public JsonResult generatePDFandEmail(string Html)
        //{                      
        //    //return Json(_calculoEconomiaService.generatePDFandEmail(Html));
        //}

        public ActionResult Index()
        {
            var data = new ListViewModel()
            {
                TipoRelacao = Request["relacao"] ?? PerfilAgente.TiposRelacao.Cliente.ToString()
            };

            if (Request["bandeira"] == null)
            {
                var bandeiraCor = _bandeiraCorService.Get("Verde");
                if (bandeiraCor != null)
                    data.DefaultBandeiraID = bandeiraCor.ID;
            }

            if (Request["ativos"].IsNotBlank())
            {
                var ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);
                if (ativos.Any())
                {
                    data.Ativos = ativos;
                    data.Impostos = Request["imposto"];
                    data.ImpostosCreditados = Request["creditaimp"];
                    data.IsBaixaTensao = ativos.Any(i => i.IsBaixaTensao == true);

                    DateTime parsedDate;
                    if (DateTime.TryParse(Request["date"], out parsedDate))
                    {
                        var mes = Dates.GetFirstDayOfMonth(parsedDate);

                        var agenteConectadoId = Request["agentecon"].ToInt(null);
                        var anoInicio = Request["anoinicio"].ToInt(null);
                        var corBandeira = _bandeiraCorService.GetCor(Request["bandeira"].ToInt(null));
                        var includeIcms = Fmt.ContainsWithNull(Request["imposto"], "icms");
                        var includeImposto = Fmt.ContainsWithNull(Request["imposto"], "imposto");
                        var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
                        var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");
                        var precoEnergia = Request["preco"].ToDouble(0);
                        var tipoEnergia = Request["tipoenergia"].ToDouble(0);
                        var tipoGestao = Request["gestao"];
                        var tipoVigencia = Request["vigencia"];
                        var valorGestao = Request["vlrgestao"].ToDouble(0);
                        var resultsView = Request["rview"].ToInt(12);
                        var mapeadorId = Request["mapceid"].ToInt(null);
                        var autoDate = Request["autodate"].ToBoolean();
                        var chkManual = Request["chkManual"].ToBoolean();
                        var mwhPonta = Request["mwhPonta"].ToDouble(0);
                        var mwhForaPonta = Request["mwhForaPonta"].ToDouble(0);
                        var spread = Request["spread"].ToDouble(0);

                        var viewMonths = 12;

                        var dtosEconomia = new List<CalculoEconomiaAtivoDto>();

                        /// ((autoDate) ? null : (DateTime?)mes)

                        if (mapeadorId == null)
                        {
                            if (autoDate && data.TipoRelacao == PerfilAgente.TiposRelacao.Potencial.ToString())
                            {
                                //Coloquei apenas para potenciais pra testes - Hilario 27/08/20
                                var mostRecentMonth = _medicaoConsolidadoService.GetRecentMonthWithMedicaoPotencial(ativos.First());
                                if (mostRecentMonth != null)
                                    mes = mostRecentMonth.Value;
                            }

                            dtosEconomia = _calculoEconomiaService.LoadCalculos(ativos, mes, precoEnergia, agenteConectadoId, tipoEnergia, corBandeira, null, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, true, viewMonths, chkManual, mwhPonta, mwhForaPonta);
                        }
                        else
                        {
                            if (data.TipoRelacao == PerfilAgente.TiposRelacao.Cliente.ToString())
                            {
                                var mapeadorCenario = _mapeadorCenarioService.FindByID(mapeadorId.Value);
                                if (mapeadorCenario != null)
                                    dtosEconomia = _calculoEconomiaService.LoadCalculos(ativos, mes, mapeadorCenario, precoEnergia, agenteConectadoId, tipoEnergia, corBandeira, null, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, true, viewMonths);
                            }
                            else if (data.TipoRelacao == PerfilAgente.TiposRelacao.Potencial.ToString())
                            {
                                var mapeadorPotencialCenario = _mapeadorPotencialCenarioService.FindByID(mapeadorId.Value);
                                if (mapeadorPotencialCenario != null)
                                    dtosEconomia = _calculoEconomiaService.LoadCalculos(ativos, mes, mapeadorPotencialCenario, precoEnergia, agenteConectadoId, tipoEnergia, corBandeira, null, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, true, viewMonths);
                            }
                        }


                        if (dtosEconomia.Any())
                        {
                            var ativosMes = new List<AtivosMesViewModel>();

                            double precoEnergiaCativo = 0;

                            if ((mapeadorId == null) || (data.Ativos.Count() > 1))
                            {
                                precoEnergiaCativo = _calculoEconomiaService.GetPrecoEnergiaMercadoCativo(ativos, mes, precoEnergia, custoCcee, agenteConectadoId, tipoEnergia, tipoVigencia, "Verde", viewMonths);
                            }
                            else
                            {
                                Ativo ativo = null;
                                var medicoes = new List<MedicaoConsolidadoConsumoMesDto>();

                                if (data.TipoRelacao == PerfilAgente.TiposRelacao.Cliente.ToString())
                                {
                                    var mapeadorCenario = _mapeadorCenarioService.FindByID(mapeadorId.Value);
                                    if (mapeadorCenario != null)
                                    {
                                        ativo = mapeadorCenario.Ativo;
                                        medicoes = _medicaoConsolidadoService.GetMedicaoMesesByMapeadorCenario(mapeadorCenario);
                                    }
                                }
                                else if (data.TipoRelacao == PerfilAgente.TiposRelacao.Potencial.ToString())
                                {
                                    var mapeadorPotencialCenario = _mapeadorPotencialCenarioService.FindByID(mapeadorId.Value);
                                    if (mapeadorPotencialCenario != null)
                                    {
                                        ativo = mapeadorPotencialCenario.Ativo;
                                        medicoes = _medicaoConsolidadoService.GetMedicaoMesesByMapeadorPotencialCenario(mapeadorPotencialCenario);
                                    }
                                }

                                if (medicoes.Any())
                                    precoEnergiaCativo = _calculoEconomiaService.GetPrecoEnergiaMercadoCativo(medicoes, ativo, precoEnergia, custoCcee, agenteConectadoId, tipoEnergia, tipoVigencia, "Verde", viewMonths);
                            }

                            foreach (var dto in dtosEconomia)
                            {

                                //ajusta o valor das bandeiras para o atual

                                foreach (var calculo in dto.Calculos)
                                {
                                    if (calculo.AuxiliarCativo != null)
                                    {
                                        var valorBandeiras = calculo.AuxiliarCativo.ConsumoForaPontaBandeira + calculo.AuxiliarCativo.ConsumoPontaBandeira;
                                        if (includeIcms || includeImposto)
                                        {
                                            calculo.AuxiliarCativo.ConsumoForaPontaBandeira = calculo.AuxiliarCativo.MWhForaPontaCapacitivo * calculo.AuxiliarCativo.Bandeira.BandeiraCor.ValoresVigentes * Convert.ToDouble(calculo.AuxiliarCativo.TotalImpostos);

                                            calculo.AuxiliarCativo.ConsumoPontaBandeira = calculo.AuxiliarCativo.MWhPontaTotal * calculo.AuxiliarCativo.Bandeira.BandeiraCor.ValoresVigentes * Convert.ToDouble(calculo.AuxiliarCativo.TotalImpostos);
                                        }
                                        else
                                        {
                                            calculo.AuxiliarCativo.ConsumoForaPontaBandeira = calculo.AuxiliarCativo.MWhForaPontaCapacitivo * calculo.AuxiliarCativo.Bandeira.BandeiraCor.ValoresVigentes;
                                            calculo.AuxiliarCativo.ConsumoPontaBandeira = calculo.AuxiliarCativo.MWhPontaTotal * calculo.AuxiliarCativo.Bandeira.BandeiraCor.ValoresVigentes;
                                        }
                                        calculo.TotalCativo += calculo.AuxiliarCativo.ConsumoPontaBandeira + calculo.AuxiliarCativo.ConsumoForaPontaBandeira - valorBandeiras;
                                        //calculo.AuxiliarCativo.Total = calculo.TotalCativo;
                                        //calculo.AuxiliarCativo.TotalLiquido = calculo.TotalCativo;

                                    }
                                }

                                var ativoMes = new AtivosMesViewModel() { Ativo = dto.Ativo };

                                if (dto.Calculos.Any())
                                {
                                    if (dto.Calculos.Any(i => Double.IsNaN(i.MWhTotal)))
                                        dto.Calculos.RemoveAll(i => Double.IsNaN(i.MWhTotal));

                                    var average = _calculoEconomiaService.GetAverage(dto.Calculos, tipoGestao, valorGestao);
                                    if (average != null)
                                        ativoMes.MediaCalculos = _calculoEconomiaService.MultiplyPerMonths(average, 1);
                                }

                                ativosMes.Add(ativoMes);
                            }

                            data.AtivosMes = ativosMes;
                            data.Consolidado = GetConsolidado(dtosEconomia, tipoEnergia, precoEnergiaCativo, tipoGestao, valorGestao, resultsView, includeIcms, includeImposto, ((creditIcms) || (creditImposto)), anoInicio, spread);
                            data.Simulacoes = GetSimulacoes(ativosMes.Select(i => i.MediaCalculos), mes, tipoGestao, valorGestao, includeIcms, includeImposto, creditIcms, creditImposto, 1);
                            data.VigenciasContratuais = _ativoService.GetAtivosContratos(ativos);
                            data.ShowGestaoFields = ((valorGestao != 0) && (!UserSession.IsCliente));
                            var MedicoesAnos = _medicaoClientePotencialService.ReportAnual(data.Ativos.ToList(), false, 5);

                           

                            var MedicaoMeses = new List<MedicaoConsolidadoConsumoMesDto>();
                            if (MedicoesAnos != null)
                            {
                                foreach (var anoConsumo in MedicoesAnos.MedicoesAnos)
                                {
                                    foreach (var mesAno in anoConsumo.Meses)
                                    {
                                        MedicaoMeses.Add(mesAno);
                                    }
                                }

                            }
                            data.MedicaoMeses = MedicaoMeses.Where(w => w.Mes > MedicaoMeses.Max(m => m.Mes).AddMonths(-12)).ToList();


                            if (!data.Simulacoes.Any())
                                data.Observacao = "Não foi possível gerar as médias pois há cadastros ausentes que fazem parte do cálculo.";
                        }
                    }
                }
                
            }
            else
            {
                if (UserSession.Agentes != null)
                {
                    var ativo = _ativoService.GetByAgentes(UserSession.Agentes);
                    if (ativo != null)
                    {
                        data.Ativos = new List<Ativo>() { ativo };

                        var opcaoImposto = _opcaoImpostoService.GetMostRecent(ativo.ID.Value);
                        if (opcaoImposto != null)
                        {
                            data.Impostos = opcaoImposto.TipoImposto;
                            data.ImpostosCreditados = opcaoImposto.TipoCredito;
                        }
                    }
                }
            }


            return AdminContent("SimulacaoEconomia/SimulacaoEconomia2Report.aspx", data);
        }
        

        private List<SimulacaoEconomiaViewModel> GetSimulacoes(IEnumerable<CalculoEconomiaDto> averagesDto, DateTime mes,
			string tipoGestao, double valorGestao,
			bool includeIcms = false, bool includeImposto = false,
			bool creditIcms = false, bool creditImposto = false,
			int resultsView = 1)
		{
			var simulacoes = new List<SimulacaoEconomiaViewModel>();

			foreach (var averageDto in averagesDto)
			{
				if (averageDto.Observacao == null)
				{
					for (int preco = 100; preco <= 500; preco = preco + 5)
					{
						double precoEnergia = _calculoEconomiaService.GetAdjustedPrecoEnergia(averageDto.Ativo, mes, preco,
							includeIcms, includeImposto, ((creditIcms) || (creditImposto)) ? averageDto.AuxiliarLivre.CreditoImposto : null);

						double valorEnergia = (averageDto.MWhTotal * precoEnergia);

						var simulacao = simulacoes.FirstOrDefault(i => i.PrecoEnergia == preco);

						var isSimulacaoExistente = (simulacao != null);
						if (!isSimulacaoExistente)
							simulacao = new SimulacaoEconomiaViewModel();
                        var economia = (averageDto.TotalCativo * resultsView)- (valorEnergia + averageDto.TotalLivre + averageDto.CceeEssRes + simulacao.Gestao) * resultsView; 
                        simulacao.Gestao += _calculoEconomiaService.GetValorGestao(tipoGestao, valorGestao, averageDto.MWhTotal, economia);
						simulacao.PrecoEnergia = preco;
						simulacao.TotalCativo += averageDto.TotalCativo * resultsView;
						simulacao.TotalMercadoLivre += (valorEnergia + averageDto.TotalLivre + averageDto.CceeEssRes + simulacao.Gestao) * resultsView;

						if (!isSimulacaoExistente)
							simulacoes.Add(simulacao);
					}
				}
			}

			return simulacoes;
		}

		private SimulacaoEconomiaConsolidadoViewModel GetConsolidado(List<CalculoEconomiaAtivoDto> calculos,
			double tipoEnergia, double precoEnergiaCativo, string tipoGestao, double valorGestao,
			int resultsView = 1,
			bool includeIcms = false, bool includeImposto = false,
			bool includeCreditImposto = false,
			int? anoInicio = null,
            double spread = 0)
		{
			SimulacaoEconomiaConsolidadoViewModel viewModel = null;

			var today = Dates.GetFirstDayOfMonth(DateTime.Today);
			if (today.Month == 12)
				today = today.AddMonths(1);

			var years = _precoFuturoEnergiaService.GetAnosSinceAno(anoInicio ?? today.Year).ToList(); // new List<int>() { today.Year, today.Year + 1, today.Year + 2, today.Year + 3, today.Year + 4 };
			if (years.Any())
			{
				var precosFuturosEnergia = _precoFuturoEnergiaValorService.GetByAnos(years, tipoEnergia);
				if (precosFuturosEnergia.Any())
				{
					foreach (var calculosAtivo in calculos)
					{
						if (viewModel == null)
							viewModel = new SimulacaoEconomiaConsolidadoViewModel() { PrevisoesLivreAnos = new List<SimulacaoEconomiaConsolidadoAnoViewModel>() };

						var icms = _icmsVigenciaService.GetByVigencia(calculosAtivo.Ativo, today);
						if (icms == null)
						{
							viewModel.Observacao = string.Format("ICMS não localizado para o ativo '{0}'.", calculosAtivo.Ativo.Nome);
							break;
						}
						var aliquotaPisCofins = _aliquotaImpostoService.GetMostRecent(today);
						if (aliquotaPisCofins == null)
						{
							viewModel.Observacao = string.Format("PIS/COFINS não localizados para o ativo '{0}'.", calculosAtivo.Ativo.Nome);
							break;
						}

						if (viewModel.Observacao != null)
							break;

						var cativos = calculosAtivo.Calculos.Where(i => i.AuxiliarCativo != null).Select(i => i.AuxiliarCativo).Where(i => i.Observacao == null && i.MWhTotal > 0);
						var livres = calculosAtivo.Calculos.Where(i => i.AuxiliarLivre != null).Select(i => i.AuxiliarLivre).Where(i => i.Observacao == null && i.MWhTotal > 0);

						if ((cativos.Any()) && (livres.Any()))
						{

                            viewModel.calculosCativos = cativos.ToList();
                            viewModel.calculosLivres = livres.ToList();

                            var mwhTotal = cativos.Average(i => i.MWhTotal);

							viewModel.PrecoEnergia = precoEnergiaCativo;
							viewModel.AliquotaIcms = icms.AliquotaEnergia;
							viewModel.ConsumoMWhTotal = (mwhTotal * resultsView);
							viewModel.Encargo += ((cativos.Average(i => i.ConsumoPontaTusd) + cativos.Average(i => i.ConsumoForaPontaTusd)) * resultsView);
							viewModel.Demanda += ((cativos.Average(i => i.DemandaPontaMedida) + cativos.Average(i => i.DemandaForaPontaMedida) + cativos.Average(i => i.DemandaPontaFaturada) + cativos.Average(i => i.DemandaForaPontaFaturada)) * resultsView);
							viewModel.Gerador += ((cativos.Average(i => i.ConsumoPontaGerador)) * resultsView);
							viewModel.TePerdas += ((cativos.Average(i => i.ConsumoPontaTe) + cativos.Average(i => i.ConsumoForaPontaTe)) * resultsView);
                            //Outro Calculo de bandeiras
                            viewModel.TotalBandeiras += ((cativos.Average(i => i.ConsumoPontaBandeira)) + (cativos.Average(i => i.ConsumoForaPontaBandeira))) * resultsView;
                            var economia = cativos.Average(i => i.Total) - livres.Average(i => i.Total);
                            viewModel.Gestao += (_calculoEconomiaService.GetValorGestao(tipoGestao, valorGestao, mwhTotal,economia) * resultsView);

							if (includeCreditImposto)
								CreditImpostosCativo(viewModel, cativos);

							foreach (var year in years)
							{
								var precoFuturo = precosFuturosEnergia.FirstOrDefault(i => i.PrecoFuturoEnergia.Ano == year);
								if (precoFuturo == null)
								{
									viewModel.Observacao = string.Format("Preço não localizado para o ano '{0}' e tipo de energia '{1}'.", year, tipoEnergia);
									break;
								}
                                precoFuturo.Preco = precoFuturo.Preco + spread;


                                var viewModelSimulacaoEconomiaLivreAno = viewModel.PrevisoesLivreAnos.FirstOrDefault(i => i.Ano == year);
								if (viewModelSimulacaoEconomiaLivreAno == null)
								{
                                    viewModelSimulacaoEconomiaLivreAno = new SimulacaoEconomiaConsolidadoAnoViewModel()   
                                    {
										Ano = year,
										PrecoFuturoEnergia = precoFuturo.Preco
									};

                                    

                                    var yearMemorialCativoMedio = new MemorialCalculoCativoDto();
                                    yearMemorialCativoMedio = viewModel.MemorialMedioCativo;                                    
                                    var yearMemorialLivreMedio = new MemorialCalculoLivreDto();
                                    yearMemorialLivreMedio = viewModel.MemorialMedioLivre;



                                    viewModelSimulacaoEconomiaLivreAno.MemorialMedioLivre = yearMemorialLivreMedio;
                                    viewModelSimulacaoEconomiaLivreAno.MemorialMedioCativo = yearMemorialCativoMedio;


                                    //viewModelSimulacaoEconomiaLivreAno.MemorialMedioLivre.EnergiaLivre.Preco = precoFuturo.Preco;

                                    viewModel.PrevisoesLivreAnos.Add(viewModelSimulacaoEconomiaLivreAno);
								}
                                //13-07-20 - jogamos toda a subvencao para o encargo - Hilario e Erica
								var precoFuturoEnergia = precoFuturo.Preco;
								if (!includeImposto)
									precoFuturoEnergia *= (1 - (aliquotaPisCofins.ValorPis + aliquotaPisCofins.ValorCofins));

								viewModelSimulacaoEconomiaLivreAno.Encargo += ((livres.Average(i => i.ConsumoPontaAtivo) + livres.Average(i => i.ConsumoPontaDescEI) + livres.Average(i => i.ConsumoForaPontaAtivo)+ livres.Average(i => i.SubvencaoTarifaria==null ? 0 : i.SubvencaoTarifaria.TotalSubvencaoTarifariaTusd) + livres.Average(i => i.SubvencaoTarifaria == null ? 0 : i.SubvencaoTarifaria.TotalSubvencaoTarifariaDemanda)) * resultsView);
								viewModelSimulacaoEconomiaLivreAno.Demanda += ((livres.Average(i => i.DemandaPontaMedida) + livres.Average(i => i.DemandaPontaFaturada) + livres.Average(i => i.DemandaPontaMedidaDescEI) + livres.Average(i => i.DemandaPontaFaturadaDescEI)
									+ livres.Average(i => i.DemandaForaPontaMedida) + livres.Average(i => i.DemandaForaPontaFaturada) + livres.Average(i => i.DemandaForaPontaMedidaDescEI) + livres.Average(i => i.DemandaForaPontaFaturadaDescEI)) * resultsView);
								viewModelSimulacaoEconomiaLivreAno.TePerdas += (((precoFuturoEnergia / (1 - ((includeIcms) ? icms.AliquotaEnergia : 0))) * livres.Average(i => i.MWhTotal)) * resultsView);
								viewModelSimulacaoEconomiaLivreAno.CceeEssRes += ((custoCcee * livres.Average(i => i.MWhTotal)) * resultsView);
                                var cat = cativos.Sum(i => i.Total);
                                var eco = cat - viewModelSimulacaoEconomiaLivreAno.TotalMercadoLivre;
                                if (eco <= 0) eco = 0;
                                if (tipoGestao =="percentual")
                                   viewModelSimulacaoEconomiaLivreAno.Gestao += (_calculoEconomiaService.GetValorGestao(tipoGestao, valorGestao, mwhTotal, eco));
                                else
                                    viewModelSimulacaoEconomiaLivreAno.Gestao += (_calculoEconomiaService.GetValorGestao(tipoGestao, valorGestao, mwhTotal, eco) * resultsView);

                                if (includeCreditImposto)
                                {
                                    var subvencaoEncargo = livres.Average(i => i.SubvencaoTarifaria == null ? 0 : i.SubvencaoTarifaria.TotalSubvencaoTarifariaTusd) * resultsView;
                                    var subvencaoDemanda = livres.Average(i => i.SubvencaoTarifaria == null ? 0 : i.SubvencaoTarifaria.TotalSubvencaoTarifariaDemanda) * resultsView;

                                    viewModelSimulacaoEconomiaLivreAno.Encargo -= (subvencaoEncargo + subvencaoDemanda );

                                    CreditImpostosLivre(viewModelSimulacaoEconomiaLivreAno, livres);
                                    viewModelSimulacaoEconomiaLivreAno.Encargo += (subvencaoEncargo + subvencaoDemanda);
                                }
									
							}
						}
					}
				}
			}

			return viewModel;
		}

		private void CreditImpostosCativo(SimulacaoEconomiaConsolidadoViewModel viewModel, IEnumerable<CalculoCativoDto> cativos)
		{
			var impostosCreditadosCativo = cativos.OrderByDescending(i => i.Mes).Where(i => i.CreditoImposto != null).Select(i => i.CreditoImposto).FirstOrDefault();
			if (impostosCreditadosCativo != null)
			{
				viewModel.Demanda *= impostosCreditadosCativo.TotalImpostosCreditados;
				viewModel.Encargo *= impostosCreditadosCativo.TotalImpostosCreditados;
				viewModel.TePerdas *= impostosCreditadosCativo.TotalImpostosCreditados;
				viewModel.TotalBandeiras *= impostosCreditadosCativo.TotalImpostosCreditados;
				viewModel.Gerador *= impostosCreditadosCativo.TotalImpostosCreditadosGerador;
			}
		}

		private void CreditImpostosLivre(SimulacaoEconomiaConsolidadoAnoViewModel viewModel, IEnumerable<CalculoLivreDto> livres)
		{
			var impostosCreditadosLivre = livres.OrderByDescending(i => i.Mes).Where(i => i.CreditoImposto != null).Select(i => i.CreditoImposto).FirstOrDefault();
			if (impostosCreditadosLivre != null)
			{
				viewModel.Demanda *= impostosCreditadosLivre.TotalImpostosCreditados;
				viewModel.Encargo *= impostosCreditadosLivre.TotalImpostosCreditados;
				viewModel.TePerdas *= impostosCreditadosLivre.TotalImpostosCreditados;
			}
		}

		private double GetPrecoEnergiaMercadoCativo(List<CalculoEconomiaAtivoDto> calculos, Ativo ativo)
		{
			if (calculos.Any())
			{
				var calculosWithAllImpostos = calculos.FirstOrDefault(i => i.Ativo == ativo).Calculos;

				var cativos = calculosWithAllImpostos.Where(i => i.AuxiliarCativo != null).Select(i => i.AuxiliarCativo).Where(i => i.Observacao == null);
				var livres = calculosWithAllImpostos.Where(i => i.AuxiliarLivre != null).Select(i => i.AuxiliarLivre).Where(i => i.Observacao == null);

				return ((cativos.Sum(i => i.TotalLiquido) - livres.Sum(i => i.TotalLiquido)) / cativos.Sum(i => i.MWhTotal));
			}
			return 0;
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public List<AtivosMesViewModel> AtivosMes { get; set; }
			public List<SimulacaoEconomiaViewModel> Simulacoes { get; set; }
			public List<AtivoContratoDto> VigenciasContratuais { get; set; }
			public SimulacaoEconomiaConsolidadoViewModel Consolidado { get; set; }
			public string TipoRelacao { get; set; }
			public string Impostos { get; set; }
			public string ImpostosCreditados { get; set; }
			public string Observacao { get; set; }
			public int? DefaultBandeiraID { get; set; }
			public bool IsBaixaTensao { get; set; }
			public bool ShowGestaoFields { get; set; }
            public List<MedicaoConsolidadoConsumoMesDto> MedicaoMeses { get; set; }
            //public MemorialCalculoCativo MemorialCativo { get; set; }
            //public MedicaoClientePotencialAnualReportDto MedicoesAnos { get; set; }           
        }

        
        public class AtivosMesViewModel
		{
			public Ativo Ativo { get; set; }
			public CalculoEconomiaDto MediaCalculos { get; set; }
			public string GetValorEconomia(List<SimulacaoEconomiaViewModel> simulacoes)
			{
				return string.Join(",", simulacoes.Select(i => i.ValorEconomia));
			}
		}

		public class SimulacaoEconomiaViewModel
		{
			public double PrecoEnergia { get; set; }
			public double Gestao { get; set; }
			public double TotalCativo { get; set; }
			public double TotalMercadoLivre { get; set; }
                  

            public double ValorEconomia
			{
				get
				{
					return (TotalCativo - TotalMercadoLivre);
				}
			}
			public double PercentualEconomia
			{
				get
				{
					return (ValorEconomia / TotalCativo);
				}
			}
		}

		public class SimulacaoEconomiaConsolidadoViewModel
		{
			public double ConsumoMWhTotal { get; set; }
			public double AliquotaIcms { get; set; }
			public double Encargo { get; set; }
			public double Demanda { get; set; }
			public double Gerador { get; set; }
			public double TePerdas { get; set; }
			public double CceeEssRes { get; set; }
			public double Gestao { get; set; }
			public double TotalBandeiras { get; set; }
			public double PrecoEnergia { get; set; }
			public double TotalMercadoCativo
			{
				get
				{
					return (Encargo + Demanda + Gerador + TePerdas + CceeEssRes + TotalBandeiras);
				}
			}
			public string Observacao { get; set; }
			public List<SimulacaoEconomiaConsolidadoAnoViewModel> PrevisoesLivreAnos { get; set; }
            public List<CalculoCativoDto> calculosCativos { get; set; }
            public List<CalculoLivreDto> calculosLivres { get; set; }
            public MemorialCalculoCativoDto MemorialMedioCativo
            {
                get
                {
                    var Memorial = new MemorialCalculoCativoDto();
                    var FaturaCativo = new MemorialCativoFaturaDistribuidoraDto();
                    var CreditoImpostos = new MemorialCativoCreditoImpostosDto();
                    var GeradorDiesel = new MemorialCativoGeradorDieselDto();

                    if (calculosCativos != null && calculosCativos.Count() > 0)
                    {
                        FaturaCativo.MontanteDemandaPonta = calculosCativos.Average(a => a.Memorial.FaturaCativo.MontanteDemandaPonta);
                        FaturaCativo.TarifasemImpostosDemandaPonta = calculosCativos.First().Memorial.FaturaCativo.TarifasemImpostosDemandaPonta;
                        FaturaCativo.ImpostosTarifaDemandaPonta = calculosCativos.First().Memorial.FaturaCativo.ImpostosTarifaDemandaPonta;

                        FaturaCativo.MontanteDemandaForaPonta = calculosCativos.Average(a => a.Memorial.FaturaCativo.MontanteDemandaForaPonta);
                        FaturaCativo.TarifasemImpostosDemandaForaPonta = calculosCativos.First().Memorial.FaturaCativo.TarifasemImpostosDemandaForaPonta;
                        FaturaCativo.ImpostosTarifaDemandaForaPonta = calculosCativos.First().Memorial.FaturaCativo.ImpostosTarifaDemandaForaPonta;

                        FaturaCativo.MontanteTUSDPonta = calculosCativos.Average(a => a.Memorial.FaturaCativo.MontanteTUSDPonta);
                        FaturaCativo.TarifasemImpostosTUSDPonta = calculosCativos.First().Memorial.FaturaCativo.TarifasemImpostosTUSDPonta;
                        FaturaCativo.ImpostosTarifaTUSDPonta = calculosCativos.First().Memorial.FaturaCativo.ImpostosTarifaTUSDPonta;

                        FaturaCativo.MontanteTUSDForaPonta = calculosCativos.Average(a => a.Memorial.FaturaCativo.MontanteTUSDForaPonta);
                        FaturaCativo.TarifasemImpostosTUSDForaPonta = calculosCativos.First().Memorial.FaturaCativo.TarifasemImpostosTUSDForaPonta;
                        FaturaCativo.ImpostosTarifaTUSDForaPonta = calculosCativos.First().Memorial.FaturaCativo.ImpostosTarifaTUSDForaPonta;

                        FaturaCativo.MontanteEnergiaPonta = calculosCativos.Average(a => a.Memorial.FaturaCativo.MontanteEnergiaPonta);
                        FaturaCativo.TarifasemImpostosEnergiaPonta = calculosCativos.First().Memorial.FaturaCativo.TarifasemImpostosEnergiaPonta;
                        FaturaCativo.ImpostosTarifaEnergiaPonta = calculosCativos.First().Memorial.FaturaCativo.ImpostosTarifaEnergiaPonta;

                        FaturaCativo.MontanteEnergiaForaPonta = calculosCativos.Average(a => a.Memorial.FaturaCativo.MontanteEnergiaForaPonta);
                        FaturaCativo.TarifasemImpostosEnergiaForaPonta = calculosCativos.First().Memorial.FaturaCativo.TarifasemImpostosEnergiaForaPonta;
                        FaturaCativo.ImpostosTarifaEnergiaForaPonta = calculosCativos.First().Memorial.FaturaCativo.ImpostosTarifaEnergiaForaPonta;

                        FaturaCativo.MontanteBandeira = FaturaCativo.MontanteEnergiaForaPonta + FaturaCativo.MontanteEnergiaPonta;
                        FaturaCativo.TarifasemImpostosBandeira = calculosCativos.First().Memorial.FaturaCativo.TarifasemImpostosBandeira;
                        FaturaCativo.ImpostosTarifaBandeira = calculosCativos.First().Memorial.FaturaCativo.ImpostosTarifaBandeira;

                        CreditoImpostos.valorICMS = calculosCativos.Average(a => a.Memorial.CreditoImpostos.valorICMS);
                        CreditoImpostos.valorPISCofins = calculosCativos.Average(a => a.Memorial.CreditoImpostos.valorPISCofins);

                        GeradorDiesel.custoMWh = calculosCativos.Average(a => a.Memorial.GeradorDiesel.custoMWh);
                        GeradorDiesel.Montante = calculosCativos.Average(a => a.Memorial.GeradorDiesel.Montante);
                    }
                    Memorial.FaturaCativo = FaturaCativo;
                    Memorial.CreditoImpostos = CreditoImpostos;
                    Memorial.GeradorDiesel = GeradorDiesel;
                    return Memorial;
                }
            }
            public MemorialCalculoLivreDto MemorialMedioLivre
            {
                get
                {
                    var Memorial = new MemorialCalculoLivreDto();
                    var FaturaLivre = new MemorialLivreFaturaDistribuidoraDto();
                    var EnergiaLivre = new MemorialLivreEnergiaDto();
                    var CreditoImpostosLivre = new MemorialLivreCreditoImpostosDto();
                    var OutrosCustos = new MemorialLivreOutrosCustosDto();

                    if (calculosLivres != null && calculosLivres.Count() > 0)
                    {
                        FaturaLivre.MontanteDemandaPonta = calculosLivres.Average(a => a.Memorial.FaturaLivre.MontanteDemandaPonta);
                        FaturaLivre.TarifasemImpostosDemandaPonta = calculosLivres.First().Memorial.FaturaLivre.TarifasemImpostosDemandaPonta;
                        FaturaLivre.ImpostosTarifaDemandaPonta = calculosLivres.First().Memorial.FaturaLivre.ImpostosTarifaDemandaPonta;

                        FaturaLivre.MontanteDemandaForaPonta = calculosLivres.Average(a => a.Memorial.FaturaLivre.MontanteDemandaForaPonta);
                        FaturaLivre.TarifasemImpostosDemandaForaPonta = calculosLivres.First().Memorial.FaturaLivre.TarifasemImpostosDemandaForaPonta;
                        FaturaLivre.ImpostosTarifaDemandaForaPonta = calculosLivres.First().Memorial.FaturaLivre.ImpostosTarifaDemandaForaPonta;

                        FaturaLivre.MontanteTUSDPonta = calculosLivres.Average(a => a.Memorial.FaturaLivre.MontanteTUSDPonta);
                        FaturaLivre.TarifasemImpostosTUSDPonta = calculosLivres.First().Memorial.FaturaLivre.TarifasemImpostosTUSDPonta;
                        FaturaLivre.ImpostosTarifaTUSDPonta = calculosLivres.First().Memorial.FaturaLivre.ImpostosTarifaTUSDPonta;

                        FaturaLivre.MontanteTUSDForaPonta = calculosLivres.Average(a => a.Memorial.FaturaLivre.MontanteTUSDForaPonta);
                        FaturaLivre.TarifasemImpostosTUSDForaPonta = calculosLivres.First().Memorial.FaturaLivre.TarifasemImpostosTUSDForaPonta;
                        FaturaLivre.ImpostosTarifaTUSDForaPonta = calculosLivres.First().Memorial.FaturaLivre.ImpostosTarifaTUSDForaPonta;

                        FaturaLivre.ValorSubvencaoTarifaria = calculosLivres.Average(a => a.Memorial.FaturaLivre.ValorSubvencaoTarifaria);
                        FaturaLivre.ValorSubstituicaoTributaria  = calculosLivres.Average(a => a.Memorial.FaturaLivre.ValorSubstituicaoTributaria);

                        EnergiaLivre.MontanteContratadoMWh = calculosLivres.Average(a => a.Memorial.EnergiaLivre.MontanteContratadoMWh);
                        //EnergiaLivre.Preco = calculosLivres.Average(a => a.Memorial.EnergiaLivre.Preco);

                        CreditoImpostosLivre.ICMSDistribuicao = calculosLivres.Average(a => a.Memorial.CreditoImpostos.ICMSDistribuicao);
                        CreditoImpostosLivre.ICMSEnergia = calculosLivres.Average(a => a.Memorial.CreditoImpostos.ICMSEnergia);
                        CreditoImpostosLivre.PISCofinsDistribuicao = calculosLivres.Average(a => a.Memorial.CreditoImpostos.PISCofinsDistribuicao);
                        CreditoImpostosLivre.PISCofinsEnergia = calculosLivres.Average(a => a.Memorial.CreditoImpostos.PISCofinsEnergia);
                        CreditoImpostosLivre.SubvencaoTarifaria  = calculosLivres.Average(a => a.Memorial.CreditoImpostos.SubvencaoTarifaria);

                        //OutrosCustos.CCEEMontante = calculosLivres.Average(a => a.Memorial.OutrosCustos.CCEEMontante);
                        //OutrosCustos.Remuneracao = 1000;
                    }
                    Memorial.FaturaLivre = FaturaLivre;
                    Memorial.EnergiaLivre = EnergiaLivre;
                    Memorial.CreditoImpostos = CreditoImpostosLivre;
                    Memorial.OutrosCustos = OutrosCustos;                   
                    return Memorial;
                }
            }
            

        }        

		public class SimulacaoEconomiaConsolidadoAnoViewModel
		{
			public int Ano { get; set; }
			public double? PrecoFuturoEnergia { get; set; }
			public double Encargo { get; set; }
			public double Demanda { get; set; }
			public double Gerador { get; set; }
			public double TePerdas { get; set; }
			public double CceeEssRes { get; set; }
			public double Gestao { get; set; }
			public double TotalBandeiras
			{
				get
				{
					return 0;
				}
			}
			public double TotalMercadoLivre
			{
				get
				{
					return (Encargo + Demanda + Gerador + TePerdas + CceeEssRes + Gestao);
				}
			}
            public MemorialCalculoCativoDto MemorialMedioCativo { get; set; }
            public MemorialCalculoLivreDto MemorialMedioLivre { get; set; }


        }
	}
}
