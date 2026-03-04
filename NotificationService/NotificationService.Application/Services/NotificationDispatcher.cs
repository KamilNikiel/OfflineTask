using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configuration;
using NotificationService.Application.Exceptions;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Application.Services
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IDelayedNotificationQueue _retryQueue;
        private readonly ILogger<NotificationDispatcher> _logger;
        private readonly NotificationSettings _settings;
        private readonly IProviderResolver _providerResolver;

        public NotificationDispatcher(
            IDelayedNotificationQueue retryQueue,
            ILogger<NotificationDispatcher> logger,
            IOptions<NotificationSettings> options,
            IProviderResolver providerResolver)
        {
            _retryQueue = retryQueue;
            _logger = logger;
            _settings = options.Value;
            _providerResolver = providerResolver;
        }

        public async Task<DispatchStatus> DispatchAsync(Notification notification, CancellationToken cancellationToken)
        {
            var retryPolicy = new RetryPolicy(
                _settings.RetryPolicy.MaxRetries,
                _settings.RetryPolicy.BaseDelayMinutes);

            var providers = _providerResolver.GetActiveProviders(notification.Channel);

            foreach (var provider in providers)
            {
                try
                {
                    var providerResult = await provider.SendAsync(notification, cancellationToken);

                    if (providerResult.IsSuccess)
                    {
                        return DispatchStatus.Success;
                    }

                }
                catch (ProviderException ex)
                {
                    _logger.LogError(ex, "Infrastructure error occurred in provider {ProviderName} for Notification {NotificationId}", provider.Name, notification.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Unexpected critical error in {ProviderName} during dispatch of Notification {NotificationId}", provider.Name, notification.Id);
                }
            }

            return await HandleFailedDispatchAsync(notification, cancellationToken);
        }

        private async Task<DispatchStatus> HandleFailedDispatchAsync(Notification notification, CancellationToken cancellationToken)
        {
            var retryPolicy = new RetryPolicy(
                _settings.RetryPolicy.MaxRetries,
                _settings.RetryPolicy.BaseDelayMinutes);

            var decision = notification.PrepareRetry(retryPolicy);

            if (!decision.ShouldRetry)
            {
                _logger.LogError(
                    "Notification {NotificationId} failed permanently.",
                    notification.Id);

                return DispatchStatus.Failed;
            }

            await _retryQueue.EnqueueAsync(
                notification,
                decision.Delay!.Value,
                cancellationToken);

            return DispatchStatus.Delayed;
        }
    }
}