namespace NotificationService.Infrastructure.Configuration
{
    internal class FirebaseOptions
    {
        public const string SectionName = "Firebase";

        public string ServerKey { get; init; } = string.Empty;

        public string SenderId { get; init; } = string.Empty;

        public string ApiUrl { get; init; } = string.Empty;
    }
}