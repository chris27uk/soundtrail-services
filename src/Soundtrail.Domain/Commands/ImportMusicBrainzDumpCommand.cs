namespace Soundtrail.Domain.Commands;

public sealed record ImportMusicBrainzDumpCommand(
    IReadOnlyList<string> RecordingDumpPaths,
    IReadOnlyList<string> ReleaseDumpPaths,
    DateTimeOffset ImportedAtUtc);
