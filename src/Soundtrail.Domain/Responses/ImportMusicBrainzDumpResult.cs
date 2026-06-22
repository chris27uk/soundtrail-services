namespace Soundtrail.Domain.Responses;

public sealed record ImportMusicBrainzDumpResult(
    int ProcessedRecordCount,
    int ImportedRecordCount,
    int SkippedRecordCount);
