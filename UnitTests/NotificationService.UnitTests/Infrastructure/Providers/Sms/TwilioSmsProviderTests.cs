using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Application.Exceptions;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.ValueObjects;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Providers.Sms;
using System.Net;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Http;
using Twilio.Rest.Api.V2010.Account;

namespace NotificationService.UnitTests.Infrastructure.Providers.Sms
{
    public class TwilioSmsProviderTests
    {
        private readonly Mock<ITwilioRestClient> _twilioClientMock;
        private readonly Mock<ILogger<TwilioSmsProvider>> _loggerMock;
        private readonly IOptions<TwilioOptions> _options;
        private readonly TwilioSmsProvider _provider;
        private readonly Notification _testNotification;

        public TwilioSmsProviderTests()
        {
            _twilioClientMock = new Mock<ITwilioRestClient>();
            _loggerMock = new Mock<ILogger<TwilioSmsProvider>>();

            _twilioClientMock.SetupGet(c => c.AccountSid).Returns("TestAccountSid");
            _twilioClientMock.SetupGet(c => c.Region).Returns("us1");

            var twilioOptions = new TwilioOptions
            {
                AccountSid = "AccountSid",
                AuthToken = "AuthToken",
                FromPhoneNumber = "+123456789"
            };

            _options = Options.Create(twilioOptions);

            var recipient = new Recipient("user", "+123456789");
            var content = new MessageContent("Subject", "Body");
            _testNotification = new Notification(recipient, content, ChannelType.Sms);

            _provider = new TwilioSmsProvider(_twilioClientMock.Object, _options, _loggerMock.Object);
        }

        [Fact]
        public void Properties_ShouldReturnCorrectValues()
        {
            Assert.Equal("Twilio", _provider.Name);
            Assert.Equal(ChannelType.Sms, _provider.SupportedChannel);
        }

        [Fact]
        public async Task SendAsync_ShouldReturnSuccess_WhenMessageIsDelivered()
        {
            SetupTwilioMockWithStatus(MessageResource.StatusEnum.Delivered);

            var result = await _provider.SendAsync(_testNotification, CancellationToken.None);

            Assert.True(result.IsSuccess);
            VerifyTwilioRequestSent();
        }

        [Fact]
        public async Task SendAsync_ShouldReturnSuccess_WhenMessageIsQueued()
        {
            SetupTwilioMockWithStatus(MessageResource.StatusEnum.Queued);

            var result = await _provider.SendAsync(_testNotification, CancellationToken.None);

            Assert.True(result.IsSuccess);
            VerifyTwilioRequestSent();
        }

        [Fact]
        public async Task SendAsync_ShouldReturnFailure_WhenMessageStatusIsFailed()
        {
            SetupTwilioMockWithStatus(MessageResource.StatusEnum.Failed);

            var result = await _provider.SendAsync(_testNotification, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task SendAsync_ShouldReturnFailure_WhenMessageStatusIsUndelivered()
        {
            SetupTwilioMockWithStatus(MessageResource.StatusEnum.Undelivered);

            var result = await _provider.SendAsync(_testNotification, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task SendAsync_ShouldThrowProviderException_WhenApiExceptionIsThrown()
        {
            _twilioClientMock
                .Setup(c => c.RequestAsync(It.IsAny<Request>()))
                .ThrowsAsync(new ApiException(400, 12345, "ApiException", "http://twilio.com"));

            await Assert.ThrowsAsync<ProviderException>(() =>
                _provider.SendAsync(_testNotification, CancellationToken.None));
        }

        [Fact]
        public async Task SendAsync_ShouldThrowProviderException_WhenGenericExceptionIsThrown()
        {
            _twilioClientMock
                .Setup(c => c.RequestAsync(It.IsAny<Request>()))
                .ThrowsAsync(new Exception("Exception"));

            await Assert.ThrowsAsync<ProviderException>(() =>
                _provider.SendAsync(_testNotification, CancellationToken.None));
        }

        private void SetupTwilioMockWithStatus(MessageResource.StatusEnum status)
        {
            string jsonResponse = $"{{\"status\": \"{status.ToString().ToLower()}\"}}";
            var response = new Response(HttpStatusCode.OK, jsonResponse);

            _twilioClientMock
                .Setup(c => c.RequestAsync(It.IsAny<Request>()))
                .ReturnsAsync(response);
        }

        private void VerifyTwilioRequestSent()
        {
            _twilioClientMock.Verify(c => c.RequestAsync(It.Is<Request>(req =>
                req.Method == Twilio.Http.HttpMethod.Post &&
                req.Uri.AbsoluteUri.Contains("Messages.json")
            )), Times.Once);
        }
    }
}