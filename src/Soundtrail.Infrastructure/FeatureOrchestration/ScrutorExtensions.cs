using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Adapters.FeatureOrchestration
{
    public static class ScrutorExtensions
    {
        public static IServiceCollection AddFeatures<TAssemblyMarkerType>(this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromAssemblyOf<IFeature>()
                .FromAssemblyOf<TAssemblyMarkerType>()
                .AddClasses(classes => classes.AssignableTo<IFeature>().WithAttribute<AutodiscoverAttribute>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            
            return services;
        }
    }

    public class AutodiscoverAttribute : Attribute { }
}
