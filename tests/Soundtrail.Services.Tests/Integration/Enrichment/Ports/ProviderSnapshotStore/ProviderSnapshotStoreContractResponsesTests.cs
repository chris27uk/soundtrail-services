using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters.Documents;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderSnapshotStore;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class ProviderSnapshotStoreContractResponsesTests
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Snapshot_For_The_Same_Provider_And_Track_When_Saved_Again_Then_It_Replaces_The_Previous_Value(SnapshotStoreMode mode)
    {
        await using var env = SnapshotStoreTestEnvironment.Create(mode);

        await env.SaveAsync(new ProviderSnapshot(
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
            "{\"value\":\"first\"}"));

        await env.SaveAsync(new ProviderSnapshot(
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 6, 12, 5, 0, TimeSpan.Zero),
            "{\"value\":\"second\"}"));

        var snapshot = await env.LoadAsync(MusicCatalogId.From("mc_track_1"), ProviderName.MusicBrainz);
        snapshot!.PayloadJson.Should().Be("{\"value\":\"second\"}");
    }

    public static IEnumerable<object[]> AllModes()
    {
        yield return [SnapshotStoreMode.InProcessFake];
        yield return [SnapshotStoreMode.RavenEmbedded];
    }

    public enum SnapshotStoreMode
    {
        InProcessFake,
        RavenEmbedded
    }

    private sealed class SnapshotStoreTestEnvironment : IAsyncDisposable
    {
        private readonly ProviderSnapshotStoreFake? fake;
        private readonly RavenEmbeddedTestDatabase? raven;

        private SnapshotStoreTestEnvironment(ProviderSnapshotStoreFake? fake, RavenEmbeddedTestDatabase? raven)
        {
            this.fake = fake;
            this.raven = raven;
        }

        public static SnapshotStoreTestEnvironment Create(SnapshotStoreMode mode) =>
            mode switch
            {
                SnapshotStoreMode.InProcessFake => new(new ProviderSnapshotStoreFake(), null),
                SnapshotStoreMode.RavenEmbedded => new(null, RavenEmbeddedTestDatabase.Create()),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

        public Task SaveAsync(ProviderSnapshot snapshot)
        {
            if (fake is not null)
            {
                return fake.SaveAsync(snapshot, CancellationToken.None);
            }

            return SaveRavenAsync(snapshot);
        }

        private async Task SaveRavenAsync(ProviderSnapshot snapshot)
        {
            using var session = raven!.Store.OpenAsyncSession();
            var store = new RavenProviderSnapshotStore(session);
            await store.SaveAsync(snapshot, CancellationToken.None);
            await session.SaveChangesAsync();
        }

        public async Task<ProviderSnapshot?> LoadAsync(MusicCatalogId musicCatalogId, ProviderName provider)
        {
            if (fake is not null)
            {
                fake.Snapshots.TryGetValue($"{musicCatalogId.Value}:{provider}", out var snapshot);
                return snapshot;
            }

            using var session = raven!.Store.OpenAsyncSession();
            var document = await session.LoadAsync<RavenProviderSnapshotDocument>(
                RavenProviderSnapshotDocument.GetDocumentId(musicCatalogId.Value, provider.ToString()));
            return document is null
                ? null
                : new ProviderSnapshot(
                    MusicCatalogId.From(document.MusicCatalogId),
                    ProviderName.From(document.Provider),
                    document.CapturedAt,
                    document.PayloadJson);
        }

        public ValueTask DisposeAsync()
        {
            raven?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
