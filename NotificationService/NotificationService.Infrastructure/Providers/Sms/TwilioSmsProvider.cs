using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Exceptions;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Configuration;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NotificationService.Infrastructure.Providers.Sms
{
    internal class TwilioSmsProvider : INotificationProvider
    {
        private readonly ITwilioRestClient _twilioClient;
        private readonly TwilioOptions _options;
        private readonly ILogger<TwilioSmsProvider> _logger;

        public string Name => "Twilio";
        public ChannelType SupportedChannel => ChannelType.Sms;

        public TwilioSmsProvider(
            ITwilioRestClient twilioClient,
            IOptions<TwilioOptions> options,
            ILogger<TwilioSmsProvider> logger)
        {
            _twilioClient = twilioClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ProviderResult> SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            try
            {
                var messageTask = MessageResource.CreateAsync(
                    to: new PhoneNumber(notification.Recipient.ContactInfo),
                    from: new PhoneNumber(_options.FromPhoneNumber),
                    body: notification.Content.Body,
                    client: _twilioClient
                );

                var message = await messageTask.WaitAsync(cancellationToken);

                if (message.Status == MessageResource.StatusEnum.Failed ||
                    message.Status == MessageResource.StatusEnum.Undelivered)
                {
                    _logger.LogWarning("{ProviderName} returned status {Status} for Notification {NotificationId}. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                        Name, message.Status, notification.Id, message.ErrorCode, message.ErrorMessage);

                    return ProviderResult.Failure(
                        message.ErrorMessage,
                        message.ErrorCode?.ToString());
                }

                return ProviderResult.Success();
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "{ProviderName} rejected the request for Notification {NotificationId}. Code: {ErrorCode}, Message: {ErrorMessage}",
                    Name, notification.Id, ex.Code, ex.Message);

                throw new ProviderException(Name, $"The {Name} request failed due to a provider-side error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while sending Notification {NotificationId} via {ProviderName}",
                    notification.Id, Name);

                throw new ProviderException(Name, $"An unexpected error occurred while sending the notification via {Name}.", ex);
            }
        }
    }
}