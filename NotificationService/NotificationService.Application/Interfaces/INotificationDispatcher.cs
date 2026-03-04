using NotificationService.Application.Models;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces
{
    public interface INotificationDispatcher
    {
        Task<DispatchStatus> DispatchAsync(Notification notification, CancellationToken cancellationToken);
    }
}

