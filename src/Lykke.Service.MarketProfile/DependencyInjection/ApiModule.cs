﻿using Autofac;
using AzureStorage.Blob;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.MarketProfile.Core.Domain;
using Lykke.Service.MarketProfile.Core.Services;
using Lykke.Service.MarketProfile.Repositories;
using Lykke.Service.MarketProfile.Services;
using Lykke.Service.MarketProfile.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.MarketProfile.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ApiModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<IAssetPairsRepository>(
                x => new AssetPairRepository(AzureBlobStorage.Create(_appSettings.ConnectionString(o => o.MarketProfileService.Db.CachePersistenceConnectionString)),
                        "assetpairs",
                        "Instance"));

            builder.RegisterType<AssetPairsCacheService>().As<IAssetPairsCacheService>();

            var settings = _appSettings.CurrentValue.MarketProfileService;

            var rabbitMqSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.QuoteFeedRabbitSettings.ConnectionString,
                QueueName = $"{settings.QuoteFeedRabbitSettings.ExchangeName}.marketprofileservice",
                ExchangeName = settings.QuoteFeedRabbitSettings.ExchangeName
            };

            builder.RegisterType<MarketProfileManager>()
                .WithParameter(TypedParameter.From(rabbitMqSettings))
                .WithParameter(TypedParameter.From(settings.CacheSettings.PersistPeriod))
                .As<IMarketProfileManager>()
                .As<IStartable>()
                .SingleInstance();
        }
    }
}