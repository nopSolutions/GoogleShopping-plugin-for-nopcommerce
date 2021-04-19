using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Stores;
using Nop.Core.Infrastructure;
using Nop.Plugin.Feed.GoogleShopping.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Plugins;
using Nop.Services.Seo;
using Nop.Services.Tax;

namespace Nop.Plugin.Feed.GoogleShopping
{
    public class GoogleShoppingService : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly GoogleShoppingSettings _googleShoppingSettings;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ICategoryService _categoryService;
        private readonly ICurrencyService _currencyService;
        private readonly IGoogleService _googleService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IMeasureService _measureService;
        private readonly INopFileProvider _nopFileProvider;
        private readonly IPictureService _pictureService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWebHelper _webHelper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IWorkContext _workContext;
        private readonly MeasureSettings _measureSettings;

        #endregion

        #region Ctor

        public GoogleShoppingService(
            CurrencySettings currencySettings,
            GoogleShoppingSettings googleShoppingSettings,
            IActionContextAccessor actionContextAccessor,
            ICategoryService categoryService,
            ICurrencyService currencyService,
            IGoogleService googleService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IMeasureService measureService,
            INopFileProvider nopFileProvider,
            IPictureService pictureService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            ISettingService settingService,
            IStoreContext storeContext,
            ITaxService taxService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IWebHelper webHelper,
            IWebHostEnvironment webHostEnvironment,
            IWorkContext workContext,
            MeasureSettings measureSettings
            )
        {
            _actionContextAccessor = actionContextAccessor;
            _categoryService = categoryService;
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _googleService = googleService;
            _googleShoppingSettings = googleShoppingSettings;
            _languageService = languageService;
            _localizationService = localizationService;
            _manufacturerService = manufacturerService;
            _measureService = measureService;
            _measureSettings = measureSettings;
            _nopFileProvider = nopFileProvider;
            _pictureService = pictureService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _settingService = settingService;
            _storeContext = storeContext;
            _taxService = taxService;
            _urlHelperFactory = urlHelperFactory;
            _urlRecordService = urlRecordService;
            _webHelper = webHelper;
            _webHostEnvironment = webHostEnvironment;
            _workContext = workContext;
        }

        #endregion

        #region Utilities
        
        /// <summary>
        /// Removes invalid characters
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="isHtmlEncoded">A value indicating whether input string is HTML encoded</param>
        /// <returns>Valid string</returns>
        protected virtual string StripInvalidChars(string input, bool isHtmlEncoded)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            //Microsoft uses a proprietary encoding (called CP-1252) for the bullet symbol and some other special characters, 
            //whereas most websites and data feeds use UTF-8. When you copy-paste from a Microsoft product into a website, 
            //some characters may appear as junk. Our system generates data feeds in the UTF-8 character encoding, 
            //which many shopping engines now require.

            //http://www.atensoftware.com/p90.php?q=182

            if (isHtmlEncoded)
                input = WebUtility.HtmlDecode(input);

            input = input.Replace("¼", "");
            input = input.Replace("½", "");
            input = input.Replace("¾", "");
            //input = input.Replace("•", "");
            //input = input.Replace("”", "");
            //input = input.Replace("“", "");
            //input = input.Replace("’", "");
            //input = input.Replace("‘", "");
            //input = input.Replace("™", "");
            //input = input.Replace("®", "");
            //input = input.Replace("°", "");
            
            if (isHtmlEncoded)
                input = WebUtility.HtmlEncode(input);

            return input;
        }

        /// <summary>
        /// Get used currency
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Currency
        /// </returns>
        protected virtual async Task<Currency> GetUsedCurrencyAsync()
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(_googleShoppingSettings.CurrencyId);
            if (currency == null || !currency.Published)
                currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
            return currency;
        }

        /// <summary>
        /// Get UrlHelper
        /// </summary>
        /// <returns>UrlHelper</returns>
        protected virtual IUrlHelper GetUrlHelper()
        {
            return _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
        }

        /// <summary>
        /// Get HTTP protocol
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Protocol name
        /// </returns>
        protected virtual async Task<string> GetHttpProtocolAsync()
        {
            return (await _storeContext.GetCurrentStoreAsync()).SslEnabled ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/FeedGoogleShopping/Configure";
        }

        /// <summary>
        /// Generate a feed
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="store">Store</param>
        /// <returns>Generated feed</returns>
        public async Task GenerateFeedAsync(Stream stream, Store store)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (store == null)
                throw new ArgumentNullException(nameof(store));

            const string googleBaseNamespace = "http://base.google.com/ns/1.0";

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8
            };

            var googleShoppingSettings = await _settingService.LoadSettingAsync<GoogleShoppingSettings>(store.Id);

            //language
            var languageId = 0;
            var languages = await _languageService.GetAllLanguagesAsync(storeId: store.Id);
            //if we have only one language, let's use it
            if (languages.Count == 1)
            {
                //let's use the first one
                var language = languages.FirstOrDefault();
                languageId = language != null ? language.Id : 0;
            }
            //otherwise, use the current one
            if (languageId == 0)
                languageId = (await _workContext.GetWorkingLanguageAsync()).Id;

            //we load all Google products here using one SQL request (performance optimization)
            var allGoogleProducts = await _googleService.GetAllAsync();

            using var writer = XmlWriter.Create(stream, settings);
            //Generate feed according to the following specs: http://www.google.com/support/merchants/bin/answer.py?answer=188494&expand=GB
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "g", null, googleBaseNamespace);
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", "Google Base feed");
            writer.WriteElementString("link", "http://base.google.com/base/");
            writer.WriteElementString("description", "Information about products");

            var products1 = await _productService.SearchProductsAsync(storeId: store.Id, visibleIndividuallyOnly: true);
            foreach (var product1 in products1)
            {
                var productsToProcess = new List<Product>();
                switch (product1.ProductType)
                {
                    case ProductType.SimpleProduct:
                        {
                            //simple product doesn't have child products
                            productsToProcess.Add(product1);
                        }
                        break;
                    case ProductType.GroupedProduct:
                        {
                            //grouped products could have several child products
                            var associatedProducts = await _productService.GetAssociatedProductsAsync(product1.Id, store.Id);
                            productsToProcess.AddRange(associatedProducts);
                        }
                        break;
                    default:
                        continue;
                }
                foreach (var product in productsToProcess)
                {
                    writer.WriteStartElement("item");

                    #region Basic Product Information

                    //id [id]- An identifier of the item
                    writer.WriteElementString("g", "id", googleBaseNamespace, product.Id.ToString());

                    //title [title] - Title of the item
                    writer.WriteStartElement("title");
                    var title = await _localizationService.GetLocalizedAsync(product, x => x.Name, languageId);
                    //title should be not longer than 70 characters
                    if (title.Length > 70)
                        title = title.Substring(0, 70);
                    writer.WriteCData(title);
                    writer.WriteEndElement(); // title

                    //description [description] - Description of the item
                    writer.WriteStartElement("description");
                    var description = await _localizationService.GetLocalizedAsync(product, x => x.FullDescription, languageId);
                    if (string.IsNullOrEmpty(description))
                        description = await _localizationService.GetLocalizedAsync(product, x => x.ShortDescription, languageId);
                    if (string.IsNullOrEmpty(description))
                        description = await _localizationService.GetLocalizedAsync(product, x => x.Name, languageId); //description is required
                                                                                                           //resolving character encoding issues in your data feed
                    description = StripInvalidChars(description, true);
                    writer.WriteCData(description);
                    writer.WriteEndElement(); // description

                    //google product category [google_product_category] - Google's category of the item
                    //the category of the product according to Google’s product taxonomy. http://www.google.com/support/merchants/bin/answer.py?answer=160081
                    var googleProductCategory = "";
                    //var googleProduct = _googleService.GetByProductId(product.Id);
                    var googleProduct = allGoogleProducts.FirstOrDefault(x => x.ProductId == product.Id);
                    if (googleProduct != null)
                        googleProductCategory = googleProduct.Taxonomy;
                    if (string.IsNullOrEmpty(googleProductCategory))
                        googleProductCategory = googleShoppingSettings.DefaultGoogleCategory;
                    if (string.IsNullOrEmpty(googleProductCategory))
                        throw new NopException("Default Google category is not set");
                    writer.WriteStartElement("g", "google_product_category", googleBaseNamespace);
                    writer.WriteCData(googleProductCategory);
                    writer.WriteFullEndElement(); // g:google_product_category

                    //product type [product_type] - Your category of the item
                    var defaultProductCategory = (await _categoryService
                        .GetProductCategoriesByProductIdAsync(product.Id))
                        .FirstOrDefault();
                    if (defaultProductCategory != null)
                    {
                        //TODO localize categories
                        var category = await _categoryService.GetFormattedBreadCrumbAsync(
                            category: await _categoryService.GetCategoryByIdAsync(defaultProductCategory.CategoryId),
                            separator: ">",
                            languageId: languageId);
                        if (!string.IsNullOrEmpty(category))
                        {
                            writer.WriteStartElement("g", "product_type", googleBaseNamespace);
                            writer.WriteCData(category);
                            writer.WriteFullEndElement(); // g:product_type
                        }
                    }

                    //link [link] - URL directly linking to your item's page on your website
                    var productUrl = GetUrlHelper().RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) }, await GetHttpProtocolAsync());
                    writer.WriteElementString("link", productUrl);

                    //image link [image_link] - URL of an image of the item
                    //additional images [additional_image_link]
                    //up to 10 pictures
                    const int maximumPictures = 10;
                    var storeLocation = _webHelper.GetStoreLocation();
                    var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id, maximumPictures);
                    for (var i = 0; i < pictures.Count; i++)
                    {
                        var picture = pictures[i];
                        var imageUrl = await _pictureService.GetPictureUrlAsync(picture.Id,
                            googleShoppingSettings.ProductPictureSize,
                            storeLocation: storeLocation);

                        if (i == 0)
                        {
                            //default image
                            writer.WriteElementString("g", "image_link", googleBaseNamespace, imageUrl);
                        }
                        else
                        {
                            //additional image
                            writer.WriteElementString("g", "additional_image_link", googleBaseNamespace, imageUrl);
                        }
                    }
                    if (!pictures.Any())
                    {
                        //no picture? submit a default one
                        var imageUrl = await _pictureService.GetDefaultPictureUrlAsync(googleShoppingSettings.ProductPictureSize, storeLocation: storeLocation);
                        writer.WriteElementString("g", "image_link", googleBaseNamespace, imageUrl);
                    }

                    //condition [condition] - Condition or state of the item
                    writer.WriteElementString("g", "condition", googleBaseNamespace, "new");

                    writer.WriteElementString("g", "expiration_date", googleBaseNamespace, DateTime.Now.AddDays(googleShoppingSettings.ExpirationNumberOfDays).ToString("yyyy-MM-dd"));

                    #endregion

                    #region Availability & Price

                    //availability [availability] - Availability status of the item
                    var availability = "in stock"; //in stock by default
                    if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock
                        && product.BackorderMode == BackorderMode.NoBackorders
                        && await _productService.GetTotalStockQuantityAsync(product) <= 0)
                    {
                        availability = "out of stock";
                    }
                    //uncomment th code below in order to support "preorder" value for "availability"
                    //if (product.AvailableForPreOrder &&
                    //    (!product.PreOrderAvailabilityStartDateTimeUtc.HasValue || 
                    //    product.PreOrderAvailabilityStartDateTimeUtc.Value >= DateTime.UtcNow))
                    //{
                    //    availability = "preorder";
                    //}
                    writer.WriteElementString("g", "availability", googleBaseNamespace, availability);

                    //price [price] - Price of the item
                    var currency = await GetUsedCurrencyAsync();
                    decimal finalPriceBase;
                    if (googleShoppingSettings.PricesConsiderPromotions)
                    {
                        var currentCustomer = await _workContext.GetCurrentCustomerAsync();
                        var minPossiblePrice = (await _priceCalculationService.GetFinalPriceAsync(product, currentCustomer)).finalPrice;

                        if (product.HasTierPrices)
                        {
                            //calculate price for the maximum quantity if we have tier prices, and choose minimal
                            minPossiblePrice = Math.Min(minPossiblePrice,
                                (await _priceCalculationService.GetFinalPriceAsync(product, currentCustomer, quantity: int.MaxValue)).finalPrice);
                        }

                        finalPriceBase = (await _taxService.GetProductPriceAsync(product, minPossiblePrice)).price;
                    }
                    else
                    {
                        finalPriceBase = product.Price;
                    }
                    var price = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(finalPriceBase, currency);
                    //round price now so it matches the product details page
                    price = await _priceCalculationService.RoundPriceAsync(price);

                    writer.WriteElementString("g", "price", googleBaseNamespace,
                                              price.ToString(new CultureInfo("en-US", false).NumberFormat) + " " +
                                              currency.CurrencyCode);

                    #endregion

                    #region Unique Product Identifiers

                    /* Unique product identifiers such as UPC, EAN, JAN or ISBN allow us to show your listing on the appropriate product page. If you don't provide the required unique product identifiers, your store may not appear on product pages, and all your items may be removed from Product Search.
                     * We require unique product identifiers for all products - except for custom made goods. For apparel, you must submit the 'brand' attribute. For media (such as books, movies, music and video games), you must submit the 'gtin' attribute. In all cases, we recommend you submit all three attributes.
                     * You need to submit at least two attributes of 'brand', 'gtin' and 'mpn', but we recommend that you submit all three if available. For media (such as books, movies, music and video games), you must submit the 'gtin' attribute, but we recommend that you include 'brand' and 'mpn' if available.
                    */

                    //GTIN [gtin] - GTIN
                    var gtin = product.Gtin;
                    if (!string.IsNullOrEmpty(gtin))
                    {
                        writer.WriteStartElement("g", "gtin", googleBaseNamespace);
                        writer.WriteCData(gtin);
                        writer.WriteFullEndElement(); // g:gtin
                    }

                    //brand [brand] - Brand of the item
                    var defaultManufacturer = (await _manufacturerService.GetProductManufacturersByProductIdAsync(product.Id)).FirstOrDefault();
                    if (defaultManufacturer != null)
                    {
                        writer.WriteStartElement("g", "brand", googleBaseNamespace);
                        writer.WriteCData((await _manufacturerService.GetManufacturerByIdAsync(defaultManufacturer.ManufacturerId))?.Name);
                        writer.WriteFullEndElement(); // g:brand
                    }

                    //mpn [mpn] - Manufacturer Part Number (MPN) of the item
                    var mpn = product.ManufacturerPartNumber;
                    if (!string.IsNullOrEmpty(mpn))
                    {
                        writer.WriteStartElement("g", "mpn", googleBaseNamespace);
                        writer.WriteCData(mpn);
                        writer.WriteFullEndElement(); // g:mpn
                    }

                    //identifier exists [identifier_exists] - Submit custom goods
                    if (googleProduct != null && googleProduct.CustomGoods)
                    {
                        writer.WriteElementString("g", "identifier_exists", googleBaseNamespace, "FALSE");
                    }

                    #endregion

                    #region Apparel Products

                    /* Apparel includes all products that fall under 'Apparel & Accessories' (including all sub-categories)
                     * in Google’s product taxonomy.
                    */

                    //gender [gender] - Gender of the item
                    if (googleProduct != null && !string.IsNullOrEmpty(googleProduct.Gender))
                    {
                        writer.WriteStartElement("g", "gender", googleBaseNamespace);
                        writer.WriteCData(googleProduct.Gender);
                        writer.WriteFullEndElement(); // g:gender
                    }

                    //age group [age_group] - Target age group of the item
                    if (googleProduct != null && !string.IsNullOrEmpty(googleProduct.AgeGroup))
                    {
                        writer.WriteStartElement("g", "age_group", googleBaseNamespace);
                        writer.WriteCData(googleProduct.AgeGroup);
                        writer.WriteFullEndElement(); // g:age_group
                    }

                    //color [color] - Color of the item
                    if (googleProduct != null && !string.IsNullOrEmpty(googleProduct.Color))
                    {
                        writer.WriteStartElement("g", "color", googleBaseNamespace);
                        writer.WriteCData(googleProduct.Color);
                        writer.WriteFullEndElement(); // g:color
                    }

                    //size [size] - Size of the item
                    if (googleProduct != null && !string.IsNullOrEmpty(googleProduct.Size))
                    {
                        writer.WriteStartElement("g", "size", googleBaseNamespace);
                        writer.WriteCData(googleProduct.Size);
                        writer.WriteFullEndElement(); // g:size
                    }

                    #endregion

                    #region Tax & Shipping

                    //tax [tax]
                    //The tax attribute is an item-level override for merchant-level tax settings as defined in your Google Merchant Center account. This attribute is only accepted in the US, if your feed targets a country outside of the US, please do not use this attribute.
                    //IMPORTANT NOTE: Set tax in your Google Merchant Center account settings

                    //IMPORTANT NOTE: Set shipping in your Google Merchant Center account settings

                    //shipping weight [shipping_weight] - Weight of the item for shipping
                    //We accept only the following units of weight: lb, oz, g, kg.
                    if (googleShoppingSettings.PassShippingInfoWeight)
                    {
                        var shippingWeight = product.Weight;
                        var weightSystemName = (await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId)).SystemKeyword;
                        var weightName = weightSystemName switch
                        {
                            "ounce" => "oz",
                            "lb" => "lb",
                            "grams" => "g",
                            "kg" => "kg",
                            _ => throw new Exception("Not supported weight. Google accepts the following units: lb, oz, g, kg."),
                        };
                        writer.WriteElementString("g", "shipping_weight", googleBaseNamespace, string.Format(CultureInfo.InvariantCulture, "{0} {1}", shippingWeight.ToString(new CultureInfo("en-US", false).NumberFormat), weightName));
                    }

                    //shipping length [shipping_length] - Length of the item for shipping
                    //shipping width [shipping_width] - Width of the item for shipping
                    //shipping height [shipping_height] - Height of the item for shipping
                    //We accept only the following units of length: in, cm
                    if (googleShoppingSettings.PassShippingInfoDimensions)
                    {
                        var length = product.Length;
                        var width = product.Width;
                        var height = product.Height;
                        var dimensionSystemName = (await _measureService.GetMeasureDimensionByIdAsync(_measureSettings.BaseDimensionId)).SystemKeyword;
                        var dimensionName = dimensionSystemName switch
                        {
                            "inches" => "in",
                            //TODO support other dimensions (convert to cm)
                            _ => throw new Exception("Not supported dimension. Google accepts the following units: in, cm."),//unknown dimension 
                        };
                        writer.WriteElementString("g", "shipping_length", googleBaseNamespace, string.Format(CultureInfo.InvariantCulture, "{0} {1}", length.ToString(new CultureInfo("en-US", false).NumberFormat), dimensionName));
                        writer.WriteElementString("g", "shipping_width", googleBaseNamespace, string.Format(CultureInfo.InvariantCulture, "{0} {1}", width.ToString(new CultureInfo("en-US", false).NumberFormat), dimensionName));
                        writer.WriteElementString("g", "shipping_height", googleBaseNamespace, string.Format(CultureInfo.InvariantCulture, "{0} {1}", height.ToString(new CultureInfo("en-US", false).NumberFormat), dimensionName));
                    }

                    #endregion

                    writer.WriteEndElement(); // item
                }
            }

            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss
            writer.WriteEndDocument();
        }

        // <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //settings
            var settings = new GoogleShoppingSettings
            {
                PricesConsiderPromotions = false,
                ProductPictureSize = 125,
                PassShippingInfoWeight = false,
                PassShippingInfoDimensions = false,
                StaticFileName = $"googleshopping_{CommonHelper.GenerateRandomDigitCode(10)}.xml",
                ExpirationNumberOfDays = 28
            };
            await _settingService.SaveSettingAsync(settings);

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Feed.GoogleShopping.Store"] = "Store",
                ["Plugins.Feed.GoogleShopping.Store.Hint"] = "Select the store that will be used to generate the feed.",
                ["Plugins.Feed.GoogleShopping.Currency"] = "Currency",
                ["Plugins.Feed.GoogleShopping.Currency.Hint"] = "Select the default currency that will be used to generate the feed.",
                ["Plugins.Feed.GoogleShopping.DefaultGoogleCategory"] = "Default Google category",
                ["Plugins.Feed.GoogleShopping.DefaultGoogleCategory.Hint"] = "The default Google category to use if one is not specified.",
                ["Plugins.Feed.GoogleShopping.ExceptionLoadPlugin"] = "Cannot load the plugin",
                ["Plugins.Feed.GoogleShopping.General"] = "General",
                ["Plugins.Feed.GoogleShopping.GeneralInstructions"] = "<p><ul><li>At least two unique product identifiers are required. So each of your product should have manufacturer (brand) and MPN (manufacturer part number) specified</li><li>Specify default tax values in your Google Merchant Center account settings</li><li>Specify default shipping values in your Google Merchant Center account settings</li><li>In order to get more info about required fields look at the following article <a href=\"http://www.google.com/support/merchants/bin/answer.py?answer=188494\" target=\"_blank\">http://www.google.com/support/merchants/bin/answer.py?answer=188494</a></li></ul></p>",
                ["Plugins.Feed.GoogleShopping.Generate"] = "Generate feed",
                ["Plugins.Feed.GoogleShopping.Override"] = "Override product settings",
                ["Plugins.Feed.GoogleShopping.OverrideInstructions"] = "<p>You can download the list of allowed Google product category attributes <a href=\"http://www.google.com/support/merchants/bin/answer.py?answer=160081\" target=\"_blank\">here</a></p>",
                ["Plugins.Feed.GoogleShopping.PassShippingInfoWeight"] = "Pass shipping info (weight)",
                ["Plugins.Feed.GoogleShopping.PassShippingInfoWeight.Hint"] = "Check if you want to include shipping information (weight) in generated XML file.",
                ["Plugins.Feed.GoogleShopping.PassShippingInfoDimensions"] = "Pass shipping info (dimensions)",
                ["Plugins.Feed.GoogleShopping.PassShippingInfoDimensions.Hint"] = "Check if you want to include shipping information (dimensions) in generated XML file.",
                ["Plugins.Feed.GoogleShopping.PricesConsiderPromotions"] = "Prices consider promotions",
                ["Plugins.Feed.GoogleShopping.PricesConsiderPromotions.Hint"] = "Check if you want prices to be calculated with promotions (tier prices] = discounts] = special prices] = tax] = etc). But please note that it can significantly reduce time required to generate the feed file.",
                ["Plugins.Feed.GoogleShopping.ProductPictureSize"] = "Product thumbnail image size",
                ["Plugins.Feed.GoogleShopping.ProductPictureSize.Hint"] = "The default size (pixels) for product thumbnail images.",
                ["Plugins.Feed.GoogleShopping.Products.ProductName"] = "Product",
                ["Plugins.Feed.GoogleShopping.Products.ProductName.Hint"] = "Product Name",
                ["Plugins.Feed.GoogleShopping.Products.GoogleCategory"] = "Google Category",
                ["Plugins.Feed.GoogleShopping.Products.GoogleCategory.Hint"] = "Product category according to the Google product taxonomy.",
                ["Plugins.Feed.GoogleShopping.Products.Gender"] = "Gender",
                ["Plugins.Feed.GoogleShopping.Products.Gender.Hint"] = "Gender of the people for whom the product is intended.",
                ["Plugins.Feed.GoogleShopping.Products.AgeGroup"] = "Age group",
                ["Plugins.Feed.GoogleShopping.Products.AgeGroup.Hint"] = "Age category of people for whom the goods are intended.",
                ["Plugins.Feed.GoogleShopping.Products.Color"] = "Color",
                ["Plugins.Feed.GoogleShopping.Products.Color.Hint"] = "Product color.",
                ["Plugins.Feed.GoogleShopping.Products.Size"] = "Size",
                ["Plugins.Feed.GoogleShopping.Products.Size.Hint"] = "Product size.",
                ["Plugins.Feed.GoogleShopping.Products.CustomGoods"] = "Custom goods",
                ["Plugins.Feed.GoogleShopping.Products.CustomGoods.Hint"] = "Custom goods (no identifier exists).",
                ["Plugins.Feed.GoogleShopping.SuccessResult"] = "Google Shopping feed has been successfully generated.",
                ["Plugins.Feed.GoogleShopping.StaticFilePath"] = "Generated file path (static)",
                ["Plugins.Feed.GoogleShopping.StaticFilePath.Hint"] = "A file path of the generated file. It's static for your store and can be shared with the Google Shopping service."
            });
            
            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<GoogleShoppingSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Feed.GoogleShopping");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Generate a static feed file
        /// </summary>
        /// <param name="store">Store</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task GenerateStaticFileAsync(Store store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            
            var filePath = _nopFileProvider.Combine(_webHostEnvironment.WebRootPath, "files", "exportimport", store.Id + "-" + _googleShoppingSettings.StaticFileName);
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            await GenerateFeedAsync(fs, store);
        }

        #endregion
    }
}