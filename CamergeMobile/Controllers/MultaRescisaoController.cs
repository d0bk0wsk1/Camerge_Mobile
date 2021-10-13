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
	public class MultaRescisaoController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IBandeiraService _bandeiraService;
		private readonly IDemandaContratadaService _demandaContratadaService;
		private readonly IMedicaoConsolidadoService _medicaoConsolidadoService;
		private readonly ITarifaVigenciaValorService _tarifaVigenciaValorService;

		public MultaRescisaoController(IAtivoService ativoService,
			IBandeiraService bandeiraService,
			IDemandaContratadaService demandaContratadaService,
			IMedicaoConsolidadoService medicaoConsolidadoService,
			ITarifaVigenciaValorService tarifaVigenciaValorService)
		{
			_ativoService = ativoService;
			_bandeiraService = bandeiraService;
			_demandaContratadaService = demandaContratadaService;
			_medicaoConsolidadoService = medicaoConsolidadoService;
			_tarifaVigenciaValorService = tarifaVigenciaValorService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel();

			if (Request["ativos"].IsNotBlank())
			{
				var ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);
				if (ativos.Any())
				{
					data.AtivosMes = GetAtivosMesViewModel(ativos);
				}
			}

			return AdminContent("MultaRescisao/MultaRescisaoReport.aspx", data);
		}

		private List<AtivosMesViewModel> GetAtivosMesViewModel(List<Ativo> ativos)
		{
			var ativosMes = new List<AtivosMesViewModel>();

			var today = Dates.GetFirstDayOfMonth(DateTime.Today);

			foreach (var ativo in ativos)
			{
				var ativoMes = new AtivosMesViewModel() { Ativo = ativo };

				if (ativo.InicioVigenciaContratual == null)
				{
					ativoMes.Observacao = "Início da vigência contratual não cadastrada para este ativo.";
					continue;
				}

				var bandeira = _bandeiraService.GetMostRecent();
				if (bandeira == null)
				{
					ativoMes.Observacao = "Bandeira não localizada.";
					continue;
				}

				var demandaContratada = _demandaContratadaService.GetMostRecent(ativo.ID.Value);
				if (demandaContratada == null)
				{
					ativoMes.Observacao = "Demanda contratada não localizada para este ativo.";
					continue;
				}

				var tarifa = _tarifaVigenciaValorService.Get(ativo.AgenteConectadoID.Value, today, ativo.ClasseID.Value, demandaContratada.Tipo);
				if (tarifa == null)
				{
					ativoMes.Observacao = "Tarifas não cadastradas para este ativo.";
					continue;
				}

				var medicoes = _medicaoConsolidadoService.GetMedicaoMeses(ativo, today.AddMonths(-11), today, Medicao.TiposLeitura.Consumo.ToString());
				if (!medicoes.Any())
				{
					ativoMes.Observacao = "Medições não localizadas para este ativo nos últimos 12 meses.";
					continue;
				}

				var contratoVigencia = new AtivoMesContratoVigenciaViewModel()
				{
					DataInicioContrato = ativo.InicioVigenciaContratual.Value,
					MediaMWhPonta = medicoes.Average(i => i.MedicaoConsumo.MwhPonta),
					MediaMWhForaPontaCapacitivo = medicoes.Average(i => (i.MedicaoConsumo.MwhForaPonta + i.MedicaoConsumo.MwhCapacitivo)),
					DemandaContratadaForaPonta = demandaContratada.ForaPonta.Value,
					TarifaDemanda = tarifa.DemandafPontaComDesconto + tarifa.DemandafPontaSemDesconto,
					TarifaEnergiaPonta = tarifa.EnergiaPonta + bandeira.Valor,
					TarifaEnergiaForaPonta = tarifa.EnergiafPonta + bandeira.Valor
				};

				ativoMes.ContratoVigencia = contratoVigencia;
				ativoMes.AtivoMes = GetContratosMes(ativo, contratoVigencia);

				ativosMes.Add(ativoMes);
			}

			return ativosMes;
		}

		private List<AtivoMesViewModel> GetContratosMes(Ativo ativo, AtivoMesContratoVigenciaViewModel contratoVigencia)
		{
			var list = new List<AtivoMesViewModel>();

			int numeroMes = 0;
			double? constDefaultEnergiaValue = null;

			for (var month = contratoVigencia.MesZero; month >= contratoVigencia.MesMaximo; month = month.AddMonths(-1))
			{
				double multaDemanda = ((Math.Min(numeroMes, 6) * contratoVigencia.DemandaContratadaForaPonta * contratoVigencia.TarifaDemanda * 1000)
					+ ((numeroMes > 6) ? (numeroMes - 6) : 0) * 30 * contratoVigencia.TarifaDemanda);
				double multaEnergia = (constDefaultEnergiaValue != null)
					? constDefaultEnergiaValue.Value
					: ((contratoVigencia.MediaMWhPonta * contratoVigencia.TarifaEnergiaPonta
						+ contratoVigencia.MediaMWhForaPontaCapacitivo * contratoVigencia.TarifaEnergiaForaPonta) * numeroMes);

				if (numeroMes == 12)
					constDefaultEnergiaValue = multaEnergia;

				list.Add(
					new AtivoMesViewModel()
					{
						Ativo = ativo,
						Mes = month,
						NumeroMes = numeroMes,
						MultaDemanda = multaDemanda,
						MultaEnergia = multaEnergia
					}
				);

				numeroMes++;
			}

			return list.OrderBy(i => i.Mes).ToList();
		}

		public class ListViewModel
		{
			public List<AtivosMesViewModel> AtivosMes { get; set; }
		}

		public class AtivosMesViewModel
		{
			public Ativo Ativo { get; set; }
			public AtivoMesContratoVigenciaViewModel ContratoVigencia { get; set; }
			public List<AtivoMesViewModel> AtivoMes { get; set; }
			public string Observacao { get; set; }
		}

		public class AtivoMesContratoVigenciaViewModel
		{
			public double DemandaContratadaForaPonta { get; set; }
			public double TarifaDemanda { get; set; }
			public double TarifaEnergiaPonta { get; set; }
			public double TarifaEnergiaForaPonta { get; set; }
			public double MediaMWhPonta { get; set; }
			public double MediaMWhForaPontaCapacitivo { get; set; }
			public DateTime DataInicioContrato { get; set; }
			public DateTime DataFimContrato
			{
				get
				{
					return Dates.GetLastDayOfMonth(DataInicioContrato.AddMonths(12));
				}
			}
			public DateTime DataUltimaRenovacao
			{
				get
				{
					return DataInicioContrato.AddMonths(6);
				}
			}
			public DateTime DataNovaVigencia
			{
				get
				{
					return (DataFimContrato > DateTime.Today.AddMonths(6)) ? DataFimContrato : DataFimContrato.AddMonths(12);
				}
			}
			public DateTime MesZero
			{
				get
				{
					return DataNovaVigencia.AddDays(1);
				}
			}
			public DateTime MesMaximo
			{
				get
				{
					return Dates.GetFirstDayOfMonth(DateTime.Today.AddMonths(1));
				}
			}
		}

		public class AtivoMesViewModel
		{
			public Ativo Ativo { get; set; }
			public DateTime Mes { get; set; }
			public int NumeroMes { get; set; }
			public double MultaDemanda { get; set; }
			public double MultaEnergia { get; set; }
		}
	}
}
