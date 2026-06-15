using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.DiscoveryStatusStore;

internal sealed class DiscoveryStatusStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Func<string, Task<DiscoveryStatusRecordDto?>> load;

    private DiscoveryStatusStoreTestEnvironment(
        IUpsertDiscoveryStatusPort port,
        Func<string, Task<DiscoveryStatusRecordDto?>> load,
        RavenEmbeddedTestDatabase? raven)
    {
        Port = port;
        this.load = load;
        this.raven = raven;
    }

    public IUpsertDiscoveryStatusPort Port { get; }

    public static DiscoveryStatusStoreTestEnvironment Create(DiscoveryStatusStoreMode mode) =>
        mode switch
        {
            DiscoveryStatusStoreMode.InProcessFake => CreateFake(),
            DiscoveryStatusStoreMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public Task<DiscoveryStatusRecordDto?> LoadAsync(string queryKey) => load(queryKey);

    public void Dispose() => raven?.Dispose();

    private static DiscoveryStatusStoreTestEnvironment CreateFake()
    {
        var fake = new InMemoryUpsertDiscoveryStatus();
        return new DiscoveryStatusStoreTestEnvironment(
            fake,
            queryKey => Task.FromResult(fake.Updates.TryGetValue(queryKey, out var update)
                ? ToDto(update)
                : null),
            raven: null);
    }

    private static DiscoveryStatusStoreTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new DiscoveryStatusStoreTestEnvironment(
            new RavenUpsertDiscoveryStatus(raven.Store),
            async queryKey =>
            {
                using var session = raven.Store.OpenAsyncSession();
                return await session.LoadAsync<DiscoveryStatusRecordDto>(
                    DiscoveryStatusRecordDto.GetDocumentId(queryKey),
                    CancellationToken.None);
            },
            raven);
    }

    private static DiscoveryStatusRecordDto ToDto(DiscoveryStatusUpdate update) =>
        new()
        {
            Id = DiscoveryStatusRecordDto.GetDocumentId(update.QueryKey.Value),
            QueryKey = update.QueryKey.Value,
            Status = update.Status.ToString(),
            Priority = update.Priority?.ToString() ?? string.Empty,
            WillBeLookedUp = update.WillBeLookedUp,
            EstimatedRetryAfterSeconds = update.EstimatedRetryAfterSeconds,
            EarliestExpectedCompletionAt = update.EarliestExpectedCompletionAt,
            Reason = update.Reason,
            UpdatedAt = update.UpdatedAt
        };
}
