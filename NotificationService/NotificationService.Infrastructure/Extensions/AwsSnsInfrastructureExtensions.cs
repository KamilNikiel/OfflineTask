using Amazon;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Providers.Sms;

namespace NotificationService.Infrastructure.Extensions
{
    public static class AwsSnsInfrastructureExtensions
    {
        public static IServiceCollection AddAwsSnsProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AwsSnsOptions>(configuration.GetSection(AwsSnsOptions.SectionName));

            services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AwsSnsOptions>>().Value;
                var region = RegionEndpoint.GetBySystemName(options.Region);
                return new AmazonSimpleNotificationServiceClient(options.AccessKey, options.SecretKey, region);
            });

            services.AddSingleton<INotificationProvider, AwsSnsSmsProvider>();

            return services;
        }
    }
}