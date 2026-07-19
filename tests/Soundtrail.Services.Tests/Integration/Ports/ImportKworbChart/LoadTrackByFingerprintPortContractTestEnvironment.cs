using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Ports;
using Soundtrail.Services.Tests.Integration.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

internal sealed class LoadTrackByFingerprintPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly string? databaseName;

    private LoadTrackByFingerprintPortContractTestEnvironment(
        ILoadTrackByFingerprintPort subject,
        TrackMatchFingerprint fingerprint,
        IDocumentStore? documentStore = null,
        string? databaseName = null)
    {
        Subject = subject;
        Fingerprint = fingerprint;
        this.documentStore = documentStore;
        this.databaseName = databaseName;
    }

    public ILoadTrackByFingerprintPort Subject { get; }

    public TrackMatchFingerprint Fingerprint { get; }

    public static async Task<LoadTrackByFingerprintPortContractTestEnvironment> ForExistingTrack(
        LoadTrackByFingerprintPortImplementation implementation,
        TrackMatchFingerprint? fingerprint = null,
        string? trackId = null)
    {
        var resolvedFingerprint = fingerprint ?? TrackMatchFingerprint.FromArtistAndTitle("Artist 1801", "Track 1801");
        var trackIdValue = trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("track-1801");

        return implementation switch
        {
            LoadTrackByFingerprintPortImplementation.Fake => new LoadTrackByFingerprintPortContractTestEnvironment(
                new LoadTrackByFingerprintPortFake(TrackId.From(trackIdValue)),
                resolvedFingerprint),
            LoadTrackByFingerprintPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedFingerprint,
                new CatalogTrackMatchFingerprintRecordDto
                {
                    Id = CatalogTrackMatchFingerprintRecordDto.GetDocumentId(resolvedFingerprint.Value),
                    Fingerprint = resolvedFingerprint.Value,
                    TrackId = trackIdValue
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<LoadTrackByFingerprintPortContractTestEnvironment> ForMissingTrack(
        LoadTrackByFingerprintPortImplementation implementation,
        TrackMatchFingerprint? fingerprint = null)
    {
        var resolvedFingerprint = fingerprint ?? TrackMatchFingerprint.FromArtistAndTitle("Artist 1802", "Track 1802");

        return implementation switch
        {
            LoadTrackByFingerprintPortImplementation.Fake => new LoadTrackByFingerprintPortContractTestEnvironment(
                new LoadTrackByFingerprintPortFake(),
                resolvedFingerprint),
            LoadTrackByFingerprintPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedFingerprint),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<LoadTrackByFingerprintPortContractTestEnvironment> ForNullFingerprint(
        LoadTrackByFingerprintPortImplementation implementation)
    {
        return implementation switch
        {
            LoadTrackByFingerprintPortImplementation.Fake => new LoadTrackByFingerprintPortContractTestEnvironment(
                new LoadTrackByFingerprintPortFake(),
                default),
            LoadTrackByFingerprintPortImplementation.Raven => await CreateRavenEnvironmentAsync(default),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        return EmbeddedRavenTestServer.DisposeAsync(documentStore, databaseName);
    }

    private static async Task<LoadTrackByFingerprintPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        TrackMatchFingerprint fingerprint,
        CatalogTrackMatchFingerprintRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new LoadTrackByFingerprintPortContractTestEnvironment(
            new RavenLoadTrackByFingerprintPort(store),
            fingerprint,
            store,
            existingRecord?.Id);
    }
}

public enum LoadTrackByFingerprintPortImplementation
{
    Fake,
    Raven
}
