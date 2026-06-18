using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchStatusStore;

internal sealed class CatalogSearchStatusStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Func<string, Task<CatalogSearchStatusRecordDto?>> load;

    private CatalogSearchStatusStoreTestEnvironment(
        IUpsertCatalogSearchStatusPort port,
        Func<string, Task<CatalogSearchStatusRecordDto?>> load,
        RavenEmbeddedTestDatabase? raven)
    {
        Port = port;
        this.load = load;
        this.raven = raven;
    }

    public IUpsertCatalogSearchStatusPort Port { get; }

    public static CatalogSearchStatusStoreTestEnvironment Create(CatalogSearchStatusStoreMode mode) =>
        mode switch
        {
            CatalogSearchStatusStoreMode.InProcessFake => CreateFake(),
            CatalogSearchStatusStoreMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public Task<CatalogSearchStatusRecordDto?> LoadAsync(string criteria) => load(criteria);

    public void Dispose() => raven?.Dispose();

    private static CatalogSearchStatusStoreTestEnvironment CreateFake()
    {
        var fake = new InMemoryUpsertCatalogSearchStatus();
        return new CatalogSearchStatusStoreTestEnvironment(
            fake,
            criteria => Task.FromResult(fake.Updates.TryGetValue(criteria, out var update)
                ? ToDto(update)
                : null),
            raven: null);
    }

    private static CatalogSearchStatusStoreTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new CatalogSearchStatusStoreTestEnvironment(
            new RavenUpsertCatalogSearchStatus(raven.Store),
            async criteria =>
            {
                using var session = raven.Store.OpenAsyncSession();
                return await session.LoadAsync<CatalogSearchStatusRecordDto>(
                    CatalogSearchStatusRecordDto.GetDocumentId(criteria),
                    CancellationToken.None);
            },
            raven);
    }

    private static CatalogSearchStatusRecordDto ToDto(CatalogSearchStatusUpdate update) =>
        new()
        {
            Id = CatalogSearchStatusRecordDto.GetDocumentId(update.Criteria.Value),
            Criteria = update.Criteria.Value,
            Status = update.Status.ToString(),
            Priority = update.Priority?.ToString() ?? string.Empty,
            WillBeLookedUp = update.WillBeLookedUp,
            EstimatedRetryAfterSeconds = update.EstimatedRetryAfterSeconds,
            EarliestExpectedCompletionAt = update.EarliestExpectedCompletionAt,
            Reason = update.Reason,
            UpdatedAt = update.UpdatedAt
        };
}
