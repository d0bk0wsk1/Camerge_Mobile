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
	public class DocumentController : ControllerBase
	{
		private readonly IDocumentService _documentService;
		private readonly IDocumentAtivoService _documentAtivoService;
		private readonly IDocumentHasAttachmentService _documentHasAttachmentService;
		private readonly IDocumentTypeService _documentTypeService;
		private readonly ITypeDocumentService _typeDocumentService;

		public DocumentController(IDocumentService documentService,
			IDocumentAtivoService documentAtivoService,
			IDocumentHasAttachmentService documentHasAttachmentService,
			IDocumentTypeService documentTypeService,
			ITypeDocumentService typeDocumentService)
		{
			_documentService = documentService;
			_documentAtivoService = documentAtivoService;
			_documentHasAttachmentService = documentHasAttachmentService;
			_documentTypeService = documentTypeService;
			_typeDocumentService = typeDocumentService;
		}

		public ActionResult Index(int? Page)
		{
			var data = new ListViewModel();

			var paging = _documentService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = (paging.Items.Count() / paging.ItemsPerPage);
			data.TotalRows = (paging.Items.Count());
			data.Documents = paging.Items;

			return AdminContent("Document/DocumentList.aspx", data);
		}

		public ActionResult Search(int? Page, bool isactive = true)
		{
			var data = new ListViewModel();

			var paging = _documentService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params, true);

			data.PageNum = paging.CurrentPage;
			data.PageCount = (paging.Items.Count() / paging.ItemsPerPage);
			data.TotalRows = (paging.Items.Count());
			data.Documents = paging.Items;

			return AdminContent("Document/DocumentSearch.aspx", data);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Document = TempData["DocumentModel"] as Document;

			if (data.Document == null)
			{
				data.Document = new Document()
				{
					IsActive = true
				};
				data.Document.UpdateFromRequest();
			}

			return AdminContent("Document/DocumentEdit.aspx", data);
		}

        public ActionResult CreateBatch()
        {
            var data = new FormViewModel();
            data.Document = TempData["DocumentModel"] as Document;

            if (data.Document == null)
            {
                data.Document = new Document()
                {
                    IsActive = true
                };
                data.Document.UpdateFromRequest();
            }

            return AdminContent("Document/DocumentEditBatch.aspx", data);
        }

        public ActionResult SaveBatch()
        {
            var mesReferencia = Convert.ToDateTime(Request["referencia"]);
            var processados = _documentService.ImportDocumentBatch(Request["AttachmentID"], mesReferencia, Request["Types"]);           

            //return AdminContent("FaturaDistribuidora/FaturaDistribuidoraImportJSON.aspx", data);

            //Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));
            var textoDisplay = processados.Aprovados.Count() + " Documentos Importados com Sucesso. " + processados.Reprovados.Count() + " Documentos nao importados.";
            if (processados.Reprovados.Count() > 0)
            {
                textoDisplay += "(";
                foreach (var reprovadas in processados.Reprovados)
                {
                    textoDisplay += reprovadas.filename + " ";
                }
                textoDisplay += ")";
            }

            Web.SetMessage(textoDisplay);

            var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

            if (Fmt.ConvertToBool(Request["ajax"]))
            {
                //var nextPage = isSaveAndRefresh ? Web.BaseUrl + "Admin/FaturaDistribuidora/Historico" : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/FaturaDistribuidora/Historico";
                var nextPage = Web.BaseUrl + "Admin/Document/CreateBatch";
                //return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
                return Json(new { success = true, message = "Documentos Importados", nextPage });
                //return AdminContent("FaturaDistribuidora/FaturaDistribuidoraImportJSON.aspx", data);
            }

            var previousUrl = Web.AdminHistory.Previous;
            if (previousUrl != null)
                return Redirect(previousUrl);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id, bool readOnly = false)
		{
			var data = new FormViewModel();
			data.Document = TempData["DocumentModel"] as Document ?? _documentService.FindByID(id);
			data.ReadOnly = readOnly;

			if (data.Document == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			return AdminContent("Document/DocumentEdit.aspx", data);
		}

		public ActionResult View(int id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(int id)
		{
			var document = _documentService.FindByID(id);
			if (document == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}

			document.ID = null;
			TempData["DocumentModel"] = document;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");

			return Create();
		}

		public ActionResult Del(int id)
		{
			try
			{
				var document = _documentService.FindByID(id);
				if (document == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_documentHasAttachmentService.DeleteMany(document.DocumentHasAttachmentList);
					_documentAtivoService.DeleteMany(document.DocumentAtivoList);
					_documentTypeService.DeleteMany(document.DocumentTypeList);
					_documentService.Delete(document);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Document" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		public ActionResult DelMultiple(string ids)
		{
			try
			{
				var idsDocument = ids.Split(',').Select(i => i.ToInt(0));
				if (idsDocument.Any())
				{
					foreach (var idDocument in idsDocument)
					{
						var document = _documentService.FindByID(idDocument);
						if (document != null)
						{
							_documentHasAttachmentService.DeleteMany(document.DocumentHasAttachmentList);
							_documentAtivoService.DeleteMany(document.DocumentAtivoList);
							_documentTypeService.DeleteMany(document.DocumentTypeList);
							_documentService.Delete(document);
						}
					}

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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Document" }, JsonRequestBehavior.AllowGet);

			var previousUrl = Web.AdminHistory.Previous;
			if (previousUrl != null)
				return Redirect(previousUrl);
			return RedirectToAction("Index");
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var document = new Document();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					document = _documentService.FindByID(Request["ID"].ToInt(0));
					if (document == null)
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
				}
				else
				{
					document.DateAdded = DateTime.Now;
				}

				document.PersonID = UserSession.Person.ID;

				document.UpdateFromRequest();
				_documentService.Save(document);

				_documentAtivoService.DeleteMany(document.DocumentAtivoList);
				_documentTypeService.DeleteMany(document.DocumentTypeList);
				_documentHasAttachmentService.DeleteMany(document.DocumentHasAttachmentList);

				document.UpdateChildrenFromRequest();

				_documentHasAttachmentService.InsertMany(document.DocumentHasAttachmentList);

				var ativosSelected = Request["Ativos"];
				if (ativosSelected.IsNotBlank())
				{
					foreach (var ativoSelected in ativosSelected.Split(','))
						document.DocumentAtivoList.Add(new DocumentAtivo() { DocumentID = document.ID, AtivoID = ativoSelected.ToInt(0) });
					_documentAtivoService.InsertMany(document.DocumentAtivoList);
				}

				var typesSelected = Request["Types"];
				if (typesSelected.IsNotBlank())
				{
					foreach (var typeSelected in typesSelected.Split(','))
						document.DocumentTypeList.Add(new DocumentType() { DocumentID = document.ID, TypeDocumentID = typeSelected.ToInt(0) });
					_documentTypeService.InsertMany(document.DocumentTypeList);
				}

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? document.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Document";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Edit", new { document.ID });

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

				TempData["DocumentModel"] = document;
				return isEdit && document != null ? RedirectToAction("Edit", new { document.ID }) : RedirectToAction("Create");
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
			public List<Document> Documents;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Document Document;
			public bool ReadOnly;
		}
	}
}
