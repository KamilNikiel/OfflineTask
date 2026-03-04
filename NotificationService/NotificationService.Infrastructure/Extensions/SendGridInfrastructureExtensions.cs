using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Providers.Email;
using SendGrid;

namespace NotificationService.Infrastructure.Extensions
{
    public static class SendGridInfrastructureExtensions
    {
        public static IServiceCollection AddSendGridProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SendGridOptions>(configuration.GetSection(SendGridOptions.SectionName));
            services.AddSingleton<ISendGridClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<SendGridOptions>>().Value;
                return new SendGridClient(options.ApiKey);
            });
            services.AddSingleton<INotificationProvider, TwilioSendGridEmailProvider>();

            return services;
        }
    }
}