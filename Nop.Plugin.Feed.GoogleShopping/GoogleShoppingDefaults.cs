namespace Nop.Plugin.Feed.GoogleShopping;

/// <summary>
/// Represents plugin constants
/// </summary>
public static class GoogleShoppingDefaults
{
    /// <summary>
    /// Gets the configuration route name
    /// </summary>
    public static string ConfigurationRouteName => "Plugin.Feed.GoogleShopping.Configure";

    /// <summary>
    /// Gets a name of the feed file
    /// </summary>
    public static string PathToFeedFile => "files/exportimport/{0}-{1}";

    /// <summary>
    /// Generic attribute name to hide general settings block on the plugin configuration page
    /// </summary>
    public static string HideGeneralBlock => "GoogleShoppingPage.HideGeneralBlock";

    /// <summary>
    /// Generic attribute name to hide product settings block on the plugin configuration page
    /// </summary>
    public static string HideProductSettingsBlock => "GoogleShoppingPage.HideProductSettingsBlock";
}
