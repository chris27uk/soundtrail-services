using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using DomainCommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Worker.Shared.Composition;

public sealed record CatalogEnrichmentReadPorts(
    Func<IServiceProvider, IReadPlaylistTracksByProviderPort> ReadPlaylistTracks,
    Func<IServiceProvider, IReadCatalogEntriesBySearchCriteriaPort> ReadCatalogEntries,
    Func<IServiceProvider, IReadStreamingLocationByProviderPort> ReadStreamingLocation,
    Func<IServiceProvider, IReadTrackForLookupPort> ReadTrackForLookup,
    Func<IServiceProvider, IReadAlbumsByArtistIdPort> ReadAlbumsByArtistId,
    Func<IServiceProvider, IReadTracksByArtistIdPort> ReadTracksByArtistId,
    Func<IServiceProvider, IReadTracksByAlbumIdPort> ReadTracksByAlbumId,
    Func<IServiceProvider, IClockPort> Clock,
    Func<IServiceProvider, DomainCommandBus> CommandBus);

public static class CatalogEnrichmentReadComposition
{
    public static void Configure(IServiceCollection services, CatalogEnrichmentReadPorts ports)
    {
        services.TryAddSingleton(ports.ReadPlaylistTracks);
        services.TryAddSingleton(ports.ReadCatalogEntries);
        services.TryAddSingleton(ports.ReadStreamingLocation);
        services.TryAddSingleton(ports.ReadTrackForLookup);
        services.TryAddSingleton(ports.ReadAlbumsByArtistId);
        services.TryAddSingleton(ports.ReadTracksByArtistId);
        services.TryAddSingleton(ports.ReadTracksByAlbumId);
        services.TryAddSingleton(ports.Clock);
        services.TryAddSingleton(ports.CommandBus);

        services.TryAddScoped<IHandler<LookupPlaylistTracksByProviderMessage>, LookupPlaylistTracksByProviderHandler>();
        services.TryAddScoped<IHandler<LookupMusicbrainzSearchResultsMessage>, LookupMusicbrainzSearchResultsHandler>();
        services.TryAddScoped<IHandler<LookupMusicbrainzArtistAlbumsMessage>, LookupMusicbrainzArtistAlbumsHandler>();
        services.TryAddScoped<IHandler<LookupMusicbrainzArtistTracksMessage>, LookupMusicbrainzArtistTracksHandler>();
        services.TryAddScoped<IHandler<LookupMusicbrainzAlbumTracksMessage>, LookupMusicbrainzAlbumTracksHandler>();
        services.TryAddScoped<IHandler<LookupStreamingLocationByIsrcMessage>, LookupStreamingLocationByIsrcHandler>();
        services.TryAddScoped<IHandler<LookupStreamingLocationByTrackMetadataMessage>, LookupStreamingLocationByTrackMetadataHandler>();
    }
}
