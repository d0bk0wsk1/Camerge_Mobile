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
	public class MapeadorPotencialCenarioController : ControllerBase
	{
		private readonly IMapeadorPotencialCenarioService _mapeadorPotencialCenarioService;
		private readonly IMapeadorPotencialCenarioMesService _mapeadorPotencialCenarioMesService;
		private readonly IMapeadorPotencialCenarioMesValorService _mapeadorPotencialCenarioMesValorService;

		public MapeadorPotencialCenarioController(IMapeadorPotencialCenarioService mapeadorPotencialCenarioService,
			IMapeadorPotencialCenarioMesService mapeadorPotencialCenarioMesService,
			IMapeadorPotencialCenarioMesValorService mapeadorPotencialCenarioMesValorService)
		{
			_mapeadorPotencialCenarioService = mapeadorPotencialCenarioService;
			_mapeadorPotencialCenarioMesService = mapeadorPotencialCenarioMesService;
			_mapeadorPotencialCenarioMesValorService = mapeadorPotencialCenarioMesValorService;
		}

		//
		// GET: /Admin/MapeadorPotencialCenario/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _mapeadorPotencialCenarioService.GetAllWithPaging(
				(UserSession.IsPerfilAgente || UserSession.IsPotencialAgente) ? UserSession.Agentes.Select(i => i.ID.Value) : null,
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.MapeadorPotencialCenarios = paging.Items;

			return AdminContent("MapeadorPotencialCenario/MapeadorPotencialCenarioList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.MapeadorPotencialCenario = TempData["MapeadorPotencialCenarioModel"] as MapeadorPotencialCenario;
			if (data.MapeadorPotencialCenario == null)
			{
				data.MapeadorPotencialCenario = new MapeadorPotencialCenario();
				data.MapeadorPotencialCenario.UpdateFromRequest();
			}
			return AdminContent("MapeadorPotencialCenario/MapeadorPotencialCenarioEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.MapeadorPotencialCenario = TempData["MapeadorPotencialCenarioModel"] as MapeadorPotencialCenario ?? _mapeadorPotencialCenarioService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.MapeadorPotencialCenario == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("MapeadorPotencialCenario/MapeadorPotencialCenarioEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var mapeadorCenario = _mapeadorPotencialCenarioService.FindByID(id);
			if (mapeadorCenario == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			mapeadorCenario.ID = null;
			TempData["MapeadorPotencialCenarioModel"] = mapeadorCenario;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var mapeadorPotencialCenario = _mapeadorPotencialCenarioService.FindByID(id);
				if (mapeadorPotencialCenario == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					foreach (var mapeadorPotencialCenarioMes in mapeadorPotencialCenario.MapeadorPotencialCenarioMesList)
					{
						_mapeadorPotencialCenarioMesValorService.DeleteMany(mapeadorPotencialCenarioMes.MapeadorPotencialCenarioMesValorList);
						_mapeadorPotencialCenarioMesService.Delete(mapeadorPotencialCenarioMes);
					}

					_mapeadorPotencialCenarioService.Delete(mapeadorPotencialCenario);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorPotencialCenario" }, JsonRequestBehavior.AllowGet);
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
				var idsMapeadorPotencialCenario = ids.Split(',').Select(i => i.ToInt(0));
				if (idsMapeadorPotencialCenario.Any())
				{
					foreach (var idMapeadorPotencialCenario in idsMapeadorPotencialCenario)
					{
						var mapeadorPotencialCenario = _mapeadorPotencialCenarioService.FindByID(idMapeadorPotencialCenario);
						if (mapeadorPotencialCenario != null)
						{
							foreach (var mapeadorPotencialCenarioMes in mapeadorPotencialCenario.MapeadorPotencialCenarioMesList)
							{
								_mapeadorPotencialCenarioMesValorService.DeleteMany(mapeadorPotencialCenarioMes.MapeadorPotencialCenarioMesValorList);
								_mapeadorPotencialCenarioMesService.Delete(mapeadorPotencialCenarioMes);
							}
						}
					}
					_mapeadorPotencialCenarioService.DeleteMany(idsMapeadorPotencialCenario);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorPotencialCenario" }, JsonRequestBehavior.AllowGet);
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

			var mapeadorPotencialCenario = _mapeadorPotencialCenarioService.FindByID(id);
			if (mapeadorPotencialCenario != null)
			{
				data.MapeadorPotencialCenario = mapeadorPotencialCenario;
				data.Consolidado = _mapeadorPotencialCenarioService.GetConsolidado(mapeadorPotencialCenario);

				if (data.Consolidado != null)
					data.Resumo = _mapeadorPotencialCenarioService.GetResumoMapeamento(data.Consolidado);

				data.FeriasVigentes = mapeadorPotencialCenario.Ativo.FeriasList;
				if (data.FeriasVigentes.Any())
				{
					data.FeriasVigentes = data.FeriasVigentes.Where(i =>
						(i.DataInicio.Value.Year == mapeadorPotencialCenario.Ano.Value)
						|| (i.DataFim.Value.Year == mapeadorPotencialCenario.Ano.Value));
				}
			}

			return AdminContent("MapeadorPotencialCenario/MapeadorPotencialCenarioReport.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var mapeadorCenario = new MapeadorPotencialCenario();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					mapeadorCenario = _mapeadorPotencialCenarioService.FindByID(Request["ID"].ToInt(0));
					if (mapeadorCenario == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				mapeadorCenario.UpdateFromRequest();
				_mapeadorPotencialCenarioService.Save(mapeadorCenario);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? mapeadorCenario.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/MapeadorPotencialCenario";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { mapeadorCenario.ID });

				/*
				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index");
				*/

				return RedirectToAction("Index", "MapeadorPotencialCenarioMes", new { mapptnceid = mapeadorCenario.ID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["MapeadorPotencialCenarioModel"] = mapeadorCenario;
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
			public List<MapeadorPotencialCenario> MapeadorPotencialCenarios;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public MapeadorPotencialCenario MapeadorPotencialCenario;
			public Boolean ReadOnly;
		}

		public class ReportViewModel
		{
			public MapeadorPotencialCenario MapeadorPotencialCenario;
			public IEnumerable<Ferias> FeriasVigentes;
			public MapeadorPotencialCenarioConsolidadoDto Consolidado;
			public MapeadorPotencialCenarioResumoMapeamentoDto Resumo;
		}
	}
}
