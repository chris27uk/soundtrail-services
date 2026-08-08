using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Ports;

namespace Soundtrail.Services.Tests.Integration.Projector.OnWorkFeedbackChanged;

public sealed class RavenStoreDiscoveryFeedbackPortTests : IAsyncDisposable
{
    private readonly IDocumentStore documentStore = EmbeddedRavenTestServer.CreateDocumentStore();
    private readonly List<string> cleanupDocumentIds = [];

    [Fact]
    public async Task Given_Target_Is_Completed_When_A_Later_Attempt_Fails_Then_Completed_Status_Is_Preserved()
    {
        var target = Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-completed-sticky"));
        var documentId = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier);
        cleanupDocumentIds.Add(documentId);
        var subject = new RavenStoreDiscoveryFeedbackPort(documentStore);

        await subject.StoreAsync(
            new WorkCompleted(
                target,
                LookupPriorityBand.High,
                "Lookup completed.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        await subject.StoreAsync(
            new WorkAttemptFailed(
                target,
                "Track does not have an ISRC.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 1, TimeSpan.Zero)),
            CancellationToken.None);

        using var session = documentStore.OpenAsyncSession();
        var record = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(documentId);
        record.Status.Should().Be("completed");
        record.Reason.Should().Be("Lookup completed.");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var documentId in cleanupDocumentIds)
        {
            await EmbeddedRavenTestServer.DisposeAsync(documentStore, documentId);
        }

        documentStore.Dispose();
    }
}
