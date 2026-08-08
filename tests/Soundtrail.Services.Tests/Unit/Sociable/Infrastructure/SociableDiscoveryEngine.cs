using System.Reflection;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Orchestrator;
using Soundtrail.Services.Enrichment.Worker;
using Soundtrail.Services.Internal.Projector;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Microsoft.Extensions.Configuration;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

internal sealed class SociableDiscoveryEngine : IDisposable
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    private SociableDiscoveryEngine(
        ServiceProvider serviceProvider,
        IServiceScope scope,
        SociableMessagePump messagePump)
    {
        this.serviceProvider = serviceProvider;
        this.scope = scope;
        this.MessagePump = messagePump;
    }

    public SociableMessagePump MessagePump { get; }

    public static SociableDiscoveryEngine Create(DateTimeOffset utcNow = default) =>
        Create(new SociableDependencies { UtcNow = utcNow });

    public static SociableDiscoveryEngine Create(SociableDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        var services = new ServiceCollection();
        services.AddSingleton(new SociableScenarioOptions(dependencies.UtcNow));
        services.AddSingleton(dependencies.CommandBus);
        services.AddSingleton<ICommandBus>(dependencies.CommandBus);
        services.AddSingleton<DiscoveryEventProjector>();

        foreach (var feature in ResolveAdapters(dependencies.ReplaceAdapters))
        {
            feature.ConfigureServices(services, EmptyConfiguration);
        }

        HandlerCollection.AddProjectionHandlersFromAssemblies(services, typeof(ProjectorAssemblyMarker));
        HandlerCollection.AddMessageHandlersFromAssemblies(
            services,
            typeof(WorkerAssemblyMarker),
            typeof(OrchestratorAssemblyMarker));

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var commandBus = scope.ServiceProvider.GetRequiredService<CommandBusFake>();
        var handlers = scope.ServiceProvider.GetRequiredService<HandlerCollection>();
        var pump = new SociableMessagePump(commandBus, handlers);

        return new SociableDiscoveryEngine(provider, scope, pump);
    }

    public TFake RequireFake<TService, TFake>() where TService : class where TFake : class, TService => this.Resolve<TService>() as TFake ?? throw new InvalidOperationException($"Expected '{typeof(TService).Name}' to be '{typeof(TFake).Name}'.");
    
    public T Resolve<T>() where T : class =>
        scope.ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }

    private static IReadOnlyList<IFeature> ResolveAdapters(IReadOnlyList<IFeature> replaceAdapters)
    {
        var adapters = DiscoverDefaultAdapters();

        foreach (var replacement in replaceAdapters)
        {
            if (replacement is not ISociableFeature)
            {
                throw new InvalidOperationException(
                    $"ReplaceAdapters entry '{replacement.GetType().Name}' must implement ISociableFeature.");
            }

            if (!adapters.ContainsKey(replacement.GetType()))
            {
                throw new InvalidOperationException(
                    $"No discovered sociable adapter of type '{replacement.GetType().Name}' to replace.");
            }

            adapters[replacement.GetType()] = replacement;
        }

        return adapters.Values
            .OrderBy(static adapter => adapter.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<Type, IFeature> DiscoverDefaultAdapters()
    {
        var adapterTypes = typeof(ISociableFeature).Assembly
            .GetTypes()
            .Where(static type => type is { IsClass: true, IsAbstract: false })
            .Where(static type => typeof(ISociableFeature).IsAssignableFrom(type))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        var adapters = new Dictionary<Type, IFeature>();

        foreach (var adapterType in adapterTypes)
        {
            var defaultFactory = adapterType.GetMethod(
                "Default",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            if (defaultFactory is null || !typeof(ISociableFeature).IsAssignableFrom(defaultFactory.ReturnType))
            {
                throw new InvalidOperationException(
                    $"Sociable adapter '{adapterType.Name}' must expose public static Default() returning ISociableFeature.");
            }

            var instance = defaultFactory.Invoke(null, null)
                ?? throw new InvalidOperationException($"'{adapterType.Name}.Default()' returned null.");

            adapters[adapterType] = (IFeature)instance;
        }

        return adapters;
    }
}
