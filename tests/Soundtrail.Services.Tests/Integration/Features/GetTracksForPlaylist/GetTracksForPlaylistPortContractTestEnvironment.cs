using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Api.Shared.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForPlaylist.Support;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly string? databaseName;

    private GetTracksForPlaylistPortContractTestEnvironment(
        IGetTracksForPlaylistPort subject,
        PlaylistId playlistId,
        IDocumentStore? documentStore = null,
        string? databaseName = null)
    {
        Subject = subject;
        PlaylistId = playlistId;
        this.documentStore = documentStore;
        this.databaseName = databaseName;
    }

    public IGetTracksForPlaylistPort Subject { get; }

    public PlaylistId PlaylistId { get; }

    public static async Task<GetTracksForPlaylistPortContractTestEnvironment> ForExistingPlaylistTracks(
        GetTracksForPlaylistPortImplementation implementation,
        string playlistName = "WorldwideSongChart",
        string? trackId = null,
        string musicCatalogId = "track-3501",
        string title = "The Track",
        string artistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2403501",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-3501.jpg",
        CatalogStreamingLocationRecordDto[]? streamingLocations = null)
    {
        var resolvedPlaylistId = PlaylistId.FromPlaylistName(playlistName);
        var trackIdValue = trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("track-3501");
        var resolvedTrackId = TrackId.From(trackIdValue);
        var resolvedStreamingLocations = streamingLocations ?? [];
        var response = new GetTracksForPlaylistResponse(
            resolvedPlaylistId,
            [
                new GetTracksForPlaylistTrackResponse(
                    resolvedTrackId,
                    title,
                    artistName,
                    albumTitle,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl,
                    resolvedStreamingLocations.Length > 0,
                    resolvedStreamingLocations
                        .Select(static location => new StreamingLocationResponse(
                            location.Provider,
                            location.ExternalId,
                            location.Url))
                        .ToArray())
            ]);

        return implementation switch
        {
            GetTracksForPlaylistPortImplementation.Fake => new GetTracksForPlaylistPortContractTestEnvironment(
                GetTracksForPlaylistPortFake.Create().WithPlaylistTracks(response),
                resolvedPlaylistId),
            GetTracksForPlaylistPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedPlaylistId,
                new CatalogPlaylistTracksRecordDto
                {
                    Id = CatalogPlaylistTracksRecordDto.GetDocumentId(resolvedPlaylistId.Value),
                    PlaylistId = resolvedPlaylistId.Value,
                    TrackIds = [trackIdValue],
                    Tracks =
                    [
                        new CatalogPlaylistTrackRecordDto
                        {
                            TrackId = trackIdValue,
                            MusicCatalogId = musicCatalogId,
                            Title = title,
                            ArtistName = artistName,
                            AlbumTitle = albumTitle,
                            DurationMs = durationMs,
                            Isrc = isrc,
                            ReleaseDate = response.Tracks[0].ReleaseDate,
                            ArtworkUrl = artworkUrl,
                            StreamingLocations = resolvedStreamingLocations
                        }
                    ]
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<GetTracksForPlaylistPortContractTestEnvironment> ForMissingPlaylistTracks(
        GetTracksForPlaylistPortImplementation implementation,
        PlaylistId? playlistId = null)
    {
        var resolvedPlaylistId = playlistId ?? PlaylistId.FromPlaylistName("WorldwideSongChart");

        return implementation switch
        {
            GetTracksForPlaylistPortImplementation.Fake => new GetTracksForPlaylistPortContractTestEnvironment(
                GetTracksForPlaylistPortFake.Create(),
                resolvedPlaylistId),
            GetTracksForPlaylistPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedPlaylistId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        return EmbeddedRavenTestServer.DisposeAsync(this.documentStore);
    }

    private static async Task<GetTracksForPlaylistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        PlaylistId playlistId,
        CatalogPlaylistTracksRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetTracksForPlaylistPortContractTestEnvironment(
            new RavenGetTracksForPlaylistPort(store, AppTypeRegistry.ServiceLocation),
            playlistId,
            store,
            existingRecord?.Id);
    }
}

public enum GetTracksForPlaylistPortImplementation
{
    Fake,
    Raven
}
