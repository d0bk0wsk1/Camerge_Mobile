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
	public class TarifaVigenciaValorController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly ITarifaService _tarifaService;
		private readonly ITarifaVigenciaService _tarifaVigenciaService;
		private readonly ITarifaVigenciaValorService _tarifaVigenciaValorService;

		public TarifaVigenciaValorController(IAtivoService ativoService,
			ITarifaService tarifaService,
			ITarifaVigenciaService tarifaVigenciaService,
			ITarifaVigenciaValorService tarifaVigenciaValorService)
		{
			_ativoService = ativoService;
			_tarifaService = tarifaService;
			_tarifaVigenciaService = tarifaVigenciaService;
			_tarifaVigenciaValorService = tarifaVigenciaValorService;
		}

		//
		// GET: /Admin/TarifaVigenciaValor/
		public ActionResult Index(Int32? taid, Int32? taviid, Int32? Page)
		{
			if ((taid != null) && (taviid != null))
			{
				var ativos = new List<Ativo>();
				var actionParams = Request.Params;

				if (UserSession.IsPerfilAgente)
					ativos = _ativoService.GetByAgentes(UserSession.Agentes.Select(i => i.ID.Value)).ToList();

				var data = new ListViewModel();

				var paging = _tarifaVigenciaValorService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), actionParams, ativos);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.TarifaVigenciaValores = paging.Items;
				data.TarifaVigencia = _tarifaVigenciaService.FindByID(taviid.Value);

				return AdminContent("TarifaVigenciaValor/TarifaVigenciaValorList.aspx", data);
			}
			return HttpNotFound();
		}

		//
		// GET: /Admin/GetTarifaVigenciaValors/
		public JsonResult GetTarifaVigenciaValores()
		{
			var TarifaVigenciaValors = _tarifaVigenciaValorService.GetAll().Select(o => new { o.ID, o.Modalidade });
			return Json(TarifaVigenciaValors, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create(Int32? taid, Int32? taviid)
		{
			if ((taid != null) && (taviid != null))
			{
				var data = new FormViewModel();
				data.TarifaVigenciaValor = TempData["TarifaVigenciaValorModel"] as TarifaVigenciaValor;
				data.TarifaVigencia = _tarifaVigenciaService.FindByID(taviid.Value);
				if (data.TarifaVigenciaValor == null)
				{
					data.TarifaVigenciaValor = new TarifaVigenciaValor();
					data.TarifaVigenciaValor.TarifaVigenciaID = taviid.Value;

					data.TarifaVigenciaValor.UpdateFromRequest();
				}
				return AdminContent("TarifaVigenciaValor/TarifaVigenciaValorEdit.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.TarifaVigenciaValor = TempData["TarifaVigenciaValorModel"] as TarifaVigenciaValor ?? _tarifaVigenciaValorService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.TarifaVigenciaValor == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.TarifaVigencia = data.TarifaVigenciaValor.TarifaVigencia;

			return AdminContent("TarifaVigenciaValor/TarifaVigenciaValorEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var TarifaVigenciaValor = _tarifaVigenciaValorService.FindByID(id);
			if (TarifaVigenciaValor == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			TarifaVigenciaValor.ID = null;
			TempData["TarifaVigenciaValorModel"] = TarifaVigenciaValor;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(TarifaVigenciaValor.TarifaVigencia.TarifaID, TarifaVigenciaValor.TarifaVigenciaID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var TarifaVigenciaValor = _tarifaVigenciaValorService.FindByID(id);
				if (TarifaVigenciaValor == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					//_tarifaVigenciaValorService.Delete(TarifaVigenciaValor);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TarifaVigenciaValor" }, JsonRequestBehavior.AllowGet);
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
				_tarifaVigenciaValorService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TarifaVigenciaValor" }, JsonRequestBehavior.AllowGet);
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
			var tarifaVigenciaValor = new TarifaVigenciaValor();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					tarifaVigenciaValor = _tarifaVigenciaValorService.FindByID(Request["ID"].ToInt(0));
					if (tarifaVigenciaValor == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				tarifaVigenciaValor.UpdateFromRequest();

				var tarifaVigencia = _tarifaVigenciaService.FindByID(tarifaVigenciaValor.TarifaVigenciaID);
				if (tarifaVigencia == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				var tarifa = tarifaVigencia.Tarifa;

				var checkVigencia = _tarifaVigenciaValorService.Get(tarifa.AgenteConectadoID.Value, tarifaVigencia.VigenciaInicio, tarifaVigenciaValor.ClasseID, tarifaVigenciaValor.Modalidade, (isEdit) ? tarifaVigenciaValor.ID : null);
				if (checkVigencia != null)
					throw new Exception("Já existe uma vigência cadastrada neste período, nesta modalidade, nesta classe e para este agente conectado.");

				_tarifaVigenciaValorService.Save(tarifaVigenciaValor);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? tarifaVigenciaValor.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + string.Format("Admin/TarifaVigenciaValor/?taid={0}&taviid={1}", tarifa.ID, tarifaVigencia.ID);
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { tarifaVigenciaValor.ID });
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
				TempData["TarifaVigenciaValorModel"] = tarifaVigenciaValor;
				return isEdit && tarifaVigenciaValor != null ? RedirectToAction("Edit", new { tarifaVigenciaValor.ID }) : RedirectToAction("Create", new { taid = tarifaVigenciaValor.TarifaVigencia.TarifaID, taviid = tarifaVigenciaValor.TarifaVigenciaID });
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
			public List<TarifaVigenciaValor> TarifaVigenciaValores;
			public TarifaVigencia TarifaVigencia;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public TarifaVigenciaValor TarifaVigenciaValor;
			public TarifaVigencia TarifaVigencia;
			public Boolean ReadOnly;
		}
	}
}
