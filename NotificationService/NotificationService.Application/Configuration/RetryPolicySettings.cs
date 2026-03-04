namespace NotificationService.Application.Configuration
{
    public class RetryPolicySettings
    {
        public int MaxRetries { get; init; } = 3;

        public double BaseDelayMinutes { get; init; } = 1;
    }
}
