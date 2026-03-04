using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Configuration;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;

namespace NotificationService.Application.Extensions
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<NotificationSettings>(configuration.GetSection(nameof(NotificationSettings)));
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

            return services;
        }
    }
}