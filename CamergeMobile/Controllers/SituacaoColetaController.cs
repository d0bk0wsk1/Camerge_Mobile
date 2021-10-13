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
	public class SituacaoColetaController : ControllerBase
	{
		private readonly IMedicaoHoraFaltanteService _medicaoHoraFaltanteService;
		private readonly IMedicaoUltimoDadoService _medicaoUltimoDadoService;
		private readonly IPerfilAgenteService _perfilAgenteService;

		public SituacaoColetaController(IMedicaoHoraFaltanteService medicaoHoraFaltanteService,
			IMedicaoUltimoDadoService medicaoUltimoDadoService,
			IPerfilAgenteService perfilAgenteService)
		{
			_medicaoHoraFaltanteService = medicaoHoraFaltanteService;
			_medicaoUltimoDadoService = medicaoUltimoDadoService;
			_perfilAgenteService = perfilAgenteService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel();

			if (Request["date"].IsNotBlank())
			{
				DateTime parsedDate;
				if (DateTime.TryParse(Request["date"], out parsedDate))
				{
					var agente = Request["agente"];

					data.Mes = Dates.GetFirstDayOfMonth(parsedDate);

					if (agente.IsNotBlank())
					{
						if (agente == "0")
							data.PerfisAgente = _perfilAgenteService.GetByTipoRelacao(PerfilAgente.TiposRelacao.Cliente.ToString(), true);
						else
							data.PerfisAgente = _perfilAgenteService.GetByAgenteId(agente.ToInt(0), PerfilAgente.TiposRelacao.Cliente.ToString());
					}

					data.SituacoesColetas = GetViewModel(_medicaoUltimoDadoService.Get(data.Mes, data.PerfisAgente));
				}
			}

			return AdminContent("SituacaoColeta/SituacaoColetaReport.aspx", data);
		}

		private List<SituacaoColetaViewModel> GetViewModel(IEnumerable<MedicaoUltimoDado> medicoesUltimoDado)
		{
			var list = new List<SituacaoColetaViewModel>();

			if (medicoesUltimoDado.Any())
			{
				foreach (var medicaoUltimoDado in medicoesUltimoDado)
				{
					var item = new SituacaoColetaViewModel()
					{
						Ativo = medicaoUltimoDado.Ativo,
						Mes = medicaoUltimoDado.DateReferencia,
						UltimoDadoRecebido = medicaoUltimoDado.UltimoDadoRecebido,
						UltimoDadoRecebidoCompleto = medicaoUltimoDado.UltimoDadoRecebidoCompleto
					};

					var tipoLeitura = ((item.Ativo.IsConsumidor) ? Medicao.TiposLeitura.Consumo.ToString() : Medicao.TiposLeitura.Geracao.ToString());
					var horasFaltantes = _medicaoHoraFaltanteService.Get(item.Ativo, tipoLeitura, item.Mes);

					if (horasFaltantes.Any())
						item.HorasFaltantesHtmlHelper = string.Join("&#13;", horasFaltantes.Select(i => string.Format("Data: {0:dd/MM/yyyy} - ({1})", i.Dia, i.HorasFaltantes)));

					list.Add(item);
				}
			}
			return list.OrderBy(i => i.Ativo.PerfilAgente.Agente.Nome).OrderBy(x => x.Ativo.Nome).ToList();
		}

		public class ListViewModel
		{
			public DateTime Mes { get; set; }
			public List<PerfilAgente> PerfisAgente { get; set; }
			public List<SituacaoColetaViewModel> SituacoesColetas { get; set; }
		}

		public class SituacaoColetaViewModel
		{
			public Ativo Ativo { get; set; }
			public DateTime Mes { get; set; }
			public DateTime? UltimoDadoRecebido { get; set; }
			public DateTime? UltimoDadoRecebidoCompleto { get; set; }
			public string HorasFaltantesHtmlHelper { get; set; }
			public bool IsOlder
			{
				get
				{
					if ((UltimoDadoRecebido != null) && (UltimoDadoRecebidoCompleto != null))
					{
						var now = DateTime.Now;
						var diff = (UltimoDadoRecebido.Value - UltimoDadoRecebidoCompleto.Value);

						return (((now - UltimoDadoRecebido.Value).TotalDays > 3) || (diff.TotalDays > 3));
					}
					return false;
				}
			}
		}
	}
}
