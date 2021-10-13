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
    public class GanhoSwapController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IBandeiraService _bandeiraService;
		private readonly IBandeiraCorService _bandeiraCorService;
		private readonly ICalculoCativoService _calculoCativoService;
		private readonly ICalculoLivreService _calculoLivreService;
		private readonly IIcmsVigenciaService _icmsVigenciaService;
		private readonly IMedicaoService _medicaoService;
		private readonly ITarifaVigenciaService _tarifaVigenciaService;
        private readonly IOpcaoImpostoService _opcaoImpostoService;
        private readonly IPerdaService _perdaService;
        private readonly IContratoService _contratoService;
        private readonly IGanhoSwapService _ganhoSwapService;

        public GanhoSwapController(IAtivoService ativoService,
			IBandeiraService bandeiraService,
			IBandeiraCorService bandeiraCorService,
			ICalculoCativoService calculoCativoService,
			ICalculoLivreService calculoLivreService,
			IIcmsVigenciaService icmsVigenciaService,
			IMedicaoService medicaoService,
			ITarifaVigenciaService tarifaVigenciaService,
            IOpcaoImpostoService opcaoImpostoService,
            IPerdaService perdaService,
            IContratoService contratoService,
            IGanhoSwapService ganhoSwapService)
		{
			_ativoService = ativoService;
			_bandeiraService = bandeiraService;
			_bandeiraCorService = bandeiraCorService;
			_calculoCativoService = calculoCativoService;
			_calculoLivreService = calculoLivreService;
			_icmsVigenciaService = icmsVigenciaService;
			_medicaoService = medicaoService;
			_tarifaVigenciaService = tarifaVigenciaService;
            _opcaoImpostoService = opcaoImpostoService;
            _perdaService = perdaService;
            _contratoService = contratoService;
            _ganhoSwapService = ganhoSwapService;
    }

		public ActionResult Index()
		{
            //var data = new ListViewModel();
            var data = getDataSwap(Request["relacao"], Request["ativos"], Request["allativo"], Request["date"], Request["autodate"], Request["vigencia"], Request["viewMonths"], Request["bandeira"]); 
			return AdminContent("GanhoSwap/GanhoSwapReport.aspx", data);
		}


        public ListViewModel getDataSwap(string r_relacao, string r_ativos, string r_allativos,string r_date, string r_autodate, string r_vigencia, string r_viewMonths, string r_bandeira)
        {
            var data = new ListViewModel()
            {
                TipoRelacao = r_relacao ?? PerfilAgente.TiposRelacao.Cliente.ToString()
            };

            if (r_ativos.IsNotBlank())
            {
                var ativos = new List<Ativo>();

                var allativos = r_allativos.ToBoolean();
                if (allativos)
                {
                    if (UserSession.IsCliente)
                        ativos = _ativoService.GetByAgentes(UserSession.Agentes.Select(i => i.ID.Value)).ToList();
                    else
                        ativos = _ativoService.GetAtivosByPerfilAgenteTipo(PerfilAgente.Tipos.Consumidor, data.TipoRelacao).Where(w=>w.IsActive == true).ToList();
                }
                else
                {
                    ativos = _ativoService.GetByConcatnatedIds(r_ativos);
                }

                if (ativos.Any())
                {
                    data.Ativos = ativos;

                    DateTime parsedDate;
                    if (DateTime.TryParse(r_date, out parsedDate))
                    {
                        var mes = Dates.GetFirstDayOfMonth(parsedDate);

                        var autoDate = r_autodate.ToBoolean();
                        if (autoDate)
                        {
                            //aqui
                            var mostRecentMonth = _medicaoService.GetLastMedicaoAtivo(ativos.First());
                            if (mostRecentMonth != null)
                                mes = Dates.GetFirstDayOfMonth(mostRecentMonth.DataLeituraInicio.Value);
                        }

                        var tipoVigencia = r_vigencia;

                        var viewMonths = (allativos) ? 1 : 13;

                        if (r_viewMonths != null && r_viewMonths != "")
                            viewMonths = r_viewMonths.ToInt();

                        //antes disso  verifica se usa creditos de impostos - Hilario 31/03/20 (COVID AGE)
                        var opcaoImposto = _opcaoImpostoService.GetMostRecent(ativos.First().ID.Value);
                        var credICMS = false;
                        var credPisCofins = false;
                        if (opcaoImposto != null)
                        {
                            if (opcaoImposto.TipoCredito == "icms+imposto")
                            {
                                credICMS = true;
                                credPisCofins = true;
                            }
                            else if (opcaoImposto.TipoCredito == "imposto")
                            {
                                credICMS = false;
                                credPisCofins = true;
                            }
                            else if (opcaoImposto.TipoCredito == "icms")
                            {
                                credICMS = true;
                                credPisCofins = false;
                            }
                        }

                        var dtosLivre = _calculoLivreService.LoadCalculos(ativos, mes, null, null, null, tipoVigencia, true,true, credICMS, credPisCofins, false, false, viewMonths, true);
                        if (dtosLivre.Any())
                        {
                            var corBandeira = _bandeiraCorService.GetCor(r_bandeira.ToInt(null));

                            var ativosMes = new List<AtivosMesViewModel>();

                            foreach (var dto in dtosLivre)
                            {
                                var ativoMes = new AtivosMesViewModel() { Ativo = dto.Ativo };

                                if (dto.Calculos.Any())
                                {
                                    var calculoAtivoMes = new List<GanhoSwapMesDto>();

                                    foreach (var calculo in dto.Calculos)
                                    {
                                        //antes disso  verifica se usa creditos de impostos - Hilario 31/03/20 (COVID AGE)
                                        opcaoImposto = _opcaoImpostoService.GetMostRecent(calculo.Ativo.ID.Value);
                                        credICMS = false;
                                        credPisCofins = false;
                                        if (opcaoImposto != null)
                                        {
                                            if (opcaoImposto.TipoCredito == "icms+imposto")
                                            {
                                                credICMS = true;
                                                credPisCofins = true;
                                            }
                                            else if (opcaoImposto.TipoCredito == "imposto")
                                            {
                                                credICMS = false;
                                                credPisCofins = true;
                                            }
                                            else if (opcaoImposto.TipoCredito == "icms")
                                            {
                                                credICMS = true;
                                                credPisCofins = false;
                                            }
                                        }

                                        var dtoCativo = calculo.Observacao != null ? null : _calculoCativoService.GetCalculo(calculo.Ativo, calculo.Mes, null, null, calculo.Vigencia, null, true, true, credICMS, credPisCofins, false, corBandeira);

                                        calculoAtivoMes.Add(_ganhoSwapService.GetPrecoEmpateMes(calculo, dtoCativo, true, true));
                                    }

                                    ativoMes.AtivoMes = calculoAtivoMes.OrderBy(i => i.Mes).ToList();
                                }

                                ativosMes.Add(ativoMes);
                            }

                            data.AtivosMes = ativosMes;
                            data.AllInOne = allativos;
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
                        data.Ativos = new List<Ativo>() { ativo };
                }
            }
            return data;
        }
              

        public JsonResult GetCalculoHtml(string ativos, DateTime mes, int vigencia)
        {
            var getBody = getBodyCalculoSwap(ativos, mes, vigencia);
            return Json(getBody, JsonRequestBehavior.AllowGet);
        }

        public string getBodyCalculoSwap(string ativos, DateTime mes, int vigencia)
        {
            var data = getDataSwap("Cliente",ativos,"False",mes.ToString(),"off",vigencia.ToString(), "1","");
            var ativoMes = data.AtivosMes.First().AtivoMes.First();
            var memorialCativo = ativoMes.dtoCativo.Memorial;
            var memorialLivre = ativoMes.dtoLivre.Memorial;

            double CreditosImpostos = 0;
            double TotalBruto = 0;


            var body = "";
            body += "<table class='main-table'>";
            body += "<tr class='main-table-row'>";
            body += "<td class='main-table-column'>";
            //Parte do Cativo
            body += "<i style='font-size:14px'>Cativo</i>";
            body += "<table class='mercado-table'>";            
            body += "<tr class='mercado-table-header-row'>";
            body += "<td class='mercado-table-header-column'>Item</td>";
            body += "<td class='mercado-table-header-column'>Montante</td>";            
            body += "<td class='mercado-table-header-column'>Valor Bruto</td>";
            body += "<td class='mercado-table-header-column'>Creditos</td>";
            body += "<td class='mercado-table-header-column'>Tot.Liquido</td>";
            body += "</tr>";
            //Itens           

            body += "<tr class='mercado-table-row'>";
            body += "<td class='mercado-table-column'>Demanda</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.MontanteDemandaForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.ValorDemandaForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialCativo.FaturaCativo.ImpostosTarifaDemandaForaPonta * memorialCativo.FaturaCativo.MontanteDemandaForaPonta * 1000) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialCativo.FaturaCativo.ValorDemandaForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            body += "<tr>";
            body += "<td class='mercado-table-column'>TUSD P</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.MontanteTUSDPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.ValorTUSDPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialCativo.FaturaCativo.ImpostosTarifaTUSDPonta * memorialCativo.FaturaCativo.MontanteTUSDPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialCativo.FaturaCativo.ValorTUSDPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            body += "<tr>";
            body += "<td class='mercado-table-column'>TUSD FP</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.MontanteTUSDForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.ValorTUSDForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialCativo.FaturaCativo.ImpostosTarifaTUSDForaPonta * memorialCativo.FaturaCativo.MontanteTUSDForaPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialCativo.FaturaCativo.ValorTUSDForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            body += "<tr>";
            body += "<td class='mercado-table-column'>TE P</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.MontanteEnergiaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.ValorEnergiaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialCativo.FaturaCativo.ImpostosTarifaEnergiaPonta * memorialCativo.FaturaCativo.MontanteEnergiaPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialCativo.FaturaCativo.ValorEnergiaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            body += "<tr>";
            body += "<td class='mercado-table-column'>TE FP</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.MontanteEnergiaForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.ValorEnergiaForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialCativo.FaturaCativo.ImpostosTarifaEnergiaForaPonta * memorialCativo.FaturaCativo.MontanteEnergiaForaPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialCativo.FaturaCativo.ValorEnergiaForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            body += "<tr>";
            body += "<td class='mercado-table-column'>Bandeira</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.MontanteBandeira.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialCativo.FaturaCativo.ValorBandeira.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialCativo.FaturaCativo.ImpostosTarifaBandeira * memorialCativo.FaturaCativo.MontanteBandeira) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialCativo.FaturaCativo.ValorBandeira + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            body += "<tr>";
            body += "<td class='mercado-table-column-footer'>Total</td>";
            body += "<td class='mercado-table-column-footer'></td>";
            TotalBruto = memorialCativo.FaturaCativo.ValorDemandaForaPonta + memorialCativo.FaturaCativo.ValorTUSDPonta + memorialCativo.FaturaCativo.ValorTUSDForaPonta + memorialCativo.FaturaCativo.ValorEnergiaPonta + memorialCativo.FaturaCativo.ValorEnergiaForaPonta + memorialCativo.FaturaCativo.ValorBandeira;
            body += "<td class='mercado-table-column-footer'>" + TotalBruto.ToString("C2") + "</td>";
            CreditosImpostos = memorialCativo.CreditoImpostos.TotalCreditos * -1;
            body += "<td class='mercado-table-column-footer'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column-footer'>" + memorialCativo.TotalCativo.ToString("C") + "</td>";
            body += "</tr>";
            body += "</table>";

            body += "</td>";
            body += "<td>";

            //Parte do Livre    
            //Convencional
            body += "<i style = 'font-size:14px'> Livre </i>";            
            body += "<table class='mercado-table'>";
            body += "<tr>";
            body += "<td colspan='5' class='mercado-table-header-column'>Convencional</td>";
            body += "</tr>";
            body += "<tr>";
            body += "<td class='mercado-table-column'>Item</td>";
            body += "<td class='mercado-table-column'>Montante</td>";
            body += "<td class='mercado-table-column'>Valor Bruto</td>";
            body += "<td class='mercado-table-column'>Creditos</td>";
            body += "<td class='mercado-table-column'>Tot.Liquido</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Demanda</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteDemandaForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorDemandaForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaDemandaForaPonta * memorialLivre.FaturaLivre.MontanteDemandaForaPonta * 1000) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorDemandaForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";           

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Energia P</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteTUSDPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorTUSDPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaTUSDPonta * memorialLivre.FaturaLivre.MontanteTUSDPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorTUSDPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Energia FP</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteTUSDForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorTUSDForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaTUSDForaPonta * memorialLivre.FaturaLivre.MontanteTUSDForaPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorTUSDForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Subvenção</td>";
            body += "<td class='mercado-table-column'></td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorSubvencaoTarifaria.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ValorSubvencaoTarifaria) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorSubvencaoTarifaria + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Total</td>";
            body += "<td class='mercado-table-column'></td>";
            TotalBruto = memorialLivre.FaturaLivre.ValorDemandaForaPonta + memorialLivre.FaturaLivre.ValorTUSDPonta + memorialLivre.FaturaLivre.ValorTUSDForaPonta + memorialLivre.FaturaLivre.ValorSubvencaoTarifaria;
            body += "<td class='mercado-table-column'>" + TotalBruto.ToString("C") + "</td>";
            CreditosImpostos = memorialLivre.CreditoImpostos.TotalCreditoImpostos;
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.TotalLivre).ToString("C") + "</td>";
            body += "</tr>";
            body += "</td>";
            body += "</tr>";
            body += "</table>";


            body += "<table class='mercado-table'>";
            body += "<tr>";
            body += "<td class='mercado-table-column'>Montante</td>";
            body += "<td class='mercado-table-column'>" + ativoMes.MWhTotal.ToString("N3") + "</td>";
            body += "<td class='mercado-table-column'>Preço Empate</td>";
            body += "<td class='mercado-table-column'>" +  ativoMes.Valor0Perc.ToString("C") + "</td>";
            body += "</tr>";
            body += "</table>";

            //memorialLivre = ativoMes.dtoLivre.Memorial;
            memorialLivre = _calculoLivreService.ApplyDescontoCcee(ativoMes.dtoLivre, 0.5, true, true).Memorial;

            //50%
            body += "<table class='mercado-table'>";
            body += "<tr>";
            body += "<td colspan='5' class='mercado-table-header-column'>Incentivada 50%</td>";
            body += "</tr>";
            body += "<tr>";
            body += "<td class='mercado-table-column'>Item</td>";
            body += "<td class='mercado-table-column'>Montante</td>";
            body += "<td class='mercado-table-column'>Valor Bruto</td>";
            body += "<td class='mercado-table-column'>Creditos</td>";
            body += "<td class='mercado-table-column'>Tot.Liquido</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Demanda</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteDemandaForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorDemandaForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaDemandaForaPonta * memorialLivre.FaturaLivre.MontanteDemandaForaPonta * 1000) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorDemandaForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Energia P</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteTUSDPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorTUSDPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaTUSDPonta * memorialLivre.FaturaLivre.MontanteTUSDPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorTUSDPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Energia FP</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteTUSDForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorTUSDForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaTUSDForaPonta * memorialLivre.FaturaLivre.MontanteTUSDForaPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorTUSDForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Subvenção</td>";
            body += "<td class='mercado-table-column'></td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorSubvencaoTarifaria.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ValorSubvencaoTarifaria) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorSubvencaoTarifaria + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Total</td>";
            body += "<td class='mercado-table-column'></td>";
            TotalBruto = memorialLivre.FaturaLivre.ValorDemandaForaPonta + memorialLivre.FaturaLivre.ValorTUSDPonta + memorialLivre.FaturaLivre.ValorTUSDForaPonta + memorialLivre.FaturaLivre.ValorSubvencaoTarifaria;
            body += "<td class='mercado-table-column'>" + TotalBruto.ToString("C") + "</td>";
            CreditosImpostos = memorialLivre.CreditoImpostos.TotalCreditoImpostos;
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.TotalLivre).ToString("C") + "</td>";
            body += "</tr>";
            body += "</td>";
            body += "</tr>";
            body += "</table>";


            body += "<table class='mercado-table'>";
            body += "<tr>";
            body += "<td class='mercado-table-column'>Montante</td>";
            body += "<td class='mercado-table-column'>" + ativoMes.MWhTotal.ToString("N3") + "</td>";
            body += "<td class='mercado-table-column'>Preço Empate</td>";
            body += "<td class='mercado-table-column'>" + ativoMes.Valor50Perc.ToString("C") + "</td>";
            body += "</tr>";
            body += "</table>";

            //memorialLivre = ativoMes.dtoLivre.Memorial;
            memorialLivre = _calculoLivreService.ApplyDescontoCcee(ativoMes.dtoLivre,1, true, true).Memorial;

            //100%           
            
            body += "<table class='mercado-table'>";
            body += "<tr>";
            body += "<td colspan='5' class='mercado-table-header-column'>Incentivada 100%</td>";
            body += "</tr>";
            body += "<tr>";
            body += "<td class='mercado-table-column'>Item</td>";
            body += "<td class='mercado-table-column'>Montante</td>";
            body += "<td class='mercado-table-column'>Valor Bruto</td>";
            body += "<td class='mercado-table-column'>Creditos</td>";
            body += "<td class='mercado-table-column'>Tot.Liquido</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Demanda</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteDemandaForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorDemandaForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaDemandaForaPonta * memorialLivre.FaturaLivre.MontanteDemandaForaPonta * 1000) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorDemandaForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Energia P</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteTUSDPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorTUSDPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaTUSDPonta * memorialLivre.FaturaLivre.MontanteTUSDPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorTUSDPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Energia FP</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.MontanteTUSDForaPonta.ToString("N2") + "</td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorTUSDForaPonta.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ImpostosTarifaTUSDForaPonta * memorialLivre.FaturaLivre.MontanteTUSDForaPonta) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorTUSDForaPonta + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Subvenção</td>";
            body += "<td class='mercado-table-column'></td>";
            body += "<td class='mercado-table-column'>" + memorialLivre.FaturaLivre.ValorSubvencaoTarifaria.ToString("C2") + "</td>";
            CreditosImpostos = ((memorialLivre.FaturaLivre.ValorSubvencaoTarifaria) * ativoMes.dtoCativo.CreditoImposto.CreditoImpostoInTotal.AproveitamentoOpcaoImpostoIcms.ToDouble() * -1);
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.FaturaLivre.ValorSubvencaoTarifaria + CreditosImpostos).ToString("C") + "</td>";
            body += "</tr>";

            //Itens  
            body += "<tr>";
            body += "<td class='mercado-table-column'>Total</td>";
            body += "<td class='mercado-table-column'></td>";
            TotalBruto = memorialLivre.FaturaLivre.ValorDemandaForaPonta + memorialLivre.FaturaLivre.ValorTUSDPonta + memorialLivre.FaturaLivre.ValorTUSDForaPonta + memorialLivre.FaturaLivre.ValorSubvencaoTarifaria;
            body += "<td class='mercado-table-column'>" + TotalBruto.ToString("C") + "</td>";
            CreditosImpostos = memorialLivre.CreditoImpostos.TotalCreditoImpostos;
            body += "<td class='mercado-table-column'>" + CreditosImpostos.ToString("C") + "</td>";
            body += "<td class='mercado-table-column'>" + (memorialLivre.TotalLivre).ToString("C") + "</td>";
            body += "</tr>";
            body += "</td>";
            body += "</tr>";
            body += "</table>";


            body += "<table class='mercado-table'>";
            body += "<tr>";
            body += "<td class='mercado-table-column'>Montante</td>";
            body += "<td class='mercado-table-column'>" + ativoMes.MWhTotal.ToString("N3") + "</td>";
            body += "<td class='mercado-table-column'>Preço Empate</td>";
            body += "<td class='mercado-table-column'>" + ativoMes.Valor100Perc.ToString("C") + "</td>";
            body += "</tr>";
            body += "</table>";

            body += "<table class='mercado-table'>";
            body += "<tr>";
            body += "<td class='mercado-table-column'>Ganho Swap</td>";           
            body += "<td class='mercado-table-column'>" + ativoMes.Diferenca.ToString("C") + "</td>";
            body += "</tr>";
            body += "</table>";
            return body;
        }




		public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public bool AllInOne { get; set; }
			public string TipoRelacao { get; set; }
			public List<AtivosMesViewModel> AtivosMes { get; set; }
			public string GetMeses()
			{
				if (AtivosMes.Any())
					return AtivosMes.First().AtivoMes.Select(i => i.Mes.ToString("MMMM/yy")).Join("','");
				return null;
			}
		}

		public class AtivosMesViewModel
		{
			public Ativo Ativo { get; set; }
			public List<GanhoSwapMesDto> AtivoMes { get; set; }
		}		
	}
}
