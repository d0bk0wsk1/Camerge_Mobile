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
	public class MedicaoClientePotencialAnualController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IMedicaoClientePotencialService _medicaoClientePotencialService;

		public MedicaoClientePotencialAnualController(IAtivoService ativoService,
			IMedicaoClientePotencialService medicaoClientePotencialService)
		{
			_ativoService = ativoService;
			_medicaoClientePotencialService = medicaoClientePotencialService;
		}

		public ActionResult Index()
		{
			/// Não considera capacitivo.

			var data = new ListViewModel();

			if (Request["ativos"].IsNotBlank())
			{
				data.Ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);
				//data.TipoLeitura = Request["tipoleitura"];
				data.TipoLeitura = Medicao.TiposLeitura.Consumo.ToString();
				data.IsMesesComum = Request["mesescomum"].ToBoolean();
				data.UnidadeMedida = (Request["unidade"] == "MWh") ? "MWh" : "MWm";

				if (data.Ativos.Any())
				{
					data.IsBaixaTensao = data.Ativos.Any(i => i.IsBaixaTensao == true);

					var medicoesAnos = _medicaoClientePotencialService.ReportAnual(data.Ativos.ToList(), data.IsMesesComum, 5);
					if (medicoesAnos != null)
					{
						data.MedicoesAnos = medicoesAnos;

						if (data.UnidadeMedida == "MWm")
						{
							if (medicoesAnos.MedicoesAnos.Any())
							{
								foreach (var medicoesAno in medicoesAnos.MedicoesAnos)
								{
									foreach (var medicoesMes in medicoesAno.Meses)
									{
										var mwhForaPontaCapacitivo = (medicoesMes.MwhForaPonta + medicoesMes.MwhCapacitivo);
										double mwhTotal = (medicoesMes.MwhPonta) + (medicoesMes.MwhForaPonta) + (medicoesMes.MwhCapacitivo) + (medicoesMes.MontanteGerador ?? 0);

										medicoesMes.MontanteGerador = (medicoesMes.MontanteGerador / medicoesMes.QtdeHorasPonta);
										medicoesMes.MwhPonta = (medicoesMes.MwhPonta / medicoesMes.QtdeHorasPonta);
										medicoesMes.MwhForaPonta = (mwhForaPontaCapacitivo / medicoesMes.QtdeHorasForaPonta);
										medicoesMes.MwhTotal = ((mwhTotal) / medicoesMes.QtdeHorasTotal);
									}
								}
							}
						}

						data.HasItems = ((data.MedicoesAnos != null) && (data.MedicoesAnos.MedicoesAnos != null) && (data.MedicoesAnos.MedicoesAnos.Any()));
					}
					else
					{
						Web.SetMessage("Não há meses em comum.", "info");
					}
				}
			}

			return AdminContent("MedicaoClientePotencialAnual/MedicaoClientePotencialAnualReport.aspx", data);
		}

		public class ListViewModel
		{
			public IEnumerable<Ativo> Ativos { get; set; }
			public string TipoLeitura { get; set; }
			public string UnidadeMedida { get; set; }
			public bool HasItems { get; set; }
			public bool IsBaixaTensao { get; set; }
			public bool IsMesesComum { get; set; }
			public MedicaoClientePotencialAnualReportDto MedicoesAnos { get; set; }
			public string GetValores(List<MedicaoConsolidadoConsumoMesDto> medicoesMeses)
			{
				var valores = new List<double>();

				var ultimoMes = (medicoesMeses.Any()) ? medicoesMeses.Max(m => m.Mes.Month) : 0;

				for (var i = 1; i <= ultimoMes; i++)
				{
					var medicoesMes = medicoesMeses.FirstOrDefault(m => m.Mes.Month == i);
					if (medicoesMes != null)
					{
						valores.Add(medicoesMes.MwhTotal);
					}
					else
					{
						valores.Add(0);
					}
				}

				return valores.Select(m =>
					m == 0.0
					? "null" // null will remove the point from the chart
					: m.ToString("N3").Remove(".").Replace(",", ".")
				).Join(",");
			}
		}
	}
}
