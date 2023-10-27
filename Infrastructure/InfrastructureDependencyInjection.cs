using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static void AddInfrastructureLayer(this IServiceCollection services)
        {
            services.AddSingleton<Application.Infrastructure.IVentLRSerialProtocol, VentLRSerialProtocol>();
        }
    }
}
