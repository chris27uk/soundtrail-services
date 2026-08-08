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

public sealed class CatalogEnrichmentReadPorts(
    Func<IServiceProvider, IReadPlaylistTracksByProviderPort> readPlaylistTracks,
    Func<IServiceProvider, IReadCatalogEntriesBySearchCriteriaPort> readCatalogEntries,
    Func<IServiceProvider, IReadStreamingLocationByProviderPort> readStreamingLocation,
    Func<IServiceProvider, IReadTrackForLookupPort> readTrackForLookup,
    Func<IServiceProvider, IReadAlbumsByArtistIdPort> readAlbumsByArtistId,
    Func<IServiceProvider, IReadTracksByArtistIdPort> readTracksByArtistId,
    Func<IServiceProvider, IReadTracksByAlbumIdPort> readTracksByAlbumId,
    Func<IServiceProvider, IClockPort> clock,
    Func<IServiceProvider, DomainCommandBus> commandBus)
{
    public Func<IServiceProvider, IReadPlaylistTracksByProviderPort> ReadPlaylistTracks { get; } = readPlaylistTracks;

    public Func<IServiceProvider, IReadCatalogEntriesBySearchCriteriaPort> ReadCatalogEntries { get; } = readCatalogEntries;

    public Func<IServiceProvider, IReadStreamingLocationByProviderPort> ReadStreamingLocation { get; } = readStreamingLocation;

    public Func<IServiceProvider, IReadTrackForLookupPort> ReadTrackForLookup { get; } = readTrackForLookup;

    public Func<IServiceProvider, IReadAlbumsByArtistIdPort> ReadAlbumsByArtistId { get; } = readAlbumsByArtistId;

    public Func<IServiceProvider, IReadTracksByArtistIdPort> ReadTracksByArtistId { get; } = readTracksByArtistId;

    public Func<IServiceProvider, IReadTracksByAlbumIdPort> ReadTracksByAlbumId { get; } = readTracksByAlbumId;

    public Func<IServiceProvider, IClockPort> Clock { get; } = clock;

    public Func<IServiceProvider, DomainCommandBus> CommandBus { get; } = commandBus;
}

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
