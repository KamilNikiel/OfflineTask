using NotificationService.Domain.Enums;

namespace NotificationService.Application.Configuration
{
    public class ProviderConfig
    {
        public string Name { get; init; } = string.Empty;

        public ChannelType Channel { get; init; }

        public bool IsEnabled { get; init; }

        public int Priority { get; init; }
    }
}
