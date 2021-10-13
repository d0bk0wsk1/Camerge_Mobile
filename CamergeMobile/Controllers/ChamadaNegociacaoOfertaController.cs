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
	public class ChamadaNegociacaoOfertaController : ControllerBase
	{
		private readonly IChamadaNegociacaoService _chamadaNegociacaoService;
		private readonly IChamadaNegociacaoOfertaService _chamadaNegociacaoOfertaService;
		private readonly IOfertaService _ofertaService;

		public ChamadaNegociacaoOfertaController(IChamadaNegociacaoService chamadaNegociacaoService,
			IChamadaNegociacaoOfertaService chamadaNegociacaoOfertaService,
			IOfertaService ofertaService)
		{
			_chamadaNegociacaoService = chamadaNegociacaoService;
			_chamadaNegociacaoOfertaService = chamadaNegociacaoOfertaService;
			_ofertaService = ofertaService;
		}

		public ActionResult Create(string ids)
		{
			var data = new FormViewModel();

			var chamadasNegociacaoID = ids.Split(',').Select(id => id.ToInt(0));
			if (chamadasNegociacaoID.Any())
			{
				var chamadasNegociacao = new List<ChamadaNegociacao>();

				foreach (var chamadaNegociacaoID in chamadasNegociacaoID)
				{
					var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamadaNegociacaoID);
					if (chamadaNegociacao != null)
						chamadasNegociacao.Add(chamadaNegociacao);
				}

				if ((chamadasNegociacao.Select(i => i.Tipo).Distinct().Count() == 1)
					&& (chamadasNegociacao.Select(i => i.DescontoID).Distinct().Count() == 1)
					&& (chamadasNegociacao.Select(i => i.PrazoInicio).Distinct().Count() == 1)
					&& (chamadasNegociacao.Select(i => i.PrazoFim).Distinct().Count() == 1)
					&& (chamadasNegociacao.Select(i => i.Status).Distinct().Count() == 1)
					&& (chamadasNegociacao.Select(i => i.SubmercadoID).Distinct().Count() == 1)
					&& (chamadasNegociacao.First().Status == ChamadaNegociacao.TiposStatus.EmAberto.ToString()))
				{
					data.Oferta = new Oferta();
					data.ChamadasNegociacaoID = ids;
					data.Mes = chamadasNegociacao.First().PrazoInicio;
					data.SubmercadoID = chamadasNegociacao.First().SubmercadoID.Value;

					return AdminContent("ChamadaNegociacaoOferta/ChamadaNegociacaoOfertaEdit.aspx", data);
				}
				else
				{
					Web.SetMessage("Todas as informações precisam ser idênticas em todas as chamadas.", "error");
					return AdminContent("ChamadaNegociacao/ChamadaNegociacaoList.aspx", data);
				}
			}
			return HttpNotFound();
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
            var nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao";
            try
			{
                
                var chamadasNegociacaoID = Request.Form["ChamadasNegociacaoID"].Split(',').Select(id => id.ToInt(0));
				if (chamadasNegociacaoID.Any())
				{
					var chamadasNegociacao = new List<ChamadaNegociacao>();

					foreach (var chamadaNegociacaoID in chamadasNegociacaoID)
					{
						var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamadaNegociacaoID);
						if (chamadaNegociacao != null)
							chamadasNegociacao.Add(chamadaNegociacao);
					}

					if (chamadasNegociacao.Any())
					{
						var oferta = new Oferta()
						{
							TipoPreco = Request.Form["TipoPreco"],
							Spread = Request.Form["Spread"].ToDouble(null),
							Preco = Request.Form["Preco"].ToDouble(null),
							Observacao = Request.Form["Observacao"],
                            PremioSwap = Request.Form["PremioSwap"].ToDouble(null),
                        };

						_ofertaService.Save(oferta);

						foreach (var chamadaNegociacao in chamadasNegociacao)
						{
                            
                            _chamadaNegociacaoOfertaService.Save(new ChamadaNegociacaoOferta() { OfertaID = oferta.ID, ChamadaNegociacaoID = chamadaNegociacao.ID, EmailSent = false });
                            if (chamadasNegociacao.Count()>1)
							    _chamadaNegociacaoOfertaService.SendEmailWithHistoricOfertas(chamadaNegociacao.ID.Value);
                            
                        }

                        if (chamadasNegociacao.Count()==1)
                        {
                            //var data = new FormViewModel();
                            //data.Oferta = new Oferta();
                            //data.ChamadasNegociacaoID = chamadasNegociacao.First().ID.ToString();
                            //data.Mes = chamadasNegociacao.First().PrazoInicio;
                            //data.SubmercadoID = chamadasNegociacao.First().SubmercadoID.Value;
                            //data.displayModalEmail = true;

                            //Web.SetMessage("ta salvo, ahora ver", "error");
                            //return AdminContent("ChamadaNegociacaoOferta/ChamadaNegociacaoOfertaEdit.aspx", data);

                            Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
                            //var nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao";
                            nextPage = Web.BaseUrl + "Admin/ChamadaNegociacaoOferta/Create/?ids=" + chamadasNegociacao.First().ID.ToString() + "&OpenModal=true";
                            return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage});
                        }                       

                        Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
					}
				}
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
			}

			//var nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao";
			return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
		}

        public ActionResult sendEmailOferta(string ids, Boolean interno)
        {
            var nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao";
            try
            {
                var chamadaNegociacaoID = ids.Split(',').Select(id => id.ToInt(0)).First();
                if (chamadaNegociacaoID != 0)
                {
                    var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamadaNegociacaoID);
                    _chamadaNegociacaoOfertaService.SendEmailWithHistoricOfertas(chamadaNegociacao.ID.Value, interno); 

                     Web.SetMessage("E-mail enviado com sucesso");
                     nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao";                     
                     


                    if (Fmt.ConvertToBool(Request["ajax"]))
                        return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage }); 
                    TempData["ChamadaNegociacaoModel"] = chamadaNegociacao;
                    return Redirect(Web.BaseUrl + "Admin/ChamadaNegociacao");
                }
                        
            }
            catch (Exception ex)
            {
                Web.SetMessage(HandleExceptionMessage(ex), "error");
            }            
            return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
        }

        public JsonResult GetEmailOfertaPreview(string chamadaNegociacaoID)
        {
            var getBodyWithImages = _chamadaNegociacaoOfertaService.selecteEmailBody(chamadaNegociacaoID.ToInt());
            return Json(getBodyWithImages, JsonRequestBehavior.AllowGet);
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
			public Oferta Oferta;
			public DateTime Mes;
			public int SubmercadoID;
			public string ChamadasNegociacaoID;
            public Boolean displayModalEmail = false;
		}

		public class ListViewModel
		{
			public List<ChamadaNegociacaoOferta> ChamadasNegociacaoOferta;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}
	}
}
