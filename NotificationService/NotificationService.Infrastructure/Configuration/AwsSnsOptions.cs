namespace NotificationService.Infrastructure.Configuration
{
    internal class AwsSnsOptions
    {
        public const string SectionName = "AwsSns";

        public string AccessKey { get; init; } = string.Empty;

        public string SecretKey { get; init; } = string.Empty;

        public string Region { get; init; } = string.Empty;

        public string SenderId { get; init; } = string.Empty;
    }
}