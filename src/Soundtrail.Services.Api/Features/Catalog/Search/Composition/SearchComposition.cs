using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Composition;

public sealed record SearchPorts(
    Func<IServiceProvider, ISearchPort> Search,
    Func<IServiceProvider, IClockPort> Clock,
    Func<IServiceProvider, ICommandBus> CommandBus,
    Func<IServiceProvider, IDiscoveryFeedbackPort> DiscoveryFeedback);

public static class SearchComposition
{
    public static void Configure(IServiceCollection services, SearchPorts ports)
    {
        services.TryAddSingleton(ports.Search);
        services.TryAddSingleton(ports.Clock);
        services.TryAddSingleton(ports.CommandBus);
        services.TryAddSingleton(ports.DiscoveryFeedback);
        services.TryAddScoped<IApiHandler<SearchRequest, SearchResponse?>, SearchHandler>();
    }
}
