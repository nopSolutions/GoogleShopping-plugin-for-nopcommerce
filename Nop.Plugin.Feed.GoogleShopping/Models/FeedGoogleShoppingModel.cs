using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Feed.GoogleShopping.Models
{
    public class FeedGoogleShoppingModel
    {
        public FeedGoogleShoppingModel()
        {
            AvailableCurrencies = new List<SelectListItem>();
            AvailableGoogleCategories = new List<SelectListItem>();
            GeneratedFiles = new List<GeneratedFileModel>();
        }

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.ProductPictureSize")]
        public int ProductPictureSize { get; set; }
        public bool ProductPictureSize_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Currency")]
        public int CurrencyId { get; set; }
        public IList<SelectListItem> AvailableCurrencies { get; set; }
        public bool CurrencyId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.DefaultGoogleCategory")]
        public string DefaultGoogleCategory { get; set; }
        public IList<SelectListItem> AvailableGoogleCategories { get; set; }
        public bool DefaultGoogleCategory_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.PassShippingInfoWeight")]
        public bool PassShippingInfoWeight { get; set; }
        public bool PassShippingInfoWeight_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.PassShippingInfoDimensions")]
        public bool PassShippingInfoDimensions { get; set; }
        public bool PassShippingInfoDimensions_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.PricesConsiderPromotions")]
        public bool PricesConsiderPromotions { get; set; }
        public bool PricesConsiderPromotions_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.StaticFilePath")]
        public IList<GeneratedFileModel> GeneratedFiles { get; set; }
        
        public class GeneratedFileModel : BaseNopModel
        {
            public string StoreName { get; set; }
            public string FileUrl { get; set; }
        }

        public class GoogleProductModel : BaseNopModel
        {
            public int ProductId { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.ProductName")]
            public string ProductName { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.GoogleCategory")]
            public string GoogleCategory { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.Gender")]
            public string Gender { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.AgeGroup")]
            public string AgeGroup { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.Color")]
            public string Color { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.Size")]
            public string GoogleSize { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.CustomGoods")]
            public bool CustomGoods { get; set; }
        }
    }
}