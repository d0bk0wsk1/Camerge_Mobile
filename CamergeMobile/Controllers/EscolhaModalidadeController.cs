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
	public class EscolhaModalidadeController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ICalculoLivreService _calculoLivreService;
		private readonly IContabilizacaoCceeService _contabilizacaoCceeService;
		private readonly IImpostoService _impostoService;
		private readonly IOpcaoImpostoService _opcaoImpostoService;
		private readonly ITarifaVigenciaService _tarifaVigenciaService;

		public EscolhaModalidadeController(IAtivoService ativoService,
			ICalculoLivreService calculoLivreService,
			IContabilizacaoCceeService contabilizacaoCceeService,
			IImpostoService impostoService,
			IOpcaoImpostoService opcaoImpostoService,
			ITarifaVigenciaService tarifaVigenciaService)
		{
			_ativoService = ativoService;
			_calculoLivreService = calculoLivreService;
			_contabilizacaoCceeService = contabilizacaoCceeService;
			_impostoService = impostoService;
			_opcaoImpostoService = opcaoImpostoService;
			_tarifaVigenciaService = tarifaVigenciaService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel()
			{
				TipoRelacao = Request["relacao"] ?? PerfilAgente.TiposRelacao.Cliente.ToString(),
				Modalidades = GetModalidadesDefault()
			};

			if (Request["ativos"].IsNotBlank())
			{
				var ativos = new List<Ativo>();

				var allativos = Request["allativo"].ToBoolean();
				if (allativos)
				{
					if (UserSession.IsCliente)
						ativos = _ativoService.GetByAgentes(UserSession.Agentes.Select(i => i.ID.Value)).ToList();
					else
						ativos = _ativoService.GetAtivosByPerfilAgenteTipo(PerfilAgente.Tipos.Consumidor, data.TipoRelacao).ToList();
				}
				else
				{
					ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);
				}

				ativos = ativos.Where(i => i.Classe.Nome != "A2" && i.Classe.Nome != "A3").ToList();

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
						var includeIcms = Request["imposto"].Contains("icms");
						var includeImposto = Request["imposto"].Contains("imposto");
						var creditIcms = Fmt.ContainsWithNull(Request["creditaimp"], "icms");
						var creditImposto = Fmt.ContainsWithNull(Request["creditaimp"], "imposto");

						var viewMonths = (allativos) ? 1 : 13;

						if (tipoEnergia != null)
							tipoEnergia /= 100;

						var dtos = _calculoLivreService.LoadCalculos(ativos, mes, null, tipoEnergia, null, tipoVigencia, includeIcms, includeImposto, creditIcms, creditImposto, true, true, viewMonths,false,false);
						if (dtos.Any())
						{
							var ativosMes = new List<AtivosMesViewModel>();

							foreach (var dto in dtos)
							{
								var ativoMes = new AtivosMesViewModel() { Ativo = dto.Ativo };

								if (dto.Calculos.Any())
								{
									var calculoAtivoMes = new List<AtivoMesViewModel>();

									data.MaxDemandaPontaMedida = dto.Calculos.Max(i => i.DemandaPonta);

									foreach (var calculo in dto.Calculos)
										calculoAtivoMes.Add(GetAtivoMesViewModel(calculo, tipoVigencia, tipoEnergia, data.MaxDemandaPontaMedida, includeIcms, includeImposto, creditIcms, creditImposto));

									ativoMes.AtivoMes = calculoAtivoMes;
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

			return AdminContent("EscolhaModalidade/EscolhaModalidadeReport.aspx", data);
		}

		private AtivoMesViewModel GetAtivoMesViewModel(CalculoLivreDto dto,
			string tipoVigencia = null, double? tipoEnergia = null, double? maxDemandaPontaMedida = null,
			bool includeIcms = false, bool includeImposto = false,
			bool creditIcms = false, bool creditImposto = false)
		{
			if (dto.Observacao == null)
			{
				var viewModel = new AtivoMesViewModel()
				{
					Ativo = dto.Ativo,
					Mes = dto.Mes,
					ModalidadeTarifaria = dto.Modalidade,
					ConsumoPontaMwh = dto.MWhPontaRede,
					DemandaPontaFaturada = Math.Max(dto.DemandaPonta, dto.DemandaPontaContratada) * 1000,
					ConsumoTotalMwh = dto.MWhTotal
				};

				var modalidades = GetModalidades(dto, tipoVigencia, tipoEnergia, maxDemandaPontaMedida, includeIcms, includeImposto, creditIcms, creditImposto);
				if (modalidades.Any())
				{
					var descendingModalidade = modalidades.Where(i => i.ValorFinanceiro != null)
						.OrderBy(i => i.Modalidade.Prioridade)
						.OrderByDescending(i => i.ValorFinanceiro.Value)
						.ToList();
					var bestModalidade = descendingModalidade.Last();

					var savedFinanceiro = GetSubtratedValorFinanceiroFromModalidade(modalidades);
					var encargoSubtracted = GetSubtratedEncargoTotalFromModalidade(modalidades);

					double demandaPontaTarifa = 0;

					// considerar a modalidade Azul para a divis�o da coluna 'Demanda Empate'
					var modalidadeAzul = modalidades.First(i => i.Modalidade.Nome == "Azul");
					if (modalidadeAzul != null)
						demandaPontaTarifa = modalidadeAzul.DemandaPontaTarifa;

					var modalidadeVerde = modalidades.First(i => i.Modalidade.Nome == "Verde");
					var descontoSelecionado = (1 - dto.DescontoCcee);

					viewModel.Modalidades = modalidades;
					viewModel.DemandaPontaEmpate = ((modalidadeVerde == null) || (descontoSelecionado == 0))
						? null : (demandaPontaTarifa == 0) ? 0 : (double?)((encargoSubtracted / demandaPontaTarifa) / descontoSelecionado);
					viewModel.GanhoOpcao = bestModalidade.Modalidade.Nome;
					viewModel.GanhoFinanceiro = savedFinanceiro;
					viewModel.GanhoFinanceiroMWh = (savedFinanceiro / viewModel.ConsumoTotalMwh);
					viewModel.GanhoPercentual = ((bestModalidade.ValorFinanceiro == null) || (bestModalidade.ValorFinanceiro == 0))
						? 0 : (savedFinanceiro / (bestModalidade.ValorFinanceiro.Value + savedFinanceiro));
					viewModel.CssClass = bestModalidade.Modalidade.CssClass;

					if ((includeIcms) || (includeImposto))
					{
						viewModel.DemandaPontaEmpate /= _impostoService.GetValorImposto(dto.Ativo, dto.Mes, includeIcms, includeImposto);
					}
				}

				return viewModel;
			}
			return new AtivoMesViewModel() { Mes = dto.Mes, Observacao = dto.Observacao };
		}

		private List<AtivoMesModalidadeViewModel> GetModalidades(CalculoLivreDto dto,
			string tipoVigencia = null, double? tipoEnergia = null, double? maxDemandaPontaMedida = null,
			bool includeIcms = false, bool includeImposto = false,
			bool creditIcms = false, bool creditImposto = false)
		{
			var list = new List<AtivoMesModalidadeViewModel>();

			var modalidades = GetModalidadesDefault();

			var tarifa = _tarifaVigenciaService.GetMesVigenciaByTipo(dto.Ativo.ID.Value, tipoVigencia);

			foreach (var modalidade in modalidades)
			{
				var calculoModalidade = _calculoLivreService.GetCalculo(dto.Ativo, dto.Mes, null, null, tipoEnergia, modalidade.Nome, tarifa, includeIcms, includeImposto, creditIcms, creditImposto, true);
				if (calculoModalidade != null)
				{
					var viewModel = new AtivoMesModalidadeViewModel()
					{
						Modalidade = modalidade,
						DemandaPontaTarifa = calculoModalidade.DemandaPontaTarifa,
						HasValue = (calculoModalidade.Modalidade != null)
					};

					if (viewModel.HasValue)
					{
						var valorFinanceiro = calculoModalidade.Total;

						if ((maxDemandaPontaMedida != null)
							&& (dto.Modalidade == DemandaContratada.Tipos.Verde.ToString())
							&& (calculoModalidade.Modalidade == DemandaContratada.Tipos.Azul.ToString()))
						{
							valorFinanceiro -= (calculoModalidade.DemandaPontaTarifa * Math.Max(calculoModalidade.DemandaPonta, calculoModalidade.DemandaPontaContratada) * 1000) * (1 - tipoEnergia.Value);
							valorFinanceiro += (calculoModalidade.DemandaPontaTarifa * maxDemandaPontaMedida.Value * 1000) * (1 - tipoEnergia.Value);
						}

						if ((includeIcms) || (includeImposto))
							calculoModalidade = _calculoLivreService.ApplyImposto(calculoModalidade, null, includeIcms, includeImposto);

						if ((creditIcms) || (creditImposto))
							_calculoLivreService.CreditImposto(calculoModalidade, creditIcms, creditImposto);

						viewModel.EncargoTotal = (calculoModalidade.ConsumoPontaAtivo + calculoModalidade.ConsumoPontaDescEI);
						viewModel.ValorFinanceiro = valorFinanceiro;
						viewModel.ValorFinanceiroMWh = (dto.MWhTotal == 0) ? 0 : (valorFinanceiro / dto.MWhTotal);

						if ((dto.Observacao == null) && (dto.CreditoImposto != null))
							CreditImposto(viewModel, dto.CreditoImposto);
					}

					list.Add(viewModel);
				}
			}

			return list;
		}

		private List<ModalidadeReportViewModel> GetModalidadesDefault()
		{
			return new List<ModalidadeReportViewModel>()
			{
				new ModalidadeReportViewModel() { Nome = "Azul", CssClass = "demanda-tipo-azul", Prioridade = 1 },
				new ModalidadeReportViewModel() { Nome = "Verde", CssClass = "demanda-tipo-verde", Prioridade = 0 }
			};
		}

		private double GetSubtratedEncargoTotalFromModalidade(List<AtivoMesModalidadeViewModel> modalidades)
		{
			double? subtracted = null;

			foreach (var modalidade in modalidades)
			{
				if (modalidade.HasValue)
				{
					if (subtracted == null)
						subtracted = modalidade.EncargoTotal;
					else
						subtracted -= modalidade.EncargoTotal;
				}
			}

			return (subtracted.Value < 0) ? (subtracted.Value * -1) : subtracted.Value;
		}

		private double GetSubtratedValorFinanceiroFromModalidade(List<AtivoMesModalidadeViewModel> modalidades)
		{
			double? subtracted = null;

			foreach (var modalidade in modalidades)
			{
				if (modalidade.HasValue)
				{
					if (subtracted == null)
						subtracted = modalidade.ValorFinanceiro;
					else
						subtracted -= modalidade.ValorFinanceiro;
				}
			}

			return (subtracted.Value < 0) ? (subtracted.Value * -1) : subtracted.Value;
		}

		private void CreditImposto(AtivoMesModalidadeViewModel ativoMes, CreditoImpostoDto creditoImposto)
		{
			ativoMes.ValorFinanceiro *= creditoImposto.TotalImpostosCreditados;
			ativoMes.ValorFinanceiroMWh *= creditoImposto.TotalImpostosCreditados;
		}

		public class ListViewModel
		{
			public List<Ativo> Ativos { get; set; }
			public bool AllInOne { get; set; }
			public string TipoRelacao { get; set; }
			public string Impostos { get; set; }
			public string ImpostosCreditados { get; set; }
			public double? MaxDemandaPontaMedida { get; set; }
			public List<AtivosMesViewModel> AtivosMes { get; set; }
			public List<ModalidadeReportViewModel> Modalidades { get; set; }
		}

		public class AtivosMesViewModel
		{
			public Ativo Ativo { get; set; }
			public List<AtivoMesViewModel> AtivoMes { get; set; }
		}

		public class AtivoMesViewModel
		{
			public Ativo Ativo { get; set; }
			public DateTime Mes { get; set; }
			public List<AtivoMesModalidadeViewModel> Modalidades { get; set; }
			public string ModalidadeTarifaria { get; set; }
			public double ConsumoPontaMwh { get; set; }
			public double ConsumoTotalMwh { get; set; }
			public double? DemandaPontaEmpate { get; set; }
			public double? DemandaPontaFaturada { get; set; }
			public string GanhoOpcao { get; set; }
			public double GanhoFinanceiro { get; set; }
			public double GanhoFinanceiroMWh { get; set; }
			public double GanhoPercentual { get; set; }
			public string CssClass { get; set; }
			public string Observacao { get; set; }
		}

		public class AtivoMesModalidadeViewModel
		{
			public ModalidadeReportViewModel Modalidade { get; set; }
			public double DemandaPontaTarifa { get; set; }
			public double? EncargoTotal { get; set; }
			public double? ValorFinanceiro { get; set; }
			public double? ValorFinanceiroMWh { get; set; }
			public bool HasValue { get; set; }
		}

		public class ModalidadeReportViewModel
		{
			public string Nome { get; set; }
			public string CssClass { get; set; }
			public int Prioridade { get; set; }
		}
	}
}
