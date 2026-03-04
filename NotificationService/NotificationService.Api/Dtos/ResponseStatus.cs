using System.Text.Json.Serialization;

namespace NotificationService.Api.Dtos
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResponseStatus
    {
        Sent,
        QueuedForRetry,
        Failed
    }
}