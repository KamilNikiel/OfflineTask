using NotificationService.Domain.Enums;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; private init; }

        public Recipient Recipient { get; private init; }

        public MessageContent Content { get; private init; }

        public ChannelType Channel { get; private init; }

        public int RetryCount { get; private set; }

        public Notification(Recipient recipient, MessageContent content, ChannelType channel)
        {
            Id = Guid.NewGuid();
            Recipient = recipient;
            Content = content;
            Channel = channel;
            RetryCount = 0;
        }

        public RetryDecision PrepareRetry(RetryPolicy policy)
        {
            if (RetryCount >= policy.MaxRetries)
                return RetryDecision.Fail();

            var delay = TimeSpan.FromMinutes(
                Math.Pow(2, RetryCount) * policy.BaseDelayMinutes);
            RetryCount++;

            return RetryDecision.Retry(delay);
        }
    }
}
