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
	public class MapeadorPotencialCacheController : ControllerBase
	{
		private readonly IAtivoService _ativoService;
		private readonly IMapeadorPotencialCacheService _mapeadorPotencialCacheService;

		public MapeadorPotencialCacheController(IAtivoService ativoService,
			IMapeadorPotencialCacheService mapeadorPotencialCacheService)
		{
			_ativoService = ativoService;
			_mapeadorPotencialCacheService = mapeadorPotencialCacheService;
		}

		//
		// GET: /Admin/MapeadorCenario/
		public ActionResult Index(Int32? Page)
		{
			var data = new ListViewModel();

			var paging = _ativoService.GetAllWithPaging(
				(UserSession.IsPerfilAgente || UserSession.IsPotencialAgente) ? UserSession.Agentes.Select(i => i.ID.Value) : null,
				PerfilAgente.TiposRelacao.Potencial.ToString(),
				Page ?? 1,
				Util.GetSettingInt("ItemsPerPage", 30),
				Request.Params);

			data.PageNum = paging.CurrentPage;
			data.PageCount = paging.TotalPages;
			data.TotalRows = paging.TotalItems;
			data.Ativos = paging.Items;

			return AdminContent("MapeadorPotencialCache/MapeadorPotencialCacheList.aspx", data);
		}

		public ActionResult Config(int ativo, DateTime? dtini = null, DateTime? dtfim = null)
		{
			var data = new ConfigViewModel()
			{
				Ativo = _ativoService.FindByID(ativo)
			};

			if (data.Ativo != null)
				data.Mapeadores = _mapeadorPotencialCacheService.GetReadyToEdit(data.Ativo, dtini, dtfim);

			if (data.Mapeadores.Any())
			{
				data.FeriasVigentes = data.Ativo.FeriasList;
				if (data.FeriasVigentes.Any())
				{
					var fromDate = data.Mapeadores.Min(i => i.DataInicio);
					var toDate = data.Mapeadores.Min(i => i.DataFim);

					data.FeriasVigentes = data.FeriasVigentes.Where(i =>
						(i.DataInicio.Value.Year >= toDate.Year)
						|| (i.DataFim.Value.Year <= fromDate.Year));
				}
			}

			return AdminContent("MapeadorPotencialCache/MapeadorPotencialCacheConfig.aspx", data);
		}

		[ValidateInput(false)]
		public ActionResult Save()
		{
			var mapeadorPotencialCache = new MapeadorPotencialCache();

			try
			{
				var list = GetFromRequestForm(Request.Form);
				if (!list.Any())
					throw new Exception("Erro ao tentar salvar este mapeador.");

				mapeadorPotencialCache.AtivoID = list.First().AtivoID;

				_mapeadorPotencialCacheService.DeleteMany(mapeadorPotencialCache.AtivoID.Value, list.Select(i => i.Mes));
				_mapeadorPotencialCacheService.InsertMany(list);

				Web.SetMessage(i18n.Gaia.Get("Forms", "SaveSuccess"));

				var isSaveAndRefresh = Request["SubmitValue"] == i18n.Gaia.Get("Forms", "SaveAndRefresh");

				if (Fmt.ConvertToBool(Request["ajax"]))
				{
					if (isSaveAndRefresh)
					{
						var nextPage = Web.BaseUrl + "Admin/MapeadorPotencialCache/Config/?ativo=" + mapeadorPotencialCache.AtivoID;
						return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
					}
					else
					{
						var nextPage = Web.BaseUrl + "Admin/MapeadorPotencialCache/";
						return Json(new { success = true, message = Web.GetFlashMessageObject(), nextPage });
					}
				}

				if (isSaveAndRefresh)
					return RedirectToAction("Config", new { ativo = mapeadorPotencialCache.AtivoID });

				var previousUrl = Web.AdminHistory.Previous;
				if (previousUrl != null)
					return Redirect(previousUrl);
				return RedirectToAction("Index", "MapeadorPotencialCache");
			}
			catch (Exception ex)
			{
				Web.SetMessage(HandleExceptionMessage(ex), "error");
				if (Fmt.ConvertToBool(Request["ajax"]))
					return Json(new { success = false, message = Web.GetFlashMessageObject() });

				TempData["AtivoModel"] = mapeadorPotencialCache;
				return RedirectToAction("Index");
			}
		}

		public JsonResult GetValue(int ativoID, DateTime mes, string tipo)
		{
			var value = _mapeadorPotencialCacheService.GetMWm(ativoID, mes, tipo);

			return Json((value == null) ? string.Empty : value.Value.ToString("N3"), JsonRequestBehavior.AllowGet);
		}

		private List<MapeadorPotencialCacheDto> GetFromRequestForm(NameValueCollection forms)
		{
			var list = new List<MapeadorPotencialCacheDto>() { new MapeadorPotencialCacheDto() };

			int index = 0;

			foreach (string key in forms.AllKeys)
			{
				if (key.StartsWith(nameof(MapeadorPotencialCache)))
				{
					var indexForm = key.Substring((key.IndexOf('[') + 1), (key.IndexOf(']') - ((key.IndexOf('[') + 1)))).ToInt();
					if (indexForm > index)
					{
						index = indexForm;
						list.Add(new MapeadorPotencialCacheDto());
					}

					var dto = list[index];
					if (dto != null)
					{
						var fieldFormName = key.Substring((key.IndexOf('.') + 1), (key.Length - (key.IndexOf('.') + 1)));

						var property = dto.GetType().GetProperties().FirstOrDefault(i => i.Name == fieldFormName);
						if (property != null)
						{
							var value = forms[key];

							var isValueNull = string.IsNullOrEmpty(value);
							var propertyType = property.PropertyType;
							var propertyTypeNullable = Nullable.GetUnderlyingType(propertyType);

							if ((isValueNull) && (propertyTypeNullable == null))
								throw new Exception(string.Format("{0}: não permite valores nulos.", property.Name));

							if (propertyTypeNullable != null)
								propertyType = propertyTypeNullable;

							if ((!isValueNull) && (propertyType == typeof(Double)))
							{
								if (value.Contains('.'))
									value = value.Replace('.', ',');
								property.SetValue(dto, Fmt.ToDouble(value, false, value.Contains("%")));
							}
							else
							{
								property.SetValue(dto, (isValueNull) ? null : Convert.ChangeType(value, propertyType));
							}
						}
					}
				}
			}

			return list;
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
			public List<Ativo> Ativos;
			public long TotalRows;
			public long PageCount;
			public long PageNum;
		}

		public class ConfigViewModel
		{
			public Ativo Ativo;
			public IEnumerable<Ferias> FeriasVigentes;
			public List<MapeadorPotencialCacheDto> Mapeadores;
		}
	}
}
