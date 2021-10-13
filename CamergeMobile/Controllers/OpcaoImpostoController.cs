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
	public class OpcaoImpostoController : ControllerBase
	{
		private readonly IOpcaoImpostoService _opcaoImpostoService;

		public OpcaoImpostoController(IOpcaoImpostoService opcaoImpostoService)
		{
			_opcaoImpostoService = opcaoImpostoService;
		}

		public ActionResult Index(Int32? Page, string relacao = null)
		{
			var data = new ListViewModel();

			var paging = _opcaoImpostoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params,
				(UserSession.IsPerfilAgente || UserSession.IsPotencialAgente) ? UserSession.Agentes.Select(i => i.ID.Value) : null);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.OpcoesCredito = paging.Items;
			data.TipoRelacao = relacao ?? PerfilAgente.TiposRelacao.Cliente.ToString();

			return AdminContent("OpcaoImposto/OpcaoImpostoList.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.OpcaoImposto = TempData["OpcaoImpostoModel"] as OpcaoImposto;
			if (data.OpcaoImposto == null)
			{
				data.OpcaoImposto = new OpcaoImposto();
				data.OpcaoImposto.UpdateFromRequest();
			}

			data.TipoRelacao = Request["relacao"];
			if (data.TipoRelacao == null)
				data.TipoRelacao = data.OpcaoImposto.Ativo.PerfilAgente.TipoRelacao;

			return AdminContent("OpcaoImposto/OpcaoImpostoEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.OpcaoImposto = TempData["OpcaoImpostoModel"] as OpcaoImposto ?? _opcaoImpostoService.FindByID(id);
			if (data.OpcaoImposto == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			data.TipoRelacao = data.OpcaoImposto.Ativo.PerfilAgente.TipoRelacao;
			data.ReadOnly = readOnly;

			return AdminContent("OpcaoImposto/OpcaoImpostoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var classe = _opcaoImpostoService.FindByID(id);
			if (classe == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			classe.ID = null;
			TempData["OpcaoImpostoModel"] = classe;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var classe = _opcaoImpostoService.FindByID(id);
				if (classe == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_opcaoImpostoService.Delete(classe);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/OpcaoImposto" }, JsonRequestBehavior.AllowGet);
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
				_opcaoImpostoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/OpcaoImposto" }, JsonRequestBehavior.AllowGet);
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
			var opcaoCredito = new OpcaoImposto();
			var isEdit = Request["ID"].IsNotBlank();

			string relacao = null;

			try
			{
				if (isEdit)
				{
					opcaoCredito = _opcaoImpostoService.FindByID(Request["ID"].ToInt(0));
					if (opcaoCredito == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				opcaoCredito.UpdateFromRequest();

				if (opcaoCredito.Aproveitamento != null)
					opcaoCredito.Aproveitamento = Fmt.ToDouble(opcaoCredito.Aproveitamento, false, true);

				_opcaoImpostoService.Save(opcaoCredito);

				relacao = opcaoCredito.Ativo.PerfilAgente.TipoRelacao;

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? opcaoCredito.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/OpcaoImposto/?relacao=" + relacao;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { opcaoCredito.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index", new { relacao = relacao });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["OpcaoImpostoModel"] = opcaoCredito;
				return isEdit && opcaoCredito != null ? RedirectToAction("Edit", new { opcaoCredito.ID }) : RedirectToAction("Create", new { relacao = relacao });
			}
		}

		public JsonResult GetAproveitamento(int ativoID)
		{
			var opcaoImposto = _opcaoImpostoService.GetMostRecent(ativoID);
			if (opcaoImposto != null)
			{
				var viewModel = new OpcaoImpostoViewModel()
				{
					TipoImposto = opcaoImposto.TipoImposto,
					TipoCredito = opcaoImposto.TipoCredito,
					Aproveitamento = opcaoImposto.Aproveitamento
				};

				return Json(viewModel, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

        public String GetAproveitamentoString(int ativoID, String tipo)
        {
            var opcaoImposto = _opcaoImpostoService.GetMostRecent(ativoID);
            if (opcaoImposto != null)   
                if (tipo == "imposto")
                    return opcaoImposto.TipoImposto; 
                else if (tipo == "creditaimp")
                    return opcaoImposto.TipoCredito;
            return "";
        }
        

        public JsonResult GetHistoric(int ativoId)
		{
			var historic = _opcaoImpostoService.Get(ativoId);
			if (historic.Any())
			{
				return Json(
					historic.Select(s => new
					{
						AtivoID = s.AtivoID,
						Mes = Fmt.MonthYear(s.Mes),
						TipoImposto = s.TipoImposto,
						TipoCredito = s.TipoCredito,
						RegimeTributario = s.RegimeTributario,
						Aproveitamento = s.Aproveitamento
					}),
					JsonRequestBehavior.AllowGet
				);
			}
			return Json(null);
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

		public class OpcaoImpostoViewModel
		{
			public string TipoImposto;
			public string TipoCredito;
			public double? Aproveitamento;
		}

		public class ListViewModel
		{
			public List<OpcaoImposto> OpcoesCredito;
			public string TipoRelacao;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public OpcaoImposto OpcaoImposto;
			public string TipoRelacao;
			public Boolean ReadOnly;
		}
	}
}
