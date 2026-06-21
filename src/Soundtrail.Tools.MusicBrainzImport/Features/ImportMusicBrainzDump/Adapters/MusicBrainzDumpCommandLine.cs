using Soundtrail.Domain.Commands;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public static class MusicBrainzDumpCommandLine
{
    public static bool TryParse(
        string[] args,
        out ImportMusicBrainzDumpCommand? command,
        out string? error)
    {
        var recordingPaths = new List<string>();
        var releasePaths = new List<string>();
        var projectCatalog = false;

        for (var index = 0; index < args.Length; index++)
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

        command = new ImportMusicBrainzDumpCommand(
            recordingPaths,
            releasePaths,
            projectCatalog,
            DateTimeOffset.UtcNow);
        error = null;
        return true;
    }

    public static string Usage() =>
        """
        Usage:
          dotnet run --project src/Soundtrail.Tools.MusicBrainzImport -- --recording-dump <path> [--recording-dump <path>] [--release-dump <path>] [--project-now]

        Notes:
          - The importer expects extracted line-delimited MusicBrainz JSON dump files.
          - Release dumps are important because many recordings appear only inside release data.
          - The importer appends catalog events to the music-track event store.
          - Use --project-now to also rebuild catalog read models in-process for the imported streams.
        """;

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
