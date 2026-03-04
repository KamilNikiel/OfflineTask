using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Exceptions;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Configuration;
using System.Net;

namespace NotificationService.Infrastructure.Providers.Sms
{
    public class AwsSnsSmsProvider : INotificationProvider
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly AwsSnsOptions _options;
        private readonly ILogger<AwsSnsSmsProvider> _logger;

        public string Name => "AwsSns";
        public ChannelType SupportedChannel => ChannelType.Sms;

        public AwsSnsSmsProvider(
            IAmazonSimpleNotificationService snsClient,
            IOptions<AwsSnsOptions> options,
            ILogger<AwsSnsSmsProvider> logger)
        {
            _snsClient = snsClient;
            _logger = logger;
            _options = options.Value;
        }
        public async Task<ProviderResult> SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            try
            {
                var request = new PublishRequest
                {
                    Message = notification.Content.Body,
                    PhoneNumber = notification.Recipient.ContactInfo
                };

                request.MessageAttributes["AWS.SNS.SMS.SenderID"] = new MessageAttributeValue
                {
                    StringValue = _options.SenderId,
                    DataType = "String"
                };

                var response = await _snsClient.PublishAsync(request, cancellationToken);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    return ProviderResult.Success();
                }

                _logger.LogWarning("{ProviderName} returned non-success status {HttpStatusCode} for Notification {NotificationId}",
                    Name, response.HttpStatusCode, notification.Id);

                return ProviderResult.Failure(
                    null,
                    response.HttpStatusCode.ToString());
            }
            catch (AmazonSimpleNotificationServiceException ex)
            {
                _logger.LogWarning(ex, "{ProviderName} API rejected Notification {NotificationId}. ApiErrorCode: {ApiErrorCode}, Message: {ErrorMessage}",
                    Name, notification.Id, ex.ErrorCode, ex.Message);

                throw new ProviderException(Name, $"The {Name} API request failed due to a provider-side error.", ex);
            }
        }
    }
}