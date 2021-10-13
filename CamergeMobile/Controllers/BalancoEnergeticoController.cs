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
    public class BalancoEnergeticoController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IAtivoService _ativoService;
		private readonly IBalancoEnergeticoReportService _balancoEnergeticoReportService;
		private readonly IMedicaoConsolidadoService _medicaoConsolidadoService;
		private readonly ISazonalizacaoService _sazonalizacaoService;
        private readonly IPerfilAgenteService _perfilAgenteService;
        private readonly IContratoService _contratoService;

		public BalancoEnergeticoController(IAgenteService agenteService,
			IAtivoService ativoService,
			IBalancoEnergeticoReportService balancoEnergeticoReportService,
			IMedicaoConsolidadoService medicaoConsolidadoService,
			ISazonalizacaoService sazonalizacaoService,
            IPerfilAgenteService perfilAgenteService,
            IContratoService contratoService)
		{
			_ativoService = ativoService;
			_agenteService = agenteService;
			_balancoEnergeticoReportService = balancoEnergeticoReportService;
			_medicaoConsolidadoService = medicaoConsolidadoService;
			_sazonalizacaoService = sazonalizacaoService;
            _perfilAgenteService = perfilAgenteService;
            _contratoService = contratoService;
		}

		//
		// GET: /Admin/BalancoEnergetico/
		public ActionResult Index()
		{
			var data = new ListViewModel();
			var forceReload = Request["forceReload"].ToBoolean();
			var energiaComercializada = Request["energiacomerc"].ToDouble(null);

			if (UserSession.IsPerfilAgente)
			{
				data.TipoLeitura = _agenteService.AgentesHasGerador(UserSession.Agentes) ? "Geracao" : "Consumo";
			}

			if (Request["ativos"].IsNotBlank())
			{
				data.Ativos = AtivoList.Load(new SqlQuery("WHERE id IN (").AddParameter(Request["ativos"], SqlQuery.SqlParameterType.IntList).Add(")"));

				if (CheckAtivosAreValid(data.Ativos))
				{
					DateTime parsedDate;
					if ((data.Ativos.Any()) && (DateTime.TryParse(Request["date"], out parsedDate)))
					{
						parsedDate = Dates.GetFirstDayOfMonth(parsedDate);

						if (data.Ativos.Any(ativo => UserSession.LoggedInUserCanSeeAtivo(ativo)))
						{
							// consumidor não tem garantia física.
							// data.HasGarantiaFisica = data.Ativos.Any(i => i.GarantiaFisicaList.Any());

							if ((energiaComercializada == null) && (data.Ativos.Count() == 1))
								energiaComercializada = GetValueEnergiaComercializada(data.Ativos.First(), parsedDate);

							data.TipoLeitura = (data.TipoLeitura) ?? (data.Ativos.First().PerfilAgente.IsGerador ? Medicao.TiposLeitura.Geracao.ToString() : Medicao.TiposLeitura.Consumo.ToString());
							data.IsParticipanteMre = data.Ativos.Any(i => i.IsGerador && i.ParticipanteMre == true);
							data.BalancosEnergeticoSemana = _balancoEnergeticoReportService.LoadBalancosEnergeticoSemana(data.Ativos, parsedDate, forceReload);
							data.TiposPrecoBalancoEnergeticoSemana = _balancoEnergeticoReportService.LoadTiposPrecoBalancoEnergeticoSemana(data.Ativos, parsedDate, forceReload, energiaComercializada);
						}
						else
						{
							data.Ativos = new List<Ativo>();
							Response.StatusCode = 403;
						}
					}
				}
				else
				{
					Web.SetMessage("Os ativos selecionados devem ser todos do tipo consumidor ou todos do tipo gerador que contenham garantia física ou apenas um.", "error");
				}
			}
			else
			{
				if (UserSession.Agentes != null)
				{
					var ativo = _ativoService.GetByAgentes(UserSession.Agentes, PerfilAgente.TiposRelacao.Cliente.ToString());
					if (ativo != null)
						data.Ativos = new List<Ativo>() { ativo };
				}
			}

			if (forceReload)
			{
				return Redirect(Fmt.RemoveFromQueryString(Web.FullUrl, "forceReload"));
			}

			return AdminContent("BalancoEnergetico/BalancoEnergeticoReport.aspx", data);
		}

        public ActionResult Diario()
        {
            var data = new ListViewModelDiario();
            var perfilAgenteList = new List<PerfilAgente>();

            if (Request["perfilagente"] != null)
            {
                var perfilagente = _perfilAgenteService.FindByID(Request["perfilagente"].ToInt(0));
                var agente = perfilagente.Agente;
                //teste por agentes, mas sem validade agora - 26/02/2021
                perfilAgenteList.AddItems(agente.PerfilAgenteList.Where(w=>w.TipoRelacao=="Cliente" && w.IsGeradorGD == false && w.AtivoList.Count()>0));
                if (Request["date"] != null)
                {

                    var mes = Dates.GetFirstDayOfMonth(Convert.ToDateTime(Request["date"]));
                    //var perfilAgente = _perfilAgenteService.FindByID(Request["perfilAgente"].ToInt(0));
                    var listContratos = new List<ContratoReportDto>();
                    var agenteList = new List<Agente>();
                    agenteList.Add(agente);

                    listContratos = _contratoService.GetReport(mes, true, agenteList, false, null, null, null, null, null, false);
                    double montanteFlat = 0;
                    if (Request["montanteflat"] != null && Request["montanteflat"] != "")
                        montanteFlat = Convert.ToDouble(Request["montanteflat"]);

                    data.balancoEnergeticoDiario = _balancoEnergeticoReportService.balancoEnergeticoDiario(mes, perfilagente, listContratos, montanteFlat);
                    //var testeBalancoList = _balancoEnergeticoReportService.balancoEnergeticoDiarioList(mes, perfilAgenteList, listContratos, montanteFlat);
                }
            }               

            return AdminContent("BalancoEnergetico/BalancoEnergeticoDiarioReport.aspx", data);
        }

        public JsonResult GetOptionsView(int perfilagenteID)
        {
            var perfilagente = _perfilAgenteService.FindByID(perfilagenteID.ToInt(0));
            var agente = perfilagente.Agente;

            var body = "";
            body += "<table>";
            body += "<tr>";
            body += "<td>";
            body += "<div>";
            body += "<fieldset class='option-fieldset'>";
            body += "<legend class='label-form-options'>Perfis de Agentes</legend>";
            foreach (var perfil in agente.PerfilAgenteList.Where(w=>w.TipoRelacao == "Cliente" && w.IsGeradorGD == false && w.AtivoList.Count() > 0))
            {
                body += "<input type='checkbox' id='chk" + perfil.ID + "' name='chk" + perfil.ID + "' value='" + perfil.ID + "' checked /> ";
                body += perfil.Sigla + "<br />";
            }
            body += "</fieldset>";
            body += "</div>";
            body += "</td>";
            body += "</tr>";
            body += "</table>";


            return Json(body, JsonRequestBehavior.AllowGet);
        }

            public JsonResult GetBalancoDiaView(string dia, int perfilAgenteID)
        {   
            var mes = Dates.GetFirstDayOfMonth(Convert.ToDateTime(dia));
            var perfilAgente = _perfilAgenteService.FindByID(perfilAgenteID.ToInt(0));
            var listContratos = new List<ContratoReportDto>();
            var agenteList = new List<Agente>();
            agenteList.Add(perfilAgente.Agente);

            listContratos = _contratoService.GetReport(mes, false, agenteList, false, null, null, null, null, null, false);

            var balancoEnergeticoDiario = _balancoEnergeticoReportService.balancoEnergeticoDiario(mes, perfilAgente, listContratos,0);
            var calendario = balancoEnergeticoDiario.calendario;

            var diaCalendario = calendario.Where(w => w.date == Convert.ToDateTime(dia));
            var body = "";
            body += "<label>Dia: " + dia + " Agente: " + perfilAgente.Sigla + "</label><br />";
            body += "<table class='view-table-border'>";
            body += "<tr class='view-block-row'>";

            body += "<td class='view-block-column'>";
            body += "Hora";
            body += "</td>";   

            body += "<td class='view-block-column'>";
            body += "Energia Consolidada";
            body += "</td>";

            body += "<td class='view-block-column'>";
            body += "Energia Prevista";
            body += "</td>";

            body += "<td class='view-block-column'>";
            body += "Perdas";
            body += "</td>";           

            body += "<td class='view-block-column'>";
            body += "Requisito Total";
            body += "</td>";

            body += "<td class='view-block-column'>";
            body += "Recurso Total";
            body += "</td>";

            body += "<td class='view-block-column'>";
            body += "Valor PLD Hora";
            body += "</td>";

            body += "<td class='view-block-column'>";
            body += "Diferença MWH";
            body += "</td>";

            body += "<td class='view-block-column'>";
            body += "Diferença R$";
            body += "</td>";

            body += "</tr>";

            foreach (var hora in diaCalendario.First().balancoHoras)
            {
                body += "<tr class='view-block-row'>";

                body += "<td class='view-block-column'>";
                body += hora.hora.ToString("HH:mm");
                body += "</td>";               

                body += "<td class='view-block-column'>";
                body += hora.requisitos.energiaConsolidada.ToString("N3");
                body += "</td>";

                body += "<td class='view-block-column'>";
                body += hora.requisitos.energiaPrevista.ToString("N3");
                body += "</td>";

                body += "<td class='view-block-column'>";
                body += hora.requisitos.montantePerdas.ToString("N3");
                body += "</td>";
               

                body += "<td class='view-block-column'>";
                body += hora.requisitosTotais.ToString("N3");
                body += "</td>";

                body += "<td class='view-block-column'>";
                body += hora.recursosTotais.ToString("N3");
                body += "</td>";

                body += "<td class='view-block-column'>";
                body += hora.PLDHora.ToString("C");
                body += "</td>";

                body += "<td class='view-block-column'>";
                body += hora.diferenca.ToString("C");
                body += "</td>";

                body += "<td class='view-block-column'>";
                body += hora.valorDiferenca.ToString("C");
                body += "</td>";

                body += "</tr>";
            }
            body += "</table>";

            return Json(body, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Consolidated()
		{
			var data = new ConsolidatedViewModel();

			if (UserSession.IsPerfilAgente)
			{
				data.TipoLeitura = Medicao.TiposLeitura.Geracao.ToString();
			}

			DateTime parsedDate;
			if (DateTime.TryParse(Request["date"], out parsedDate))
			{
				parsedDate = Dates.GetFirstDayOfMonth(parsedDate);

				data.Ativos = _ativoService.GetNotConsumidores().ToList();
				if (data.Ativos.Any(ativo => UserSession.LoggedInUserCanSeeAtivo(ativo)))
				{
					data.BalancosEnergeticoConsolidado = _balancoEnergeticoReportService.GetBalancosEnergeticoConsolidado(data.Ativos, parsedDate, false, data.TipoLeitura);
				}
				else
				{
					Response.StatusCode = 403;
				}
			}

			return AdminContent("BalancoEnergetico/BalancoEnergeticoConsolidadoReport.aspx", data);
		}

		public JsonResult GetEnergiaComercializada(string ids, DateTime date)
		{
			double? energiaComercializada = null;

			var singleId = false;

			var id = Request["ids"];
			if (id.IsNotBlank())
			{
				singleId = (!id.Contains(','));

				int ativoID;
				if ((singleId) && (int.TryParse(id, out ativoID)))
				{
					var ativo = _ativoService.FindByID(ativoID);
					if (ativo != null)
					{
						energiaComercializada = GetValueEnergiaComercializada(ativo, date);
					}
				}
			}

			if (energiaComercializada == null)
				return Json(null, JsonRequestBehavior.AllowGet);
			return Json(energiaComercializada.Value.ToString("N3"), JsonRequestBehavior.AllowGet);
		}

        public JsonResult GetEnergiaDisponivelPerfilAgente(string ids, DateTime date)
        {
            //mudado pra pegar de todos os perfis de agente - 06/04/21
            double energiaComercializada = 0;           
            var perfilAgente = _perfilAgenteService.FindByID(Request["ids"].ToInt(0));
            if (perfilAgente != null)            
                foreach (var perfil in perfilAgente.Agente.PerfilAgenteList)                
                    foreach (var ativo in perfil.AtivoList)                    
                        energiaComercializada += Convert.ToDouble(GetValueEnergiaComercializada(ativo, date));

            if (energiaComercializada == 0)
                return Json("0", JsonRequestBehavior.AllowGet);
            return Json(energiaComercializada.ToString("N3"), JsonRequestBehavior.AllowGet);
        }



        public JsonResult GetEnergiaVendidaemContratosPerfilAgente(string ids, DateTime date)
        {
            var perfilAgente = _perfilAgenteService.FindByID(Request["ids"].ToInt(0));
            var agenteList = new List<Agente>();
            agenteList.Add(perfilAgente.Agente);
            var contratos = _contratoService.GetReport(date, false, agenteList, false, null, null, null, null, null, false);
            double energiaVendidaContratos = Convert.ToDouble(contratos.Where(w=>w.PerfilAgenteVendedor.AgenteID == perfilAgente.AgenteID).Sum(s => s.MontanteApuracao.MontanteApuracao));
            energiaVendidaContratos -= Convert.ToDouble(contratos.Where(w => w.PerfilAgenteComprador.AgenteID == perfilAgente.AgenteID).Sum(s => s.MontanteApuracao.MontanteApuracao));

            if (energiaVendidaContratos == 0)
                return Json("0", JsonRequestBehavior.AllowGet);
            return Json(energiaVendidaContratos.ToString("N3"), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetEnergiaDisponivelAgente(string ids, DateTime date)
        {
            double energiaComercializada = 0;
            var agente = _agenteService.FindByID(Request["ids"].ToInt(0));
            if (agente != null)
            {
                foreach( var perfilAgente in agente.PerfilAgenteList)
                {
                    if (perfilAgente != null)
                    {
                        foreach (var ativo in perfilAgente.AtivoList)
                        {
                            energiaComercializada += Convert.ToDouble(GetValueEnergiaComercializada(ativo, date));

                        }
                    }

                }
            }

            if (energiaComercializada == 0)
                return Json(null, JsonRequestBehavior.AllowGet);
            return Json(energiaComercializada.ToString("N3"), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetEnergiaVendidaemContratosAgente(string ids, DateTime date)
        {
            var agente = _agenteService.FindByID(Request["ids"].ToInt(0));
            var agenteList = new List<Agente>();
            agenteList.Add(agente);
            var contratos = _contratoService.GetReport(date, false, agenteList, false, null, null, null, null, null, false);
            contratos = contratos.Where(w => w.PerfilAgenteVendedor.AgenteID == agente.ID).ToList();
            double energiaVendidaContratos = Convert.ToDouble(contratos.Sum(s => s.MontanteApuracao.MontanteApuracao));

            if (energiaVendidaContratos == 0)
                return Json("0", JsonRequestBehavior.AllowGet);
            return Json(energiaVendidaContratos.ToString("N3"), JsonRequestBehavior.AllowGet);
        }


        private bool CheckAtivosAreValid(List<Ativo> ativos)
		{
			if (ativos.Any())
			{
				int count = ativos.Count();

				if (count == 1)
					return true;
				else if (ativos.Count(i => i.IsConsumidor) == count)
					return true;
				else if ((ativos.Count(i => i.IsGerador) == count)
					&& (ativos.Count(i => i.GarantiaFisicaList.Any()) == count))
					return true;
			}
			return false;
		}

		public double? GetValueEnergiaComercializada(Ativo ativo, DateTime date)
		{
			double? energiaComercializada = null;

			if (ativo.PerfilAgente.IsGerador)
			{
				if ((ativo.GarantiaFisicaList.Any()) && (ativo.SazonalizacaoList.Any()))
				{
					var sazonalizacao = ativo.SazonalizacaoList.OrderBy(i => i.MesFimVigencia).FirstOrDefault(i => i.MesFimVigencia >= date);
					if (sazonalizacao != null)
					{
						sazonalizacao = _sazonalizacaoService.GetSazonalizacaoWithPerdasGF(sazonalizacao);

						energiaComercializada = sazonalizacao.LastroEnergia;
					}
				}
				else
				{
					// Format JS date
					date = new DateTime(date.Year, date.Month, 1);

					var medicao = _medicaoConsolidadoService.GetMedicaoMes(ativo, date, null);
					if ((medicao != null) && ((medicao.MedicaoConsumo != null) && (medicao.MedicaoGeracao != null)))
					{
						var totalHorasMes = (medicao.MedicaoGeracao.QtdeHorasLeve + medicao.MedicaoGeracao.QtdeHorasMedio + medicao.MedicaoGeracao.QtdeHorasPesado);

						energiaComercializada = ((medicao.MedicaoGeracao.MWhTotal - medicao.MedicaoConsumo.MwhTotal) / totalHorasMes);
					}
				}
			}

			return energiaComercializada;
		}

		public class ListViewModel
		{
			public bool IsParticipanteMre;
			public string TipoLeitura;
			public List<Ativo> Ativos = new List<Ativo>();
			public List<BalancoEnergeticoSemanaDto> BalancosEnergeticoSemana;
			public List<TipoPrecoBalancoEnergeticoSemanaDto> TiposPrecoBalancoEnergeticoSemana;
		}

        public class ListViewModelDiario
        {
            public BalancoEnergeticoDiarioDto balancoEnergeticoDiario;
        }        

        public class ConsolidatedViewModel
		{
			public string TipoLeitura;
			public List<Ativo> Ativos = new List<Ativo>();
			public List<BalancoEnergeticoConsolidadoDto> BalancosEnergeticoConsolidado;
		}
	}
}
