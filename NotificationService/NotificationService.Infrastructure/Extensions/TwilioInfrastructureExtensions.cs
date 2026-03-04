using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Providers.Sms;
using Twilio.Clients;
using Twilio.Http;

namespace NotificationService.Infrastructure.Extensions
{
    public static class TwilioInfrastructureExtensions
    {
        public static IServiceCollection AddTwilioProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));
            services.AddHttpClient<ITwilioRestClient, TwilioRestClient>((httpClient, sp) =>
            {
                var options = sp.GetRequiredService<IOptions<TwilioOptions>>().Value;
                return new TwilioRestClient(
                    options.AccountSid,
                    options.AuthToken,
                    httpClient: new SystemNetHttpClient(httpClient));
            });
            services.AddSingleton<INotificationProvider, TwilioSmsProvider>();

            return services;
        }
    }
}