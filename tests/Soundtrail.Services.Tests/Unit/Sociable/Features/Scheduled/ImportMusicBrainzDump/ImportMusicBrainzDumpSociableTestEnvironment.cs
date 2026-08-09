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
        DownloadDumpAndShardWorkQueueFake downloadWorkQueue,
        ImportCatalogShardWorkQueueFake shardWorkQueue,
        ICatalogImportLeaseOwner leaseOwner)
    {
        this.engine = engine;
        this.scheduledHandler = scheduledHandler;
        JobStore = jobStore;
        this.downloadWorkQueue = downloadWorkQueue;
        this.shardWorkQueue = shardWorkQueue;
        LeaseOwner = leaseOwner;
    }

    public MusicBrainzDumpImportJobStoreFake JobStore { get; }

    public ICatalogImportLeaseOwner LeaseOwner { get; }

    public IReadOnlyList<StartMusicBrainzDumpImport> SentStarts =>
        engine.MessagePump.SentMessages<StartMusicBrainzDumpImport>();

    public StartMusicBrainzDumpImport SentStart =>
        engine.MessagePump.SentMessage<StartMusicBrainzDumpImport>();

    public IReadOnlyList<ImportMusicBrainzDumpShard> SentShards =>
        engine.MessagePump.SentMessages<ImportMusicBrainzDumpShard>();

    public static ImportMusicBrainzDumpSociableTestEnvironment Create(DateTimeOffset utcNow = default)
    {
        var engine = SociableDiscoveryEngine.Create(utcNow);
        return new ImportMusicBrainzDumpSociableTestEnvironment(
            engine,
            engine.Resolve<IScheduledMessageHandler<ImportMusicBrainzDumpCommand>>(),
            engine.RequireFake<IMusicBrainzDumpImportJobStore, MusicBrainzDumpImportJobStoreFake>(),
            engine.RequireFake<IDownloadDumpAndShardWorkQueue, DownloadDumpAndShardWorkQueueFake>(),
            engine.RequireFake<IImportCatalogShardWorkQueue, ImportCatalogShardWorkQueueFake>(),
            engine.Resolve<ICatalogImportLeaseOwner>());
    }

    public Task TriggerAsync(DateTimeOffset triggeredAt) =>
        scheduledHandler.HandleAsync(new ImportMusicBrainzDumpCommand(triggeredAt));

    public async Task TriggerAndProcessAsync(DateTimeOffset triggeredAt)
    {
        await engine.MessagePump.ProjectOnChange(
            async handler =>
            {
                await handler.HandleAsync(new ImportMusicBrainzDumpCommand(triggeredAt));
                return true;
            },
            scheduledHandler);
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

    public MusicBrainzDumpImportJob RequireJob(MusicBrainzDumpImportJobId jobId) =>
        JobStore.Jobs.Single(job => job.Id == jobId);

    public void Dispose() => engine.Dispose();

    private async Task DrainCatalogImportWorkAsync()
    {
        var downloadJob = engine.Resolve<IDownloadDumpAndShardJob>();
        foreach (var work in downloadWorkQueue.DequeueAll())
        {
            await downloadJob.RunAsync(work.JobId);
        }

        var shardJob = engine.Resolve<IImportCatalogShardJob>();
        foreach (var work in shardWorkQueue.DequeueAll())
        {
            await shardJob.RunAsync(work.JobId, work.Phase, work.ShardId);
        }
    }
}
