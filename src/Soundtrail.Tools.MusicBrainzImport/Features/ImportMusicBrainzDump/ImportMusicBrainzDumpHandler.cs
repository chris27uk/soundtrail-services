using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Input;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump;

public sealed class ImportMusicBrainzDumpHandler(
    IReadMusicBrainzDumpPort readPort,
    IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> repository) : IHandler<ImportMusicBrainzDumpCommand>
{
    public async Task Handle(
        ImportMusicBrainzDumpCommand command,
        CancellationToken cancellationToken = default)
    {
        await foreach (var record in readPort.ReadAsync(
                           command.RecordingDumpPaths,
                           command.ReleaseDumpPaths,
                           cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(record.Title) || string.IsNullOrWhiteSpace(record.Artist))
            {
                continue;
            }

            var musicCatalogId = BuildMusicCatalogId(record);
            var loaded = await MusicTrack.LoadAsync(repository, musicCatalogId, cancellationToken);
            var commandId = BuildCommandId(record);

            loaded.Aggregate.MetadataFetched(
                new MusicCatalogMetadataFetched(
                    commandId,
                    musicCatalogId,
                    LookupSource.MusicBrainz,
                    LookupPriorityBand.High,
                    command.ImportedAtUtc,
                    new SongMetadata(
                        record.Title,
                        record.Artist,
                        record.Isrc,
                        record.MusicBrainzRecordingId,
                        record.DurationMs,
                        record.AlbumTitle,
                        record.ReleaseDate,
                        record.SourceArtistId,
                        record.SourceAlbumId),
                    [],
                    [],
                    new CatalogTrackHierarchy(
                        BuildArtistId(record),
                        BuildAlbumId(record)),
                    CorrelationId.From($"musicbrainz-dump:{record.SourceRecordKey}")));

            var append = await loaded.Aggregate.SaveAsync(repository, loaded.Stream, commandId, cancellationToken);
            if (!append.Appended || append.AppendedEvents.Count == 0)
            {
                continue;
            }
        }
    }

    private static MusicCatalogId BuildMusicCatalogId(MusicBrainzCatalogSeedRecord record)
    {
        var compactRecordingId = MusicIdentityText.NormalizeCompact(record.MusicBrainzRecordingId);
        if (!string.IsNullOrWhiteSpace(compactRecordingId))
        {
            return MusicCatalogId.From($"mc_track_{compactRecordingId}");
        }

        var compactTrackId = MusicIdentityText.NormalizeCompact(record.SourceTrackId);
        if (!string.IsNullOrWhiteSpace(compactTrackId))
        {
            return MusicCatalogId.From($"mc_track_{compactTrackId}");
        }

        return MusicCatalogId.From(
            $"mc_track_{MusicIdentityText.NormalizeCompact($"{record.Title}_{record.Artist}_{record.AlbumTitle}")}");
    }

    private static ArtistId? BuildArtistId(MusicBrainzCatalogSeedRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.SourceArtistId))
        {
            return ArtistId.From($"artist_{MusicIdentityText.NormalizeCompact(record.SourceArtistId)}");
        }

        var normalized = MusicIdentityText.NormalizeCompact(record.Artist);
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : ArtistId.From($"artist_{normalized}");
    }

    private static AlbumId? BuildAlbumId(MusicBrainzCatalogSeedRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.AlbumTitle) && string.IsNullOrWhiteSpace(record.SourceAlbumId))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(record.SourceAlbumId))
        {
            return AlbumId.From($"album_{MusicIdentityText.NormalizeCompact(record.SourceAlbumId)}");
        }

        return AlbumId.From(
            $"album_{MusicIdentityText.NormalizeCompact($"{record.AlbumTitle}_{record.Artist}")}");
    }

    private static CommandId BuildCommandId(MusicBrainzCatalogSeedRecord record) =>
        CommandId.For($"ImportMusicBrainzDump:{record.SourceRecordKey}");
}
