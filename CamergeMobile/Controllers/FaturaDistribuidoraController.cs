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
	public class FaturaDistribuidoraController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ICalculoLivreService _calculoLivreService;
		private readonly IDevecService _devecService;
		private readonly IIcmsVigenciaService _icmsVigenciaService;
		private readonly IOpcaoImpostoService _opcaoImpostoService;
		private readonly ITarifaVigenciaValorService _tarifaVigenciaValorService;
        private readonly IFaturaDistribuidoraService _faturaDistribuidoraService;

		public FaturaDistribuidoraController(IAtivoService ativoService,
			ICalculoLivreService calculoLivreService,
			IDevecService devecService,
			IIcmsVigenciaService icmsVigenciaService,
			IOpcaoImpostoService opcaoImpostoService,
			ITarifaVigenciaValorService tarifaVigenciaValorService,
            IFaturaDistribuidoraService faturaDistribuidoraService)
		{
			_ativoService = ativoService;
			_calculoLivreService = calculoLivreService;
			_devecService = devecService;
			_icmsVigenciaService = icmsVigenciaService;
			_opcaoImpostoService = opcaoImpostoService;
			_tarifaVigenciaValorService = tarifaVigenciaValorService;
            _faturaDistribuidoraService = faturaDistribuidoraService;
		}
        
        public JsonResult GetFaturaPreview(int idFatura)
        {
            //var fatura = _faturaDistribuidoraService.GetbyID(idFatura);
            var getBody = _faturaDistribuidoraService.GetBodyPreview(idFatura);
            return Json(getBody, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Index()
		{
			var data = new ListViewModel();
			var allativos = Request["allativo"].ToBoolean();

			if (Request["ativos"].IsNotBlank())
			{
				var ativos = new List<Ativo>();
				MedicaoConsolidadoConsumoMesDto medicaoConsolidadoMesDto = null;

				if (Request["allativo"].IsBlank())
				{
					ativos = new List<Ativo>() { new Ativo() { Nome = "Simulação" } };

					medicaoConsolidadoMesDto = new MedicaoConsolidadoConsumoMesDto()
					{
						MwhPonta = Request["cp"].ToDouble(0),
						MwhForaPonta = Request["cfp"].ToDouble(0),
						DemandaPonta = Request["dp"].ToDouble(0),
						DemandaForaPonta = Request["dfp"].ToDouble(0)
					};
				}

				if (medicaoConsolidadoMesDto == null)
				{
					if (allativos)
						ativos = _ativoService.GetAtivosByPerfilAgenteTipo(PerfilAgente.Tipos.Consumidor, PerfilAgente.TiposRelacao.Cliente.ToString()).ToList();
					else
						ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);
				}

				if (ativos.Any())
				{
					data.Ativos = ativos;
					data.Impostos = Request["imposto"];
					data.ImpostosCreditados = Request["creditaimp"];

					DateTime parsedDate;
					if (DateTime.TryParse(Request["date"], out parsedDate))
					{
						var mes = Dates.GetFirstDayOfMonth(parsedDate);

						var tipoEnergia = Request["tipoenergia"].ToDouble(null);
						var tipoVigencia = Request["vigencia"];
                        var tipoVigenciaCompara = Request["vigencia_compara"];
                        var includeIcms = Request["imposto"].Contains("icms");
						var includeImposto = Request["imposto"].Contains("imposto");
						var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
						var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");
                        var mesunico = Request["enablemesunico"];

                        var viewMonths = (allativos) ? 1 : 13;

                        if (mesunico == "on") viewMonths = 1;
                        
                        var dtos = (medicaoConsolidadoMesDto == null)
							? _calculoLivreService.LoadCalculos(ativos, mes, null, tipoEnergia, null, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, true, false, viewMonths,true)
							: new List<CalculoLivreAtivoDto>()
							{
								new CalculoLivreAtivoDto()
								{
									Ativo = ativos.First(),
									Calculos = new List<CalculoLivreDto>() { GetCalculoLivreDtoBySimulacao(medicaoConsolidadoMesDto, mes, Request["ativo"].ToInt(), Request["agentecon"].ToInt(null), Request["modalidade"], tipoEnergia, includeIcms, includeImposto, creditIcms, creditImposto) }
								}
							};

						if (dtos.Any())
						{
							var ativosMes = new List<AtivosMesViewModel>();                           

                            foreach (var dto in dtos)
							{
								var ativoMes = new AtivosMesViewModel() { Ativo = dto.Ativo };
                                
                                if (dto.Calculos.Any())
								{
									var calculoAtivoMes = new List<AtivoMesViewModel>();
                                    
                                    foreach (var calculo in dto.Calculos)
									{
										var viewModel = GetAtivoMesViewModel(calculo, includeIcms);
										if ((viewModel.Observacao == null) && (calculo.CreditoImposto != null))
											CreditImposto(viewModel, calculo.CreditoImposto);



										calculoAtivoMes.Add(viewModel);
                                    }

									ativoMes.AtivoMes = calculoAtivoMes;
                                    
                                }

								ativosMes.Add(ativoMes);
                                
							}

							data.AtivosMes = ativosMes;
                            //data.AtivosCompara = ativosCompara;
							data.AllInOne = allativos;
						}

                        var dtosCompara = (medicaoConsolidadoMesDto == null)
                            ? _calculoLivreService.LoadCalculos(ativos, mes, null, tipoEnergia, null, tipoVigenciaCompara, includeIcms, includeImposto, creditIcms, creditImposto, true, false, viewMonths)
                            : new List<CalculoLivreAtivoDto>()
                            {
                                new CalculoLivreAtivoDto()
                                {
                                    Ativo = ativos.First(),
                                    Calculos = new List<CalculoLivreDto>() { GetCalculoLivreDtoBySimulacao(medicaoConsolidadoMesDto, mes, Request["ativo"].ToInt(), Request["agentecon"].ToInt(null), Request["modalidade"], tipoEnergia, includeIcms, includeImposto, creditIcms, creditImposto) }
                                }
                            };

                        if (dtosCompara.Any())
                        {
                            //var ativosMes = new List<AtivosMesViewModel>();
                            var ativosCompara = new List<AtivosMesViewModel>();

                            foreach (var dto in dtosCompara)
                            {
                                //var ativoMes = new AtivosMesViewModel() { Ativo = dto.Ativo };
                                var ativoCompara = new AtivosMesViewModel() { Ativo = dto.Ativo };

                                if (dto.Calculos.Any())
                                {
                                    //var calculoAtivoMes = new List<AtivoMesViewModel>();
                                    var calculoAtivoCompara = new List<AtivoMesViewModel>();

                                    foreach (var calculo in dto.Calculos)
                                    {
                                        var viewModel = GetAtivoMesViewModel(calculo, includeIcms);
                                        if ((viewModel.Observacao == null) && (calculo.CreditoImposto != null))
                                            CreditImposto(viewModel, calculo.CreditoImposto);

                                        //calculoAtivoMes.Add(viewModel);
                                        calculoAtivoCompara.Add(viewModel);
                                    }

                                    //ativoMes.AtivoMes = calculoAtivoMes;
                                    ativoCompara.AtivoCompara = calculoAtivoCompara;
                                }

                                //ativosCompara.Add(ativoMes);
                                ativosCompara.Add(ativoCompara);
                            }

                            //data.AtivosMes = ativosMes;
                            data.AtivosCompara = ativosCompara;
                            //data.AllInOne = allativos;
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

			return AdminContent("FaturaDistribuidora/FaturaDistribuidoraReport.aspx", data);
		}


        public ActionResult Historico(int? Page, bool isactive = true)
        {
            var data = new HistoricoListViewModel();
            var paging = new Page<FaturaDistribuidora>();


            if (UserSession.IsPerfilAgente || UserSession.IsPotencialAgente || UserSession.IsComercializadora)
                paging = _faturaDistribuidoraService.GetAllWithPaging(UserSession.Agentes.Select(i => i.ID.Value), Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);            
            else
                 paging = _faturaDistribuidoraService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);     
            
        
            data.PageNum = paging.CurrentPage;
            data.PageCount = paging.TotalPages;
            data.TotalRows = paging.TotalItems;
            data.FaturasDistribuidora = paging.Items;

            return AdminContent("FaturaDistribuidora/FaturaDistribuidoraHistorico.aspx", data);
        }

        public ActionResult ImportJSON()
        {
            return AdminContent("FaturaDistribuidora/FaturaDistribuidoraImportJSON.aspx");
        }

        public ActionResult ProcessImportJSON()
        {
            var data = new ImportJSONModel();
            var AttIds = Request["AttachmentID"];
            var sobre = Request["SobrescreverExistentes"];
            var processados = _faturaDistribuidoraService.ImportaMedicoesJSON(Request["AttachmentID"], Request["SobrescreverExistentes"].ToBoolean());
            data.Resultado = processados;

            //return AdminContent("FaturaDistribuidora/FaturaDistribuidoraImportJSON.aspx", data);

            //Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
            var textoDisplay = processados.Aprovados.Count() + " Faturas Importadas com Sucesso. " + processados.Reprovados.Count() + " Faturas nao importadas.";
            if (processados.Reprovados.Count() >0 )
            {
                textoDisplay += "(";
                foreach (var reprovadas in processados.Reprovados)
                {
                    textoDisplay += reprovadas.filename + " ";
                }
                textoDisplay += ")";
            }

            Web.SetMessage(textoDisplay);

            var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

            if (Fmt.ConvertToBool(Request["ajax"]))
            {
                //var nextPage = isSaveAndRefresh ? Web.BaseUrl + "Admin/FaturaDistribuidora/Historico" : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/FaturaDistribuidora/Historico";
                var nextPage = Web.BaseUrl + "Admin/FaturaDistribuidora/Historico";
                //return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
                return Json(new { success = true, message = "Faturas Importadas", nextPage });
                //return AdminContent("FaturaDistribuidora/FaturaDistribuidoraImportJSON.aspx", data);
            }

            var previousUrl = Web.AdminHistory.Previous;
            if (previousUrl != null)
                return Redirect(previousUrl);
            return RedirectToAction("Index");
        }


        private void CreditImposto(AtivoMesViewModel ativoMes, CreditoImpostoDto creditoImposto)
		{
			ativoMes.ConsumoPonta *= creditoImposto.TotalImpostosCreditados;
			ativoMes.ConsumoForaPonta *= creditoImposto.TotalImpostosCreditados;
			ativoMes.DemandaPonta *= creditoImposto.TotalImpostosCreditados;
			ativoMes.DemandaForaPonta *= creditoImposto.TotalImpostosCreditados;
			ativoMes.IcmsSt *= (creditoImposto.HasIcms) ? 0 : creditoImposto.TotalImpostosCreditados;
		}

		private void CalcIcmsSt(AtivoMesViewModel viewModel, double mwhTotal, bool includeIcms = false)
		{
			if (includeIcms)
			{
				var devec = _devecService.GetMostRecent(viewModel.Ativo.ID.Value, viewModel.Mes);
				if (devec != null)
				{
					var icms = _icmsVigenciaService.GetByVigencia(viewModel.Ativo, viewModel.Mes);
                    
					if (icms != null)
					{
						viewModel.Icms = icms.AliquotaEnergia;
						viewModel.IcmsSt = (mwhTotal * devec.Preco / (1 - icms.AliquotaEnergia) * icms.AliquotaEnergia);
					}
				}
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
					HasDemandaForaPontaUltrapassagem = (dto.DemandaForaPontaUltrapassagem > 0),
                    SubvencaoTarifaria = dto.SubvencaoTarifaria
                    
				};

				CalcIcmsSt(viewModel, dto.MWhTotal, includeIcms);

				return viewModel;
			}
			return new AtivoMesViewModel() { Mes = dto.Mes, Observacao = dto.Observacao };
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

				return _calculoLivreService.GetCalculoBySimplified(ativo, mes, medicaoConsolidadoConsumoMes, demandaContratada, tarifaVigenciaValor, agenteConectadoID ?? ativo.AgenteConectadoID.Value, null, descontoCcee, true, includeIcms, includePisConfins, creditIcms, creditImposto,true);
			}
			return new CalculoLivreDto();
		}


        public class HistoricoListViewModel
        {
            public List<FaturaDistribuidora> FaturasDistribuidora;
            public long TotalRows;
            public long PageCount;
            public long PageNum;
        }


        public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public bool AllInOne { get; set; }
			public string Impostos { get; set; }
			public string ImpostosCreditados { get; set; }
			public List<AtivosMesViewModel> AtivosMes { get; set; }
            public List<AtivosMesViewModel> AtivosCompara { get; set; }
            public bool HasCustoGeradorTotal
			{
				get
				{
					return (AtivosMes.Any(i => i.AtivoMes.Any(x => x.CustoGeradorTotal > 0)));
				}
			}
		}

		public class AtivosMesViewModel
		{
			public Ativo Ativo { get; set; }
			public List<AtivoMesViewModel> AtivoMes { get; set; }
            public List<AtivoMesViewModel> AtivoCompara{ get; set; }
        }

        public class ImportJSONModel
        {
            public FaturaDistribuidoraImportJSONResultListDto Resultado { get; set; }           
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
            public SubvencaoTarifariaDto SubvencaoTarifaria { get; set; }
            public double Total
			{
				get
				{
                    if (SubvencaoTarifaria ==null )
					    return ConsumoPonta + ConsumoForaPonta + DemandaPonta + DemandaForaPonta + IcmsSt ;
                    else
                        return ConsumoPonta + ConsumoForaPonta + DemandaPonta + DemandaForaPonta + IcmsSt + SubvencaoTarifaria.TotalSubvencaoTarifaria;

                }
			}
			public bool HasDemandaPontaUltrapassagem { get; set; }
			public bool HasDemandaForaPontaUltrapassagem { get; set; }
			public string Observacao { get; set; }
            
		}
	}
}
