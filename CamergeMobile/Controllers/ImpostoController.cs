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
	public class ImpostoController : Controller
	{
		private readonly IAgenteConectadoService _agenteConectadoService;
		private readonly IImpostoService _impostoService;
		private readonly IImpostoVigenciaService _impostoVigenciaService;

		public ImpostoController(IAgenteConectadoService agenteConectadoService,
			IImpostoService impostoService,
			IImpostoVigenciaService impostoVigenciaService)
		{
			_agenteConectadoService = agenteConectadoService;
			_impostoService = impostoService;
			_impostoVigenciaService = impostoVigenciaService;
		}

		//
		// GET: /Admin/Imposto/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			IEnumerable<int> agentesConectadosId = null;
			if (UserSession.IsPerfilAgente || UserSession.IsPotencialAgente)
				agentesConectadosId = _agenteConectadoService.GetIdsByAgentes(UserSession.Agentes);

			var paging = _impostoService.GetDetailedDtoPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 10), Request.Params, agentesConectadosId);

			//data.PageNum = paging.CurrentPage;
			//data.PageCount = (paging.Items.Count() / paging.ItemsPerPage); // paging.TotalPages;
			//data.TotalRows = (paging.Items.Count()); // paging.TotalItems;
			//data.Impostos = paging.Items;

            data.PageNum = paging.CurrentPage;
            data.PageCount = paging.TotalPages;
            data.TotalRows = paging.TotalItems;
            data.Impostos = paging.Items;

            return AdminContent("Imposto/ImpostoList.aspx", data);
		}

		//
		// GET: /Admin/GetImpostos/
		public JsonResult GetImpostos()
		{
			var impostos = _impostoService.GetAll().Select(o => new { o.ID, o.AgenteConectado.Nome });
			return Json(impostos, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Imposto = TempData["ImpostoModel"] as Imposto;
			if (data.Imposto == null)
			{
				data.Imposto = new Imposto();
				data.Imposto.UpdateFromRequest();
			}
			return AdminContent("Imposto/ImpostoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Imposto = TempData["ImpostoModel"] as Imposto ?? _impostoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Imposto == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Imposto/ImpostoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var imposto = _impostoService.FindByID(id);
			if (imposto == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			imposto.ID = null;
			TempData["ImpostoModel"] = imposto;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var imposto = _impostoService.FindByID(id);
				if (imposto == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_impostoVigenciaService.DeleteByImpostoID(imposto.ID.Value);
					_impostoService.Delete(imposto);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Imposto" }, JsonRequestBehavior.AllowGet);
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
				var idsImposto = ids.Split(',').Select(i => i.ToInt(0));
				if (idsImposto.Any())
				{
					foreach (var idImposto in idsImposto)
						_impostoVigenciaService.DeleteByImpostoID(idImposto);
					_impostoService.DeleteMany(idsImposto);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Imposto" }, JsonRequestBehavior.AllowGet);
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
			var imposto = new Imposto();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					imposto = _impostoService.FindByID(Request["ID"].ToInt(0));
					if (imposto == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				imposto.UpdateFromRequest();

				if (!isEdit)
				{
					var checkAgenteConectado = Imposto.LoadByAgenteConectadoID(imposto.AgenteConectadoID);
					if (checkAgenteConectado != null)
						throw new Exception("Agente conectado já possui imposto cadastrado.");
				}

				_impostoService.Save(imposto);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? imposto.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Imposto";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { imposto.ID });
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
				TempData["ImpostoModel"] = imposto;
				return isEdit && imposto != null ? RedirectToAction("Edit", new { imposto.ID }) : RedirectToAction("Create");
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
			public List<ImpostoDetailedDto> Impostos;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Imposto Imposto;
			public Boolean ReadOnly;
		}
	}
}
