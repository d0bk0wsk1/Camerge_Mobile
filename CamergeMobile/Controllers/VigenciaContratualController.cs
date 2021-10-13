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
	public class VigenciaContratualController : ControllerBase
	{
		private readonly IAtivoService _ativoService;

		public VigenciaContratualController(IAtivoService ativoService)
		{
			_ativoService = ativoService;
		}

		public ActionResult Index()
		{
			var data = new ListViewModel();

			IEnumerable<Ativo> ativos;

			if (Request["ativos"].IsNotBlank())
			{
				ativos = _ativoService.GetByConcatnatedIds(Request["ativos"]);
			}
			else
			{
				if (UserSession.IsPerfilAgente)
					ativos = _ativoService.GetByAgentes(UserSession.Agentes.Select(i => i.ID.Value));
				else
					ativos = _ativoService.GetAll();
			}

			data.AtivosContratos = _ativoService.GetAtivosContratos(ativos, null);

			return AdminContent("VigenciaContratual/VigenciaContratualReport.aspx", data);
		}

		public class ListViewModel
		{
			public List<AtivoContratoDto> AtivosContratos { get; set; }
		}
	}
}
