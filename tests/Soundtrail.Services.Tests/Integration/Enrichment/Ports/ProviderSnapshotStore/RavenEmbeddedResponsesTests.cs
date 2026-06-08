using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderSnapshotStore;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenEmbeddedResponsesTests
{
    [Fact]
    public async Task Given_A_Snapshot_For_The_Same_Provider_And_Track_When_Saved_Again_Then_It_Replaces_The_Previous_Value()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();

        using (var firstSession = raven.Store.OpenAsyncSession())
        {
            var store = CreateStore(firstSession);
            await store.SaveAsync(
                new ProviderSnapshot(
                    MusicCatalogId.From("mc_track_1"),
                    ProviderName.MusicBrainz,
                    new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
                    "{\"value\":\"first\"}"),
                CancellationToken.None);

            await firstSession.SaveChangesAsync();
        }

        using (var secondSession = raven.Store.OpenAsyncSession())
        {
            var store = CreateStore(secondSession);
            await store.SaveAsync(
                new ProviderSnapshot(
                    MusicCatalogId.From("mc_track_1"),
                    ProviderName.MusicBrainz,
                    new DateTimeOffset(2026, 6, 6, 12, 5, 0, TimeSpan.Zero),
                    "{\"value\":\"second\"}"),
                CancellationToken.None);

            await secondSession.SaveChangesAsync();
        }

        using var verificationSession = raven.Store.OpenAsyncSession();
        var document = await verificationSession.LoadAsync<RavenProviderSnapshotDocument>(
            "provider-snapshots/mc_track_1/MusicBrainz");

        document!.PayloadJson.Should().Be("{\"value\":\"second\"}");
    }

    private static IProviderSnapshotStore CreateStore(IAsyncDocumentSession session) =>
        new RavenProviderSnapshotStore(session);
}
