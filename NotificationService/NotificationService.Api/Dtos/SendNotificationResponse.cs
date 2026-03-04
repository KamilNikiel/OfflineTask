namespace NotificationService.Api.Dtos
{
    public class SendNotificationResponse
    {
        public Guid NotificationId { get; private init; }

        public ResponseStatus ResponseStatus { get; private init; }

        public SendNotificationResponse(Guid notificationId, ResponseStatus responseStatus)
        {
            NotificationId = notificationId;
            ResponseStatus = responseStatus;
        }
    }
}