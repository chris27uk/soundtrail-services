using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Composition;

public sealed record OnArtistCatalogChangedPorts(
    Func<IServiceProvider, IStoreArtistCatalogReadModelPort> ArtistCatalog,
    Func<IServiceProvider, IEventStreamRepository<ArtistId>> ArtistRepository);

public static class OnArtistCatalogChangedComposition
{
    public static void Configure(IServiceCollection services, OnArtistCatalogChangedPorts ports)
    {
        services.TryAddScoped(ports.ArtistCatalog);
        services.TryAddScoped(ports.ArtistRepository);
        // CatalogItemChanged invokes this after appending; must not rely on Scrutor handler order.
        services.TryAddScoped<ArtistCatalogChangedProjectorHandler>();
    }
}
