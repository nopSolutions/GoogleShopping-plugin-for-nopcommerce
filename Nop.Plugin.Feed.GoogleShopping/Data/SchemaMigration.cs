using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Data
{
    [SkipMigrationOnUpdate]
    [NopMigration("2020/06/02 12:00:00", "Feed.GoogleShopping base schema")]
    public class SchemaMigration : AutoReversingMigration
    {
        #region Fields

        protected IMigrationManager _migrationManager;

        #endregion

        #region Ctor

        public SchemaMigration(IMigrationManager migrationManager)
        {
            _migrationManager = migrationManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            _migrationManager.BuildTable<GoogleProductRecord>(Create);
        }

        #endregion
    }
}
