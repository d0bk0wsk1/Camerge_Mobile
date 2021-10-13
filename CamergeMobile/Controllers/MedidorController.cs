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
	public class MedidorController : ControllerBase
	{
		private readonly IMedidorService _medidorService;

		public MedidorController(IMedidorService medidorService)
		{
			_medidorService = medidorService;
		}

		//
		// GET: /Admin/Medidor/
		public ActionResult Index(Int32? Page)
		{

			var data = new ListViewModel();
			var paging = _medidorService.GetAllWithPaging(
				UserSession.IsCliente ? UserSession.Agentes.Select(i => i.ID.Value) : null,
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Medidores = paging.Items;

			return AdminContent("Medidor/MedidorList.aspx", data);
		}

		//
		// GET: /Admin/GetMedidores/
		public JsonResult GetMedidores()
		{
			Object medidores;

			if (Request["ativo"].IsNotBlank())
			{
				medidores = MedidorList.LoadByAtivoID(Request["ativo"].ToInt(0)).Select(o => new { o.ID, o.Codigo });
			}
			else
			{
				medidores = _medidorService.GetAll().Select(o => new { o.ID, o.Codigo });
			}

			return Json(medidores, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Create()
		{
			var data = new FormViewModel();
			data.Medidor = TempData["MedidorModel"] as Medidor;
			if (data.Medidor == null)
			{
				data.Medidor = _medidorService.GetDefaultMedidor();
				data.Medidor.UpdateFromRequest();
			}
			return AdminContent("Medidor/MedidorEdit.aspx", data);
		}

		public ActionResult Edit(Int32 id, Boolean readOnly = false)
		{
			var data = new FormViewModel();
			data.Medidor = TempData["MedidorModel"] as Medidor ?? _medidorService.FindByID(id);
			data.ReadOnly = readOnly;
			if (data.Medidor == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Medidor/MedidorEdit.aspx", data);
		}

		public ActionResult View(Int32 id)
		{
			return Edit(id, true);
		}

		public ActionResult Duplicate(Int32 id)
		{
			var medidor = _medidorService.FindByID(id);
			if (medidor == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
				return RedirectToAction("Index");
			}
			medidor.ID = null;
			TempData["MedidorModel"] = medidor;
			Web.SetMessage(i18n.Gaia.Get("Forms", "EditingDuplicate"), "info");
			return Create();
		}

		public ActionResult Del(Int32 id)
		{
			var medidor = _medidorService.FindByID(id);
			if (medidor == null)
			{
				Web.SetMessage(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"), "error");
			}
			else
			{
				_medidorService.Delete(medidor);
				Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));
			}

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Medidor" }, JsonRequestBehavior.AllowGet);
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

			_medidorService.DeleteMany(ids.Split(',').Select(id => id.ToInt(0)));

			Web.SetMessage(i18n.Gaia.Get("Lists", "DeleteSuccess"));

			if (Fmt.ConvertToBool(Request["ajax"]))
			{
				return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage = Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Medidor" }, JsonRequestBehavior.AllowGet);
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

			var medidor = _medidorService.GetDefaultMedidor();
			var isEdit = Request["ID"].IsNotBlank();

			try
			{

				if (isEdit)
				{
					medidor = _medidorService.FindByID(Request["ID"].ToInt(0));
					if (medidor == null)
					{
						throw new Exception(i18n.Gaia.Get("FormValidation", "EditRecordNotFound"));
					}
				}

				medidor.UpdateFromRequest();
				//DomainMapper.MapFromRequest(medidor, Web.Request);

				if (medidor.Tipo == Medidor.Tipos.Principal.ToString())
				{
					var medidorPrincipalExistente = Medidor.Load(new SqlQuery("WHERE tipo = '" + Medidor.Tipos.Principal + "' AND ativo_id = ").AddParameter(medidor.AtivoID));
					if (medidorPrincipalExistente != null && medidor.ID != medidorPrincipalExistente.ID)
					{
						throw new ArgumentException("Já existe um medidor principal cadastrado para este ativo");
					}
				}

				_medidorService.Save(medidor);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					var nextPage = isSaveAndRefresh ? medidor.GetAdminURL() : Web.AdminHistory.Previous ?? Web.BaseUrl + "Admin/Medidor";
					return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
				}

				if (isSaveAndRefresh)
				{
					return RedirectToAction("Edit", new { medidor.ID });
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
				TempData["MedidorModel"] = medidor;
				return isEdit && medidor != null ? RedirectToAction("Edit", new { medidor.ID }) : RedirectToAction("Create");
			}
		}

		public ActionResult Medicoes(Int32 id)
		{
			var data = new MedicoesViewModel();
			data.Medidor = _medidorService.FindByID(id);
			data.Medicoes = MedicaoRawList.Load(new SqlQuery("SELECT * FROM medicao_raw WHERE medidor_id = ").AddParameter(data.Medidor.ID).Add(" ORDER BY data_leitura DESC LIMIT 50"));
			if (data.Medidor == null)
			{
				Web.SetMessage("O medidor que você está tentando visualizar não existe mais", "error");
				return RedirectToAction("Index");
			}
			return AdminContent("Medidor/MedidorMedicoes.aspx", data);
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
			public List<Medidor> Medidores;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class MedicoesViewModel
		{
			public Medidor Medidor;
			public List<MedicaoRaw> Medicoes = new List<MedicaoRaw>();
		}

		public class FormViewModel
		{
			public Medidor Medidor;
			public Boolean ReadOnly;
		}
	}
}
