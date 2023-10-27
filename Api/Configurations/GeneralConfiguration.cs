using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;

using Application;
using Infrastructure;

namespace API.Configurations
{
    public static class GeneralConfiguration
    {
        public static IServiceCollection ConfigureVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.Conventions.Add(new VersionByNamespaceConvention());
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });
            return services;
        }

        public static IServiceCollection ConfigureHeaders(this IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
            return services;
        }

        public static IServiceCollection ConfigureDependancies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationLayer();
            services.AddInfrastructureLayer();
            return services;
        }
    }
}
