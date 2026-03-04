using NotificationService.Application.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Interfaces
{
    public interface INotificationProvider
    {
        string Name { get; }

        ChannelType SupportedChannel { get; }

        Task<ProviderResult> SendAsync(Notification notification, CancellationToken cancellationToken);
    }
}
