using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Providers;

namespace NotificationService.Infrastructure.Extensions
{
    public static class MessagingInfrastructureExtensions
    {
        public static IServiceCollection AddMessagingInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IProviderResolver, ProviderResolver>();
            services.AddSingleton<InMemoryDelayedQueue>();
            services.AddSingleton<IDelayedNotificationQueue>(sp => sp.GetRequiredService<InMemoryDelayedQueue>());
            services.AddHostedService<DelayedNotificationWorker>();

            return services;
        }
    }
}