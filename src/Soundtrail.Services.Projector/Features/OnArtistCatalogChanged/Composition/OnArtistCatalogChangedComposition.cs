using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Composition;

public sealed class OnArtistCatalogChangedPorts(
    Func<IServiceProvider, IStoreArtistCatalogReadModelPort> artistCatalog,
    Func<IServiceProvider, IEventStreamRepository<ArtistId>> artistRepository)
{
    public Func<IServiceProvider, IStoreArtistCatalogReadModelPort> ArtistCatalog { get; } = artistCatalog;

    public Func<IServiceProvider, IEventStreamRepository<ArtistId>> ArtistRepository { get; } = artistRepository;
}

public static class OnArtistCatalogChangedComposition
{
    public static void Configure(IServiceCollection services, OnArtistCatalogChangedPorts ports)
    {
        services.TryAddSingleton(ports.ArtistCatalog);
        services.TryAddSingleton(ports.ArtistRepository);
    }
}
