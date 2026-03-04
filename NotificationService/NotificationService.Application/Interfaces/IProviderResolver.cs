using NotificationService.Domain.Enums;

namespace NotificationService.Application.Interfaces
{
    public interface IProviderResolver
    {
        IEnumerable<INotificationProvider> GetActiveProviders(ChannelType channelType);
    }
}
