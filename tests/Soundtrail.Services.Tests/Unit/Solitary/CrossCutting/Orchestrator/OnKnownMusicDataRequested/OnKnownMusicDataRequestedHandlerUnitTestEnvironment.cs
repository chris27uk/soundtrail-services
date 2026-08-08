using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnKnownMusicDataRequested;

internal sealed class OnKnownMusicDataRequestedHandlerUnitTestEnvironment
{
    private OnKnownMusicDataRequestedHandlerUnitTestEnvironment(EventStreamRepositoryFake repository) =>
        Repository = repository;

    public EventStreamRepositoryFake Repository { get; }

    public static OnKnownMusicDataRequestedHandlerUnitTestEnvironment Create() =>
        new(new EventStreamRepositoryFake());

    public OnKnownMusicDataRequestedHandler CreateSubject() => new(new WorkPlanner(), Repository);

    public static RequestKnownMusicDataMessage CreateKnownArtistRequest(
        string artistId = "artist-123",
        LookupPriorityBand priority = LookupPriorityBand.High,
        int trustLevel = 100,
        int riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string commandId = "cmd-1",
        string correlationId = "corr-1") =>
        new(
            new CatalogItemOperation.ChildTracksForArtist(ArtistId.From(artistId)),
            priority,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero))
        {
            Id = MessageId.For(commandId),
            CorrelationId = CorrelationId.From(correlationId)
        };

    public static RequestKnownMusicDataMessage CreateKnownTrackRequest(
        string? trackId = null,
        int trustLevel = 100,
        int riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string commandId = "cmd-track",
        string correlationId = "corr-track") =>
        new(
            new CatalogItemOperation.StreamingLocationForTrack(TrackId.From(trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("track-123"))),
            LookupPriorityBand.High,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero))
        {
            Id = MessageId.For(commandId),
            CorrelationId = CorrelationId.From(correlationId)
        };

    public static RequestKnownMusicDataMessage CreateKnownAlbumRequest(
        string artistId = "artist-123",
        string albumId = "album-123",
        int trustLevel = 100,
        int riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string commandId = "cmd-album",
        string correlationId = "corr-album") =>
        new(
            new CatalogItemOperation.ChildTracksForAlbum(AlbumId.From(artistId, albumId)),
            LookupPriorityBand.High,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero))
        {
            Id = MessageId.For(commandId),
            CorrelationId = CorrelationId.From(correlationId)
        };

    public static RequestKnownMusicDataMessage CreateKnownPlaylistRequest(
        string playlistName = "road trip",
        int trustLevel = 100,
        int riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string commandId = "cmd-playlist",
        string correlationId = "corr-playlist") =>
        new(
            new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId.FromPlaylistName(playlistName)),
            LookupPriorityBand.High,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero))
        {
            Id = MessageId.For(commandId),
            CorrelationId = CorrelationId.From(correlationId)
        };
}
