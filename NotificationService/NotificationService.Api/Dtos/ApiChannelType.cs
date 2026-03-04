using System.Text.Json.Serialization;

namespace NotificationService.Api.Dtos
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChannelType
    {
        Sms,
        Email,
        Push
    }
}