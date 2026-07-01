using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Input;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump;

public sealed class ImportMusicBrainzDumpHandler(
    IReadMusicBrainzDumpPort readPort,
    IEventStreamRepository<ArtistId, IDomainEvent> repository) : IHandler<ImportMusicBrainzDumpCommand>
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
            var commandId = BuildCommandId(record);
            var artistId = BuildArtistId(record)
                           ?? throw new InvalidOperationException($"Unable to determine artist id for imported record '{record.SourceRecordKey}'.");
            var loaded = await ArtistCatalog.LoadAsync(repository, artistId, cancellationToken);

            loaded.Aggregate.TrackMetadataFetched(
                artistId,
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
                        artistId,
                        BuildAlbumId(record)),
                    CorrelationId.From($"musicbrainz-dump:{record.SourceRecordKey}")));

            await loaded.Aggregate.SaveAsync(repository, loaded.Stream, commandId, cancellationToken);
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
        => ArtistCatalogIdentity.ResolveArtistIdOrNull(record.SourceArtistId, record.Artist);

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
