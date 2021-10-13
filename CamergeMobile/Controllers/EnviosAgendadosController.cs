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
	public class EnviosAgendadosController : ControllerBase
	{
		private readonly IEnviosAgendadosService _enviosAgendadosService;

		public EnviosAgendadosController(IEnviosAgendadosService enviosAgendadosService)
		{
            _enviosAgendadosService = enviosAgendadosService;
		}

		//
		// GET: /Admin/AliquotaImposto/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _enviosAgendadosService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.EnviosAgendados = paging.Items;

			return AdminContent("EnviosAgendados/EnviosAgendadosList.aspx", data);
		}

		
		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.EnviosAgendados = TempData["EnviosAgendadosModel"] as EnviosAgendados;
			if (data.EnviosAgendados == null)
			{
				data.EnviosAgendados = new EnviosAgendados();
				data.EnviosAgendados.UpdateFromRequest();
			}
			return AdminContent("EnviosAgendados/EnviosAgendadosEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.EnviosAgendados = TempData["EnviosAgendadosModel"] as EnviosAgendados ?? _enviosAgendadosService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.EnviosAgendados == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("EnviosAgendados/EnviosAgendadosEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var enviosAgendados = _enviosAgendadosService.FindByID(id);
			if (enviosAgendados == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
            enviosAgendados.ID = null;
			TempData["EnviosAgendadosModel"] = enviosAgendados;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}


        public ActionResult RunEnviosAgendados(int? id)
        {
            if (id !=null && id >0)
                _enviosAgendadosService.doYourJumps(id.ToInt());
            else
                _enviosAgendadosService.doYourJumps();

            //UserSession.Person 
            Web.SetMessage("Envios Executados");
            if (Fmt.ConvertToBool(Request["ajax"]))
            {
                return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.BaseUrl + "Admin/EnviosAgendados" }, JsonRequestBehavior.AllowGet);
            }

            
            var nextPage = Web.BaseUrl + "Admin/EnviosAgendados";
            if (nextPage != null)
            {
                return Redirect(nextPage);
            }

            return Redirect(nextPage);
        }


        public ActionResult Del(Int32 id)
		{
			try
			{
				var enviosAgendados = _enviosAgendadosService.FindByID(id);
				if (enviosAgendados == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
                    _enviosAgendadosService.Delete(enviosAgendados);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/EnviosAgendados" }, JsonRequestBehavior.AllowGet);
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
                _enviosAgendadosService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/EnviosAgendados" }, JsonRequestBehavior.AllowGet);
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

			var enviosAgendados = new EnviosAgendados();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
                    enviosAgendados = _enviosAgendadosService.FindByID(Request["ID"].ToInt(0));
					if (enviosAgendados == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

                enviosAgendados.UpdateFromRequest();
                enviosAgendados.Aplicacao = Request["aplicacao"];
                enviosAgendados.Parametros = "mes=" + Request["Mes"] + "; isTeste=" + Request["isTeste"];
                if (Request["dataLiquidacao"] != null)
                    enviosAgendados.Parametros += "; dataLiquidacao=" + Request["dataLiquidacao"];
                if (Request["dataAporte"] != null)
                        enviosAgendados.Parametros += "; dataAporte=" + Request["dataAporte"];
                if (Request["hasEncargo"] != null)
                    enviosAgendados.Parametros += "; hasEncargo=" + Request["hasEncargo"];
                enviosAgendados.PersonID = UserSession.Person.ID;
                enviosAgendados.DateAdded = DateTime.Now;
                enviosAgendados.Status = "Pendente";
                _enviosAgendadosService.Save(enviosAgendados);               
               

                // Agendamento do Envio do Aviso
                if (!Request["isTeste"].ToBoolean())
                {
                    var enviosAgendadosAviso = new EnviosAgendados();

                    //so pro teste
                    //enviosAgendadosAviso.DataHoraEnvio = enviosAgendados.DataHoraEnvio.AddMinutes(5);

                    if (enviosAgendados.Aplicacao == "EmailAporteGarantias")
                    {
                        enviosAgendadosAviso.Aplicacao = "EmailAvisoAporteGarantias";
                        enviosAgendadosAviso.DataHoraEnvio = Convert.ToDateTime(Request["dataAporte"] + " 08:00:00");
                    }
                    else if (enviosAgendados.Aplicacao == "EmailBoletoContribuicao")
                    {
                        enviosAgendadosAviso.Aplicacao = "EmailAvisoBoletoContribuicao";
                        enviosAgendadosAviso.DataHoraEnvio = Convert.ToDateTime(Request["dataLiquidacao"] + " 08:00:00");
                    }
                    else if (enviosAgendados.Aplicacao == "EmailEnergiaReserva")
                    {
                        enviosAgendadosAviso.Aplicacao = "EmailAvisoEnergiaReserva";
                        enviosAgendadosAviso.DataHoraEnvio = Convert.ToDateTime(Request["dataLiquidacao"] + " 08:00:00");
                    }
                    else if (enviosAgendados.Aplicacao == "EmailLiquidacaoFinanceira")
                    {
                        //No caso da liquidacao, tem o aviso do dia anterior pra complementar o aporte
                        //e o aviso para a liquidação de todos
                        var enviosAgendadosAvisoComplementar = new EnviosAgendados();
                        enviosAgendadosAvisoComplementar.Aplicacao = "EmailAvisoComplementarLiquidacaoFinanceira";                       

                        var diaAnteriorLiquidacao = Convert.ToDateTime(Request["dataLiquidacao"] + " 08:00:00").AddDays(-1);
                        if (diaAnteriorLiquidacao.DayOfWeek == DayOfWeek.Sunday)
                            diaAnteriorLiquidacao = diaAnteriorLiquidacao.AddDays(-2);

                        enviosAgendadosAvisoComplementar.DataHoraEnvio = diaAnteriorLiquidacao;
                        enviosAgendadosAvisoComplementar.DateAdded = enviosAgendados.DateAdded;
                        enviosAgendadosAvisoComplementar.Parametros = enviosAgendados.Parametros + "; dataEnvioEmail=" + enviosAgendados.DataHoraEnvio.ToString("dd/MM/yyyy");
                        enviosAgendadosAvisoComplementar.PersonID = enviosAgendados.PersonID;
                        enviosAgendadosAvisoComplementar.Status = enviosAgendados.Status;
                        _enviosAgendadosService.Save(enviosAgendadosAvisoComplementar);
                        enviosAgendadosAviso.Aplicacao = "EmailAvisoLiquidacaoFinanceira";
                        enviosAgendadosAviso.DataHoraEnvio = Convert.ToDateTime(Request["dataLiquidacao"] + " 08:00:00"); 

                    }
                    enviosAgendadosAviso.DateAdded = enviosAgendados.DateAdded;
                    enviosAgendadosAviso.Parametros = enviosAgendados.Parametros + "; dataEnvioEmail=" + enviosAgendados.DataHoraEnvio.ToString("dd/MM/yyyy");
                    enviosAgendadosAviso.PersonID  = enviosAgendados.PersonID;
                    enviosAgendadosAviso.Status = enviosAgendados.Status;
                    _enviosAgendadosService.Save(enviosAgendadosAviso);
                }

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage =  Web.BaseUrl + "Admin/EnviosAgendados";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { enviosAgendados.ID });
				}

				var previousUrl = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/EnviosAgendados";
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
				TempData["EnviosAgendadosModel"] = enviosAgendados;
				return isEdit && enviosAgendados != null ? RedirectToAction("Edit", new { enviosAgendados.ID }) : RedirectToAction("Create");
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
			public List<EnviosAgendados> EnviosAgendados;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public EnviosAgendados EnviosAgendados;
			public Boolean ReadOnly;
		}
	}
}
