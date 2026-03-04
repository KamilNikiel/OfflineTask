namespace NotificationService.Domain.ValueObjects
{
    public record RetryDecision(bool ShouldRetry, TimeSpan? Delay)
    {
        public static RetryDecision Fail() => new(false, null);
        public static RetryDecision Retry(TimeSpan delay) => new(true, delay);
    }
}
