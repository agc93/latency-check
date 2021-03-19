using Microsoft.Extensions.DependencyInjection;

namespace LatencyCheck.Service
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddConnectionClient(this IServiceCollection services, string executableName)
        {
            services.AddSingleton<ProcessConnectionClient>(p => ProcessConnectionClient.Create(executableName.EndsWith(".exe") ? executableName : $"{executableName}.exe"));
            return services;
        }
    }
}