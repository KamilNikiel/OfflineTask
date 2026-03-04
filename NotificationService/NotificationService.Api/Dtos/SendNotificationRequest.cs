namespace NotificationService.Api.Dtos
{
    public class SendNotificationRequest
    {
        public string UserId { get; private init; }

        public string ContactInfo { get; private init; }

        public string Subject { get; private init; }

        public string Body { get; private init; }

        public ChannelType Channel { get; private init; }

        public SendNotificationRequest(string userId, string contactInfo, string subject, string body, ChannelType channel)
        {
            UserId = userId;
            ContactInfo = contactInfo;
            Subject = subject;
            Body = body;
            Channel = channel;
        }
    }
}