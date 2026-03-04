namespace NotificationService.Domain.ValueObjects
{
    public record RetryPolicy(int MaxRetries, double BaseDelayMinutes);
}
