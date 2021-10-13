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
	public class MedicaoErroResumoController : ControllerBase
	{
		private readonly IMedicaoErroResumoService _medicaoErroResumoService;

		public MedicaoErroResumoController(IMedicaoErroResumoService medicaoErroResumoService)
		{
			_medicaoErroResumoService = medicaoErroResumoService;
		}

		//
		// GET: /Admin/MedicaoErroResumo/
		public ActionResult Index()
		{
			var data = new ListViewModel();
			data.MedicaoErros = _medicaoErroResumoService
				.GetAll()
				.OrderBy(me => me.Ativo.Nome)
				.ThenBy(me => me.TipoLeitura)
				.ThenBy(me => me.DataInicio);

			return AdminContent("MedicaoErroResumo/MedicaoErroResumoList.aspx", data);
		}

		public class ListViewModel
		{
			public IEnumerable<MedicaoErroResumo> MedicaoErros;
		}
	}
}
