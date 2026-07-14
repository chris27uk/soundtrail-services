using Raven.Client.Documents;
using Raven.Embedded;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Adapters;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

internal sealed class LoadTrackByFingerprintPortContractTestEnvironment : IAsyncDisposable
{
    private static int serverStarted;
    private readonly IDocumentStore? documentStore;

    private LoadTrackByFingerprintPortContractTestEnvironment(
        ILoadTrackByFingerprintPort subject,
        TrackMatchFingerprint fingerprint,
        IDocumentStore? documentStore = null)
    {
        Subject = subject;
        Fingerprint = fingerprint;
        this.documentStore = documentStore;
    }

    public ILoadTrackByFingerprintPort Subject { get; }

    public TrackMatchFingerprint Fingerprint { get; }

    public static async Task<LoadTrackByFingerprintPortContractTestEnvironment> ForExistingTrack(
        LoadTrackByFingerprintPortImplementation implementation,
        TrackMatchFingerprint? fingerprint = null,
        string trackId = "track-1801")
    {
        var resolvedFingerprint = fingerprint ?? TrackMatchFingerprint.FromArtistAndTitle("Artist 1801", "Track 1801");

        return implementation switch
        {
            LoadTrackByFingerprintPortImplementation.Fake => new LoadTrackByFingerprintPortContractTestEnvironment(
                new LoadTrackByFingerprintPortFake(TrackId.From(trackId)),
                resolvedFingerprint),
            LoadTrackByFingerprintPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedFingerprint,
                new CatalogTrackMatchFingerprintRecordDto
                {
                    Id = CatalogTrackMatchFingerprintRecordDto.GetDocumentId(resolvedFingerprint.Value),
                    Fingerprint = resolvedFingerprint.Value,
                    TrackId = trackId
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
        documentStore?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<LoadTrackByFingerprintPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        TrackMatchFingerprint fingerprint,
        CatalogTrackMatchFingerprintRecordDto? existingRecord = null)
    {
        EnsureEmbeddedServerStarted();
        var store = EmbeddedServer.Instance.GetDocumentStore($"soundtrail-services-tests-{Guid.NewGuid():N}");

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new LoadTrackByFingerprintPortContractTestEnvironment(
            new RavenLoadTrackByFingerprintPort(store),
            fingerprint,
            store);
    }

    private static void EnsureEmbeddedServerStarted()
    {
        if (Interlocked.Exchange(ref serverStarted, 1) == 1)
        {
            return;
        }

        try
        {
            EmbeddedServer.Instance.StartServer();
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already started", StringComparison.OrdinalIgnoreCase))
        {
        }
    }
}

public enum LoadTrackByFingerprintPortImplementation
{
    Fake,
    Raven
}
