using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Composition;

public sealed class OnLookupWorkReadyPorts(Func<IServiceProvider, ICommandBus> commandBus)
{
    public Func<IServiceProvider, ICommandBus> CommandBus { get; } = commandBus;
}

public static class OnLookupWorkReadyComposition
{
    public static void Configure(IServiceCollection services, OnLookupWorkReadyPorts ports)
    {
        services.TryAddSingleton(ports.CommandBus);
        services.TryAddScoped<LookupWorkReadyHandler>();
        services.TryAddScoped<IHandler<DispatchLookupWork>, LookupWorkReadyHandler>();
    }
}
