using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Data
{
    [NopMigration("2020/06/02 12:00:00", "Feed.GoogleShopping base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        #region Methods

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            Create.TableFor<GoogleProductRecord>();
        }

        #endregion
    }
}
