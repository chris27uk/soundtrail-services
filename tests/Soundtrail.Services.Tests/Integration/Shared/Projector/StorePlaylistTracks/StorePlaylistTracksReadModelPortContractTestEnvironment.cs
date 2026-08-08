using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

namespace Soundtrail.Services.Tests.Integration.Shared.Projector.StorePlaylistTracks;

internal sealed class StorePlaylistTracksReadModelPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds = [];
    private readonly StorePlaylistTracksReadModelPortFake? fake;

    private StorePlaylistTracksReadModelPortContractTestEnvironment(
        IStorePlaylistTracksReadModelPort subject,
        PlaylistId playlistId,
        IDocumentStore? documentStore,
        StorePlaylistTracksReadModelPortFake? fake)
    {
        Subject = subject;
        PlaylistId = playlistId;
        this.documentStore = documentStore;
        this.fake = fake;
    }

    public IStorePlaylistTracksReadModelPort Subject { get; }

    public PlaylistId PlaylistId { get; }

    public static StorePlaylistTracksReadModelPortContractTestEnvironment Create(
        StorePlaylistTracksReadModelPortImplementation implementation,
        string playlistName = "projector_playlist_tracks")
    {
        var playlistId = PlaylistId.FromPlaylistName(playlistName);
        return implementation switch
        {
            StorePlaylistTracksReadModelPortImplementation.Fake => CreateFake(playlistId),
            StorePlaylistTracksReadModelPortImplementation.Raven => CreateRaven(playlistId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public async Task SeedCatalogTrackAsync(CatalogTrackRecordDto track)
    {
        cleanupDocumentIds.Add(CatalogTrackRecordDto.GetDocumentId(track.TrackId));
        cleanupDocumentIds.Add(CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value));

        if (fake is not null)
        {
            fake.SeedCatalogTrack(track);
            return;
        }

        using var session = documentStore!.OpenAsyncSession();
        await session.StoreAsync(track, CatalogTrackRecordDto.GetDocumentId(track.TrackId));
        await session.SaveChangesAsync();
    }

    public async Task<CatalogPlaylistTracksRecordDto?> LoadPlaylistTracksAsync()
    {
        if (fake is not null)
        {
            return fake.Load(PlaylistId);
        }

        using var session = documentStore!.OpenAsyncSession();
        return await session.LoadAsync<CatalogPlaylistTracksRecordDto>(
            CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value));
    }

    public async ValueTask DisposeAsync()
    {
        if (documentStore is null)
        {
            return;
        }

        foreach (var documentId in cleanupDocumentIds.Distinct(StringComparer.Ordinal))
        {
            await EmbeddedRavenTestServer.DisposeAsync(documentStore, documentId);
        }
    }

    private static StorePlaylistTracksReadModelPortContractTestEnvironment CreateFake(PlaylistId playlistId)
    {
        var fake = new StorePlaylistTracksReadModelPortFake();
        return new StorePlaylistTracksReadModelPortContractTestEnvironment(fake, playlistId, null, fake);
    }

    private static StorePlaylistTracksReadModelPortContractTestEnvironment CreateRaven(PlaylistId playlistId)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();
        return new StorePlaylistTracksReadModelPortContractTestEnvironment(
            new RavenStorePlaylistTracksReadModelPort(store),
            playlistId,
            store,
            fake: null);
    }
}

internal sealed class StorePlaylistTracksReadModelPortFake : IStorePlaylistTracksReadModelPort
{
    private readonly Dictionary<string, CatalogTrackRecordDto> catalogTracks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CatalogPlaylistTracksRecordDto> playlists = new(StringComparer.Ordinal);

    public void SeedCatalogTrack(CatalogTrackRecordDto track) =>
        catalogTracks[track.TrackId] = track;

    public CatalogPlaylistTracksRecordDto? Load(PlaylistId playlistId) =>
        playlists.GetValueOrDefault(CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId.Value));

    public Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken)
    {
        var documentId = CatalogPlaylistTracksRecordDto.GetDocumentId(@event.PlaylistId.Value);
        var existing = playlists.GetValueOrDefault(documentId);
        var trackIds = MergeTrackIds(existing?.TrackIds, @event.Tracks.Select(static id => id.Value));
        playlists[documentId] = BuildRecord(@event.PlaylistId.Value, trackIds, @event.ObservedAt, existing?.Discovery);
        return Task.CompletedTask;
    }

    public Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        var requested = TrackIdIndexProjection.From(trackId);
        foreach (var playlist in playlists.Values.Where(record =>
                     record.TrackIds.Select(TrackId.From).Select(TrackIdIndexProjection.From)
                         .Any(existing => existing.SharesBaseWith(requested))))
        {
            playlists[playlist.Id] = BuildRecord(playlist.PlaylistId, playlist.TrackIds, playlist.UpdatedAt, playlist.Discovery);
        }

        return Task.CompletedTask;
    }

    private CatalogPlaylistTracksRecordDto BuildRecord(
        string playlistId,
        IReadOnlyList<string> trackIds,
        DateTimeOffset updatedAt,
        CatalogDiscoveryFeedbackRecordDto? discovery)
    {
        var playlistTrackIds = trackIds.Select(TrackId.From).ToArray();
        var requestedBases = playlistTrackIds
            .Select(TrackIdIndexProjection.From)
            .DistinctBy(static projection => (projection.BaseHigh, projection.BaseLow))
            .ToArray();

        var tracksByBase = catalogTracks.Values
            .Select(track => (Track: track, Projection: TrackIdIndexProjection.From(TrackId.From(track.TrackId))))
            .Where(entry => requestedBases.Any(requested => requested.SharesBaseWith(entry.Projection)))
            .GroupBy(static entry => (entry.Projection.BaseHigh, entry.Projection.BaseLow))
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static entry => entry.Track).ToArray());

        return new CatalogPlaylistTracksRecordDto
        {
            Id = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
            PlaylistId = playlistId,
            TrackIds = trackIds.ToArray(),
            Tracks = playlistTrackIds
                .Select(trackId => SelectPreferredTrack(tracksByBase, trackId))
                .Where(static track => track is not null)
                .Select(track => new CatalogPlaylistTrackRecordDto
                {
                    TrackId = track!.TrackId,
                    MusicCatalogId = track.MusicCatalogId,
                    Title = track.Title,
                    ArtistName = track.ArtistName,
                    AlbumTitle = track.AlbumTitle,
                    DurationMs = track.DurationMs,
                    Isrc = track.Isrc,
                    ReleaseDate = track.ReleaseDate,
                    ReleaseType = track.ReleaseType,
                    ArtworkUrl = track.ArtworkUrl,
                    StreamingLocations = track.StreamingLocations
                })
                .ToArray(),
            Discovery = discovery,
            UpdatedAt = updatedAt
        };
    }

    private static CatalogTrackRecordDto? SelectPreferredTrack(
        IReadOnlyDictionary<(ulong BaseHigh, ulong BaseLow), CatalogTrackRecordDto[]> tracksByBase,
        TrackId requestedTrackId)
    {
        var requestedProjection = TrackIdIndexProjection.From(requestedTrackId);
        if (!tracksByBase.TryGetValue((requestedProjection.BaseHigh, requestedProjection.BaseLow), out var candidates))
        {
            return null;
        }

        return candidates
            .Select(track => (Track: track, Projection: TrackIdIndexProjection.From(TrackId.From(track.TrackId))))
            .OrderByDescending(static entry => entry.Track.StreamingLocations.Length)
            .ThenBy(entry => entry.Projection.GetDistanceTo(requestedProjection))
            .ThenByDescending(static entry => entry.Track.UpdatedAt)
            .Select(static entry => entry.Track)
            .FirstOrDefault();
    }

    private static string[] MergeTrackIds(
        IReadOnlyCollection<string>? existingTrackIds,
        IEnumerable<string> discoveredTrackIds)
    {
        if (existingTrackIds is null || existingTrackIds.Count == 0)
        {
            return discoveredTrackIds.Distinct(StringComparer.Ordinal).ToArray();
        }

        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var trackId in existingTrackIds.Concat(discoveredTrackIds))
        {
            if (seen.Add(trackId))
            {
                merged.Add(trackId);
            }
        }

        return merged.ToArray();
    }
}

public enum StorePlaylistTracksReadModelPortImplementation
{
    Fake,
    Raven
}
