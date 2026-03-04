using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.ValueObjects;
using System.Threading.Channels;

namespace NotificationService.Infrastructure.Messaging
{
    // InMemory is demo implementation. Production would use persistent queue (Kafka, Redis)
    internal class InMemoryDelayedQueue : IDelayedNotificationQueue
    {
        private readonly Channel<DelayedMessage> _channel = Channel.CreateUnbounded<DelayedMessage>();

        public async Task EnqueueAsync(Notification notification, TimeSpan delay, CancellationToken cancellationToken)
        {
            var deliverAt = DateTime.UtcNow.Add(delay);
            var message = new DelayedMessage(notification, deliverAt);

            await _channel.Writer.WriteAsync(message, cancellationToken);
        }

        public bool TryRead(out DelayedMessage message)
        {
            return _channel.Reader.TryRead(out message!);
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.WaitToReadAsync(cancellationToken);
        }
    }
}