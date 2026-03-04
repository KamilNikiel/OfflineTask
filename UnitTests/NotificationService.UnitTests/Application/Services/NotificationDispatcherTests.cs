using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Application.Configuration;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Models;
using NotificationService.Application.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.UnitTests.Application.Services
{
    public class NotificationDispatcherTests
    {
        private readonly Mock<IDelayedNotificationQueue> _retryQueueMock;

        private readonly Mock<ILogger<NotificationDispatcher>> _loggerMock;

        private readonly Mock<IProviderResolver> _providerResolverMock;

        private readonly NotificationSettings _settings;

        private readonly IOptions<NotificationSettings> _options;

        public NotificationDispatcherTests()
        {
            _retryQueueMock = new Mock<IDelayedNotificationQueue>();
            _loggerMock = new Mock<ILogger<NotificationDispatcher>>();
            _providerResolverMock = new Mock<IProviderResolver>();

            _settings = new NotificationSettings
            {
                RetryPolicy = new RetryPolicySettings { MaxRetries = 3, BaseDelayMinutes = 2 },
                Providers = new List<ProviderConfig>
                {
                  new ProviderConfig { Name = "ProviderA", Channel = ChannelType.Sms, IsEnabled = true, Priority = 1 },
                  new ProviderConfig { Name = "ProviderB", Channel = ChannelType.Sms, IsEnabled = true, Priority = 2 }
                }
            };

            _options = Options.Create(_settings);
        }

        private Mock<INotificationProvider> CreateProviderMock(string name, ChannelType channel, bool successResult)
        {
            var mock = new Mock<INotificationProvider>();
            mock.Setup(p => p.Name).Returns(name);
            mock.Setup(p => p.SupportedChannel).Returns(channel);
            mock.Setup(p => p.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(successResult
                ? ProviderResult.Success()
                : ProviderResult.Failure("Error message", "500"));

            return mock;
        }

        private Notification CreateTestNotification(ChannelType channelType = ChannelType.Sms)
        {
            var recipient = new Recipient("user", "+123456789");
            var content = new MessageContent("Subject", "Body");

            return new Notification(recipient, content, channelType);
        }

        [Fact]
        public async Task DispatchAsync_ShouldUseFirstProvider_WhenSuccessful()
        {
            var providerAMock = CreateProviderMock("ProviderA", ChannelType.Sms, true);
            var providerBMock = CreateProviderMock("ProviderB", ChannelType.Sms, true);

            _providerResolverMock.Setup(r => r.GetActiveProviders(ChannelType.Sms))
                .Returns(new List<INotificationProvider> { providerAMock.Object, providerBMock.Object });

            var dispatcher = new NotificationDispatcher(_retryQueueMock.Object, _loggerMock.Object, _options, _providerResolverMock.Object);
            var notification = CreateTestNotification();

            var result = await dispatcher.DispatchAsync(notification, CancellationToken.None);

            Assert.Equal(DispatchStatus.Success, result);
            providerAMock.Verify(p => p.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
            providerBMock.Verify(p => p.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
            _retryQueueMock.Verify(q => q.EnqueueAsync(It.IsAny<Notification>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DispatchAsync_ShouldFailoverToSecondProvider_WhenFirstFails()
        {
            var providerAMock = CreateProviderMock("ProviderA", ChannelType.Sms, false);
            var providerBMock = CreateProviderMock("ProviderB", ChannelType.Sms, true);

            _providerResolverMock.Setup(r => r.GetActiveProviders(ChannelType.Sms))
                .Returns(new List<INotificationProvider> { providerAMock.Object, providerBMock.Object });

            var dispatcher = new NotificationDispatcher(_retryQueueMock.Object, _loggerMock.Object, _options, _providerResolverMock.Object);
            var notification = CreateTestNotification();

            var result = await dispatcher.DispatchAsync(notification, CancellationToken.None);

            Assert.Equal(DispatchStatus.Success, result);
            providerAMock.Verify(p => p.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
            providerBMock.Verify(p => p.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
            _retryQueueMock.Verify(q => q.EnqueueAsync(It.IsAny<Notification>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DispatchAsync_ShouldEnqueueForRetry_WhenAllProvidersFail()
        {
            var providerAMock = CreateProviderMock("ProviderA", ChannelType.Sms, false);
            var providerBMock = CreateProviderMock("ProviderB", ChannelType.Sms, false);

            _providerResolverMock.Setup(r => r.GetActiveProviders(ChannelType.Sms))
                .Returns(new List<INotificationProvider> { providerAMock.Object, providerBMock.Object });

            var dispatcher = new NotificationDispatcher(_retryQueueMock.Object, _loggerMock.Object, _options, _providerResolverMock.Object);
            var notification = CreateTestNotification();
            var result = await dispatcher.DispatchAsync(notification, CancellationToken.None);

            Assert.Equal(DispatchStatus.Delayed, result);
            providerAMock.Verify(p => p.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
            providerBMock.Verify(p => p.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);

            _retryQueueMock.Verify(q => q.EnqueueAsync(
              notification,
              It.IsAny<TimeSpan>(),
              It.IsAny<CancellationToken>()),
              Times.Once);

            Assert.Equal(1, notification.RetryCount);
        }

        [Fact]
        public async Task DispatchAsync_ShouldFailover_WhenFirstProviderThrowsException()
        {
            var providerAMock = CreateProviderMock("ProviderA", ChannelType.Sms, false);
            providerAMock.Setup(p => p.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());
            var providerBMock = CreateProviderMock("ProviderB", ChannelType.Sms, true);
            _providerResolverMock.Setup(r => r.GetActiveProviders(ChannelType.Sms))
                .Returns(new List<INotificationProvider> { providerAMock.Object, providerBMock.Object });

            var dispatcher = new NotificationDispatcher(_retryQueueMock.Object, _loggerMock.Object, _options, _providerResolverMock.Object);
            var notification = CreateTestNotification();

            var result = await dispatcher.DispatchAsync(notification, CancellationToken.None);

            Assert.Equal(DispatchStatus.Success, result);
            providerAMock.Verify(p => p.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
            providerBMock.Verify(p => p.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_ShouldNotEnqueue_WhenMaxRetriesReached()
        {
            var providerAMock = CreateProviderMock("ProviderA", ChannelType.Sms, false);
            _providerResolverMock.Setup(r => r.GetActiveProviders(ChannelType.Sms))
                .Returns(new List<INotificationProvider> { providerAMock.Object });

            var dispatcher = new NotificationDispatcher(_retryQueueMock.Object, _loggerMock.Object, _options, _providerResolverMock.Object);
            var notification = CreateTestNotification();
            var retryPolicySettings = _settings.RetryPolicy;

            var retryPolicy = new RetryPolicy(retryPolicySettings.MaxRetries, retryPolicySettings.BaseDelayMinutes);
            for (int i = 0; i < retryPolicySettings.MaxRetries; i++)
            {
                notification.PrepareRetry(retryPolicy);
            }

            var result = await dispatcher.DispatchAsync(notification, CancellationToken.None);

            Assert.Equal(DispatchStatus.Failed, result);
            _retryQueueMock.Verify(q => q.EnqueueAsync(It.IsAny<Notification>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DispatchAsync_ShouldEnqueue_WhenNoEligibleProvidersFound()
        {
            _providerResolverMock.Setup(r => r.GetActiveProviders(ChannelType.Email))
                .Returns(new List<INotificationProvider>());
            var dispatcher = new NotificationDispatcher(_retryQueueMock.Object, _loggerMock.Object, _options, _providerResolverMock.Object);
            var emailNotification = CreateTestNotification(ChannelType.Email);

            var result = await dispatcher.DispatchAsync(emailNotification, CancellationToken.None);

            Assert.Equal(DispatchStatus.Delayed, result);
            _retryQueueMock.Verify(q => q.EnqueueAsync(It.IsAny<Notification>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_ShouldCalculateExponentialBackoffCorrectly()
        {
            var providerAMock = CreateProviderMock("ProviderA", ChannelType.Sms, false); // Fails
            _providerResolverMock.Setup(r => r.GetActiveProviders(ChannelType.Sms))
                .Returns(new List<INotificationProvider> { providerAMock.Object });

            var dispatcher = new NotificationDispatcher(_retryQueueMock.Object, _loggerMock.Object, _options, _providerResolverMock.Object);
            var notification = CreateTestNotification();

            await dispatcher.DispatchAsync(notification, CancellationToken.None);

            var expectedDelay = TimeSpan.FromMinutes(_settings.RetryPolicy.BaseDelayMinutes * Math.Pow(2, notification.RetryCount - 1));

            _retryQueueMock.Verify(q => q.EnqueueAsync(
                notification,
                It.Is<TimeSpan>(ts => ts == expectedDelay),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}