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
	public class MapeadorCenarioController : ControllerBase
	{
		private readonly IMapeadorCenarioService _mapeadorCenarioService;
		private readonly IMapeadorCenarioMesService _mapeadorCenarioMesService;
		private readonly IMapeadorCenarioTurnoService _mapeadorCenarioTurnoService;
		private readonly IMapeadorCenarioTurnoValorService _mapeadorCenarioTurnoValorService;
		private readonly ITarifaVigenciaService _tarifaVigenciaService;

		public MapeadorCenarioController(IMapeadorCenarioService mapeadorCenarioService,
			IMapeadorCenarioMesService mapeadorCenarioMesService,
			IMapeadorCenarioTurnoService mapeadorCenarioTurnoService,
			IMapeadorCenarioTurnoValorService mapeadorCenarioTurnoValorService,
			ITarifaVigenciaService tarifaVigenciaService)
		{
			_mapeadorCenarioService = mapeadorCenarioService;
			_mapeadorCenarioMesService = mapeadorCenarioMesService;
			_mapeadorCenarioTurnoService = mapeadorCenarioTurnoService;
			_mapeadorCenarioTurnoValorService = mapeadorCenarioTurnoValorService;
			_tarifaVigenciaService = tarifaVigenciaService;
		}

		//
		// GET: /Admin/MapeadorCenario/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _mapeadorCenarioService.GetAllWithPaging(
				(UserSession.IsPerfilAgente || UserSession.IsPotencialAgente) ? UserSession.Agentes.Select(i => i.ID.Value) : null,
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;

			data.MapeadorCenarios = paging.Items;
			if (data.MapeadorCenarios.Any())
			{
				var tarifas = new Dictionary<int, TarifaVigencia>();

				foreach (var mapeadorCenario in data.MapeadorCenarios)
					tarifas.Add(mapeadorCenario.ID.Value, _tarifaVigenciaService.GetMostRecentByAtivo(mapeadorCenario.AtivoID.Value));

				data.TarifasVigencia = tarifas;
			}

			return AdminContent("MapeadorCenario/MapeadorCenarioList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.MapeadorCenario = TempData["MapeadorCenarioModel"] as MapeadorCenario;
			if (data.MapeadorCenario == null)
			{
				data.MapeadorCenario = new MapeadorCenario();
				data.MapeadorCenario.UpdateFromRequest();
			}
			return AdminContent("MapeadorCenario/MapeadorCenarioEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.MapeadorCenario = TempData["MapeadorCenarioModel"] as MapeadorCenario ?? _mapeadorCenarioService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.MapeadorCenario == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("MapeadorCenario/MapeadorCenarioEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var mapeadorCenario = _mapeadorCenarioService.FindByID(id);
			if (mapeadorCenario == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			mapeadorCenario.ID = null;
			TempData["MapeadorCenarioModel"] = mapeadorCenario;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var mapeadorCenario = _mapeadorCenarioService.FindByID(id);
				if (mapeadorCenario == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					foreach (var mapeadorCenarioMes in mapeadorCenario.MapeadorCenarioMesList)
					{
						_mapeadorCenarioTurnoValorService.DeleteMany(_mapeadorCenarioTurnoValorService.GetValoresFromTurnos(mapeadorCenarioMes.MapeadorCenarioTurnoList, true));
						_mapeadorCenarioTurnoService.DeleteMany(mapeadorCenarioMes.MapeadorCenarioTurnoList);
						_mapeadorCenarioMesService.Delete(mapeadorCenarioMes);
					}

					_mapeadorCenarioService.Delete(mapeadorCenario);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
				}
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorCenario" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids)
		{
			try
			{
				var idsMapeadorCenario = ids.Split(',').Select(i => i.ToInt(0));
				if (idsMapeadorCenario.Any())
				{
					foreach (var idMapeadorCenario in idsMapeadorCenario)
					{
						var mapeadorCenario = _mapeadorCenarioService.FindByID(idMapeadorCenario);
						if (mapeadorCenario != null)
						{
							foreach (var mapeadorCenarioMes in mapeadorCenario.MapeadorCenarioMesList)
							{
								_mapeadorCenarioTurnoValorService.DeleteMany(_mapeadorCenarioTurnoValorService.GetValoresFromTurnos(mapeadorCenarioMes.MapeadorCenarioTurnoList, true));
								_mapeadorCenarioTurnoService.DeleteMany(mapeadorCenarioMes.MapeadorCenarioTurnoList);
								_mapeadorCenarioMesService.Delete(mapeadorCenarioMes);
							}
						}
					}
					_mapeadorCenarioService.DeleteMany(idsMapeadorCenario);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
				}
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorCenario" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult Report(int id)
		{
			var data = new ReportViewModel();

			var mapeadorCenario = _mapeadorCenarioService.FindByID(id);
			if (mapeadorCenario != null)
			{
				data.MapeadorCenario = mapeadorCenario;
				data.FeriasVigentes = mapeadorCenario.Ativo.FeriasList;
				if (data.FeriasVigentes.Any())
				{
					data.FeriasVigentes = data.FeriasVigentes.Where(i =>
						(i.DataInicio.Value.Year == mapeadorCenario.Ano.Value)
						|| (i.DataFim.Value.Year == mapeadorCenario.Ano.Value));
				}

				data.Report = _mapeadorCenarioService.GetPrevisao(mapeadorCenario, null, true, false, false);
			}

			return AdminContent("MapeadorCenario/MapeadorCenarioReport.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var mapeadorCenario = new MapeadorCenario();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					mapeadorCenario = _mapeadorCenarioService.FindByID(Request["ID"].ToInt(0));
					if (mapeadorCenario == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				mapeadorCenario.UpdateFromRequest();

				if (!isEdit)
				{
					var checkAtivo = _mapeadorCenarioService.Get(mapeadorCenario.AtivoID.Value, mapeadorCenario.Titulo);
					if (checkAtivo != null)
						throw new Exception("Ativo já possui meses/turnos cadastrados.");
				}

				_mapeadorCenarioService.Save(mapeadorCenario);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? mapeadorCenario.GetAdminURL() : /* Web.AdminHistory.Previous ?? */ Web.BaseUrl + "Admin/MapeadorCenarioMes/?mapceid=" + mapeadorCenario.ID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { mapeadorCenario.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["MapeadorCenarioModel"] = mapeadorCenario;
				return isEdit && mapeadorCenario != null ? RedirectToAction("Edit", new { mapeadorCenario.ID }) : RedirectToAction("Create");
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

		public class ListViewModel
		{
			public List<MapeadorCenario> MapeadorCenarios;
			public Dictionary<int, TarifaVigencia> TarifasVigencia;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public MapeadorCenario MapeadorCenario;
			public Boolean ReadOnly;
		}

		public class ReportViewModel
		{
			public MapeadorCenario MapeadorCenario;
			public MapeadorCenarioPrevisaoDto Report;
			public IEnumerable<Ferias> FeriasVigentes;
		}
	}
}
