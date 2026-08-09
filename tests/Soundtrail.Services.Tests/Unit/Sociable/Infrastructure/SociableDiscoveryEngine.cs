using System.Reflection;
using Microsoft.Extensions.Configuration;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Orchestrator;
using Soundtrail.Services.Enrichment.Scheduler;
using Soundtrail.Services.Enrichment.CatalogImport;
using Soundtrail.Services.Enrichment.Worker;
using Soundtrail.Services.Internal.Projector;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

internal sealed class SociableDiscoveryEngine : IDisposable
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    /// <summary>
    /// Default adapter ConfigureServices + handler assembly scanning, frozen once.
    /// Per-test Create clones these recipes and builds a fresh ServiceProvider.
    /// Fakes are not shared across tests; assembly scanning runs once.
    /// </summary>
    private static readonly Lazy<ServiceDescriptor[]> DefaultRegistrations = new(BuildDefaultRegistrations);

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

        if (dependencies.ReplaceAdapters.Count > 0)
        {
            return CreateFromScratch(dependencies);
        }

        IServiceCollection services = new ServiceCollection();
        AddPerTestOverlays(services, dependencies);

        foreach (var descriptor in DefaultRegistrations.Value)
        {
            if (IsPerTestOverlay(descriptor))
            {
                continue;
            }

            services.Add(descriptor);
        }

        return CreateEngine(services);
    }

    public TFake RequireFake<TService, TFake>() where TService : class where TFake : class, TService =>
        this.Resolve<TService>() as TFake
        ?? throw new InvalidOperationException(
            $"Expected '{typeof(TService).Name}' to be '{typeof(TFake).Name}'.");

    public T Resolve<T>() where T : class =>
        scope.ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }

    private static ServiceDescriptor[] BuildDefaultRegistrations()
    {
        var services = new ServiceCollection();

        // Sentinels so adapter TryAddScoped(ports.CommandBus) skips and does not leave
        // a recursive sp => GetRequiredService<ICommandBus>() factory in the template.
        AddPerTestOverlays(
            services,
            new SociableDependencies { CommandBus = CommandBusFake.Empty() });
        services.AddSingleton<DiscoveryEventProjector>();

        foreach (var feature in ResolveAdapters(replaceAdapters: []))
        {
            feature.ConfigureServices(services, EmptyConfiguration);
        }

        AddHandlerRegistrations(services);
        return PromoteScopedToSingleton(services).ToArray();
    }

    private static SociableDiscoveryEngine CreateFromScratch(SociableDependencies dependencies)
    {
        var services = new ServiceCollection();
        AddPerTestOverlays(services, dependencies);
        services.AddSingleton<DiscoveryEventProjector>();

        foreach (var feature in ResolveAdapters(dependencies.ReplaceAdapters))
        {
            feature.ConfigureServices(services, EmptyConfiguration);
        }

        AddHandlerRegistrations(services);
        return CreateEngine(PromoteScopedToSingleton(services));
    }

    /// <summary>
    /// Production compositions register ports as scoped (correct for Raven/request graphs).
    /// Sociable builds one ServiceProvider per test, but DiscoveryEventProjector opens nested
    /// scopes — scoped fakes would be empty there after seeding in the outer scope.
    /// Promote scoped → singleton for this provider only so nested scopes share fakes/SUTs,
    /// while each test still gets a fresh provider (fakes are never shared across tests).
    /// </summary>
    private static IServiceCollection PromoteScopedToSingleton(IServiceCollection services)
    {
        IServiceCollection promoted = new ServiceCollection();
        foreach (var descriptor in services)
        {
            promoted.Add(PromoteScopedToSingleton(descriptor));
        }

        return promoted;
    }

    private static ServiceDescriptor PromoteScopedToSingleton(ServiceDescriptor descriptor)
    {
        if (descriptor.Lifetime != ServiceLifetime.Scoped)
        {
            return descriptor;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return ServiceDescriptor.Singleton(descriptor.ServiceType, descriptor.ImplementationFactory);
        }

        if (descriptor.ImplementationType is not null)
        {
            return ServiceDescriptor.Singleton(descriptor.ServiceType, descriptor.ImplementationType);
        }

        if (descriptor.ImplementationInstance is not null)
        {
            return ServiceDescriptor.Singleton(descriptor.ServiceType, descriptor.ImplementationInstance);
        }

        return descriptor;
    }

    private static void AddPerTestOverlays(IServiceCollection services, SociableDependencies dependencies)
    {
        services.AddSingleton(new SociableScenarioOptions(dependencies.UtcNow));
        services.AddSingleton(dependencies.CommandBus);
        services.AddSingleton<ICommandBus>(dependencies.CommandBus);
    }

    private static bool IsPerTestOverlay(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(SociableScenarioOptions)
        || descriptor.ServiceType == typeof(CommandBusFake)
        || (descriptor.ServiceType == typeof(ICommandBus) && descriptor.ImplementationInstance is not null);

    private static void AddHandlerRegistrations(IServiceCollection services)
    {
        HandlerCollection.AddProjectionHandlersFromAssemblies(services, typeof(ProjectorAssemblyMarker));
        HandlerCollection.AddMessageHandlersFromAssemblies(
            services,
            typeof(WorkerAssemblyMarker),
            typeof(OrchestratorAssemblyMarker),
            typeof(CatalogImportAssemblyMarker));
        HandlerCollection.AddScheduledMessageHandlersFromAssemblies(
            services,
            typeof(SchedulerAssemblyMarker));
    }

    private static SociableDiscoveryEngine CreateEngine(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var commandBus = scope.ServiceProvider.GetRequiredService<CommandBusFake>();
        var handlers = scope.ServiceProvider.GetRequiredService<HandlerCollection>();
        var pump = new SociableMessagePump(commandBus, handlers);

        return new SociableDiscoveryEngine(provider, scope, pump);
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
            .OrderBy(static adapter => AdapterSortKey(adapter.GetType()), StringComparer.Ordinal)
            .ThenBy(static adapter => adapter.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static string AdapterSortKey(Type adapterType)
    {
        var fullName = adapterType.FullName ?? adapterType.Name;
        // Shared cross-cutting adapters must configure before feature adapters so
        // handler graphs resolve against the intended shared registrations.
        return fullName.Contains(".Infrastructure.DependencyConfiguration.", StringComparison.Ordinal)
            ? "0:" + fullName
            : "1:" + fullName;
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
