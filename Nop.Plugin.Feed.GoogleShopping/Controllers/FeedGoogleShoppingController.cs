using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Feed.GoogleShopping.Domain;
using Nop.Plugin.Feed.GoogleShopping.Models;
using Nop.Plugin.Feed.GoogleShopping.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Feed.GoogleShopping.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class FeedGoogleShoppingController : BasePluginController
    {
        #region Fields

        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGoogleService _googleService;
        private readonly ILocalizationService _localizationService;        
        private readonly INotificationService _notificationService;
        private readonly ILogger _logger;
        private readonly IPermissionService _permissionService;
        private readonly IPluginService _pluginService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;        
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWebHelper _webHelper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public FeedGoogleShoppingController(ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IGoogleService googleService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            ILogger logger,
            IPermissionService permissionService,
            IPluginService pluginService,
            IProductService productService,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWebHelper webHelper,
            IWebHostEnvironment webHostEnvironment,
            IWorkContext workContext)
        {
            _currencyService = currencyService;
            _genericAttributeService = genericAttributeService;
            _googleService = googleService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _logger = logger;
            _permissionService = permissionService;
            _pluginService = pluginService;
            _productService = productService;
            _settingService = settingService;
            _storeContext = storeContext;
            _storeService = storeService;
            _webHelper = webHelper;
            _webHostEnvironment = webHostEnvironment;
            _workContext = workContext;
        }

        #endregion

        #region Utilites

        /// <summary>
        /// Prepare FeedGoogleShoppingModel
        /// </summary>
        /// <param name="model">Model</param>
        private void PrepareModel(FeedGoogleShoppingModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return;

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var googleShoppingSettings = _settingService.LoadSetting<GoogleShoppingSettings>(storeScope);

            model.ProductPictureSize = googleShoppingSettings.ProductPictureSize;
            model.PassShippingInfoWeight = googleShoppingSettings.PassShippingInfoWeight;
            model.PassShippingInfoDimensions = googleShoppingSettings.PassShippingInfoDimensions;
            model.PricesConsiderPromotions = googleShoppingSettings.PricesConsiderPromotions;

            //currencies
            model.CurrencyId = googleShoppingSettings.CurrencyId;
            foreach (var c in _currencyService.GetAllCurrencies())
                model.AvailableCurrencies.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //Google categories
            model.DefaultGoogleCategory = googleShoppingSettings.DefaultGoogleCategory;
            model.AvailableGoogleCategories.Add(new SelectListItem { Text = "Select a category", Value = "" });
            foreach (var gc in _googleService.GetTaxonomyList())
                model.AvailableGoogleCategories.Add(new SelectListItem { Text = gc, Value = gc });

            model.HideGeneralBlock = _genericAttributeService.GetAttribute<bool>(_workContext.CurrentCustomer, GoogleShoppingDefaults.HideGeneralBlock);
            model.HideProductSettingsBlock = _genericAttributeService.GetAttribute<bool>(_workContext.CurrentCustomer, GoogleShoppingDefaults.HideProductSettingsBlock);

            //prepare nested search models
            model.GoogleProductSearchModel.SetGridPageSize();

            //file paths
            foreach (var store in _storeService.GetAllStores())
            {
                var localFilePath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "files\\exportimport", store.Id + "-" + googleShoppingSettings.StaticFileName);
                if (System.IO.File.Exists(localFilePath))
                    model.GeneratedFiles.Add(new GeneratedFileModel
                    {
                        StoreName = store.Name,
                        FileUrl = $"{_webHelper.GetStoreLocation(false)}files/exportimport/{store.Id}-{googleShoppingSettings.StaticFileName}"
                    });
            }

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.CurrencyId_OverrideForStore = _settingService.SettingExists(googleShoppingSettings, x => x.CurrencyId, storeScope);
                model.DefaultGoogleCategory_OverrideForStore = _settingService.SettingExists(googleShoppingSettings, x => x.DefaultGoogleCategory, storeScope);
                model.PassShippingInfoDimensions_OverrideForStore = _settingService.SettingExists(googleShoppingSettings, x => x.PassShippingInfoDimensions, storeScope);
                model.PassShippingInfoWeight_OverrideForStore = _settingService.SettingExists(googleShoppingSettings, x => x.PassShippingInfoWeight, storeScope);
                model.PricesConsiderPromotions_OverrideForStore = _settingService.SettingExists(googleShoppingSettings, x => x.PricesConsiderPromotions, storeScope);
                model.ProductPictureSize_OverrideForStore = _settingService.SettingExists(googleShoppingSettings, x => x.ProductPictureSize, storeScope);
            }
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            var model = new FeedGoogleShoppingModel();
            PrepareModel(model);           

            return View("~/Plugins/Feed.GoogleShopping/Views/Configure.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [HttpPost]
        [FormValueRequired("save")]
        [AutoValidateAntiforgeryToken]
        public IActionResult Configure(FeedGoogleShoppingModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var googleShoppingSettings = _settingService.LoadSetting<GoogleShoppingSettings>(storeScope);

            //save settings
            googleShoppingSettings.ProductPictureSize = model.ProductPictureSize;
            googleShoppingSettings.PassShippingInfoWeight = model.PassShippingInfoWeight;
            googleShoppingSettings.PassShippingInfoDimensions = model.PassShippingInfoDimensions;
            googleShoppingSettings.PricesConsiderPromotions = model.PricesConsiderPromotions;
            googleShoppingSettings.CurrencyId = model.CurrencyId;
            googleShoppingSettings.DefaultGoogleCategory = model.DefaultGoogleCategory;

            //_settingService.SaveSetting(_googleShoppingSettings);

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(googleShoppingSettings, x => x.CurrencyId, model.CurrencyId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleShoppingSettings, x => x.DefaultGoogleCategory, model.DefaultGoogleCategory_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleShoppingSettings, x => x.PassShippingInfoDimensions, model.PassShippingInfoDimensions_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleShoppingSettings, x => x.PassShippingInfoWeight, model.PassShippingInfoWeight_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleShoppingSettings, x => x.PricesConsiderPromotions, model.PricesConsiderPromotions_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleShoppingSettings, x => x.ProductPictureSize, model.ProductPictureSize_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            //redisplay the form
            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("generate")]
        [AutoValidateAntiforgeryToken]
        public IActionResult GenerateFeed(FeedGoogleShoppingModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            
            try
            {
                //plugin
                var pluginDescriptor = _pluginService.GetPluginDescriptorBySystemName<IPlugin>("PromotionFeed.GoogleShopping");
                if (pluginDescriptor == null || !(pluginDescriptor.Instance<IPlugin>() is GoogleShoppingService plugin))
                    throw new Exception(_localizationService.GetResource("Plugins.Feed.GoogleShopping.ExceptionLoadPlugin"));

                var stores = new List<Store>();
                var storeById = _storeService.GetStoreById(storeScope);
                if (storeScope > 0)
                    stores.Add(storeById);
                else
                    stores.AddRange(_storeService.GetAllStores());

                foreach (var store in stores)
                    plugin.GenerateStaticFile(store);

                _notificationService.SuccessNotification(_localizationService.GetResource("Plugins.Feed.GoogleShopping.SuccessResult"));
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                _logger.Error(exc.Message, exc);
            }

            return Configure();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult GoogleProductList(GoogleProductSearchModel searchModel)
        {
            var storeId = _storeContext.ActiveStoreScopeConfiguration;
            var products = _productService.SearchProducts(
                storeId: storeId,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize,
                showHidden: true);

            //prepare list model
            var model = new GoogleProductListModel().PrepareToGrid(searchModel, products, () =>
            {
                return products.Select(product =>
                {
                    var gModel = new GoogleProductModel
                    {
                        ProductId = product.Id,
                        ProductName = product.Name
                    };
                    var googleProduct = _googleService.GetByProductId(product.Id);
                    if (googleProduct != null)
                    {
                        gModel.GoogleCategory = googleProduct.Taxonomy;
                        gModel.Gender = googleProduct.Gender;
                        gModel.AgeGroup = googleProduct.AgeGroup;
                        gModel.Color = googleProduct.Color;
                        gModel.GoogleSize = googleProduct.Size;
                        gModel.CustomGoods = googleProduct.CustomGoods;
                    }
                    return gModel;
                });
            });

            return Json(model);
        }

        public IActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var googleProduct = _googleService.GetByProductId(id);

            var model = new GoogleProductModel
            {
                ProductId = id
            };

            if (googleProduct == null)
                return View("~/Plugins/Feed.GoogleShopping/Views/Edit.cshtml", model);

            model = new GoogleProductModel
            {
                Id = googleProduct.Id,
                ProductId = googleProduct.ProductId,
                Color = googleProduct.Color,
                AgeGroup = googleProduct.AgeGroup,
                CustomGoods = googleProduct.CustomGoods,
                Gender = googleProduct.Gender,
                GoogleSize = googleProduct.Size,
                GoogleCategory = googleProduct.Taxonomy
            };           

            return View("~/Plugins/Feed.GoogleShopping/Views/Edit.cshtml", model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Edit(GoogleProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var googleProduct = _googleService.GetByProductId(model.ProductId);
            if (googleProduct != null)
            {
                googleProduct.Taxonomy = model.GoogleCategory;
                googleProduct.Gender = model.Gender;
                googleProduct.AgeGroup = model.AgeGroup;
                googleProduct.Color = model.Color;
                googleProduct.Size = model.GoogleSize;
                googleProduct.CustomGoods = model.CustomGoods;
                _googleService.UpdateGoogleProductRecord(googleProduct);
            }
            else
            {
                //insert
                googleProduct = new GoogleProductRecord
                {
                    ProductId = model.ProductId,
                    Taxonomy = model.GoogleCategory,
                    Gender = model.Gender,
                    AgeGroup = model.AgeGroup,
                    Color = model.Color,
                    Size = model.GoogleSize,
                    CustomGoods = model.CustomGoods
                };
                _googleService.InsertGoogleProductRecord(googleProduct);
            }
            
            ViewBag.RefreshPage = true;

            return View("~/Plugins/Feed.GoogleShopping/Views/Edit.cshtml", model);
        }

        #endregion
    }
}