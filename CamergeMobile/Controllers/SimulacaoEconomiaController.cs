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
	public class SimulacaoEconomiaController : ControllerBase
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
		private const int custoCcee = 5;

		public SimulacaoEconomiaController(IAliquotaImpostoService aliquotaImpostoService,
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
			IPrecoFuturoEnergiaValorService precoFuturoEnergiaValorService)
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
		}

        public ActionResult Estudo()
        {
            var data = new EstudoViewModel();
			data.valorEconomia = 1852.87;
			
			var estudoDemandaList = new List<EstudoDemandas>();

			// for (DateTime mes = DateTime.Today; mes > mes.AddMonths(-12);mes = mes.AddMonths(-1) )
			for (int i = 0; i<12; i++)
            {
				DateTime mes = DateTime.Today.AddMonths(i);
				var estudodemandas = new EstudoDemandas();
				estudodemandas.mes = mes;
				estudodemandas.demandaForaPonta = 1253;
				estudodemandas.demandaPonta = 854;

				estudoDemandaList.Add(estudodemandas);

            }
			//data.estudoDemandasList = estudoDemandaList;
            return AdminContent("SimulacaoEconomia/SimulacaoEconomia2Report.aspx", data);


        }

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

							dtosEconomia = _calculoEconomiaService.LoadCalculos(ativos, mes, precoEnergia, agenteConectadoId, tipoEnergia, corBandeira, null, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, true, viewMonths, chkManual,mwhPonta,mwhForaPonta);
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
							data.Consolidado = GetConsolidado(dtosEconomia, tipoEnergia, precoEnergiaCativo, tipoGestao, valorGestao, resultsView, includeIcms, includeImposto, ((creditIcms) || (creditImposto)), anoInicio,spread);
							data.Simulacoes = GetSimulacoes(ativosMes.Select(i => i.MediaCalculos), mes, tipoGestao, valorGestao, includeIcms, includeImposto, creditIcms, creditImposto, 1);
							data.VigenciasContratuais = _ativoService.GetAtivosContratos(ativos);
							data.ShowGestaoFields = ((valorGestao != 0) && (!UserSession.IsCliente));

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

			return AdminContent("SimulacaoEconomia/SimulacaoEconomiaReport.aspx", data);
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

		public class ReportGeneratorViewModelTeste
		{
			public DateTime mes;
		}

		public class EstudoViewModel
        {
            public List<Ativo> Ativos { get; set; }
            public string TipoRelacao { get; set; }
			public DateTime mes;
			public string Impostos { get; set; }
            public string ImpostosCreditados { get; set; }
            public string Observacao { get; set; }
            public int? DefaultBandeiraID { get; set; }
            public bool IsBaixaTensao { get; set; }
            public bool ShowGestaoFields { get; set; }
			public double valorEconomia;
			public List<EconomiaCativoLivreDto> estudoDemandasList;
			public ReportGeneratorPerfilConsumo PerfilConsumo;
			public MemorialCalculoCativoDto MemorialCalculoCativo;
			public MemorialCalculoLivreDto MemorialCalculoLivre;

		}

		public class EstudoDemandas
        {
			public DateTime mes;
			public double demandaPonta;
			public double demandaForaPonta;
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
			public ReportGeneratorPerfilConsumo PerfilConsumo { get; set; }
			public MemorialCalculoCativoDto MemorialCalculoCativo { get; set; }
			public MemorialCalculoLivreDto MemorialCalculoLivre { get; set; }
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
		}
	}
}
