using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Feed.GoogleShopping.Models
{
    public record GoogleProductModel : BaseNopEntityModel
    {
        #region Ctor

        public GoogleProductModel()
        {
            GoogleProductListSearchModel = new GoogleProductSearchModel();
        }

        #endregion

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

        public GoogleProductSearchModel GoogleProductListSearchModel { get; set; }
    }
}
