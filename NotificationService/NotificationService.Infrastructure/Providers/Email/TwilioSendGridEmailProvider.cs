using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Exceptions;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio.Exceptions;

namespace NotificationService.Infrastructure.Providers.Email
{
    public class TwilioSendGridEmailProvider : INotificationProvider
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly SendGridOptions _options;
        private readonly ILogger<TwilioSendGridEmailProvider> _logger;

        public string Name => "SendGrid";
        public ChannelType SupportedChannel => ChannelType.Email;

        public TwilioSendGridEmailProvider(
            ISendGridClient sendGridClient,
            IOptions<SendGridOptions> options,
            ILogger<TwilioSendGridEmailProvider> logger)
        {
            _sendGridClient = sendGridClient;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<ProviderResult> SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            try
            {
                var from = new EmailAddress(_options.FromEmail, _options.FromName);
                var to = new EmailAddress(notification.Recipient.ContactInfo);
                var subject = notification.Content.Subject;
                var plainTextContent = notification.Content.Body;
                var htmlContent = $"<p>{notification.Content.Body}</p>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return ProviderResult.Success();
                }

                _logger.LogWarning("{ProviderName} returned non-success status {StatusCode} for Notification {NotificationId}", Name, response.StatusCode, notification.Id);

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
                _logger.LogError(ex, "Unexpected error occurred while sending Notification {NotificationId} via {ProviderName}",
                    notification.Id, Name);

                throw new ProviderException(Name, $"An unexpected error occurred while sending the notification via {Name}.", ex);
            }
        }
    }
}