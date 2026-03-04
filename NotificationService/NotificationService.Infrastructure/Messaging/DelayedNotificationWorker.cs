using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Infrastructure.Messaging
{
    public class DelayedNotificationWorker : BackgroundService
    {
        private readonly IDelayedNotificationQueue _queue;

        private readonly PriorityQueue<DelayedMessage, DateTime> _priorityQueue = new();

        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<DelayedNotificationWorker> _logger;

        public DelayedNotificationWorker(
            IDelayedNotificationQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<DelayedNotificationWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_queue.TryRead(out var message))
                {
                    _priorityQueue.Enqueue(message, message.DeliverAt);
                }

                TimeSpan timeToWait = Timeout.InfiniteTimeSpan;

                if (_priorityQueue.TryPeek(out var nextMessage, out var deliverAt))
                {
                    var delay = deliverAt - DateTime.UtcNow;

                    if (delay <= TimeSpan.Zero)
                    {
                        var messageToProcess = _priorityQueue.Dequeue();
                        _ = ProcessMessageSafeAsync(messageToProcess, stoppingToken);
                        continue;
                    }
                    else
                    {
                        timeToWait = delay;
                    }
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                if (timeToWait != Timeout.InfiniteTimeSpan)
                {
                    cts.CancelAfter(timeToWait);
                }

                try
                {
                    await _queue.WaitToReadAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task ProcessMessageSafeAsync(DelayedMessage message, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                await dispatcher.DispatchAsync(message.Notification, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process retry for Notification {message.Notification.Id}");
            }
        }
    }
}