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
	public class MapeadorMedicaoCacheController : ControllerBase
	{
		private readonly IMapeadorMedicaoCacheService _mapeadorMedicaoCacheService;

		public MapeadorMedicaoCacheController(IMapeadorMedicaoCacheService mapeadorMedicaoCacheService)
		{
			_mapeadorMedicaoCacheService = mapeadorMedicaoCacheService;
		}

		public JsonResult GetValue(int ativoID, DateTime mes, DateTime turnoInicio, int numeroTurno, string tipo)
		{
			// Format JS date
			turnoInicio = mes.Add(turnoInicio.TimeOfDay);

			var value = _mapeadorMedicaoCacheService.GetMWm(ativoID, mes, turnoInicio, numeroTurno, tipo);

			return Json((value == null) ? string.Empty : value.Value.ToString("N6"), JsonRequestBehavior.AllowGet);
		}
	}
}
