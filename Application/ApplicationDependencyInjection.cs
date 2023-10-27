using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class ApplicationDependencyInjection
    {
        public static void AddApplicationLayer(this IServiceCollection services)
        {
            services.AddSingleton<I_HW_API, Services.Impl.HW_API>();
        }
    }
}
