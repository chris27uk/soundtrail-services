using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Adapters.FeatureOrchestration;

public static class ScrutorExtensions
{
    public static IServiceCollection AddFeatures<TAssemblyMarkerType>(this IServiceCollection services) =>
        AddFeatures(services, typeof(TAssemblyMarkerType));

    public static IServiceCollection AddFeatures(
        this IServiceCollection services,
        params Type[] assemblyMarkers)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblyMarkers);

        services.Scan(scan =>
        {
            var from = scan.FromAssemblyOf<IFeature>();
            foreach (var marker in assemblyMarkers)
            {
                from = from.FromAssembliesOf(marker);
            }

            from.AddClasses(classes => classes
                    .AssignableTo<IFeature>()
                    .WithAttribute<AutodiscoverAttribute>())
                .AsImplementedInterfaces()
                .WithScopedLifetime();
        });

        return services;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class AutodiscoverAttribute : Attribute;
