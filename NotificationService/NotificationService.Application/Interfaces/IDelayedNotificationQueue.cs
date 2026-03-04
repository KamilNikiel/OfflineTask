using NotificationService.Domain.Entities;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Application.Interfaces
{
    public interface IDelayedNotificationQueue
    {
        Task EnqueueAsync(Notification notification, TimeSpan delay, CancellationToken cancellationToken);

        bool TryRead(out DelayedMessage message);

        ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken);
    }
}
