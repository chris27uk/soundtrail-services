using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.GetTracksForPlaylist.Api.Ports;

internal sealed class GetTracksForPlaylistPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private GetTracksForPlaylistPortContractTestEnvironment(
        IGetTracksForPlaylistPort subject,
        PlaylistId playlistId,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null)
    {
        Subject = subject;
        PlaylistId = playlistId;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
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
        if (implementation == GetTracksForPlaylistPortImplementation.Fake)
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
                            .Select(static location => new Soundtrail.Services.Api.Shared.Contract.StreamingLocationResponse(
                                location.Provider,
                                location.ExternalId,
                                location.Url))
                            .ToArray())
                ]);

            return new GetTracksForPlaylistPortContractTestEnvironment(
                GetTracksForPlaylistPortFake.Create().WithPlaylistTracks(response),
                resolvedPlaylistId);
        }

        if (implementation != GetTracksForPlaylistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var uniquePlaylistName = $"{playlistName}-{isolation}";
        var ravenPlaylistId = PlaylistId.FromPlaylistName(uniquePlaylistName);
        var ravenTrackIdValue = global::Soundtrail.Services.Tests.TestTrackIds.Value($"track-3501-{isolation}");
        var seededStreamingLocations = streamingLocations ?? [];
        var resolvedReleaseDate = releaseDate ?? new DateOnly(2024, 1, 2);

        return await CreateRavenEnvironmentAsync(
            ravenPlaylistId,
            new CatalogPlaylistTracksRecordDto
            {
                Id = CatalogPlaylistTracksRecordDto.GetDocumentId(ravenPlaylistId.Value),
                PlaylistId = ravenPlaylistId.Value,
                TrackIds = [ravenTrackIdValue],
                Tracks =
                [
                    new CatalogPlaylistTrackRecordDto
                    {
                        TrackId = ravenTrackIdValue,
                        MusicCatalogId = $"{musicCatalogId}-{isolation}",
                        Title = title,
                        ArtistName = artistName,
                        AlbumTitle = albumTitle,
                        DurationMs = durationMs,
                        Isrc = isrc,
                        ReleaseDate = resolvedReleaseDate,
                        ArtworkUrl = artworkUrl,
                        StreamingLocations = seededStreamingLocations
                    }
                ]
            });
    }

    public static async Task<GetTracksForPlaylistPortContractTestEnvironment> ForMissingPlaylistTracks(
        GetTracksForPlaylistPortImplementation implementation,
        PlaylistId? playlistId = null)
    {
        if (implementation == GetTracksForPlaylistPortImplementation.Fake)
        {
            var resolvedPlaylistId = playlistId ?? PlaylistId.FromPlaylistName("WorldwideSongChart");
            return new GetTracksForPlaylistPortContractTestEnvironment(
                GetTracksForPlaylistPortFake.Create(),
                resolvedPlaylistId);
        }

        if (implementation != GetTracksForPlaylistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var ravenPlaylistId = PlaylistId.FromPlaylistName($"WorldwideSongChart-{isolation}");
        return await CreateRavenEnvironmentAsync(ravenPlaylistId);
    }

    public async ValueTask DisposeAsync()
    {
        if (documentStore is null)
        {
            return;
        }

        await EmbeddedRavenTestServer.DeleteDocumentsAsync(documentStore, cleanupDocumentIds);
        await EmbeddedRavenTestServer.DisposeAsync(documentStore);
    }

    private static async Task<GetTracksForPlaylistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        PlaylistId playlistId,
        CatalogPlaylistTracksRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();
        var cleanupDocumentIds = new List<string>();

        if (existingRecord is not null)
        {
            cleanupDocumentIds.Add(existingRecord.Id);
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetTracksForPlaylistPortContractTestEnvironment(
            new RavenGetTracksForPlaylistPort(store, AppTypeRegistry.ServiceLocation),
            playlistId,
            store,
            cleanupDocumentIds);
    }
}

public enum GetTracksForPlaylistPortImplementation
{
    Fake,
    Raven
}
