using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Providers.Push;

namespace NotificationService.Infrastructure.Extensions
{
    public static class FirebaseInfrastructureExtensions
    {
        public static IServiceCollection AddFirebaseProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
            services.AddHttpClient<FirebasePushProvider>();
            services.AddSingleton<INotificationProvider, FirebasePushProvider>();

            return services;
        }
    }
}