using NotificationService.Api.Dtos;
using NotificationService.Application.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Api.Mappers
{
    public static class NotificationMapper
    {
        public static Notification MapToNotification(SendNotificationRequest request)
        {
            var recipient = new Recipient(request.UserId, request.ContactInfo);
            var content = new MessageContent(request.Subject, request.Body);

            var domainChannel = request.Channel switch
            {
                Dtos.ChannelType.Sms => Domain.Enums.ChannelType.Sms,
                Dtos.ChannelType.Email => Domain.Enums.ChannelType.Email,
                Dtos.ChannelType.Push => Domain.Enums.ChannelType.Push,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Channel))
            };

            return new Notification(recipient, content, domainChannel);
        }

        public static SendNotificationResponse MapToResponse(Notification notification, DispatchStatus status)
        {
            var responseStatus = status switch
            {
                DispatchStatus.Success => ResponseStatus.Sent,
                DispatchStatus.Delayed => ResponseStatus.QueuedForRetry,
                DispatchStatus.Failed => ResponseStatus.Failed,
                _ => ResponseStatus.Failed
            };

            return new SendNotificationResponse(notification.Id, responseStatus);
        }
    }
}