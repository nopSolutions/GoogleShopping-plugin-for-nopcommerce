using FluentMigrator;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Feed.GoogleShopping.Data;

[NopMigration("2026-11-05 12:00:00", "Feed.GoogleShopping 5.00.2. Product images must be at least 500 x 500 pixels.", MigrationProcessType.Update)]
public class GoogleShoppingMigration : MigrationBase
{
    #region Methods        

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        if (!DataSettingsManager.IsDatabaseInstalled())
            return;

        this.SetSetting<GoogleShoppingSettings, int>(settings => settings.ProductPictureSize,
            settings => settings.ProductPictureSize = 500);
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
