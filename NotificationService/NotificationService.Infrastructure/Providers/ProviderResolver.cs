using Microsoft.Extensions.Options;
using NotificationService.Application.Configuration;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Providers
{
    public class ProviderResolver : IProviderResolver
    {
        private readonly IEnumerable<INotificationProvider> _providers;

        private readonly NotificationSettings _settings;

        public ProviderResolver(
            IEnumerable<INotificationProvider> providers,
            IOptions<NotificationSettings> options)
        {
            _providers = providers;
            _settings = options.Value;
        }

        public IEnumerable<INotificationProvider> GetActiveProviders(ChannelType channelType)
        {
            var providerConfigs = _settings.Providers
                .Where(p => p.Channel == channelType && p.IsEnabled)
                .OrderBy(p => p.Priority);

            return providerConfigs
                .Select(config => _providers.SingleOrDefault(p =>
                    p.Name == config.Name &&
                    p.SupportedChannel == config.Channel))
                .Where(p => p != null)!;
        }
    }
}