using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump;

public sealed class ImportMusicBrainzDumpHandler(
    IReadMusicBrainzDumpPort readPort,
    IMusicTrackEventRepository repository,
    ProjectMusicTrackCatalogHandler projectMusicTrackCatalogHandler) : IHandler<ImportMusicBrainzDumpCommand, ImportMusicBrainzDumpResult>
{
    public async Task<ImportMusicBrainzDumpResult> Handle(
        ImportMusicBrainzDumpCommand command,
        CancellationToken cancellationToken = default)
    {
        var processed = 0;
        var imported = 0;
        var projected = 0;
        var skipped = 0;

        await foreach (var record in readPort.ReadAsync(
                           command.RecordingDumpPaths,
                           command.ReleaseDumpPaths,
                           cancellationToken))
        {
            processed++;

            if (string.IsNullOrWhiteSpace(record.Title) || string.IsNullOrWhiteSpace(record.Artist))
            {
                skipped++;
                continue;
            }

            var musicCatalogId = BuildMusicCatalogId(record);
            var aggregate = await CatalogEntityAggregate.LoadAsync(repository, musicCatalogId, cancellationToken);
            var commandId = BuildCommandId(record);

            aggregate.RecordEnrichmentResponse(
                new EnrichmentResponse(
                    commandId,
                    musicCatalogId,
                    ProviderName.MusicBrainz,
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

            var append = await aggregate.SaveAsync(repository, commandId, cancellationToken);
            if (!append.Appended || append.AppendedEvents.Count == 0)
            {
                skipped++;
                continue;
            }

            imported++;

            if (!command.ProjectCatalogAfterImport)
            {
                continue;
            }

            var firstVersion = append.Version - append.AppendedEvents.Count + 1;
            await projectMusicTrackCatalogHandler.Handle(
                new(
                    musicCatalogId,
                    append.AppendedEvents
                        .Select((@event, index) => new VersionedMusicTrackEvent(firstVersion + index, @event))
                        .ToArray()),
                cancellationToken);
            projected++;
        }

        return new ImportMusicBrainzDumpResult(processed, imported, projected, skipped);
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
