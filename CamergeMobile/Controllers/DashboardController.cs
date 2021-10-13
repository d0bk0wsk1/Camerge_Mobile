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
    public class DashboardController : ControllerBase
    {
        private readonly IAcessoRapidoService _acessoRapidoService;
        private readonly IAtivoService _ativoService;
        private readonly IEventoService _eventoService;
        private readonly IMedidorService _medidorService;
        private readonly IMedicaoErroService _medicaoErroService;
        private readonly IPerfilAgenteService _perfilAgenteService;
        private readonly IRelatorioEconomiaService _relatorioEconomiaService;
        private readonly IAgenteService _agenteService;
        private readonly IMedicaoHorarioReportService _medicaoDiarioDetailedReportService;
        private readonly IGarantiaFisicaService _garantiaFisicaService;
        private readonly IMedicaoMensalReportService _medicaoMensalReportService;
        private readonly IAcompanhamentoAsseguradaReportService _acompanhamentoAsseguradaReportService;
        private readonly IMedicaoAnualReportService _medicaoAnualReportService;

        public DashboardController(IAcessoRapidoService acessoRapidoService,
            IAtivoService ativoService,
            IEventoService eventoService,
            IMedidorService medidorService,
            IMedicaoErroService medicaoErroService,
            IPerfilAgenteService perfilAgenteService,
            IRelatorioEconomiaService relatorioEconomiaService,
            IAgenteService agenteService,
            IMedicaoHorarioReportService medicaoHorarioReportService,
            IGarantiaFisicaService garantiaFisicaService, 
            IMedicaoMensalReportService medicaoMensalReportService,
            IAcompanhamentoAsseguradaReportService acompanhamentoAsseguradaReportService,
            IMedicaoAnualReportService medicaoAnualReportService)
        {
            _acessoRapidoService = acessoRapidoService;
            _ativoService = ativoService;
            _eventoService = eventoService;
            _medidorService = medidorService;
            _medicaoErroService = medicaoErroService;
            _perfilAgenteService = perfilAgenteService;
            _relatorioEconomiaService = relatorioEconomiaService;
            _agenteService = agenteService;
            _medicaoDiarioDetailedReportService = medicaoHorarioReportService;
            _garantiaFisicaService = garantiaFisicaService;
            _medicaoMensalReportService = medicaoMensalReportService;
            _acompanhamentoAsseguradaReportService = acompanhamentoAsseguradaReportService;
            _medicaoAnualReportService = medicaoAnualReportService;
        }

        //
        // GET: /Home/
        public ActionResult Index()
        {
            var data = new ViewModel();

            /// data.MedidoresSemComunicacao = _medidorService.GetMedidoresSemComunicacao(UserSession.IsPerfilAgente ? UserSession.Agente.PerfilAgenteList : null);
            data.AcessosRapido = _acessoRapidoService.Get(true);
            data.AtivosSemComunicacao = _medidorService.GetAtivosSemComunicacao(UserSession.IsPerfilAgente ? _perfilAgenteService.GetByAgentes(UserSession.Agentes) : null);
            data.UltimaMedicaoLida = _medidorService.GetUltimaMedicaoLida();
            data.AuditoriaCount = _medicaoErroService.GetPendentesCount();

            if ((UserSession.IsDeveloper) || (UserSession.IsAdmin) || (UserSession.IsAnalista))
                data.Eventos = GetEventos(null);

            if (UserSession.IsPerfilAgente)
            {
                foreach (var perfilAgente in _perfilAgenteService.GetByAgentes(UserSession.Agentes))
                    foreach (var ativo in perfilAgente.AtivoList)
                        data.HasMedidores = ativo.MedidorList.Any();

                data.Eventos = GetEventos(UserSession.Agentes);

                data.HasRelatorioEconomia = (_relatorioEconomiaService.Count(_ativoService.GetByAgentes(UserSession.Agentes.Select(i => i.ID.Value))) > 0);
            }
            else
            {
                data.HasMedidores = true;
            }

            return AdminContent("Dashboard/Dashboard.aspx", data);
        }

        private List<EventoDto> GetEventos(IEnumerable<Agente> agentes)
        {
            var list = new List<EventoDto>();

            var dtIni = Dates.ToInitialHours(DateTime.Today);
            var dtFim = Dates.ToFinalHours(dtIni);



            if ((agentes == null) || (!agentes.Any()))
            {
                // list.AddRange(_eventoService.GetDtoByCategoria(null, null, dtIni, dtFim.AddDays(30), true));
                list.AddRange(_eventoService.GetDtoByCategoria(null, null, dtIni, dtFim.AddDays(90), false));
                // list.AddRange(_eventoService.GetDtoByAgente(null, dtIni, dtFim.AddDays(30), true));
                list.AddRange(_eventoService.GetDtoByAgente(null, dtIni, dtFim.AddDays(90), false));
                //agentes = _agenteService.GetAll().Where(w => w.DataInicioVigencia != null && w.IsActive);



                var eventosCategoria = new List<EventoDto>();
                eventosCategoria.AddRange(_eventoService.GetDtoByContrato(0, dtIni, dtFim.AddDays(35)));
                if (eventosCategoria.Any())
                {
                    //eventosCategoria.ForEach(delegate (EventoDto dto) { dto.Detailed = string.Format("{0} ({1})", dto.Detailed, agente.Sigla); });
                    list.AddRange(eventosCategoria);
                }

            }
            else
            {
                foreach (var agente in agentes)
                {
                    var perfilAgente = agente.PerfilAgenteList.FirstOrDefault();
                    if (perfilAgente != null)
                    {
                        var categoria = perfilAgente.Tipo;
                        if (categoria != null)
                        {
                            var eventosCategoria = new List<EventoDto>();

                            eventosCategoria.AddRange(_eventoService.GetDtoByCategoria(agente.ID.Value, categoria, dtIni, dtFim.AddDays(30), true));
                            eventosCategoria.AddRange(_eventoService.GetDtoByCategoria(agente.ID.Value, categoria, dtIni, dtFim.AddDays(90), false));
                            eventosCategoria.AddRange(_eventoService.GetDtoByContrato(agente.ID.Value, dtIni, dtFim.AddDays(35)));

                            if (eventosCategoria.Any())
                            {
                                eventosCategoria.ForEach(delegate (EventoDto dto) { dto.Detailed = string.Format("{0} ({1})", dto.Detailed, agente.Sigla); });

                                list.AddRange(eventosCategoria);
                            }
                        }

                        list.AddRange(_eventoService.GetDtoByAgente(agente.ID.Value, dtIni, dtFim.AddDays(30), true));
                        list.AddRange(_eventoService.GetDtoByAgente(agente.ID.Value, dtIni, dtFim.AddDays(90), false));
                    }

                    var eventosVigenciaContratual = _eventoService.GetDtoByVigenciaContratualDistribuidora(agente.ID.Value, dtIni);
                    if (eventosVigenciaContratual.Any())
                        list.AddRange(eventosVigenciaContratual);
                }
            }

            if (list.Any())
            {
                _eventoService.ApplyTipoRule(list);

                list = list.Where(w => w.DateEvento >= DateTime.Today).OrderBy(i => i.DateEvento).ToList();

                if (agentes == null)
                    list = list.Where(w => w.Titulo.DoesntContain("DUEM") && w.Descricao.DoesntContain("Nota Fiscal") && w.Titulo.DoesntContain("Prazo para manifestação de desistência do MRRM")).ToList();
            }

            //ultimos ajustes
            foreach (var li in list)
            {
                if (li.Titulo == "Ajuste Demanda Período de Teste")
                {
                    li.Detailed = li.Descricao.Substring(li.Descricao.IndexOf("Ativo:") + 7);
                }
            }

            return list;
        }

        public ActionResult GadgetMedicao()
        {
            var data = new GadgetMedicaoViewModelList();
            List<Ativo> ativos;
            ativos = _ativoService.GetByConcatnatedIds("1965,1191");
            var dtHoje = DateTime.Now;
            //dtHoje = Convert.ToDateTime("06/05/2020");
            var tipoLeitura = "Geracao";

            foreach (var ativo in ativos)
            {
                var singleData = new GadgetMedicaoViewModel();
                singleData.Resumo = _medicaoDiarioDetailedReportService.LoadMedicoesDia(ativos.Where(s => s.ID == ativo.ID).ToList(), dtHoje, tipoLeitura);
                singleData.Ativo = ativo;
                var gf = _garantiaFisicaService.GetGarantiaFisicaEmVigencia(ativo);
                if (gf != null)
                    singleData.GarantiaFisica = gf.Potencia;

                singleData.ResumoGeracaoMensal = _medicaoMensalReportService.LoadMedicoesMesGeracao(ativos.Where(s => s.ID == ativo.ID).ToList(), dtHoje, false);
                if (singleData.ResumoGeracaoMensal.MedicoesMes.Any())
                {
                    var diasHorasFaltantes = singleData.ResumoGeracaoMensal.MedicoesMes.Where(i => i.HorasFaltantes != null);
                    //if (diasHorasFaltantes.Any())
                      //  singleData.MensagemAtualizacaoHelper = string.Join("&#13;", diasHorasFaltantes.Select(i => string.Format("Data: {0:dd/MM/yyyy} - ({1})", i.Dia, i.HorasFaltantes)));
                }
                singleData.GarantiaFisicaPotenciaMensal = _medicaoMensalReportService.GarantiaFisicaPotencia(ativos.Where(s => s.ID == ativo.ID).ToList(), "MWh");
                
                var start = DateTime.Now;
                var monthYearRange = Request["monthYearRange"].ConvertToDate(null);

                var acompanhamentos = _acompanhamentoAsseguradaReportService.LoadAcompanhamentos(ativo, monthYearRange);

                singleData.AcompanhamentoAssegurada = new AcompanhamentoAsseguradaDto
                {
                    Acompanhamentos = acompanhamentos,
                    Resumos = _acompanhamentoAsseguradaReportService.LoadResumos(acompanhamentos)
                };

                singleData.MedicoesAno = _medicaoAnualReportService.LoadMedicoesAno(ativos.Where(s => s.ID == ativo.ID).ToList(), tipoLeitura, null, false);
                //singleData.GarantiaFisicaPotenciaAno = _medicaoAnualReportService.GetGarantiaFisicaPotencia(data.Ativos);
                //singleData.ValoresGF = _medicaoAnualReportService.GetValoresGarantiaFisica(data.GarantiaFisicaPotencia, data.UnidadeMedida);

                //if (data.Ativos.Count() == 1 && data.MedicoesAno.Any())
                //{
                 //   var maxDate = data.MedicoesAno.Max(i => i.Mes);
                 //   var minDate = data.MedicoesAno.Min(i => i.Mes);

                //    data.FeriasVigentes = data.Ativos.First().FeriasList;
                //}
                data.geracaoHojeAtivo.Add(singleData);
            }
            return AdminContent("Dashboard/DashboardGadgetMedicao.aspx", data);
        }

        public class ViewModel
        {
            //public List<Medidor> MedidoresSemComunicacao = new List<Medidor>();
            public List<AcessoRapido> AcessosRapido;
            public List<AtivoSemComunicacaoDto> AtivosSemComunicacao;
            public List<EventoDto> Eventos;
            public DateTime? UltimaMedicaoLida;
            public int AuditoriaCount;
            public bool HasMedidores;
            public bool HasRelatorioEconomia;
        }

        public class GadgetMedicaoViewModel
        {            
            public Ativo Ativo = new Ativo();
            public MedicaoHorarioResumoDto Resumo;
            public double? GarantiaFisica;
            public string TipoLeitura;
            public string MensagemAtualizacao;
            public string MensagemMedidor;
            public MedicaoMensalResumoGeracaoDto ResumoGeracaoMensal;
            public double? GarantiaFisicaPotenciaMensal;
            public AcompanhamentoAsseguradaDto AcompanhamentoAssegurada;
            public List<MedicaoAnualMedicaoMesDto> MedicoesAno;            

            public string EmpilhaGeracaoMensalMWhLeve()
            {
                return ResumoGeracaoMensal.MedicoesMes.Select(m => m.MWhLeve.ToString("N3").Remove(".").Replace(",", ".")).Join(",");
            }

            public string EmpilhaGeracaoMensalMWhMedio()
            {
                return ResumoGeracaoMensal.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhLeve + medicaoDia.MWhMedio).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
            }

            public string EmpilhaGeracaoMensalMWhPesado()
            {
                return ResumoGeracaoMensal.MedicoesMes.Select(medicaoDia => (medicaoDia.MWhLeve + medicaoDia.MWhMedio + medicaoDia.MWhPesado).ToString("N3").Remove(".").Replace(",", ".")).Join(",");
            }
        }
        public class GadgetMedicaoViewModelList
        {
            public List<GadgetMedicaoViewModel> geracaoHojeAtivo = new List<GadgetMedicaoViewModel>();           
        }
    }
}
