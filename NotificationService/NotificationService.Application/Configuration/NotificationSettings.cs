namespace NotificationService.Application.Configuration
{
    public class NotificationSettings
    {
        public IReadOnlyList<ProviderConfig> Providers { get; init; } = [];

        public RetryPolicySettings RetryPolicy { get; init; } = new();
    }
}
