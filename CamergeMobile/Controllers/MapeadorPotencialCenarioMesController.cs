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
	public class MapeadorPotencialCenarioMesController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IMapeadorPotencialCenarioService _mapeadorPotencialCenarioService;
		private readonly IMapeadorPotencialCenarioMesService _mapeadorPotencialCenarioMesService;
		private readonly IMapeadorPotencialCenarioMesValorService _mapeadorPotencialCenarioMesValorService;
		private readonly IMapeadorPotencialCacheService _mapeadorPotencialCacheService;

		public MapeadorPotencialCenarioMesController(IAtivoService ativoService,
			IMapeadorPotencialCenarioService mapeadorPotencialCenarioService,
			IMapeadorPotencialCenarioMesService mapeadorPotencialCenarioMesService,
			IMapeadorPotencialCenarioMesValorService mapeadorPotencialCenarioMesValorService,
			IMapeadorPotencialCacheService mapeadorPotencialCacheService)
		{
			_ativoService = ativoService;
			_mapeadorPotencialCenarioService = mapeadorPotencialCenarioService;
			_mapeadorPotencialCenarioMesService = mapeadorPotencialCenarioMesService;
			_mapeadorPotencialCenarioMesValorService = mapeadorPotencialCenarioMesValorService;
			_mapeadorPotencialCacheService = mapeadorPotencialCacheService;
		}

		public ActionResult Index(Int32? mapptnceid, Int32? Page)
		{
			if (mapptnceid != null)
			{
				var mapeadorPotencialCenario = _mapeadorPotencialCenarioService.FindByID(mapptnceid.Value);
				if (mapeadorPotencialCenario != null)
				{
					mapeadorPotencialCenario = _mapeadorPotencialCenarioService.GetReadyToEdit(mapeadorPotencialCenario);

					var data = new FormViewModel()
					{
						MapeadorPotencialCenario = mapeadorPotencialCenario,
						AgenteConectado = mapeadorPotencialCenario.Ativo.AgenteConectado,
						ReadOnly = false
					};

					data.MesesInDateFormat = _mapeadorPotencialCacheService.GetMesesInDateFormat(
						mapeadorPotencialCenario.AtivoID.Value,
						(new DateTime(mapeadorPotencialCenario.Ano.Value, 1, 1).AddMonths(-36)),
						(new DateTime(mapeadorPotencialCenario.Ano.Value, 1, 1).AddMonths(12))
					);

					return AdminContent("MapeadorPotencialCenarioMes/MapeadorPotencialCenarioMesEdit.aspx", data);
				}
			}
			return HttpNotFound();
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var mapeadorPotencialCenario = new MapeadorPotencialCenario();

			try
			{
				mapeadorPotencialCenario.UpdateFromRequest();
				mapeadorPotencialCenario.UpdateChildrenFromRequest();

				_mapeadorPotencialCenarioService.Update(mapeadorPotencialCenario);

				UpdateGrandchildrenFromRequest(mapeadorPotencialCenario);

				_mapeadorPotencialCenarioMesService.DeleteByMapeadorPotencialCenarioMesID(mapeadorPotencialCenario.ID.Value);
				_mapeadorPotencialCenarioMesValorService.DeleteManyByMapeadorPotencialCenarioMesID(mapeadorPotencialCenario.MapeadorPotencialCenarioMesList);

				InsertMesesValores(mapeadorPotencialCenario.MapeadorPotencialCenarioMesList);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					if (isSaveAndRefresh)
					{
						var nextPage = Web.BaseUrl + "Admin/MapeadorPotencialCenarioMes/?mapptnceid=" + mapeadorPotencialCenario.ID;
						return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
					}
					else
					{
						var nextPage = Web.BaseUrl + "Admin/MapeadorPotencialCenario";
						return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
					}
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Index", new { mapptnceid = mapeadorPotencialCenario.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index", "MapeadorPotencialCenarioMes", new { mapptnceid = mapeadorPotencialCenario.ID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });

				TempData["AtivoModel"] = mapeadorPotencialCenario;
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

		private void InsertMesesValores(List<MapeadorPotencialCenarioMes> mapeadorPotencialCenarioMeses)
		{
			if (mapeadorPotencialCenarioMeses.Any())
			{
				foreach (var mapeadorPotencialCenarioMes in mapeadorPotencialCenarioMeses)
				{
					_mapeadorPotencialCenarioMesService.Insert(mapeadorPotencialCenarioMes);

					if (mapeadorPotencialCenarioMes.MapeadorPotencialCenarioMesValorList.Any())
					{
						foreach (var mapeadorPotencialCenarioMesValor in mapeadorPotencialCenarioMes.MapeadorPotencialCenarioMesValorList)
						{
							mapeadorPotencialCenarioMesValor.MapeadorPotencialCenarioMesID = mapeadorPotencialCenarioMes.ID;

							_mapeadorPotencialCenarioMesValorService.Insert(mapeadorPotencialCenarioMesValor);
						}
					}
				}
			}
		}

		private void UpdateGrandchildrenFromRequest(MapeadorPotencialCenario mapeadorPotencialCenario)
		{
			var tipos = _mapeadorPotencialCenarioMesValorService.GetTipos();

			for (int t = 0; t < mapeadorPotencialCenario.MapeadorPotencialCenarioMesList.Count(); t++)
			{
				var mapeadorPotencialCenarioMes = mapeadorPotencialCenario.MapeadorPotencialCenarioMesList[t];
				var mapeadorPotencialCenarioMesValorList = mapeadorPotencialCenarioMes.MapeadorPotencialCenarioMesValorList;
				mapeadorPotencialCenarioMesValorList.Clear();

				var prefixMes = string.Concat("MapeadorPotencialCenarioMes[" + t + "]");

				for (int v = 0; v < tipos.Count(); v++)
				{
					var keyValor = string.Concat(prefixMes, ".", nameof(MapeadorPotencialCenarioMesValor), "[", v, "]");

					var mapeadorPotencialCenarioMesValor = new MapeadorPotencialCenarioMesValor() { Tipo = Request[string.Concat(keyValor, ".Tipo")] };

					var parseDateTime = new DateTime();
					var propDataValue = Request[string.Concat(keyValor, ".MesValor")];
					var propDataValueIsEditable = propDataValue.Contains("Edit");
					if ((propDataValueIsEditable) || (DateTime.TryParse(propDataValue, out parseDateTime)))
					{
						mapeadorPotencialCenarioMesValor.Mes = (propDataValueIsEditable) ? null : (DateTime?)parseDateTime;

						var propMWmValue = Request[string.Concat(keyValor, ".Mwm")];
						if (propDataValueIsEditable)
							propMWmValue = Request[string.Concat(keyValor, ".RefreshableMwm")];
						if (string.IsNullOrEmpty(propMWmValue))
						{
							mapeadorPotencialCenarioMesValor.Mwm = null;
							mapeadorPotencialCenarioMesValor.Percentual = null;
						}
						else
						{
							mapeadorPotencialCenarioMesValor.Mwm = Fmt.ToDouble(propMWmValue, false);

							var propPercentualValue = Request[string.Concat(keyValor, ".Percentual")];
							if (!string.IsNullOrEmpty(propPercentualValue))
								mapeadorPotencialCenarioMesValor.Percentual = Fmt.ToDouble(propPercentualValue, false, true);
						}

						mapeadorPotencialCenarioMesValorList.Add(mapeadorPotencialCenarioMesValor);
					}
				}
			}
		}

		public class FormViewModel
		{
			public MapeadorPotencialCenario MapeadorPotencialCenario;
			public AgenteConectado AgenteConectado;
			public List<DateTime> MesesInDateFormat;
			public bool ReadOnly;
		}
	}
}
