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
	public class ChamadaNegociacaoController : ControllerBase
	{
		private readonly IChamadaNegociacaoService _chamadaNegociacaoService;
		private readonly IContratoService _contratoService;
		private readonly IPatamarService _patamarService;
		private readonly IPerfilAgenteService _perfilAgenteService;

		public ChamadaNegociacaoController(IChamadaNegociacaoService chamadaNegociacaoService,
			IContratoService contratoService,
			IPatamarService patamarService,
			IPerfilAgenteService perfilAgenteService)
		{
			_chamadaNegociacaoService = chamadaNegociacaoService;
			_contratoService = contratoService;
			_patamarService = patamarService;
			_perfilAgenteService = perfilAgenteService;
		}

		public ActionResult Index(int? Page, string ids = null)
		{
			var data = new ListViewModel();
			var paging = _chamadaNegociacaoService.GetAllWithPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params,
				(UserSession.IsPerfilAgente || UserSession.IsPotencialAgente) ? UserSession.Agentes.Select(i => i.ID.Value) : null);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.ChamadasNegociacao = paging.Items;

			if (paging.Items.Any())
				data.HorasMeses = _patamarService.GetHorasMeses(paging.Items.Min(i => i.PrazoInicio), paging.Items.Max(i => i.PrazoFim));

           if (ids == null)
                data.balancoChamadas = _chamadaNegociacaoService.getBalancoAbertas();

            return AdminContent("ChamadaNegociacao/ChamadaNegociacaoList.aspx", data);
		}

		public ActionResult Create(int? contrato = null, int? perfilagente = null, DateTime? mes = null, bool split = false)
		{
			var data = new FormViewModel();
			data.ChamadaNegociacao = TempData["ChamadaNegociacaoModel"] as ChamadaNegociacao;
			if (data.ChamadaNegociacao == null)
			{
				Submercado submercado = null;

				var today = DateTime.Today;

				if (contrato != null)
				{
					var contratoSubmercado = _contratoService.FindByID(contrato.Value);
					if (contratoSubmercado != null)
						submercado = contratoSubmercado.PerfilAgenteVendedor.Submercado;
				}

				data.ChamadaNegociacao = new ChamadaNegociacao()
				{
					ContratoID = contrato,
					SubmercadoID = ((submercado == null) ? null : submercado.ID),
					Status = ChamadaNegociacao.TiposStatus.EmAberto.ToString(),
					IsLongoPrazo = false,
					IsMontanteAproximado = false
				};

				if (today.Day < 15)
				{
					data.ChamadaNegociacao.PrazoInicio = Dates.GetFirstDayOfMonth(today).AddMonths(-1);
					data.ChamadaNegociacao.PrazoFim = Dates.GetLastDayOfMonth(today).AddMonths(-1);
				}
				else
				{
					data.ChamadaNegociacao.PrazoInicio = Dates.GetFirstDayOfMonth(today);
					data.ChamadaNegociacao.PrazoFim = Dates.GetLastDayOfMonth(today);
				}

				if (perfilagente != null)
				{
					data.ChamadaNegociacao.PerfilAgenteID = perfilagente;
				}
				if (mes != null)
				{
					data.ChamadaNegociacao.PrazoInicio = Dates.GetFirstDayOfMonth(mes.Value);
					data.ChamadaNegociacao.PrazoFim = Dates.GetLastDayOfMonth(mes.Value);
				}

				data.ChamadaNegociacao.UpdateFromRequest();
			}

			if (split)
			{
				data.SplitChamadaNegociacaoID = data.ChamadaNegociacao.ID;
				data.ChamadaNegociacao.ID = null;
			}

			return AdminContent("ChamadaNegociacao/ChamadaNegociacaoEdit.aspx", data);
		}

		public ActionResult Edit(int id, bool readOnly = false)
		{
			var data = new FormViewModel();
			data.ChamadaNegociacao = TempData["ChamadaNegociacaoModel"] as ChamadaNegociacao ?? _chamadaNegociacaoService.FindByID(id);
			data.ReadOnly = readOnly;

			if (data.ChamadaNegociacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("ChamadaNegociacao/ChamadaNegociacaoEdit.aspx", data);
		}

		public ActionResult View(int id)
		{
            var AG = UserSession.Agentes.Where(w => w.ID == _chamadaNegociacaoService.FindByID(id).PerfilAgente.AgenteID).Count();

            if (UserSession.IsAdmin || UserSession.IsAnalista || UserSession.IsDeveloper || (UserSession.Agentes.Where(w => w.ID == _chamadaNegociacaoService.FindByID(id).PerfilAgente.AgenteID).Count() > 0))
            {
                return Edit(id, true);
            }
            else
            {
                Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
                return RedirectToAction("Index");
            }        

    }

		public ActionResult Duplicate(int id)
		{
			var chamadaNegociacao = _chamadaNegociacaoService.FindByID(id);
			if (chamadaNegociacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			chamadaNegociacao.ID = null;
			TempData["ChamadaNegociacaoModel"] = chamadaNegociacao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(chamadaNegociacao.ContratoID);
		}

		public ActionResult CreateOferta(int id)
		{
			var chamadaNegociacao = _chamadaNegociacaoService.FindByID(id);
			if (chamadaNegociacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			chamadaNegociacao.ID = null;
			TempData["ChamadaNegociacaoModel"] = chamadaNegociacao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(chamadaNegociacao.ContratoID);
		}

		public ActionResult Del(Int32 id)
		{
			try
			{
				var chamadaNegociacao = _chamadaNegociacaoService.FindByID(id);
				if (chamadaNegociacao == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_chamadaNegociacaoService.Delete(chamadaNegociacao);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ChamadaNegociacao" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(string ids)
		{
			_chamadaNegociacaoService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ChamadaNegociacao" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult CreateOfertaMultiple(string ids)
		{
			return Json(new { success = true, message = "", nextPage = Web.BaseUrl + "Admin/ChamadaNegociacaoOferta/Create/?ids=" + ids }, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Split(int id)
		{
			var chamadaNegociacao = _chamadaNegociacaoService.FindByID(id);
			if (chamadaNegociacao == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			// chamadaNegociacao.ID = null;
			TempData["ChamadaNegociacaoModel"] = chamadaNegociacao;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(chamadaNegociacao.ContratoID, null, null, true);
		}

		public ActionResult UpdateStatus(int id)
		{
			var data = new FormViewModel()
			{
				ChamadaNegociacao = _chamadaNegociacaoService.FindByID(id)
			};

			return AdminContent("ChamadaNegociacao/ChamadaNegociacaoUpdateStatus.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var chamadaNegociacao = new ChamadaNegociacao();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					chamadaNegociacao = _chamadaNegociacaoService.FindByID(Request["ID"].ToInt(0));
					if (chamadaNegociacao == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				chamadaNegociacao.UpdateFromRequest();

				if (chamadaNegociacao.PrazoInicio > chamadaNegociacao.PrazoFim)
					throw new Exception("Prazo inicial não pode ser superior a prazo final.");

				chamadaNegociacao.PrazoInicio = Dates.GetFirstDayOfMonth(chamadaNegociacao.PrazoInicio);
				chamadaNegociacao.PrazoFim = Dates.GetLastDayOfMonth(chamadaNegociacao.PrazoFim);

				var splitChamadaNegociacaoID = Request["SplitChamadaNegociacaoID"].ToInt(null);
				if (splitChamadaNegociacaoID != null)
				{
					var splitChamadaNegociacao = _chamadaNegociacaoService.FindByID(splitChamadaNegociacaoID.Value);
					if (splitChamadaNegociacao != null)
					{
						splitChamadaNegociacao.MontanteMwm -= chamadaNegociacao.MontanteMwm;

						if (splitChamadaNegociacao.MontanteMwm < 0)
							throw new Exception("O montante inserido está resultando em um valor negativo.");

						_chamadaNegociacaoService.Update(splitChamadaNegociacao);
					}
				}

				_chamadaNegociacaoService.Save(chamadaNegociacao);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? chamadaNegociacao.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/ChamadaNegociacao";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { chamadaNegociacao.ID });

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
				TempData["ChamadaNegociacaoModel"] = chamadaNegociacao;
				return isEdit && chamadaNegociacao != null ? RedirectToAction("Edit", new { chamadaNegociacao.ID }) : RedirectToAction("Create");
			}
		}

		[ValidateInput(false)]
		public ActionResult SaveStatus()
		{
			try
			{
				var id = Request["ID"].IsNotBlank();

				var chamadaNegociacao = _chamadaNegociacaoService.FindByID(Request["ID"].ToInt(0));
				if (chamadaNegociacao == null)
					throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));

				var action = Request["SubmitValue"];
				if (action.Contains("Excluir"))
				{
					_chamadaNegociacaoService.Delete(chamadaNegociacao);
				}
				else
				{
					chamadaNegociacao.Status = ChamadaNegociacao.TiposStatus.EmAberto.ToString();
					_chamadaNegociacaoService.Update(chamadaNegociacao);
				}

				if (chamadaNegociacao.Contrato != null)
					_contratoService.Delete(chamadaNegociacao.Contrato);

				Web.SetMessage("Chamada excluída com sucesso.");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

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
				return RedirectToAction("Index");
			}
		}

		public JsonResult UpdateCheckboxes(int id, string field, bool value)
		{
			var chamadaNegociacao = _chamadaNegociacaoService.FindByID(id);
			if (chamadaNegociacao != null)
			{
				switch (field)
				{
					case "IsMontanteAproximado": chamadaNegociacao.IsMontanteAproximado = value; break;
				}

				_chamadaNegociacaoService.Update(chamadaNegociacao);

				return Json(new { success = true }, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

        public JsonResult SendEmailEncerraChamada(int chamadaNegociacaoId, double spread)
        {

            var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamadaNegociacaoId);
            if (chamadaNegociacao != null)
            {
                _chamadaNegociacaoService.SendEmailEncerraChamada(chamadaNegociacao, spread);

                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return Json(null); 
        }

        public JsonResult permiteSwap(int chamadaNegociacaoId)
        {

            var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamadaNegociacaoId);
            var detalhamentoChamadaSwapAndVenda = _chamadaNegociacaoService.getDetalhamentoChamadaSwapAndVenda(chamadaNegociacao);
            if (chamadaNegociacao != null && detalhamentoChamadaSwapAndVenda.permiteSwapAndVenda) 
                return Json(true, JsonRequestBehavior.AllowGet);
         
           return Json(false);
        }

        public JsonResult GetEmailFinalizaPreview(string chamadaNegociacaoID, string tipoFechamento)
        {
            var getBodyWithImages = _chamadaNegociacaoService.selecteEmailFinalizaBody(chamadaNegociacaoID.ToInt(), tipoFechamento);
            return Json(getBodyWithImages, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SendEmailFinalizaPreview(string chamadaNegociacaoID, string tipoFechamento, Boolean interno)
        {
            _chamadaNegociacaoService.sendEmailFinalizaChamada(chamadaNegociacaoID.ToInt(), tipoFechamento, interno);
            return Json(true, JsonRequestBehavior.AllowGet);
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

		public class FormViewModel
		{
			public ChamadaNegociacao ChamadaNegociacao;
			public int? SplitChamadaNegociacaoID;
			public bool ReadOnly;
		}

		public class ListViewModel
		{
			public List<ChamadaNegociacao> ChamadasNegociacao;
			public IEnumerable<PatamarHorasMesDto> HorasMeses;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
            public List<BalancoChamadasItemDto> balancoChamadas;
		}
	}
}
