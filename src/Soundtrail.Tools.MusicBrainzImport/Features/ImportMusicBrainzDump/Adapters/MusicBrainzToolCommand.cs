using Soundtrail.Domain.Commands;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public abstract record MusicBrainzToolCommand
{
    private MusicBrainzToolCommand()
    {
    }

    public sealed record Import(ImportMusicBrainzDumpCommand Command) : MusicBrainzToolCommand;

    public sealed record ReplayCatalog(ReplayCatalogProjectionCommand Command) : MusicBrainzToolCommand;
}
