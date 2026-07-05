using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public abstract record MusicBrainzToolCommand
{
    private MusicBrainzToolCommand()
    {
    }

    public sealed record Import(ImportMusicBrainzDumpCommand Command) : MusicBrainzToolCommand;

    public sealed record ReplayCatalog(ReplayCatalogProjectionCommand Command) : MusicBrainzToolCommand;

    public sealed record ReplayDiscoveryLifecycle(ReplayDiscoveryLifecycleProjectionBatchCommand Command) : MusicBrainzToolCommand;

    public sealed record RebuildAllReadModels(RebuildAllReadModelsCommand Command) : MusicBrainzToolCommand;
}
