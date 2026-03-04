using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Exceptions;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Configuration;
using System.Net.Http.Json;
using Twilio.Exceptions;

namespace NotificationService.Infrastructure.Providers.Push
{
    internal class FirebasePushProvider : INotificationProvider
    {
        private const string AuthorizationHeader = "Authorization";

        private readonly HttpClient _httpClient;
        private readonly FirebaseOptions _options;
        private readonly ILogger<FirebasePushProvider> _logger;

        public string Name => "Firebase";
        public ChannelType SupportedChannel => ChannelType.Push;

        public FirebasePushProvider(
            HttpClient httpClient,
            IOptions<FirebaseOptions> options,
            ILogger<FirebasePushProvider> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ProviderResult> SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            try
            {
                var payload = new
                {
                    to = notification.Recipient.ContactInfo,
                    notification = new
                    {
                        title = notification.Content.Subject,
                        body = notification.Content.Body
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);

                request.Headers.TryAddWithoutValidation(
                    AuthorizationHeader,
                    $"key={_options.ServerKey}");

                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return ProviderResult.Success();
                }

                _logger.LogWarning("{ProviderName} returned non-success status {StatusCode} for Notification {NotificationId}",
                    Name, response.StatusCode, notification.Id);

                return ProviderResult.Failure(
                    null,
                    response.StatusCode.ToString());
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "{ProviderName} rejected the request for Notification {NotificationId}. ApiErrorCode: {ApiErrorCode}, Message: {ErrorMessage}",
                    Name, notification.Id, ex.Code, ex.Message);

                throw new ProviderException(Name, $"The {Name} request failed due to a provider-side error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while sending Notification {NotificationId} via {ProviderName}", notification.Id, Name);

                throw new ProviderException(Name, $"An unexpected error occurred while sending the notification via {Name}.", ex);
            }
        }
    }
}