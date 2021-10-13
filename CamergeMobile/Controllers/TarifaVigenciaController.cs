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
	public class TarifaVigenciaController : ControllerBase
	{
		private readonly IAgenteConectadoService _agenteConectadoService;
		private readonly IAtivoService _ativoService;
		private readonly ITarifaService _tarifaService;
		private readonly ITarifaVigenciaService _tarifaVigenciaService;
		private readonly ITarifaVigenciaValorService _tarifaVigenciaValorService;

		public TarifaVigenciaController(IAgenteConectadoService agenteConectadoService,
			IAtivoService ativoService,
			ITarifaService tarifaService,
			ITarifaVigenciaService tarifaVigenciaService,
			ITarifaVigenciaValorService tarifaVigenciaValorService)
		{
			_agenteConectadoService = agenteConectadoService;
			_ativoService = ativoService;
			_tarifaService = tarifaService;
			_tarifaVigenciaService = tarifaVigenciaService;
			_tarifaVigenciaValorService = tarifaVigenciaValorService;
		}

		//
		// GET: /Admin/TarifaVigencia/
		public ActionResult Index(Int32? agcoid, Int32? taid, Int32? Page)
		{
			var actionParams = Request.Params;

			if ((agcoid != null) && (taid == null))
			{
				if (UserSession.IsPerfilAgente)
				{
					if (!UserSession.Agentes.Any(ag => ag.PerfilAgenteList.Any(peag => peag.AtivoList.Any(at => at.AgenteConectadoID == agcoid.Value))))
						throw new Exception("Este perfil de agente não possui acesso para visualizar esta página.");
				}

				taid = GetIdByAutoCreatedVigencia(agcoid);
				actionParams = Fmt.GetNewNameValueCollection(new { taid = taid, Page = Page }, Request.Params);
			}

			if (taid != null)
			{
				var data = new ListViewModel();

				var paging = _tarifaVigenciaService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), actionParams);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.TarifaVigencias = paging.Items;
				data.Tarifa = _tarifaService.FindByID(taid.Value);

				return AdminContent("TarifaVigencia/TarifaVigenciaList.aspx", data);
			}
			return HttpNotFound();
		}

		public JsonResult GetTarifaVigenciasByAgenteConectado(int? agenteConectadoId = null)
		{
			var options = new Dictionary<string, string>();
			options.Add("hstc", "Histórico");

			if (agenteConectadoId != null)
			{
				var vigencias = _tarifaVigenciaService.GetByAgenteConectado(agenteConectadoId.Value);
				if (vigencias.Any())
				{
					foreach (var vigencia in vigencias)
					{
						options.Add(vigencia.ID.Value.ToString(), string.Format("{0:dd/MMM/yyyy}-{1:dd/MMM/yyyy}", vigencia.VigenciaInicio, vigencia.VigenciaFim));
					}
				}
			}

			if (options.Count() == 1)
				options.Add("rcnt", "Mais Recente");

			return Json(options.Select(i => new { i.Key, i.Value }), JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetTarifaVigenciasByAtivo(string ids, int? agenteConectadoId = null)
		{
			var options = new Dictionary<string, string>();
			options.Add("hstc", "Histórico");

			var singleId = false;

			var id = Request["ids"];
			if (id.IsNotBlank())
			{
				singleId = (!id.Contains(','));

				int ativoID;
				if ((singleId) && (int.TryParse(id, out ativoID)))
				{
					var vigencias = _tarifaVigenciaService.GetByAtivo(ativoID);
					if (vigencias.Any())
						foreach (var vigencia in vigencias)
							options.Add(vigencia.ID.Value.ToString(), string.Format("{0:dd/MMM/yyyy}-{1:dd/MMM/yyyy}", vigencia.VigenciaInicio, vigencia.VigenciaFim));
				}
			}

			if (options.Count() == 1)
				options.Add("rcnt", "Mais Recente");

			return Json(options.Select(i => new { i.Key, i.Value }), JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create(Int32? taid)
		{
			if (taid != null)
			{
				var data = new FormViewModel();
				data.TarifaVigencia = TempData["TarifaVigenciaModel"] as TarifaVigencia;
				data.Tarifa = _tarifaService.FindByID(taid.Value);
				if (data.TarifaVigencia == null)
				{
					data.TarifaVigencia = new TarifaVigencia();
					data.TarifaVigencia.TarifaID = taid.Value;

					data.TarifaVigencia.UpdateFromRequest();
				}
				return AdminContent("TarifaVigencia/TarifaVigenciaEdit.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.TarifaVigencia = TempData["TarifaVigenciaModel"] as TarifaVigencia ?? _tarifaVigenciaService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.TarifaVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.Tarifa = data.TarifaVigencia.Tarifa;

			return AdminContent("TarifaVigencia/TarifaVigenciaEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var tarifaVigencia = _tarifaVigenciaService.FindByID(id);
			if (tarifaVigencia == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			tarifaVigencia.ID = null;
			TempData["TarifaVigenciaModel"] = tarifaVigencia;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(tarifaVigencia.TarifaID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var tarifaVigencia = _tarifaVigenciaService.FindByID(id);
				if (tarifaVigencia == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_tarifaVigenciaValorService.DeleteByTarifaVigenciaID(tarifaVigencia.ID.Value);
					_tarifaVigenciaService.Delete(tarifaVigencia);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TarifaVigencia" }, JsonRequestBehavior.AllowGet);
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
				var idsTarifaVigencia = ids.Split(',').Select(i => i.ToInt(0));
				if (idsTarifaVigencia.Any())
				{
					foreach (var idTarifaVigencia in idsTarifaVigencia)
						_tarifaVigenciaValorService.DeleteByTarifaVigenciaID(idTarifaVigencia);
					_tarifaVigenciaService.DeleteMany(idsTarifaVigencia);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/TarifaVigencia" }, JsonRequestBehavior.AllowGet);
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
			var tarifaVigencia = new TarifaVigencia();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					tarifaVigencia = _tarifaVigenciaService.FindByID(Request["ID"].ToInt(0));
					if (tarifaVigencia == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				tarifaVigencia.UpdateFromRequest();

				var tarifa = _tarifaService.FindByID(tarifaVigencia.TarifaID);
				if (tarifa == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				if (tarifaVigencia.VigenciaInicio > tarifaVigencia.VigenciaFim)
					throw new Exception("Data final deve ser superior a data inicial.");

				var checkVigencia = _tarifaVigenciaService.Get(tarifa.AgenteConectadoID.Value, tarifaVigencia.VigenciaInicio, (isEdit) ? tarifaVigencia.ID : null);
				if (checkVigencia != null)
					throw new Exception("Já existe uma vigência cadastrada neste período para este agente conectado.");

				_tarifaVigenciaService.Save(tarifaVigencia);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? tarifaVigencia.GetAdminURL() : Web.BaseUrl + "Admin/TarifaVigencia/?taid=" + tarifaVigencia.TarifaID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { tarifaVigencia.ID });
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
				TempData["TarifaVigenciaModel"] = tarifaVigencia;
				return isEdit && tarifaVigencia != null ? RedirectToAction("Edit", new { tarifaVigencia.ID }) : RedirectToAction("Create", tarifaVigencia.TarifaID);
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

		public int? GetIdByAutoCreatedVigencia(int? agcoid = null)
		{
			if (agcoid != null)
			{
				var agenteConectado = _agenteConectadoService.FindByID(agcoid.Value);
				if (agenteConectado != null)
				{
					var tarifa = _tarifaService.Find(new Sql("WHERE agente_conectado_id = @0;", agenteConectado.ID));
					if (tarifa == null)
					{
						var model = new Tarifa() { AgenteConectadoID = agenteConectado.ID };

						_tarifaService.Insert(model);

						return model.ID.Value;
					}
					return tarifa.ID.Value;
				}
			}
			return null;
		}

		public class ListViewModel
		{
			public List<TarifaVigencia> TarifaVigencias;
			public Tarifa Tarifa;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public TarifaVigencia TarifaVigencia;
			public Tarifa Tarifa;
			public Boolean ReadOnly;
		}
	}
}
