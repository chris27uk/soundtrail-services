using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicBrainzLookupExecutionListenerTestEnvironment
{
    private static readonly LookupCanonicalMusicMetadataCommandDto DefaultCommand =
        new(
            CommandId.For("LookupCanonicalMusicMetadata:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            "isrc-1",
            null,
            null,
            null);

    private MusicBrainzLookupExecutionListenerTestEnvironment(LookupExecutionReceiptStoreFake.State state)
    {
        MetadataSource = new FakeMusicBrainzMetadataSource();
        Listener = new MusicBrainzLookupExecutionListener(
            new ExecuteMusicBrainzLookupHandler(
                new LookupExecutionReceiptStoreFake(state),
                MetadataSource));
    }

    public MusicBrainzLookupExecutionListener Listener { get; }

    public FakeMusicBrainzMetadataSource MetadataSource { get; }

    public static MusicBrainzLookupExecutionListenerTestEnvironment WithANewExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public static MusicBrainzLookupExecutionListenerTestEnvironment WithADuplicateExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public void SeedMusicBrainzIsrc(string isrc, SongMetadata metadata) => MetadataSource.SeedIsrc(isrc, metadata);

    public void SeedMusicBrainzNames(string title, string artist, string? albumName, SongMetadata metadata) =>
        MetadataSource.SeedNames(title, artist, albumName, metadata);

    public Task<object[]> HandleNewExecutionCommand(CanonicalMusicMetadataLookup? lookup = null) =>
        Listener.Handle(ToDto(lookup ?? CanonicalMusicMetadataLookup.FromIsrc("isrc-1")), null!);

    public async Task<object[]> HandleDuplicateExecutionCommand(CanonicalMusicMetadataLookup? lookup = null)
    {
        var dto = ToDto(lookup ?? CanonicalMusicMetadataLookup.FromIsrc("isrc-1"));
        await Listener.Handle(dto, null!);
        return await Listener.Handle(dto, null!);
    }

    private static LookupCanonicalMusicMetadataCommandDto ToDto(CanonicalMusicMetadataLookup lookup) =>
        lookup switch
        {
            CanonicalMusicMetadataLookup.ByIsrc byIsrc => DefaultCommand with
            {
                Isrc = byIsrc.Isrc,
                TrackName = null,
                ArtistName = null,
                AlbumName = null
            },
            CanonicalMusicMetadataLookup.ByTrackNameArtistAndAlbum byTrack => DefaultCommand with
            {
                Isrc = null,
                TrackName = byTrack.TrackName,
                ArtistName = byTrack.ArtistName,
                AlbumName = byTrack.AlbumName
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lookup), lookup, null)
        };
}
