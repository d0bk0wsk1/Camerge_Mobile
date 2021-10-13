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
	public class ContratoController : ControllerBase
	{
		private readonly IAgenteService _agenteService;
		private readonly IChamadaNegociacaoService _chamadaNegociacaoService;
		private readonly ICliqCceeService _cliqCceeService;
		private readonly IContratoCaracteristicaService _contratoCaracteristicaService;
		private readonly IContratoHasAttachmentService _contratoHasAttachmentService;
		private readonly IContratoService _contratoService;
		private readonly IContratoVigenciaService _contratoVigenciaService;
		private readonly IContratoVigenciaBalancoService _contratoVigenciaBalancoService;
		private readonly IContratoVigenciaTransferenciaService _contratoVigenciaTransferenciaService;
		private readonly ILoggerService _loggerService;
		private readonly IPerfilAgenteService _perfilAgenteService;

		public ContratoController(IAgenteService agenteService,
			IChamadaNegociacaoService chamadaNegociacaoService,
			ICliqCceeService cliqCceeService,
			IContratoCaracteristicaService contratoCaracteristicaService,
			IContratoHasAttachmentService contratoHasAttachmentService,
			IContratoService contratoService,
			IContratoVigenciaService contratoVigenciaService,
			IContratoVigenciaBalancoService contratoVigenciaBalancoService,
			IContratoVigenciaTransferenciaService contratoVigenciaTransferenciaService,
			ILoggerService loggerService,
			IPerfilAgenteService perfilAgenteService)
		{
			_agenteService = agenteService;
			_chamadaNegociacaoService = chamadaNegociacaoService;
			_cliqCceeService = cliqCceeService;
			_contratoCaracteristicaService = contratoCaracteristicaService;
			_contratoHasAttachmentService = contratoHasAttachmentService;
			_contratoService = contratoService;
			_contratoVigenciaService = contratoVigenciaService;
			_contratoVigenciaBalancoService = contratoVigenciaBalancoService;
			_contratoVigenciaTransferenciaService = contratoVigenciaTransferenciaService;
			_loggerService = loggerService;
			_perfilAgenteService = perfilAgenteService;
		}

		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();
			var paging = _contratoService.GetDetailedDtoPaging(
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params,
				((UserSession.IsCliente) ? UserSession.Agentes.Select(i => i.ID.Value) : null));

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Contratos = paging.Items;

			if ((data.Contratos.Any()) && (UserSession.IsCliente))
			{
				data.PerfisAgenteCompradorList = _perfilAgenteService.GetByIds(data.Contratos.Select(i => i.PerfilAgenteCompradorID));
				data.PerfisAgenteVendedorList = _perfilAgenteService.GetByIds(data.Contratos.Select(i => i.PerfilAgenteVendedorID));
			}
			else
			{
				var perfisAgente = _perfilAgenteService.GetByTiposRelacao(new[] { PerfilAgente.TiposRelacao.Cliente.ToString(), PerfilAgente.TiposRelacao.Terceiro.ToString() });

				data.PerfisAgenteCompradorList = perfisAgente;
				data.PerfisAgenteVendedorList = perfisAgente;
			}

			if (data.Contratos.Any())
				data.HasContratoOrigem = (data.Contratos.Any(i => i.Numero_Interno_Controle_Origem != null));

			return AdminContent("Contrato/ContratoList.aspx", data);
		}

		public ActionResult Create(int? chamada = null)
		{
			var data = new FormViewModel();
			data.Contrato = TempData["ContratoModel"] as Contrato;
			if (data.Contrato == null)
			{
				data.Contrato = new Contrato();

				var caracteristica = _contratoCaracteristicaService.GetByNome("Curto Prazo");
				if (caracteristica != null)
				{
					data.Contrato.ContratoCaracteristicaID = caracteristica.ID;
					data.Contrato.IndiceReajuste = Contrato.IndicesReajuste.SemReajuste.ToString();
				}

				/// data.Contrato.IndiceReajuste = ((caracteristica.Nome != "Longo Prazo") ? Contrato.IndicesReajuste.SemReajuste.ToString() : null);

				if (chamada != null)
				{
					var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamada.Value);
					if (chamadaNegociacao != null)
					{
						if (chamadaNegociacao.Status == ChamadaNegociacao.TiposStatus.Finalizado.ToString())
							throw new Exception("Não é possível finalizar uma chamada já finalizada.");

						data.ChamadaNegociacao = chamadaNegociacao;

						if (!Dates.IsSameMonthYear(chamadaNegociacao.PrazoFim, chamadaNegociacao.PrazoInicio))
						{
							caracteristica = _contratoCaracteristicaService.GetByNome("Longo Prazo");
							if (caracteristica != null)
								data.Contrato.ContratoCaracteristicaID = caracteristica.ID;
						}

						data.Contrato.PrazoInicio = chamadaNegociacao.PrazoInicio;
						data.Contrato.PrazoFim = chamadaNegociacao.PrazoFim;

						// Trello
						data.Contrato.TrelloCard = chamadaNegociacao.TrelloCard;

						if (chamadaNegociacao.Tipo == ChamadaNegociacao.Tipos.Compra.ToString())
							data.Contrato.PerfilAgenteCompradorID = chamadaNegociacao.PerfilAgenteID;
						else if (chamadaNegociacao.Tipo == ChamadaNegociacao.Tipos.Venda.ToString())
							data.Contrato.PerfilAgenteVendedorID = chamadaNegociacao.PerfilAgenteID;

						data.HasAutoVigencias = Dates.IsSameMonthYear(chamadaNegociacao.PrazoInicio, chamadaNegociacao.PrazoFim);
					}
				}

				data.Contrato.UpdateFromRequest();
			}

			return AdminContent("Contrato/ContratoEdit.aspx", data);
		}

        public ActionResult CreateSwap(int? chamada = null)
        {
            var data = new FormViewModel();
            data.Contrato = TempData["ContratoModel"] as Contrato;
            if (data.Contrato == null)
            {
                data.Contrato = new Contrato();

                var caracteristica = _contratoCaracteristicaService.GetByNome("Curto Prazo");
                if (caracteristica != null)
                {
                    data.Contrato.ContratoCaracteristicaID = caracteristica.ID;
                    data.Contrato.IndiceReajuste = Contrato.IndicesReajuste.SemReajuste.ToString();
                }

                /// data.Contrato.IndiceReajuste = ((caracteristica.Nome != "Longo Prazo") ? Contrato.IndicesReajuste.SemReajuste.ToString() : null);

                if (chamada != null)
                {
                    var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamada.Value);
                    if (chamadaNegociacao != null)
                    {
                        if (chamadaNegociacao.Status == ChamadaNegociacao.TiposStatus.Finalizado.ToString())
                            throw new Exception("Não é possível finalizar uma chamada já finalizada.");

                        data.ChamadaNegociacao = chamadaNegociacao;
                        data.detalhamentoChamadaSwapAndVenda = _chamadaNegociacaoService.getDetalhamentoChamadaSwapAndVenda(chamadaNegociacao);

                        if (!Dates.IsSameMonthYear(chamadaNegociacao.PrazoFim, chamadaNegociacao.PrazoInicio))
                        {
                            caracteristica = _contratoCaracteristicaService.GetByNome("Longo Prazo");
                            if (caracteristica != null)
                                data.Contrato.ContratoCaracteristicaID = caracteristica.ID;
                        }

                        data.Contrato.PrazoInicio = chamadaNegociacao.PrazoInicio;
                        data.Contrato.PrazoFim = chamadaNegociacao.PrazoFim;

                        // Trello
                        data.Contrato.TrelloCard = chamadaNegociacao.TrelloCard;

                        if (chamadaNegociacao.Tipo == ChamadaNegociacao.Tipos.Compra.ToString())
                            data.Contrato.PerfilAgenteCompradorID = chamadaNegociacao.PerfilAgenteID;
                        else if (chamadaNegociacao.Tipo == ChamadaNegociacao.Tipos.Venda.ToString())
                            data.Contrato.PerfilAgenteVendedorID = chamadaNegociacao.PerfilAgenteID;

                        data.HasAutoVigencias = Dates.IsSameMonthYear(chamadaNegociacao.PrazoInicio, chamadaNegociacao.PrazoFim);
                    }
                }

                data.Contrato.UpdateFromRequest();
            }

            return AdminContent("Contrato/ContratoEditSwap.aspx", data);
        }

        [HttpGet]
		public ActionResult Import()
		{
			return AdminContent("Contrato/ContratoImport.aspx");
		}

		[HttpPost]
		public ActionResult Import(string RawData)
		{
			_loggerService.Setup("contrato_import");

			Exception exception = null;
			string friendlyErrorMessage = null;

			try
			{
				_loggerService.Log("Iniciando Importação", false);

				var sobrescreverExistentes = Request["SobrescreverExistentes"].ToBoolean();

				var processados = _contratoService.ImportaContratos(RawData, sobrescreverExistentes);
				if (processados == 0)
				{
					Web.SetMessage("Nenhum dado foi importado", "info");
				}
				else
				{
					Web.SetMessage("Dados importados com sucesso");
				}
			}
			catch (GenericImportException ex)
			{
				exception = ex;
				friendlyErrorMessage = string.Format("Falha na importação. {0}", ex.Message);
			}
			catch (Exception ex)
			{
				exception = ex;
				friendlyErrorMessage = "Falha na importação. Verifique se os dados estão corretos e tente novamente";
			}

			if (exception != null)
			{
				_loggerService.Log("Exception: " + exception.Message, false);
				Web.SetMessage(friendlyErrorMessage, "error");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				}
				return RedirectToAction("Import");
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				var nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Contrato";
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
			}

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
			{
				return Redirect(previousUrl);
			}

			return RedirectToAction("Index");
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Contrato = TempData["ContratoModel"] as Contrato ?? _contratoService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Contrato == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			//if (data.Contrato.ContratoOrigemID != null)
			//{
			//	data.ReadOnly = true;
			//	/Web.SetMessage("Não é possível a edição de contratos quando já existe contrato origem preenchido.", "error");
			//}

			data.HasVigencias = data.Contrato.ContratoVigenciaList.Any();

			return AdminContent("Contrato/ContratoEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var contrato = _contratoService.FindByID(id);
			if (contrato == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			contrato.ID = null;
			TempData["ContratoModel"] = contrato;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var contrato = _contratoService.FindByID(id);
			if (contrato == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else if (_contratoService.IsContratoOrigem(contrato.ID.Value))
			{
				Web.SetMessage("Não é posível excluir este contrato pois o mesmo é contrato origem de outro(s) contrato(s).", "error");
			}
			else
			{
				var chamadaNegociacao = _chamadaNegociacaoService.GetByContrato(contrato.ID.Value);
				if (chamadaNegociacao != null)
					if (Fmt.ConvertToBool(Request["ajax"]))
						return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao/UpdateStatus/?id=" + chamadaNegociacao.ID }, JsonRequestBehavior.AllowGet);

				_contratoService.Delete(contrato);

				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Contrato" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(String ids)
		{
			var contratosID = ids.Split(',').Select(id => id.ToInt(0));
			if (contratosID.Any())
			{
				var contratosWithChamadaNegociacao = new List<string>();

				foreach (var contratoID in contratosID)
				{
					var contrato = _contratoService.FindByID(contratoID);
					if (contrato != null)
					{
						if (_contratoService.IsContratoOrigem(contrato.ID.Value))
						{
							continue;
						}

						if ((_chamadaNegociacaoService.GetByContrato(contrato.ID.Value)) != null)
						{
							contratosWithChamadaNegociacao.Add(contrato.NumeroInternoControle);
							continue;
						}

						// Código comentado pois a exclusão está sendo feita via CASCADE pelo PostgreSQL
						/*
						foreach (var contratoVigencia in contrato.ContratoVigenciaList)
						{
							_contratoVigenciaBalancoService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);
							_contratoVigenciaService.DeleteByContratoVigenciaID(contratoVigencia.ID.Value);
						}
						*/

						_contratoService.Delete(contrato);
					}
				}

				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess") + string.Format("O(s) contrato(s) {0} não pode(ram) ser deletado(s) devido a associação com chamada de negociação.", string.Join(",", contratosWithChamadaNegociacao)));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Contrato" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult Synthetic()
		{

            //verifica permissoes
            var bPermissao = true;
            var agentes = _agenteService.GetByConcatnatedIds(Request["agentes"]);
            Response.StatusCode = 200;

            if (agentes.Count()>0)
            {
                bPermissao = false;
                foreach (var agente in UserSession.Agentes)
                {
                    if (agentes.Where(w => w.ID == agente.ID).Count() >= 1 )
                    {
                        bPermissao = true;                        
                    }

                }
                if (!bPermissao && UserSession.IsPerfilAgente)
                {
                    Response.StatusCode = 403;
                    return AdminContent("Contrato/ContratoSynthetic.aspx", new SyntheticReportViewModel());
                }
            }                  


            var chkAtivado = Request["chkAtivado"].ToBoolean();
            var data = new SyntheticReportViewModel()
            {
                Contratos = GetContratosReportByRequest()
            };

            if (data != null && data.Contratos.Any())
                data.HorasMes = data.Contratos.FirstOrDefault().HorasMes;

            

			if (Request["caracteristica"] != "" && Request["caracteristica"] != null)
				data.Contratos = data.Contratos.Where(w => w.ContratoCaracteristica.ID == Request["caracteristica"].ToInt()).ToList();


			return AdminContent("Contrato/ContratoSynthetic.aspx", data);
		}

		public FileResult ExportToXml()
		{
			//var fullUrl = Web.AdminHistory.Previous;
			//var basicUrl = "".Substring(fullUrl.IndexOf("/Admin"));
			var basicUrl = "";

			var zipPath = _contratoService.ExportToXml(GetContratosReportByRequest().Select(i => i.ContratoVigenciaBalanco).ToList(), true, UserSession.Person.ID, basicUrl);
			if (zipPath != null)
			{
				var fileName = zipPath.RightFrom("\\\\");
				Web.SetMessage("Contrato(s) exportado(s) com sucesso.", "SaveSuccess");
				return File(zipPath, "application/zip", fileName);
			}
			Web.SetMessage("Nenhum contrato exportado.", "error");
			return null;
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var contrato = new Contrato();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					contrato = _contratoService.FindByID(Request["ID"].ToInt(0));
					if (contrato == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}

				contrato.UpdateFromRequest();

				if (contrato.ContratoCaracteristica.Nome.Equals("Curto Prazo", StringComparison.InvariantCultureIgnoreCase))
				{
					var prazo = Request["PrazoCP"].ConvertToDate(null);
					if (prazo == null)
						prazo = contrato.PrazoInicio;

					contrato.PrazoInicio = prazo.Value;
					contrato.PrazoFim = prazo.Value;
				}

                if (contrato.ContratoCaracteristica.Nome.Equals("Curto Prazo", StringComparison.InvariantCultureIgnoreCase) || contrato.ContratoCaracteristica.Nome.Equals("SWAP", StringComparison.InvariantCultureIgnoreCase))
                   //pega numero interno do ultimo contrato de CP do mesmo mes
                   if (!isEdit)
                        contrato.NumeroInternoControle = _contratoService.fGetNewNumeroInternoControle(contrato.ContratoCaracteristicaID.ToInt(), contrato.PrazoInicio, contrato.PrazoFim);
               

                contrato.PrazoInicio = Dates.GetFirstDayOfMonth(contrato.PrazoInicio);
				contrato.PrazoFim = Dates.GetLastDayOfMonth(contrato.PrazoFim);
				contrato.NumeroInternoControle = contrato.NumeroInternoControle ?? GetNumControleInternoDefault(contrato);

				if (contrato.PrazoInicio > contrato.PrazoFim)
					throw new Exception("Prazo inicial não pode ser superior a prazo final.");

				var contratoExistente = _contratoService.GetByNumeroInternoControle(contrato.NumeroInternoControle);
				if (contratoExistente != null)
				{
					if ((!isEdit) || ((isEdit) && (contrato.ID != contratoExistente.ID)))
						throw new Exception("Número interno controle já cadastrado.");
				}
				contratoExistente = _contratoService.Get(contrato.NumeroInternoControle, contrato.PerfilAgenteCompradorID.Value, contrato.PerfilAgenteVendedorID.Value, contrato.ContratoCaracteristicaID.Value);
				if (contratoExistente != null)
				{
					if ((!isEdit) || ((isEdit) && (contrato.ID != contratoExistente.ID)))
						throw new Exception("Contrato já cadastrado.");
				}

				var contratoCaracteristica = _contratoCaracteristicaService.FindByID(contrato.ContratoCaracteristicaID.Value);
				if (contratoCaracteristica != null)
				{
					if ((contratoCaracteristica.ConsideraCliqCcee) && (contrato.ContratoOrigemID == null))
						throw new Exception("Contrato origem precisa ser selcionado para este tipo de característica de contrato.");
				}

				if (contrato.IndiceReajuste == Contrato.IndicesReajuste.SemReajuste.ToString())
				{
					contrato.HasReajusteInicioContrato = false;
					contrato.HasReajusteNegativo = false;
				}

				var contratosVigencia = contrato.ContratoVigenciaList;
				if (contratosVigencia.Any())
				{
					if (contratosVigencia.Any(i => (i.PrazoInicio <= contrato.PrazoInicio && i.PrazoFim <= contrato.PrazoInicio)
						|| (i.PrazoInicio >= contrato.PrazoFim && i.PrazoFim >= contrato.PrazoFim)))
						throw new Exception("Não é possível a edição deste contrato pois existem vigências com prazo diferentes do prazo cadastrado neste contrato.");
				}

				if ((contrato.TipoVencimentoNf != null) && (contrato.DiaVencimentoNf == null))
					throw new Exception("Campo 'dia de vencimento de NF' precisa ser preenchido para este tipo selecionado.");
				if ((contrato.TipoVencimentoNf == null) && (contrato.DiaVencimentoNf != null))
					throw new Exception("Campo 'tipo de vencimento de NF' precisa ser preenchido.");
				if ((contrato.DiaVencimentoNf != null) && ((contrato.DiaVencimentoNf < 1) || (contrato.DiaVencimentoNf > 31)))
					throw new Exception("Campo 'dia de vencimento de NF' não é válido.");

				_contratoService.Save(contrato);

				_contratoHasAttachmentService.DeleteMany(contrato.ContratoHasAttachmentList);

				contrato.UpdateChildrenFromRequest();

				var message = i18n.Gaia.Get("Forms", "SaveSuccess");

				var chamadaNegociacaoID = Request.Form["ChamadaID"].ToInt(null);
				if (chamadaNegociacaoID != null)
				{
					var chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamadaNegociacaoID.Value);
					if (chamadaNegociacao != null)
					{
						chamadaNegociacao.ContratoID = contrato.ID;
						chamadaNegociacao.Status = ChamadaNegociacao.TiposStatus.Finalizado.ToString();
						_chamadaNegociacaoService.Update(chamadaNegociacao);

						if (Request.Form["autovigencia"].ToBoolean())
						{
							CliqCcee cliqCcee = null;
							var cliqsCcee = _cliqCceeService.GetByContrato(contrato, chamadaNegociacao.SubmercadoID.Value, null, true);
							if (cliqsCcee.Any())
								cliqCcee = cliqsCcee.Last();
							else
								message += " (Obs: CliqCCEE não localizado no contrato criado).";

							AddContratoVigenciaBalancos(contrato, chamadaNegociacao, Request.Form["Preco"].ToDouble(0), cliqCcee);
						}
                        //Aqui manda email
                        
                    }
				}

				_contratoHasAttachmentService.InsertMany(contrato.ContratoHasAttachmentList);

				Web.SetMessage(message);
				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? contrato.GetAdminURL() : /*Web.AdminHistory.Previous ??*/ Web.BaseUrl + "Admin/ContratoVigencia/?contrato=" + contrato.ID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { contrato.ID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index", "ContratoVigencia", new { contrato = contrato.ID });
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });
				TempData["ContratoModel"] = contrato;

				if (contrato != null)
				{
					if (isEdit)
						return RedirectToAction("Edit", new { contrato.ID });
					else if (contrato.ID != null)
						return RedirectToAction("Index", "ContratoVigencia", new { contrato.ID });
				}

				return RedirectToAction("Create");
			}
		}


        [ValidateInput(false)]
        public ActionResult SaveSwap()
        {
            //jogada pra criar contratos um atras do outro - Hilario  15/03/21

            var chamadaNegociacaoID = Request.Form["ChamadaID"].ToInt(null);
            var chamadaNegociacao = new ChamadaNegociacao();
            if (chamadaNegociacaoID != null)           
                chamadaNegociacao = _chamadaNegociacaoService.FindByID(chamadaNegociacaoID.Value);

            var detalhamentoChamadaSwapAndVenda = _chamadaNegociacaoService.getDetalhamentoChamadaSwapAndVenda(chamadaNegociacao);

            var prefixos = new List<String>();
            prefixos.Add("swapVenda_");
            prefixos.Add("swapCompra_");
            if (detalhamentoChamadaSwapAndVenda.opcaoSwapAndVenda.montanteMWhVenda > 0)
                prefixos.Add("venda_");
            try
            {
                var message = i18n.Gaia.Get("Forms", "SaveSuccess");
                foreach (var prefixo in prefixos)
                {
                    var contrato = new Contrato();                    
                    contrato.PerfilAgenteVendedorID = Request[prefixo + "PerfilAgenteVendedorID"].ToInt();
                    contrato.PerfilAgenteCompradorID = Request[prefixo + "PerfilAgenteCompradorID"].ToInt();
                    //contrato.NumeroInternoControle = Request[prefixo + "NumeroInternoControle"];
                    contrato.IndiceReajuste = Request[prefixo + "IndiceReajuste"];                    
                    contrato.MesReajuste = Request[prefixo + "MesReajuste"].ToInt();
                    contrato.TipoVencimentoNf = Request[prefixo + "TipoVencimentoNf"];
                    contrato.DiaVencimentoNf = Convert.ToInt16(Request[prefixo + "diaVencimentoNf"]);
                    contrato.ApresentacaoGaranFinanc = null;
                    contrato.HasReajusteNegativo = Convert.ToBoolean(Request[prefixo + "hasReajusteNegativo"]);
                    contrato.HasReajusteInicioContrato = Convert.ToBoolean(Request[prefixo + "HasReajusteInicioContrato"]);
                    if (prefixo.Substring(0, 4) == "swap")
                        contrato.ContratoCaracteristicaID = 11;
                    else
                        contrato.ContratoCaracteristicaID = 1;

                   
                    var prazo = Request["PrazoCP"].ConvertToDate(null);
                    if (prazo == null)
                        prazo = contrato.PrazoInicio;
                    
                    contrato.PrazoInicio = prazo.Value;
                    contrato.PrazoFim = prazo.Value;
                  


                    contrato.PrazoInicio = Dates.GetFirstDayOfMonth(contrato.PrazoInicio);
                    contrato.PrazoFim = Dates.GetLastDayOfMonth(contrato.PrazoFim);
                    //contrato.NumeroInternoControle = contrato.NumeroInternoControle ?? GetNumControleInternoDefault(contrato);

                    contrato.DataBaseReajuste = contrato.PrazoInicio;


                    if (contrato.PrazoInicio > contrato.PrazoFim)
                        throw new Exception("Prazo inicial não pode ser superior a prazo final.");

                    //var contratoExistente = _contratoService.GetByNumeroInternoControle(contrato.NumeroInternoControle);
                    
                    //contratoExistente = _contratoService.Get(contrato.NumeroInternoControle, contrato.PerfilAgenteCompradorID.Value, contrato.PerfilAgenteVendedorID.Value, contrato.ContratoCaracteristicaID.Value);
                    
                    var contratoCaracteristica = _contratoCaracteristicaService.FindByID(contrato.ContratoCaracteristicaID.Value);
                    if (contratoCaracteristica != null)
                    {
                        if ((contratoCaracteristica.ConsideraCliqCcee) && (contrato.ContratoOrigemID == null))
                            throw new Exception("Contrato origem precisa ser selcionado para este tipo de característica de contrato.");
                    }

                    var contratosVigencia = contrato.ContratoVigenciaList;
                    if (contratosVigencia.Any())
                    {
                        if (contratosVigencia.Any(i => (i.PrazoInicio <= contrato.PrazoInicio && i.PrazoFim <= contrato.PrazoInicio)
                            || (i.PrazoInicio >= contrato.PrazoFim && i.PrazoFim >= contrato.PrazoFim)))
                            throw new Exception("Não é possível a edição deste contrato pois existem vigências com prazo diferentes do prazo cadastrado neste contrato.");
                    }

                    if ((contrato.TipoVencimentoNf != null) && (contrato.DiaVencimentoNf == null))
                        throw new Exception("Campo 'dia de vencimento de NF' precisa ser preenchido para este tipo selecionado.");
                    if ((contrato.TipoVencimentoNf == null) && (contrato.DiaVencimentoNf != null))
                        throw new Exception("Campo 'tipo de vencimento de NF' precisa ser preenchido.");
                    if ((contrato.DiaVencimentoNf != null) && ((contrato.DiaVencimentoNf < 1) || (contrato.DiaVencimentoNf > 31)))
                        throw new Exception("Campo 'dia de vencimento de NF' não é válido.");
                }

                //agora grava
                foreach (var prefixo in prefixos)
                {
                    var contrato = new Contrato();
                    var isEdit = Request["ID"].IsNotBlank();
                    //contrato.UpdateFromRequest();
                    contrato.PerfilAgenteVendedorID = Request[prefixo + "PerfilAgenteVendedorID"].ToInt(); 
                    contrato.PerfilAgenteCompradorID = Request[prefixo + "PerfilAgenteCompradorID"].ToInt(); 
                    //contrato.NumeroInternoControle = Request[prefixo + "NumeroInternoControle"];
                    contrato.IndiceReajuste = Request[prefixo + "IndiceReajuste"];
                    //contrato.DataBaseReajuste = Convert.ToDateTime(Request[prefixo + "DataBaseReajuste"]);
                    contrato.MesReajuste = Request[prefixo + "MesReajuste"].ToInt();
                    contrato.TipoVencimentoNf = Request[prefixo + "TipoVencimentoNf"];
                    contrato.DiaVencimentoNf = Convert.ToInt16(Request[prefixo + "diaVencimentoNf"]);
                    contrato.ApresentacaoGaranFinanc = null;
                    contrato.HasReajusteNegativo = Convert.ToBoolean(Request[prefixo + "hasReajusteNegativo"]);
                    contrato.HasReajusteInicioContrato = Convert.ToBoolean(Request[prefixo + "HasReajusteInicioContrato"]);
                    //contrato.ContratoCaracteristicaID = Convert.ToInt16(Request["ContratoCaracteristicaID"]);
                    contrato.EmailActive = false;
                    //contrato.TipoPreco = Request[prefixo + "PerfilAgenteVendedorID"].ToInt();
                    //contrato.Spread = Request[prefixo + "PerfilAgenteVendedorID"].ToInt();
                    //contrato.Pld= Request[prefixo + "PerfilAgenteVendedorID"].ToInt(); 
                    //contrato.Preco = Request[prefixo + "PerfilAgenteVendedorID"].ToInt();

                    if (prefixo.Substring(0, 4) == "swap")
                        contrato.ContratoCaracteristicaID = 11;
                    else
                        contrato.ContratoCaracteristicaID = 1;


                    
                    var prazo = Request["PrazoCP"].ConvertToDate(null);
                    if (prazo == null)
                        prazo = contrato.PrazoInicio;
                    
                    contrato.PrazoInicio = prazo.Value;
                    contrato.PrazoFim = prazo.Value;                   
                    

                    contrato.PrazoInicio = Dates.GetFirstDayOfMonth(contrato.PrazoInicio);
                    contrato.PrazoFim = Dates.GetLastDayOfMonth(contrato.PrazoFim);
                    //contrato.NumeroInternoControle = contrato.NumeroInternoControle ?? GetNumControleInternoDefault(contrato);
                    contrato.DataBaseReajuste = contrato.PrazoInicio;
                    contrato.MesReajuste = null;

                    //pega numero interno do ultimo contrato de CP do mesmo mes
                    contrato.NumeroInternoControle = _contratoService.fGetNewNumeroInternoControle(contrato.ContratoCaracteristicaID.ToInt(), contrato.PrazoInicio, contrato.PrazoFim);
                    _contratoService.Save(contrato);
                  
                    if (chamadaNegociacao != null)
                    {
                        chamadaNegociacao.ContratoID = contrato.ID;
                        chamadaNegociacao.Status = ChamadaNegociacao.TiposStatus.Finalizado.ToString();
                        _chamadaNegociacaoService.Update(chamadaNegociacao);

                        //var autovigencia = Request.Form[prefixo + "autovigencia"].ToBoolean();

                       // if (autovigencia)
                        //{ 
                        CliqCcee cliqCcee = null;
                        var cliqsCcee = _cliqCceeService.GetByContrato(contrato, chamadaNegociacao.SubmercadoID.Value, null, true);
                        if (cliqsCcee.Any())
                            cliqCcee = cliqsCcee.Last();
                        //else
                        //    message += " (Obs: CliqCCEE não localizado no contrato criado).";

                        //AddContratoVigenciaBalancos(contrato, chamadaNegociacao,, cliqCcee);
                        double spread = 0;
                        if (prefixo == "venda_")
                            spread = Request["venda_Spread"].ToDouble();
                        else
                            spread = chamadaNegociacao.OfertaList.OrderBy(o => o.DateAdded).Last().Spread.ToDouble();




                        AddContratoVigenciaBalancos(contrato, Request.Form[prefixo + "Preco"].ToDouble(0), Request.Form[prefixo + "montante"].ToDouble(0), Request.Form[prefixo + "TipoEnergia"].ToInt(), chamadaNegociacao.SubmercadoID.Value, chamadaNegociacao.OfertaList.OrderBy(o=>o.DateAdded).Last().TipoPreco, spread, cliqCcee);
                         
                        //contrato.TipoPreco = Request[prefixo + "PerfilAgenteVendedorID"].ToInt();
                        //contrato.Spread = Request[prefixo + "PerfilAgenteVendedorID"].ToInt();



                        //}
                        //Aqui manda email
                    }
                }
                //contrato.UpdateChildrenFromRequest();
                Web.SetMessage(message);
                var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

                if (Fmt.ConvertToBool(Request["ajax"]))
                {
                //var nextPage = Web.AdminHistory.Previous;
                    var nextPage = Web.BaseUrl + "Admin/ChamadaNegociacao";
                    return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
                }

               
                var previousUrl = Web.AdminHistory.Previous;
                if (previousUrl != null)
                    return Redirect(previousUrl);
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                //Web.SetMessage(HandleExceptionMessage(ex), "error");
                if (Fmt.ConvertToBool(Request["ajax"]))
                    return Json(new { success = false, message = Web.GetFlashMessageObject() });             

                return RedirectToAction("Create");
            }
        }
        

        public JsonResult GetContrato(int id)
		{
			var contrato = _contratoService.FindByID(id);
			if (contrato != null)
			{
				var viewModel = new ContratoViewModel()
				{
					Vendedor = contrato.PerfilAgenteVendedor.Sigla,
					ContratoCaracteristica = contrato.ContratoCaracteristica.Nome,
					NumeroInternoControle = contrato.NumeroInternoControle,
					PrazoInicio = contrato.PrazoInicio,
					PrazoFim = contrato.PrazoFim,
					Observacao = contrato.Observacao
				};

				return Json(viewModel, JsonRequestBehavior.AllowGet);
			}
			return Json(null);
		}

		public JsonResult GetContratosOrigem(int vendedor, DateTime inicio, DateTime fim)
		{
			var perfilAgente = _perfilAgenteService.FindByID(vendedor);
			if ((perfilAgente != null) && (perfilAgente.IsConsumidor))
			{
				var contratos = _contratoService.GetByPerfilAgenteComprador(vendedor, false, inicio, Dates.GetLastDayOfMonth(fim)).ToList();
                // pegar transferencias

                IEnumerable<PerfilAgente> perfilList = new List<PerfilAgente>() { perfilAgente }.AsEnumerable();


                var contratosTransferencias = _contratoService.GetTransferencias(inicio, false, perfilList);
                if (contratosTransferencias.Count >0)
                {
                    foreach (var contratoTransferencias in contratosTransferencias)
                    {
                        contratos.Add(_contratoService.FindByID(contratoTransferencias.ContratoVigenciaBalanco.ContratoVigencia.ContratoID.ToInt()));
                    }
                }


                if (contratos.Any())
					return Json(contratos.Select(i => new { i.ID, i.NumeroInternoControle }), JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetInfoContrato(int caracteristica, DateTime inicio, DateTime fim)
		{
			var contrato = _contratoService.GetMostRecent(caracteristica, inicio, Dates.GetLastDayOfMonth(fim));
			if (contrato != null)
			{
				var viewModel = new ContratoViewModel()
				{
					Vendedor = contrato.PerfilAgenteVendedor.Sigla,
					ContratoCaracteristica = contrato.ContratoCaracteristica.Nome,
					NumeroInternoControle = contrato.NumeroInternoControle,
					PrazoInicio = contrato.PrazoInicio,
					PrazoFim = contrato.PrazoFim,
					Observacao = contrato.Observacao
				};

				return Json(viewModel, JsonRequestBehavior.AllowGet);
			}
			return Json(null);
		}

		public JsonResult SendEmailFaturamentoByBalanco(int id, double? montante, double? preco)
		{
			var contratoVigenciaBalanco = _contratoVigenciaBalancoService.FindByID(id);
			if (contratoVigenciaBalanco != null)
			{
				_contratoService.SendEmailToContratoVendedor(contratoVigenciaBalanco, montante, preco);

				return Json(true, JsonRequestBehavior.AllowGet);
			}
			return Json(null);
		}

        

        public JsonResult SendEmailFaturamentoByTransferencia(int id, double? montante, double? preco)
		{
			var contratoVigenciaTransferencia = _contratoVigenciaTransferenciaService.FindByID(id);
			if (contratoVigenciaTransferencia != null)
			{
				_contratoService.SendEmailToContratoVendedor(_contratoService.GetSimulatedContratoVigenciaBalanco(contratoVigenciaTransferencia), montante, preco);

				return Json(true, JsonRequestBehavior.AllowGet);
			}
			return Json(null);
		}

		private void AddContratoVigenciaBalancos(Contrato contrato, ChamadaNegociacao chamadaNegociacao, double valorPreco, CliqCcee cliqCcee = null)
		{
			var tipoPreco = Request.Form["TipoPreco"];
			var spread = Request.Form["Spread"].ToDouble(0);

			var contratoVigencia = new ContratoVigencia()
			{
				ContratoID = contrato.ID,
				DescontoID = chamadaNegociacao.DescontoID,
				SubmercadoID = chamadaNegociacao.SubmercadoID,
				MontanteMwm = chamadaNegociacao.MontanteMwm,
				PrazoInicio = contrato.PrazoInicio,
				PrazoFim = contrato.PrazoFim,
				ApuracaoMontante = ContratoVigencia.ApuracoesMontante.Fixo.ToString(),
				Modulacao = ContratoVigencia.Modulacoes.Flat.ToString(),
				Preco = valorPreco,
				SazonalizacaoNegativa = 0,
				SazonalizacaoPositiva = 0,
				FlexibilidadeNegativa = 0,
				FlexibilidadePositiva = 0,
				Retusd = 0,
				TipoEnergia = 0,
				Observacao = "Vigencia criada automaticamente",
				DateAdded = DateTime.Now,
				Spread = spread,
				TipoPreco = tipoPreco
			};

			if (cliqCcee != null)
				contratoVigencia.CliqCceeID = cliqCcee.ID;

			_contratoVigenciaService.Save(contratoVigencia);

			var contratoVigenciaBalancos = _contratoVigenciaBalancoService.GetInsertedBalancos(contratoVigencia);
			if (contratoVigenciaBalancos.Any())
				_contratoVigenciaBalancoService.InsertMany(contratoVigenciaBalancos);
		}

        private void AddContratoVigenciaBalancos(Contrato contrato, double valorPreco, double montanteMwm, int descontoID, int submercadoID, string tipoPreco, double spread, CliqCcee cliqCcee = null)
        {

            
            //var tipoPreco = Request.Form["TipoPreco"];
            //var spread = Request.Form["Spread"].ToDouble(0);

            var contratoVigencia = new ContratoVigencia()
            {
                ContratoID = contrato.ID,
                DescontoID = descontoID,
                SubmercadoID = submercadoID,
                MontanteMwm = montanteMwm,
                PrazoInicio = contrato.PrazoInicio,
                PrazoFim = contrato.PrazoFim,
                ApuracaoMontante = ContratoVigencia.ApuracoesMontante.Fixo.ToString(),
                Modulacao = ContratoVigencia.Modulacoes.Flat.ToString(),
                Preco = valorPreco,
                SazonalizacaoNegativa = 0,
                SazonalizacaoPositiva = 0,
                FlexibilidadeNegativa = 0,
                FlexibilidadePositiva = 0,
                Retusd = 0,
                TipoEnergia = 0,
                Observacao = "Vigencia criada automaticamente",
                DateAdded = DateTime.Now,
                Spread = spread,
                TipoPreco = tipoPreco
            };

            if (cliqCcee != null)
                contratoVigencia.CliqCceeID = cliqCcee.ID;

            _contratoVigenciaService.Save(contratoVigencia);

            var contratoVigenciaBalancos = _contratoVigenciaBalancoService.GetInsertedBalancos(contratoVigencia);
            if (contratoVigenciaBalancos.Any())
                _contratoVigenciaBalancoService.InsertMany(contratoVigenciaBalancos);
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

		private string GetNumControleInternoDefault(Contrato contrato)
		{
			return string.Concat(contrato.PerfilAgenteVendedor.Agente.Sigla, " - ", contrato.PerfilAgenteComprador.Agente.Sigla, ": ", contrato.PrazoInicio.ToString("MM/yy")).Replace(" ", null);
		}

		private List<ContratoReportDto> GetContratosReportByRequest()
		{
			var list = new List<ContratoReportDto>();

			if (Request["date"] != null)
			{
				//var hasProinfa = Request["proinfa"].ToBoolean();
				var hasProinfa = true;

				DateTime parsedDate;
				if (DateTime.TryParse(Request["date"], out parsedDate))
				{
					var mes = Dates.GetFirstDayOfMonth(parsedDate);
					var agentes = _agenteService.GetByConcatnatedIds(Request["agentes"]);
					if (agentes.Count() == 0 & (UserSession.IsPerfilAgente))
					{
						foreach (var item in UserSession.Agentes)
						{
							agentes.Add(item);
						}
					}
					//var isDesativado = (string.IsNullOrEmpty(Request["isDesativado"]) ? null : (bool?)Request["isDesativado"].ToBoolean());
					//var isFaturado = (string.IsNullOrEmpty(Request["isFaturado"]) ? null : (bool?)Request["isFaturado"].ToBoolean());
					//var isAjustado = (string.IsNullOrEmpty(Request["isAjustado"]) ? null : (bool?)Request["isAjustado"].ToBoolean());
					//var isAptoAjuste = (string.IsNullOrEmpty(Request["isAptoAjuste"]) ? null : (bool?)Request["isAptoAjuste"].ToBoolean());
					//var IsAptoValidacao = (string.IsNullOrEmpty(Request["IsAptoValidacao"]) ? null : (bool?)Request["IsAptoValidacao"].ToBoolean());
					//var isValidado = (string.IsNullOrEmpty(Request["isValidado"]) ? null : (bool?)Request["isValidado"].ToBoolean());

					bool? isDesativado = null;
					if (Request["chkDesativado"].ToBoolean() && !Request["chkAtivado"].ToBoolean())
						isDesativado = true;
					if (!Request["chkDesativado"].ToBoolean() && Request["chkAtivado"].ToBoolean())
						isDesativado = false;

					bool? isFaturado = null;
					if (Request["chkNaoFaturado"].ToBoolean() && !Request["chkFaturado"].ToBoolean())
						isFaturado = false;
					if (!Request["chkNaoFaturado"].ToBoolean() && Request["chkFaturado"].ToBoolean())
						isFaturado = true;

					bool? isAjustado = null;
					if (Request["chkNaoAjustado"].ToBoolean() && !Request["chkAjustado"].ToBoolean())
						isAjustado = false;
					if (!Request["chkNaoAjustado"].ToBoolean() && Request["chkAjustado"].ToBoolean())
						isAjustado = true;

					bool? isAptoAjuste = null;
					if (Request["chkNaoAptoAjuste"].ToBoolean() && !Request["chkAptoAjuste"].ToBoolean())
						isAptoAjuste = false;
					if (!Request["chkNaoAptoAjuste"].ToBoolean() && Request["chkAptoAjuste"].ToBoolean())
						isAptoAjuste = true;

					bool? IsAptoValidacao = null;
					if (Request["chkNaoAptoValidacao"].ToBoolean() && !Request["chkAptoValidacao"].ToBoolean())
						IsAptoValidacao = false;
					if (!Request["chkNaoAptoValidacao"].ToBoolean() && Request["chkAptoValidacao"].ToBoolean())
						IsAptoValidacao = true;

					bool? isValidado = null;
					if (Request["chkNaoValidado"].ToBoolean() && !Request["chkValidado"].ToBoolean())
						isValidado = false;
					if (!Request["chkNaoValidado"].ToBoolean() && Request["chkValidado"].ToBoolean())
						isValidado = true;

					var chkListCaracteristicas = ContratoCaracteristicaHelper.GetDropdownOptions();
					var selectedCaracteristicas = new List<int>();
					foreach (var carac in chkListCaracteristicas)
					{
						if (Request["chk" + carac.Value.ToString()] == "on")
							selectedCaracteristicas.Add(Convert.ToInt16(carac.Value.ToString()));
					}

					if (selectedCaracteristicas.Count == 0 && !Request["chkDesativado"].ToBoolean() && !Request["chkAtivado"].ToBoolean())
					{
						isDesativado = false;
						foreach (var carac in chkListCaracteristicas)
						{
							if (Convert.ToInt16(carac.Value.ToString()) != 9) //default tira proinfa
								selectedCaracteristicas.Add(Convert.ToInt16(carac.Value.ToString()));
						}
					}



					list = _contratoService.GetReport(mes, hasProinfa, agentes, false, isAjustado, isAptoAjuste, IsAptoValidacao, isFaturado, isValidado, isDesativado, selectedCaracteristicas);
				}
			}

			return list;
		}

		public class FormViewModel
		{
			public Contrato Contrato;
			public ChamadaNegociacao ChamadaNegociacao;
			public bool HasVigencias;
			public bool HasAutoVigencias;
			public bool ReadOnly;
            public DetalhamentoChamadaSwapAndVendaDto detalhamentoChamadaSwapAndVenda;
        }

		public class ListViewModel
		{
			public List<ContratoDetailedDto> Contratos;
			public List<PerfilAgente> PerfisAgenteCompradorList;
			public List<PerfilAgente> PerfisAgenteVendedorList;
			public bool HasContratoOrigem;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
			public List<DateTime> Prazos
			{
				get
				{
					if (Contratos.Any())
						return Dates.GetByIntervalInMonths(Contratos.Min(i => i.PrazoInicio), Contratos.Max(i => i.PrazoFim));
					return new List<DateTime>();
				}
			}
		}

		public class SyntheticReportViewModel
		{
			public List<ContratoReportDto> Contratos { get; set; }
			public int HorasMes { get; set; }
			public bool HasMedicaoEnviada
			{
				get
				{
					return (Contratos.Any(i => i.ContratoVigenciaBalanco.IsMedicaoEnviada));
				}
			}
			public bool HasObservacao
			{
				get
				{
					return (Contratos.Any(i => i.Observacao != null));
				}
			}
		}

		public class ContratoViewModel
		{
			public string Vendedor { get; set; }
			public string ContratoCaracteristica { get; set; }
			public string NumeroInternoControle { get; set; }
			public DateTime PrazoInicio { get; set; }
			public DateTime PrazoFim { get; set; }
			public string Observacao { get; set; }
			public string FormattedPrazoInicio
			{
				get
				{
					return PrazoInicio.ToString("dd/MM/yyyy HH:mm:ss");
				}
			}
			public string FormattedPrazoFim
			{
				get
				{
					return PrazoFim.ToString("dd/MM/yyyy HH:mm:ss");
				}
			}
		}
	}
}
