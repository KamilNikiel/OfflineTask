using NotificationService.Domain.Entities;

namespace NotificationService.Domain.ValueObjects
{
    public record DelayedMessage(Notification Notification, DateTime DeliverAt);
}