using Soundtrail.Domain.Commands;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public static class MusicBrainzDumpCommandLine
{
    public static bool TryParse(
        string[] args,
        out MusicBrainzToolCommand? command,
        out string? error)
    {
        var offset = 0;
        var action = "import";

        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            action = args[0];
            offset = 1;
        }

        return action switch
        {
            "import" => TryParseImport(args, offset, out command, out error),
            "replay-catalog" => TryParseReplayCatalog(args, offset, out command, out error),
            _ => UnknownAction(action, out command, out error)
        };
    }

    public static string Usage() =>
        """
        Usage:
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- import --recording-dump <path> [--recording-dump <path>] [--release-dump <path>] [--project-now]
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- replay-catalog --all
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- replay-catalog --music-catalog-id <id> [--music-catalog-id <id>]

        Notes:
          - The importer expects extracted line-delimited MusicBrainz JSON dump files.
          - Release dumps are important because many recordings appear only inside release data.
          - The importer appends catalog events to the music-track event store.
          - Use --project-now to also rebuild catalog read models in-process for imported streams.
          - replay-catalog resets the catalog track document and replay checkpoint, then rebuilds from persisted events.
        """;

    private static bool TryParseImport(
        IReadOnlyList<string> args,
        int offset,
        out MusicBrainzToolCommand? command,
        out string? error)
    {
        var recordingPaths = new List<string>();
        var releasePaths = new List<string>();
        var projectCatalog = false;

        for (var index = offset; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--recording-dump":
                    if (!TryReadValue(args, ++index, out var recordingPath, out error))
                    {
                        command = null;
                        return false;
                    }

                    recordingPaths.Add(recordingPath);
                    break;
                case "--release-dump":
                    if (!TryReadValue(args, ++index, out var releasePath, out error))
                    {
                        command = null;
                        return false;
                    }

                    releasePaths.Add(releasePath);
                    break;
                case "--project-now":
                    projectCatalog = true;
                    break;
                case "--help":
                case "-h":
                    command = null;
                    error = null;
                    return false;
                default:
                    command = null;
                    error = $"Unknown argument '{args[index]}'.";
                    return false;
            }
        }

        if (recordingPaths.Count == 0 && releasePaths.Count == 0)
        {
            command = null;
            error = "At least one --recording-dump or --release-dump path is required.";
            return false;
        }

        command = new MusicBrainzToolCommand.Import(
            new ImportMusicBrainzDumpCommand(
                recordingPaths,
                releasePaths,
                projectCatalog,
                DateTimeOffset.UtcNow));
        error = null;
        return true;
    }

    private static bool TryParseReplayCatalog(
        IReadOnlyList<string> args,
        int offset,
        out MusicBrainzToolCommand? command,
        out string? error)
    {
        var replayAll = false;
        var musicCatalogIds = new List<MusicCatalogId>();

        for (var index = offset; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--all":
                    replayAll = true;
                    break;
                case "--music-catalog-id":
                    if (!TryReadValue(args, ++index, out var musicCatalogId, out error))
                    {
                        command = null;
                        return false;
                    }

                    musicCatalogIds.Add(MusicCatalogId.From(musicCatalogId));
                    break;
                case "--help":
                case "-h":
                    command = null;
                    error = null;
                    return false;
                default:
                    command = null;
                    error = $"Unknown argument '{args[index]}'.";
                    return false;
            }
        }

        if (!replayAll && musicCatalogIds.Count == 0)
        {
            command = null;
            error = "replay-catalog requires --all or at least one --music-catalog-id.";
            return false;
        }

        command = new MusicBrainzToolCommand.ReplayCatalog(
            new ReplayCatalogProjectionCommand(replayAll, musicCatalogIds));
        error = null;
        return true;
    }

    private static bool UnknownAction(
        string action,
        out MusicBrainzToolCommand? command,
        out string? error)
    {
        command = null;
        error = $"Unknown action '{action}'.";
        return false;
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        int index,
        out string value,
        out string? error)
    {
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
        {
            value = string.Empty;
            error = "A path value is required.";
            return false;
        }

        value = args[index];
        error = null;
        return true;
    }
}
