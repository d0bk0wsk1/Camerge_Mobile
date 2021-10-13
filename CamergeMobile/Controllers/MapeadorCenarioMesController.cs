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
	public class MapeadorCenarioMesController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IMapeadorCenarioService _mapeadorCenarioService;
		private readonly IMapeadorCenarioMesService _mapeadorCenarioMesService;
		private readonly IMapeadorCenarioTurnoService _mapeadorCenarioTurnoService;
		private readonly IMapeadorCenarioTurnoValorService _mapeadorCenarioTurnoValorService;
		private readonly IMapeadorMedicaoCacheService _mapeadorMedicaoCacheService;
		private readonly IMedicaoCacheMesTarifacaoService _medicaoCacheMesTarifacaoService;

		public MapeadorCenarioMesController(IAtivoService ativoService,
			IMapeadorCenarioService mapeadorCenarioService,
			IMapeadorCenarioMesService mapeadorCenarioMesService,
			IMapeadorCenarioTurnoService mapeadorCenarioTurnoService,
			IMapeadorCenarioTurnoValorService mapeadorCenarioTurnoValorService,
			IMapeadorMedicaoCacheService mapeadorMedicaoCacheService,
			IMedicaoCacheMesTarifacaoService medicaoCacheMesTarifacaoService
			)
		{
			_ativoService = ativoService;
			_mapeadorCenarioService = mapeadorCenarioService;
			_mapeadorCenarioMesService = mapeadorCenarioMesService;
			_mapeadorCenarioTurnoService = mapeadorCenarioTurnoService;
			_mapeadorCenarioTurnoValorService = mapeadorCenarioTurnoValorService;
			_mapeadorMedicaoCacheService = mapeadorMedicaoCacheService;
			_medicaoCacheMesTarifacaoService = medicaoCacheMesTarifacaoService;
			;
		}

		//
		// GET: /Admin/MapeadorCenarioMes/
		public ActionResult Index(Int32? mapceid, Int32? Page)
		{
			var actionParams = Request.Params;

			if (mapceid != null)
			{
				var data = new ListViewModel();

				var paging = _mapeadorCenarioMesService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), actionParams);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.MapeadorCenarioMeses = paging.Items;
				data.MapeadorCenario = _mapeadorCenarioService.FindByID(mapceid.Value);

				return AdminContent("MapeadorCenarioMes/MapeadorCenarioMesList.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Create(Int32? mapceid)
		{
			if (mapceid != null)
			{
				var data = new FormViewModel();
				data.MapeadorCenarioMes = TempData["MapeadorCenarioMesModel"] as MapeadorCenarioMes;
				data.MapeadorCenario = _mapeadorCenarioService.FindByID(mapceid.Value);
				if (data.MapeadorCenarioMes == null)
				{
					data.MapeadorCenarioMes = new MapeadorCenarioMes();
					data.MapeadorCenarioMes.MapeadorCenarioID = mapceid.Value;

					data.MapeadorCenarioMes.UpdateFromRequest();
				}

				var today = DateTime.Today;

				data.TurnosInDateFormat = _mapeadorMedicaoCacheService.GetTurnosInDateFormat(
					data.MapeadorCenario.AtivoID.Value,
					today.AddMonths(12)
				);

				return AdminContent("MapeadorCenarioMes/MapeadorCenarioMesEdit.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.MapeadorCenarioMes = TempData["MapeadorCenarioMesModel"] as MapeadorCenarioMes ?? _mapeadorCenarioMesService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.MapeadorCenarioMes == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.MapeadorCenario = data.MapeadorCenarioMes.MapeadorCenario;

			/*
			data.TurnosInDateFormat = _mapeadorMedicaoCacheService.GetTurnosInDateFormat(
				data.MapeadorCenario.AtivoID.Value,
				data.MapeadorCenarioMes.Mes.AddMonths(-24),
				Dates.GetLastDayOfMonth(data.MapeadorCenarioMes.Mes.AddMonths(6))
			);
			*/

			return AdminContent("MapeadorCenarioMes/MapeadorCenarioMesEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var mapeadorCenarioTurno = _mapeadorCenarioMesService.FindByID(id);
			if (mapeadorCenarioTurno == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			mapeadorCenarioTurno.ID = null;
			TempData["MapeadorCenarioMesModel"] = mapeadorCenarioTurno;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(mapeadorCenarioTurno.MapeadorCenarioID);
		}

		public ActionResult Del(Int32 id)
		{
			var mapeadorCenarioMes = _mapeadorCenarioMesService.FindByID(id);
			if (mapeadorCenarioMes == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_mapeadorCenarioTurnoValorService.DeleteMany(_mapeadorCenarioTurnoValorService.GetValoresFromTurnos(mapeadorCenarioMes.MapeadorCenarioTurnoList, true));
				_mapeadorCenarioTurnoService.DeleteMany(mapeadorCenarioMes.MapeadorCenarioTurnoList);
				_mapeadorCenarioMesService.Delete(mapeadorCenarioMes);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorCenarioMes/?mapceid=" + mapeadorCenarioMes.MapeadorCenarioID }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index", new { mapceid = mapeadorCenarioMes.MapeadorCenarioID });
		}

		public ActionResult DelMultiple(String ids)
		{
			int? mapceid = null;

			try
			{
				var idsMapeadorCenarioMes = ids.Split(',').Select(i => i.ToInt(0));
				if (idsMapeadorCenarioMes.Any())
				{
					foreach (var idMapeadorCenarioMes in idsMapeadorCenarioMes)
					{
						var mapeadorCenarioMes = _mapeadorCenarioMesService.FindByID(idMapeadorCenarioMes);
						if (mapeadorCenarioMes != null)
						{
							if (mapceid == null)
								mapceid = mapeadorCenarioMes.MapeadorCenarioID;

							_mapeadorCenarioTurnoValorService.DeleteMany(_mapeadorCenarioTurnoValorService.GetValoresFromTurnos(mapeadorCenarioMes.MapeadorCenarioTurnoList, true));
							_mapeadorCenarioTurnoService.DeleteMany(mapeadorCenarioMes.MapeadorCenarioTurnoList);
							_mapeadorCenarioMesService.Delete(mapeadorCenarioMes);
						}
					}
					_mapeadorCenarioMesService.DeleteMany(idsMapeadorCenarioMes);
					Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() }, JsonRequestBehavior.AllowGet);
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorCenarioMes/?mapceid=" + mapceid }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index", new { mapceid = mapceid });
		}

		public ActionResult GenerateMonths(int mapceid, bool rewrite = false)
		{
			var mapeadorCenario = _mapeadorCenarioService.FindByID(mapceid);
			if (mapeadorCenario == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				var ano = mapeadorCenario.Ano.Value;
				var mapeadorCenarioMeses = mapeadorCenario.MapeadorCenarioMesList;

				for (var mes = new DateTime(ano, 1, 1); mes <= new DateTime(ano, 12, 1); mes = mes.AddMonths(1))
				{
					var mapeadorCenarioMes = mapeadorCenarioMeses.FirstOrDefault(i => i.Mes == mes);
					if (mapeadorCenarioMes != null)
					{
						if (rewrite)
						{
							_mapeadorCenarioTurnoValorService.DeleteMany(_mapeadorCenarioTurnoValorService.GetValoresFromTurnos(mapeadorCenarioMes.MapeadorCenarioTurnoList, true));
							_mapeadorCenarioTurnoService.DeleteMany(mapeadorCenarioMes.MapeadorCenarioTurnoList);
							_mapeadorCenarioMesService.Delete(mapeadorCenarioMes);
						}
						else
						{
							continue;
						}
					}

					mapeadorCenarioMes = new MapeadorCenarioMes() { MapeadorCenarioID = mapeadorCenario.ID, Mes = mes };
					mapeadorCenarioMes.MapeadorCenarioTurnoList = _mapeadorCenarioTurnoService.GetCompletedTurnos(mapeadorCenarioMes, true);

					if (mapeadorCenarioMes.MapeadorCenarioTurnoList.Any())
						Web.SetMessage("Meses criados com suesso.");
					else
						return Json(new { success = false, message = "Falha ao criar o meses automaticamente." }, JsonRequestBehavior.AllowGet);
				}
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorCenarioMes/?mapceid=" + mapeadorCenario.ID }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index", new { mapceid = mapeadorCenario.ID });
		}

		public ActionResult Report(int mapceid)
		{
			var data = new ReportViewModel();

			var mapeadorCenario = _mapeadorCenarioService.FindByID(mapceid);
			if (mapeadorCenario != null)
			{
				// var mapeadorCenarioMeses = _mapeadorCenarioMesService.Report(mapeadorCenario);

				var mapeadorCenarioMeses = _mapeadorCenarioService.GetPrevisao(mapeadorCenario, null, false, false, true);
				if ((mapeadorCenarioMeses != null) && (mapeadorCenarioMeses.Meses.Any()))
				{
					data.Report = mapeadorCenarioMeses;
					data.Chart = GetChartViewModel(mapeadorCenarioMeses.Meses, mapeadorCenario.Ativo, mapeadorCenario.Ano.Value);
					data.Resumo = GetReportResumoViewModel(mapeadorCenarioMeses.Meses);
				}

				data.MapeadorCenario = mapeadorCenario;
				data.UnidadeMedida = Request["unidade"] == "MWm" ? "MWm" : "MWh";

				data.FeriasVigentes = mapeadorCenario.Ativo.FeriasList;
				if (data.FeriasVigentes.Any())
				{
					data.FeriasVigentes = data.FeriasVigentes.Where(i =>
						(i.DataInicio.Value.Year == mapeadorCenario.Ano.Value)
						|| (i.DataFim.Value.Year == mapeadorCenario.Ano.Value));
				}
			}

			return AdminContent("MapeadorCenarioMes/MapeadorCenarioMesReport.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var mapeadorCenarioMes = new MapeadorCenarioMes();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					mapeadorCenarioMes = _mapeadorCenarioMesService.FindByID(Request["ID"].ToInt(0));
					if (mapeadorCenarioMes == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				mapeadorCenarioMes.UpdateFromRequest();

				var mapeadorCenario = _mapeadorCenarioService.FindByID(mapeadorCenarioMes.MapeadorCenarioID.Value);
				if (mapeadorCenario == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				var checkMes = _mapeadorCenarioMesService.Get(mapeadorCenario.ID.Value, mapeadorCenarioMes.Mes);
				if ((checkMes != null) && (checkMes.ID != mapeadorCenarioMes.ID))
					throw new Exception("Já existe um mês cadastrado para este ativo.");

				_mapeadorCenarioMesService.Save(mapeadorCenarioMes);

				// Insert Turnos Valores
				var turnoValorMes = Request["TurnoValorMes"].ConvertToDate(null);
				if (turnoValorMes != null)
				{
					var turnos = _mapeadorCenarioTurnoService.GetInsertedTurnos(mapeadorCenario.Ativo, mapeadorCenarioMes.ID.Value, mapeadorCenarioMes.Mes);
					if (turnos.Any())
						_mapeadorCenarioTurnoValorService.AddByTurnos(turnos, turnoValorMes.Value, Fmt.ToDouble(Request["TurnoValorPercentual"], false, true));
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? mapeadorCenarioMes.GetAdminURL() : Web.BaseUrl + "Admin/MapeadorCenarioMes/?mapceid=" + mapeadorCenarioMes.MapeadorCenarioID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { mapeadorCenarioMes.ID });
				}

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
				{
					return Redirect(previousUrl);
				}

				return RedirectToAction("Index");

			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				TempData["MapeadorCenarioMesModel"] = mapeadorCenarioMes;
				return isEdit && mapeadorCenarioMes != null ? RedirectToAction("Edit", new { mapeadorCenarioMes.ID }) : RedirectToAction("Create", mapeadorCenarioMes.MapeadorCenarioID);
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

		private ReportResumoViewModel GetReportResumoViewModel(List<MapeadorCenarioPrevisaoMesDto> mapeadorCenarioMeses)
		{
			if (mapeadorCenarioMeses != null)
			{
				var totalMWh = mapeadorCenarioMeses.Sum(i => i.MWhTotal);

				return new ReportResumoViewModel()
				{
					TotalMWh = totalMWh,
					TotalMWm = (totalMWh / mapeadorCenarioMeses.Sum(i => i.HorasTotal)),
					MaximaMWmPositiva = mapeadorCenarioMeses.Max(i => i.MWmTotal ?? 0),
					MaximaMWmNegativa = mapeadorCenarioMeses.Min(i => i.MWmTotal ?? 0)
				};
			}
			return null;
		}

		private List<ChartViewModel> GetChartViewModel(List<MapeadorCenarioPrevisaoMesDto> mapeadorMeses, Ativo ativo, int ano)
		{
			var list = new List<ChartViewModel>();

			var tipoLeitura = (ativo.IsConsumidor) ? Medicao.TiposLeitura.Consumo.ToString() : Medicao.TiposLeitura.Geracao.ToString();

			for (int y = ano; y >= (ano - 2); y--)
			{
				var dd = _medicaoCacheMesTarifacaoService.Get(ativo, tipoLeitura, new DateTime(ano, 1, 1), new DateTime(ano, 12, 31));
				var medicoes = _medicaoCacheMesTarifacaoService.Get(ativo, tipoLeitura, new DateTime(ano, 1, 1), new DateTime(ano, 12, 31));
				if (medicoes.Any())
				{
					var chart = new ChartViewModel() { Nome = y.ToString(), Items = new List<ChartItemViewModel>() };

					foreach (var medicao in medicoes)
						chart.Items.Add(new ChartItemViewModel() { Mes = medicao.Mes.Value, MWh = medicao.MwhTotal, MWm = (medicao.MwhTotal / medicao.HorasTotal) });

					list.Add(chart);
				}
			}

			if (mapeadorMeses.Any())
			{
				var chart = new ChartViewModel() { Nome = "Previsão", Items = new List<ChartItemViewModel>() };

				foreach (var mapeadorMes in mapeadorMeses)
					chart.Items.Add(new ChartItemViewModel() { Mes = mapeadorMes.Mes, MWh = mapeadorMes.MWhTotal, MWm = mapeadorMes.MWmTotal });

				list.Add(chart);
			}

			return list;
		}

		public class ListViewModel
		{
			public List<MapeadorCenarioMes> MapeadorCenarioMeses;
			public MapeadorCenario MapeadorCenario;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public MapeadorCenarioMes MapeadorCenarioMes;
			public MapeadorCenario MapeadorCenario;
			public List<DateTime> TurnosInDateFormat;
			public Boolean ReadOnly;
		}

		public class ReportViewModel
		{
			public MapeadorCenario MapeadorCenario;
			public MapeadorCenarioPrevisaoDto Report;
			public ReportResumoViewModel Resumo;
			public string UnidadeMedida;
			public IEnumerable<Ferias> FeriasVigentes;
			public List<ChartViewModel> Chart;
		}

		public class ReportResumoViewModel
		{
			public double TotalMWh;
			public double TotalMWm;
			public double MaximaMWmPositiva;
			public double MaximaMWmNegativa;
			public double VariacaoMaximaPositiva
			{
				get
				{
					return ((MaximaMWmPositiva / TotalMWm) - 1);
				}
			}
			public double VariacaoMaximaNegativa
			{
				get
				{
					return ((MaximaMWmNegativa / TotalMWm) - 1);
				}
			}
		}

		public class ChartViewModel
		{
			public string Nome;
			public List<ChartItemViewModel> Items;
			public string GetChartData(string unidadeMedida)
			{
				var values = new List<string>();
				var items = new List<ChartItemViewModel>();

				if (Items.Any())
				{
					var lastMes = Items.Max(m => m.Mes);

					for (var mes = new DateTime(lastMes.Year, 1, 1); mes <= lastMes; mes = mes.AddMonths(1))
					{
						var item = Items.FirstOrDefault(i => i.Mes == mes);
						if (item == null)
							items.Add(new ChartItemViewModel() { Mes = mes });
						else
							items.Add(item);
					}

					// items = items.OrderBy(i => i.Mes).ToList();
					items.Sort((x, y) => x.Mes.CompareTo(y.Mes));

					if (unidadeMedida == "MWh")
						values = items.Select(x => ((x.MWh == null) || (x.MWh == 0)) ? "null" : x.MWh.Value.ToString("N3").Replace(".", null).Replace(',', '.')).ToList();
					else
						values = items.Select(x => ((x.MWm == null) || (x.MWm == 0)) ? "null" : x.MWm.Value.ToString("N3").Replace(".", null).Replace(',', '.')).ToList();
				}

				return values.Join(",");
			}
		}

		public class ChartItemViewModel
		{
			public DateTime Mes;
			public double? MWh;
			public double? MWm;
		}
	}
}
