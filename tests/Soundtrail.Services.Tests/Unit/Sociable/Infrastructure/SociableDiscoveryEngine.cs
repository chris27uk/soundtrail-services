using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Orchestrator;
using Soundtrail.Services.Enrichment.Worker;
using Soundtrail.Services.Internal.Projector;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        MessagePump = messagePump;
    }

    public SociableMessagePump MessagePump { get; }

    public static SociableDiscoveryEngine Create(
        DateTimeOffset utcNow = default,
        ApiTestAdapters? api = null,
        OrchestratorTestAdapters? orchestrator = null,
        WorkerTestAdapters? worker = null,
        ProjectorTestAdapters? projector = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SociableScenarioOptions(utcNow));
        services.AddSingleton<CommandBusFake>();
        services.AddSingleton<ICommandBus>(sp => sp.GetRequiredService<CommandBusFake>());
        services.AddSingleton<DiscoveryEventProjector>();

        IFeature[] adapters =
        [
            api ?? ApiTestAdapters.Default(),
            orchestrator ?? OrchestratorTestAdapters.Default(),
            worker ?? WorkerTestAdapters.Default(),
            projector ?? ProjectorTestAdapters.Default(),
        ];

        foreach (var feature in adapters)
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

    public T Resolve<T>() where T : class =>
        scope.ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }
}
