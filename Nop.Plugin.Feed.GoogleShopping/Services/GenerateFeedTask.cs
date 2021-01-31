using Nop.Core;
using Nop.Core.Domain.Stores;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Services.Stores;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;

namespace Nop.Plugin.Feed.GoogleShopping.Services
{
    public class GenerateFeedTask : IScheduleTask
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPluginService _pluginService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly ILogger _logger;

        #endregion Fields

        #region Ctor

        public GenerateFeedTask(ILocalizationService localizationService,
            INotificationService notificationService,
            IPluginService pluginService,
            IStoreContext storeContext,
            IStoreService storeService, ILogger logger)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _pluginService = pluginService;
            _storeContext = storeContext;
            _storeService = storeService;
            _logger = logger;
        }

        #endregion Ctor

        #region Methods

        public void Execute()
        {
            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;

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
        }

        #endregion Methods
    }
}