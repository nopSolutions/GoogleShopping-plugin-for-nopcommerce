using FluentMigrator;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Configuration;

namespace Nop.Plugin.Feed.GoogleShopping.Data;

[NopMigration("2026-11-05 12:00:00", "Feed.GoogleShopping 4.90.2. Product images must be at least 500 x 500 pixels.", MigrationProcessType.Update)]
public class GoogleShoppingMigration : MigrationBase
{
    #region Fields

    protected readonly ISettingService _settingService;
    protected readonly GoogleShoppingSettings _googleShoppingSettings;

    #endregion

    #region Ctor

    public GoogleShoppingMigration(ISettingService settingService,
        GoogleShoppingSettings googleShoppingSettings)
    {
        _settingService = settingService;
        _googleShoppingSettings = googleShoppingSettings;
    }

    #endregion

    #region Methods        

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        if (_settingService.SettingExists(_googleShoppingSettings, settings => settings.ProductPictureSize))
            _googleShoppingSettings.ProductPictureSize = 500;

        _settingService.SaveSetting(_googleShoppingSettings);
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        //add the downgrade logic if necessary 
    }

    #endregion
}
