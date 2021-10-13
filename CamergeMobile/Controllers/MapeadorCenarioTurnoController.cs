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
	public class MapeadorCenarioTurnoController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IHorarioVeraoService _horarioVeraoService;
		private readonly IMapeadorCenarioService _mapeadorCenarioService;
		private readonly IMapeadorCenarioMesService _mapeadorCenarioMesService;
		private readonly IMapeadorCenarioTurnoService _mapeadorCenarioTurnoService;
		private readonly IMapeadorCenarioTurnoValorService _mapeadorCenarioTurnoValorService;
		private readonly IMapeadorMedicaoCacheService _mapeadorMedicaoCacheService;

		public MapeadorCenarioTurnoController(IAtivoService ativoService,
			IHorarioVeraoService horarioVeraoService,
			IMapeadorCenarioService mapeadorCenarioService,
			IMapeadorCenarioMesService mapeadorCenarioMesService,
			IMapeadorCenarioTurnoService mapeadorCenarioTurnoService,
			IMapeadorCenarioTurnoValorService mapeadorCenarioTurnoValorService,
			IMapeadorMedicaoCacheService mapeadorMedicaoCacheService)
		{
			_ativoService = ativoService;
			_horarioVeraoService = horarioVeraoService;
			_mapeadorCenarioService = mapeadorCenarioService;
			_mapeadorCenarioMesService = mapeadorCenarioMesService;
			_mapeadorCenarioTurnoService = mapeadorCenarioTurnoService;
			_mapeadorCenarioTurnoValorService = mapeadorCenarioTurnoValorService;
			_mapeadorMedicaoCacheService = mapeadorMedicaoCacheService;
		}

		public ActionResult Index(Int32? mapceid, Int32? mapcemesid, Int32? Page)
		{
			if ((mapceid != null) && (mapcemesid != null))
			{
				var mapeadorCenarioMes = GetMesTurnos(mapcemesid.Value);

				var data = new FormViewModel()
				{
					MapeadorCenarioMes = mapeadorCenarioMes,
					AgenteConectado = mapeadorCenarioMes.MapeadorCenario.Ativo.AgenteConectado,
					HoursToAddByHorarioVeraoVigencia = _horarioVeraoService.AddHoursInTarifacaoByHorarioVerao(mapeadorCenarioMes.Mes),
					ReadOnly = false
				};

				data.TurnosInDateFormat = _mapeadorMedicaoCacheService.GetTurnosInDateFormat(
					mapeadorCenarioMes.MapeadorCenario.AtivoID.Value,
					mapeadorCenarioMes.Mes.AddMonths(12)
				);

				return AdminContent("MapeadorCenarioTurno/MapeadorCenarioTurnoEdit.aspx", data);
			}
			return HttpNotFound();
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var mapeadorCenarioMes = new MapeadorCenarioMes();

			try
			{
				mapeadorCenarioMes.UpdateFromRequest();
				mapeadorCenarioMes.UpdateChildrenFromRequest();

				_mapeadorCenarioMesService.Update(mapeadorCenarioMes);

				UpdateGrandchildrenFromRequest(mapeadorCenarioMes);

				_mapeadorCenarioTurnoService.DeleteByMapeadorCenarioMesID(mapeadorCenarioMes.ID.Value);
				_mapeadorCenarioTurnoValorService.DeleteManyByMapeadorCenarioTurnoID(mapeadorCenarioMes.MapeadorCenarioTurnoList);

				InsertTurnosValores(mapeadorCenarioMes.MapeadorCenarioTurnoList);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					if (isSaveAndRefresh)
					{
						var nextPage = Web.BaseUrl + "Admin/MapeadorCenarioTurno/?mapceid=" + mapeadorCenarioMes.MapeadorCenario.ID + "&mapcemesid=" + mapeadorCenarioMes.ID;
						return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
						// return RedirectToAction("Index", new { mapceid = mapeadorCenarioMes.MapeadorCenario.ID, mapcemesid = mapeadorCenarioMes.ID });
					}
					else
					{
						var nextPage = Web.BaseUrl + "Admin/MapeadorCenarioMes/?mapceid=" + mapeadorCenarioMes.MapeadorCenario.ID;
						return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
					}
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Index", new { mapceid = mapeadorCenarioMes.MapeadorCenario.ID, mapcemesid = mapeadorCenarioMes.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index", "MapeadorCenarioMes", new { mapceid = mapeadorCenarioMes.MapeadorCenario.ID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });

				TempData["AtivoModel"] = mapeadorCenarioMes;
				return RedirectToAction("Index");
			}
		}

		private string HandleExceptionMessage(Exception ex)
		{
			string errorMessage;
			if (ex is RequiredFieldNullException)
			{
				var fieldName = ((RequiredFieldNullException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "NullException").Replace("XXX", friendlyFieldName);
			}
			else if (ex is FieldLengthException)
			{
				var fieldName = ((FieldLengthException)ex).FieldName;
				var friendlyFieldName = "<strong>" + (Web.Request[fieldName + "_Label"] ?? fieldName) + "</strong>";
				errorMessage = i18n.Gaia.Get("FormValidation", "LengthException").Replace("XXX", friendlyFieldName);
			}
			else
			{
				errorMessage = ex.Message;
			}

			return errorMessage;
		}

		private MapeadorCenarioMes GetMesTurnos(int idMapeadorCenarioMes)
		{
			var mapeadorCenarioMes = _mapeadorCenarioMesService.FindByID(idMapeadorCenarioMes);
			if (mapeadorCenarioMes != null)
			{
				var mapeadorCenarioTurnos = _mapeadorCenarioTurnoService.GetReadyToEdit(
					mapeadorCenarioMes.MapeadorCenario.Ativo, mapeadorCenarioMes.ID.Value, mapeadorCenarioMes.Mes);
				if (mapeadorCenarioTurnos.Any())
				{
					var turnos = new List<MapeadorCenarioTurno>();

					var tipos = _mapeadorCenarioTurnoValorService.GetTipos();
					var tiposTurnoValor = _mapeadorCenarioTurnoValorService.GetTipos().Select(i => i.ToString()).ToList();

					foreach (var turno in mapeadorCenarioTurnos)
					{
						var valoresTurnoViewModel = new List<MapeadorCenarioTurnoValor>();

						var valores = turno.MapeadorCenarioTurnoValorList;
						if (!valores.Any())
							foreach (var tipo in tipos)
								valores.Add(new MapeadorCenarioTurnoValor() { Tipo = tipo.ToString(), NumeroTurno = turno.NumeroTurno });

						foreach (var valor in valores)
							valoresTurnoViewModel.Add(valor);

						turnos.Add(
							new MapeadorCenarioTurno()
							{
								TurnoInicio = turno.TurnoInicio,
								TurnoFim = turno.TurnoFim,
								NumeroTurno = turno.NumeroTurno,
								MapeadorCenarioTurnoValorList = valores.OrderBy(item => tiposTurnoValor.IndexOf(item.Tipo)).ToList()
							}
						);
					}

					mapeadorCenarioMes.MapeadorCenarioTurnoList = turnos;
				}
			}

			return mapeadorCenarioMes;
		}

		private void InsertTurnosValores(List<MapeadorCenarioTurno> mapeadorCenarioTurnos)
		{
			if (mapeadorCenarioTurnos.Any())
			{
				foreach (var mapeadorCenarioTurno in mapeadorCenarioTurnos)
				{
					_mapeadorCenarioTurnoService.Insert(mapeadorCenarioTurno);

					if (mapeadorCenarioTurno.MapeadorCenarioTurnoValorList.Any())
					{
						foreach (var mapeadorCenarioTurnoValor in mapeadorCenarioTurno.MapeadorCenarioTurnoValorList)
						{
							mapeadorCenarioTurnoValor.MapeadorCenarioTurnoID = mapeadorCenarioTurno.ID;

							_mapeadorCenarioTurnoValorService.Insert(mapeadorCenarioTurnoValor);
						}
					}
				}
			}
		}

		private void UpdateGrandchildrenFromRequest(MapeadorCenarioMes mapeadorCenarioMes)
		{
			var tipos = _mapeadorCenarioTurnoValorService.GetTipos();

			for (int t = 0; t < mapeadorCenarioMes.MapeadorCenarioTurnoList.Count(); t++)
			{
				var mapeadorCenarioTurno = mapeadorCenarioMes.MapeadorCenarioTurnoList[t];
				var mapeadorCenarioTurnoValorList = mapeadorCenarioTurno.MapeadorCenarioTurnoValorList;
				mapeadorCenarioTurnoValorList.Clear();

				var prefixTurno = string.Concat("MapeadorCenarioTurno[" + t + "]");

				for (int v = 0; v < tipos.Count(); v++)
				{
					var keyValor = string.Concat(prefixTurno, ".", nameof(MapeadorCenarioTurnoValor), "[", v, "]");

					var mapeadorCenarioTurnoValor = new MapeadorCenarioTurnoValor() { Tipo = Request[string.Concat(keyValor, ".Tipo")] };

					var parseDateTime = new DateTime();
					var propDataValue = Request[string.Concat(keyValor, ".Mes")];
					var propDataValueIsEditable = propDataValue.Contains("Edit");
					if ((propDataValueIsEditable) || (DateTime.TryParse(propDataValue, out parseDateTime)))
					{
						int numeroTurno = Request[string.Concat(keyValor, ".NumeroTurno")].ToInt(0);

						mapeadorCenarioTurnoValor.Mes = (propDataValueIsEditable) ? null : (DateTime?)parseDateTime;
						mapeadorCenarioTurnoValor.NumeroTurno = (numeroTurno == 0) ? mapeadorCenarioTurno.NumeroTurno : numeroTurno;

						var propMWmValue = Request[string.Concat(keyValor, ".Mwm")];
						if (propDataValueIsEditable)
							propMWmValue = Request[string.Concat(keyValor, ".RefreshableMwm")];
						if (string.IsNullOrEmpty(propMWmValue))
						{
							mapeadorCenarioTurnoValor.Mwm = null;
							mapeadorCenarioTurnoValor.Percentual = null;
						}
						else
						{
							mapeadorCenarioTurnoValor.Mwm = Fmt.ToDouble(propMWmValue, false);

							var propPercentualValue = Request[string.Concat(keyValor, ".Percentual")];
							if (!string.IsNullOrEmpty(propPercentualValue))
								mapeadorCenarioTurnoValor.Percentual = Fmt.ToDouble(propPercentualValue, false, true);
						}

						mapeadorCenarioTurnoValorList.Add(mapeadorCenarioTurnoValor);
					}
				}
			}
		}

		public class FormViewModel
		{
			public MapeadorCenarioMes MapeadorCenarioMes;
			public AgenteConectado AgenteConectado;
			public List<DateTime> TurnosInDateFormat;
			public int HoursToAddByHorarioVeraoVigencia;
			public bool ReadOnly;
		}
	}
}
