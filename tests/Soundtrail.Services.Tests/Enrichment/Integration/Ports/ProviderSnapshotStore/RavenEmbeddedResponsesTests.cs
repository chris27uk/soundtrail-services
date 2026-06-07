using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ProviderSnapshotStore;

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
        var document = await LoadInternalDocumentAsync(
            verificationSession,
            RavenProviderSnapshotDocumentType,
            "provider-snapshots/mc_track_1/MusicBrainz");

        RavenProviderSnapshotDocumentType
            .GetProperty("PayloadJson", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(document)
            .Should().Be("{\"value\":\"second\"}");
    }

    private static readonly Type RavenProviderSnapshotDocumentType = typeof(RavenRankedMusicCandidateStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.Documents.RavenProviderSnapshotDocument", throwOnError: true)!;

    private static readonly Type RavenProviderSnapshotStoreType = typeof(RavenRankedMusicCandidateStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.RavenProviderSnapshotStore", throwOnError: true)!;

    private static async Task<object?> LoadInternalDocumentAsync(
        IAsyncDocumentSession session,
        Type documentType,
        string id)
    {
        var method = typeof(IAsyncDocumentSession)
            .GetMethods()
            .Single(x =>
                x.Name == nameof(IAsyncDocumentSession.LoadAsync)
                && x.IsGenericMethodDefinition
                && x.GetParameters() is var parameters
                && parameters.Length == 2
                && parameters[0].ParameterType == typeof(string)
                && parameters[1].ParameterType == typeof(CancellationToken))
            .MakeGenericMethod(documentType);

        var task = (Task)method.Invoke(session, [id, CancellationToken.None])!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    private static IProviderSnapshotStore CreateStore(IAsyncDocumentSession session) =>
        (IProviderSnapshotStore)Activator.CreateInstance(
            RavenProviderSnapshotStoreType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: [session],
            culture: null)!;
}
