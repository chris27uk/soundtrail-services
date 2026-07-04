using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Operations;

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
            "replay-discovery-lifecycle" => TryParseReplayDiscoveryLifecycle(args, offset, out command, out error),
            "rebuild-all" => TryParseRebuildAllReadModels(args, offset, out command, out error),
            _ => UnknownAction(action, out command, out error)
        };
    }

    public static string Usage() =>
        """
        Usage:
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- import --recording-dump <path> [--recording-dump <path>] [--release-dump <path>]
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- replay-catalog
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- replay-discovery-lifecycle
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- rebuild-all

        Notes:
          - The importer expects extracted line-delimited MusicBrainz JSON dump files.
          - Release dumps are important because many recordings appear only inside release data.
          - Import appends new catalog events when source data changes; projections update automatically from events.
          - replay-catalog rebuilds the entire catalog projection model from persisted events after projection logic changes or repair scenarios.
          - replay-discovery-lifecycle rebuilds the entire discovery lifecycle projection model from persisted events after projection logic changes or repair scenarios.
          - rebuild-all clears planner operational state and rebuilds all persisted read models from events for operational recovery.
        """;

    private static bool TryParseImport(
        IReadOnlyList<string> args,
        int offset,
        out MusicBrainzToolCommand? command,
        out string? error)
    {
        var recordingPaths = new List<string>();
        var releasePaths = new List<string>();
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
        if (offset < args.Count)
        {
            switch (args[offset])
            {
                case "--help":
                case "-h":
                    command = null;
                    error = null;
                    return false;
                default:
                    command = null;
                    error = $"Unknown argument '{args[offset]}'.";
                    return false;
            }
        }

        command = new MusicBrainzToolCommand.ReplayCatalog(
            new ReplayCatalogProjectionCommand());
        error = null;
        return true;
    }

    private static bool TryParseReplayDiscoveryLifecycle(
        IReadOnlyList<string> args,
        int offset,
        out MusicBrainzToolCommand? command,
        out string? error)
    {
        if (offset < args.Count)
        {
            switch (args[offset])
            {
                case "--help":
                case "-h":
                    command = null;
                    error = null;
                    return false;
                default:
                    command = null;
                    error = $"Unknown argument '{args[offset]}'.";
                    return false;
            }
        }

        command = new MusicBrainzToolCommand.ReplayDiscoveryLifecycle(
            new ReplayDiscoveryLifecycleProjectionBatchCommand());
        error = null;
        return true;
    }

    private static bool TryParseRebuildAllReadModels(
        IReadOnlyList<string> args,
        int offset,
        out MusicBrainzToolCommand? command,
        out string? error)
    {
        if (offset < args.Count)
        {
            switch (args[offset])
            {
                case "--help":
                case "-h":
                    command = null;
                    error = null;
                    return false;
                default:
                    command = null;
                    error = $"Unknown argument '{args[offset]}'.";
                    return false;
            }
        }

        command = new MusicBrainzToolCommand.RebuildAllReadModels(new RebuildAllReadModelsCommand());
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
