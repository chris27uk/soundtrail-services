using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;

internal sealed class ImportMusicBrainzDumpSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly IScheduledMessageHandler<ImportMusicBrainzDumpCommand> scheduledHandler;
    private readonly DownloadDumpAndShardWorkQueueFake downloadWorkQueue;
    private readonly ImportCatalogShardWorkQueueFake shardWorkQueue;

    private ImportMusicBrainzDumpSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        IScheduledMessageHandler<ImportMusicBrainzDumpCommand> scheduledHandler,
        MusicBrainzDumpImportJobStoreFake jobStore,
        MusicBrainzDumpArchiveStoreFake archiveStore,
        CatalogArtistImportWriterFake artistWriter,
        CatalogAlbumImportWriterFake albumWriter,
        CatalogTrackImportWriterFake trackWriter,
        DownloadDumpAndShardWorkQueueFake downloadWorkQueue,
        ImportCatalogShardWorkQueueFake shardWorkQueue,
        ICatalogImportLeaseOwner leaseOwner)
    {
        this.engine = engine;
        this.scheduledHandler = scheduledHandler;
        JobStore = jobStore;
        ArchiveStore = archiveStore;
        ArtistWriter = artistWriter;
        AlbumWriter = albumWriter;
        TrackWriter = trackWriter;
        this.downloadWorkQueue = downloadWorkQueue;
        this.shardWorkQueue = shardWorkQueue;
        LeaseOwner = leaseOwner;
    }

    public MusicBrainzDumpImportJobStoreFake JobStore { get; }

    public MusicBrainzDumpArchiveStoreFake ArchiveStore { get; }

    public CatalogArtistImportWriterFake ArtistWriter { get; }

    public CatalogAlbumImportWriterFake AlbumWriter { get; }

    public CatalogTrackImportWriterFake TrackWriter { get; }

    public ICatalogImportLeaseOwner LeaseOwner { get; }

    public IReadOnlyList<StartMusicBrainzDumpImport> SentStarts =>
        engine.MessagePump.SentMessages<StartMusicBrainzDumpImport>();

    public StartMusicBrainzDumpImport SentStart =>
        engine.MessagePump.SentMessage<StartMusicBrainzDumpImport>();

    public IReadOnlyList<ImportMusicBrainzDumpShard> SentShards =>
        engine.MessagePump.SentMessages<ImportMusicBrainzDumpShard>();

    public static ImportMusicBrainzDumpSociableTestEnvironment Create(DateTimeOffset utcNow = default) =>
        Compose(utcNow);

    public static ImportMusicBrainzDumpSociableTestEnvironment ForDumpContainingArtists(
        DateTimeOffset utcNow,
        params string[] artistsJsonlLines)
    {
        var environment = Compose(utcNow);
        environment.ArchiveStore.WithArtistsJsonl(artistsJsonlLines);
        return environment;
    }

    public static ImportMusicBrainzDumpSociableTestEnvironment ForDumpContainingArtistsAndAlbums(
        DateTimeOffset utcNow,
        IReadOnlyList<string> artistsJsonlLines,
        IReadOnlyList<string> releaseGroupsJsonlLines)
    {
        var environment = Compose(utcNow);
        environment.ArchiveStore.WithArtistsJsonl(artistsJsonlLines.ToArray());
        environment.ArchiveStore.WithReleaseGroupsJsonl(releaseGroupsJsonlLines.ToArray());
        return environment;
    }

    public static ImportMusicBrainzDumpSociableTestEnvironment ForDumpContainingArtistsAlbumsAndTracks(
        DateTimeOffset utcNow,
        IReadOnlyList<string> artistsJsonlLines,
        IReadOnlyList<string> releaseGroupsJsonlLines,
        IReadOnlyList<string> tracksJsonlLines)
    {
        var environment = Compose(utcNow);
        environment.ArchiveStore.WithArtistsJsonl(artistsJsonlLines.ToArray());
        environment.ArchiveStore.WithReleaseGroupsJsonl(releaseGroupsJsonlLines.ToArray());
        environment.ArchiveStore.WithTracksJsonl(tracksJsonlLines.ToArray());
        return environment;
    }

    public Task TriggerAsync(DateTimeOffset triggeredAt) =>
        scheduledHandler.HandleAsync(new ImportMusicBrainzDumpCommand(triggeredAt));

    public async Task TriggerStartOnlyAsync(DateTimeOffset triggeredAt)
    {
        await engine.MessagePump.ProjectOnChange(
            async handler =>
            {
                await handler.HandleAsync(new ImportMusicBrainzDumpCommand(triggeredAt));
                return true;
            },
            scheduledHandler);
    }

    public async Task TriggerAndProcessAsync(DateTimeOffset triggeredAt)
    {
        await TriggerStartOnlyAsync(triggeredAt);
        await DrainCatalogImportWorkAsync();
    }

    public async Task ProcessBusAndCatalogImportWorkAsync()
    {
        await engine.MessagePump.PumpAsync();
        await DrainCatalogImportWorkAsync();
    }

    public Task EnqueueShardAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        DateTimeOffset requestedAt) =>
        engine.Resolve<ICommandBus>().SendAsync(
            ImportMusicBrainzDumpShard.Create(jobId, phase, shardId, requestedAt));

    public async Task EnqueueShardAndProcessAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        DateTimeOffset requestedAt)
    {
        await EnqueueShardAsync(jobId, phase, shardId, requestedAt);
        await ProcessBusAndCatalogImportWorkAsync();
    }

    public async Task EnqueueShardAndProcessShardHandlersOnlyAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        DateTimeOffset requestedAt)
    {
        await EnqueueShardAsync(jobId, phase, shardId, requestedAt);
        await engine.MessagePump.PumpAsync();
        await DrainShardWorkAsync();
    }

    public MusicBrainzDumpImportJob RequireJob(MusicBrainzDumpImportJobId jobId) =>
        JobStore.Jobs.Single(job => job.Id == jobId);

    public void Dispose() => engine.Dispose();

    private static ImportMusicBrainzDumpSociableTestEnvironment Compose(DateTimeOffset utcNow)
    {
        var engine = SociableDiscoveryEngine.Create(utcNow);
        return new ImportMusicBrainzDumpSociableTestEnvironment(
            engine,
            engine.Resolve<IScheduledMessageHandler<ImportMusicBrainzDumpCommand>>(),
            engine.RequireFake<IMusicBrainzDumpImportJobStore, MusicBrainzDumpImportJobStoreFake>(),
            engine.RequireFake<IMusicBrainzDumpArchiveStore, MusicBrainzDumpArchiveStoreFake>(),
            engine.RequireFake<ICatalogArtistImportWriter, CatalogArtistImportWriterFake>(),
            engine.RequireFake<ICatalogAlbumImportWriter, CatalogAlbumImportWriterFake>(),
            engine.RequireFake<ICatalogTrackImportWriter, CatalogTrackImportWriterFake>(),
            engine.RequireFake<IDownloadDumpAndShardWorkQueue, DownloadDumpAndShardWorkQueueFake>(),
            engine.RequireFake<IImportCatalogShardWorkQueue, ImportCatalogShardWorkQueueFake>(),
            engine.Resolve<ICatalogImportLeaseOwner>());
    }

    private async Task DrainCatalogImportWorkAsync()
    {
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var downloadJob = engine.Resolve<IDownloadDumpAndShardJob>();
            var downloadWork = downloadWorkQueue.DequeueAll();
            foreach (var work in downloadWork)
            {
                await downloadJob.RunAsync(work.JobId);
            }

            await engine.MessagePump.PumpAsync();

            var shardWork = await DrainShardWorkAsync();

            await engine.MessagePump.PumpAsync();

            if (downloadWork.Count == 0 && shardWork == 0)
            {
                return;
            }
        }

        throw new InvalidOperationException("CatalogImport work did not drain.");
    }

    private async Task<int> DrainShardWorkAsync()
    {
        var shardJob = engine.Resolve<IImportCatalogShardJob>();
        var shardWork = shardWorkQueue.DequeueAll();
        foreach (var work in shardWork)
        {
            await shardJob.RunAsync(work.JobId, work.Phase, work.ShardId);
        }

        return shardWork.Count;
    }
}
