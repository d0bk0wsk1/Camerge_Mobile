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
	public class CidadeController : ControllerBase
	{
		private readonly ICidadeService _cidadeService;
		private readonly IUnidadeFederativaService _unidadeFederativaService;

		public CidadeController(ICidadeService cidadeService,
			IUnidadeFederativaService unidadeFederativaService)
		{
			_cidadeService = cidadeService;
			_unidadeFederativaService = unidadeFederativaService;
		}

		//
		// GET: /Admin/UnidadeFederativa/
		public ActionResult Index(Int32? uf, Int32? Page)
		{
			var actionParams = Request.Params;

			if (uf != null)
			{
				var data = new ListViewModel();

				var paging = _cidadeService.GetAllWithPaging(Page ?? 1, Util.GetSettingInt("ItemsPerPage", 30), actionParams);

				data.PageNum = paging.CurrentPage;
				data.PageCount = paging.TotalPages;
				data.TotalRows = paging.TotalItems;
				data.Cidades = paging.Items;
				data.UnidadeFederativa = _unidadeFederativaService.FindByID(uf.Value);

				return AdminContent("Cidade/CidadeList.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Create(Int32? uf)
		{
			if (uf != null)
			{
				var data = new FormViewModel();
				data.Cidade = TempData["CidadeModel"] as Cidade;
				data.UnidadeFederativa = _unidadeFederativaService.FindByID(uf.Value);
				if (data.Cidade == null)
				{
					data.Cidade = new Cidade();
					data.Cidade.UnidadeFederativaID = uf.Value;

					data.Cidade.UpdateFromRequest();
				}
				return AdminContent("Cidade/CidadeEdit.aspx", data);
			}
			return HttpNotFound();
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Cidade = TempData["CidadeModel"] as Cidade ?? _cidadeService.FindByID(id);
			data.ReadOnly = readOnly;
			data.UnidadeFederativa = data.Cidade.UnidadeFederativa;
			if (data.Cidade == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Cidade/CidadeEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var cidade = _cidadeService.FindByID(id);
			if (cidade == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			cidade.ID = null;
			TempData["CidadeModel"] = cidade;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create(cidade.UnidadeFederativaID);
		}

		public ActionResult Del(Int32 id)
		{
			var cidade = _cidadeService.FindByID(id);
			try
			{
				if (cidade == null)
				{
					Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				}
				else
				{
					_cidadeService.Delete(cidade);
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Cidade/?uf=" + cidade.UnidadeFederativaID }, JsonRequestBehavior.AllowGet);
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
				_cidadeService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));
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
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Cidade" }, JsonRequestBehavior.AllowGet);
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

			var cidade = new Cidade();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{
				if (isEdit)
				{
					cidade = _cidadeService.FindByID(Request["ID"].ToInt(0));
					if (cidade == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				cidade.UpdateFromRequest();
				_cidadeService.Save(cidade);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? cidade.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Cidade/?uf=" + cidade.UnidadeFederativaID;
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { cidade.ID });
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
				TempData["UnidadeFederativaModel"] = cidade;
				return isEdit && cidade != null ? RedirectToAction("Edit", new { cidade.ID }) : RedirectToAction("Create");
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
			public List<Cidade> Cidades;
			public UnidadeFederativa UnidadeFederativa;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class FormViewModel
		{
			public Cidade Cidade;
			public UnidadeFederativa UnidadeFederativa;
			public Boolean ReadOnly;
		}
	}
}
