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
	public class TarifaController : ControllerBase
	{
		private readonly IAgenteConectadoService _agenteConectadoService;
		private readonly ITarifaService _tarifaService;
		private readonly ITarifaVigenciaService _tarifaVigenciaService;

		public TarifaController(IAgenteConectadoService agenteConectadoService,
			ITarifaService tarifaService,
			ITarifaVigenciaService tarifaVigenciaService)
		{
			_agenteConectadoService = agenteConectadoService;
			_tarifaService = tarifaService;
			_tarifaVigenciaService = tarifaVigenciaService;
		}

		//
		// GET: /Admin/Tarifa/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			IEnumerable<int> agentesConectadosId = null;
			if (UserSession.IsPerfilAgente || UserSession.IsPotencialAgente)
				agentesConectadosId = _agenteConectadoService.GetIdsByAgentes(UserSession.Agentes);

			var paging = _tarifaService.GetDetailedDtoPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params,
				agentesConectadosId);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Tarifas = paging.Items;

			return AdminContent("Tarifa/TarifaList.aspx", data);
		}

		//
		// GET: /Admin/GetTarifas/
		public JsonResult GetTarifas()
		{
			var tarifas = _tarifaService.GetAll().Select(o => new { o.ID, o.AgenteConectado.Nome });
			return Json(tarifas, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Tarifa = TempData["TarifaModel"] as Tarifa;
			if (data.Tarifa == null)
			{
				data.Tarifa = new Tarifa();
				data.Tarifa.UpdateFromRequest();
			}
			return AdminContent("Tarifa/TarifaEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Tarifa = TempData["TarifaModel"] as Tarifa ?? _tarifaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Tarifa == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Tarifa/TarifaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var tarifa = _tarifaService.FindByID(id);
			if (tarifa == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			tarifa.ID = null;
			TempData["TarifaModel"] = tarifa;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var tarifa = _tarifaService.FindByID(id);
				if (tarifa == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_tarifaVigenciaService.DeleteByTarifaID(tarifa.ID.Value);
					_tarifaService.Delete(tarifa);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Tarifa" }, JsonRequestBehavior.AllowGet);
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
				var idsTarifa = ids.Split(',').Select(i => i.ToInt(0));
				if (idsTarifa.Any())
				{
					foreach (var idTarifa in idsTarifa)
						_tarifaVigenciaService.DeleteByTarifaID(idTarifa);
					_tarifaService.DeleteMany(idsTarifa);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Tarifa" }, JsonRequestBehavior.AllowGet);
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var tarifa = new Tarifa();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					tarifa = _tarifaService.FindByID(Request["ID"].ToInt(0));
					if (tarifa == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				tarifa.UpdateFromRequest();

				if (!isEdit)
				{
					var checkAgenteConectado = Tarifa.LoadByAgenteConectadoID(tarifa.AgenteConectadoID);
					if (checkAgenteConectado != null)
						throw new Exception("Agente conectado já possui vigências tarifárias cadastradas.");
				}

				_tarifaService.Save(tarifa);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? tarifa.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Tarifa";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { tarifa.ID });
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
				TempData["TarifaModel"] = tarifa;
				return isEdit && tarifa != null ? RedirectToAction("Edit", new { tarifa.ID }) : RedirectToAction("Create");
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
			public List<TarifaDetailedDto> Tarifas;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Tarifa Tarifa;
			public Boolean ReadOnly;
		}
	}
}
