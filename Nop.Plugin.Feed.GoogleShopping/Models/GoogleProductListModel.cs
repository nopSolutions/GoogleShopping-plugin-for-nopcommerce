using Nop.Web.Framework.Models;

namespace Nop.Plugin.Feed.GoogleShopping.Models
{
    /// <summary>
    /// Represents Google product list Model
    /// </summary>
    public record GoogleProductListModel : BasePagedListModel<GoogleProductModel>
    {
    }
}
