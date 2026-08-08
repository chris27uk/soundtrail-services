using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Ports;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Integration.GetTracksForPlaylist.Projector.Ports.StoreDiscoveryFeedback;

internal sealed class StoreDiscoveryFeedbackPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly StoreDiscoveryFeedbackPortFake? fake;
    private readonly List<string> cleanupDocumentIds = [];

    private StoreDiscoveryFeedbackPortContractTestEnvironment(
        IStoreDiscoveryFeedbackPort subject,
        IDocumentStore? documentStore,
        StoreDiscoveryFeedbackPortFake? fake)
    {
        Subject = subject;
        this.documentStore = documentStore;
        this.fake = fake;
    }

    public IStoreDiscoveryFeedbackPort Subject { get; }

    public static StoreDiscoveryFeedbackPortContractTestEnvironment Create(
        StoreDiscoveryFeedbackPortImplementation implementation) =>
        implementation switch
        {
            StoreDiscoveryFeedbackPortImplementation.Fake => CreateFake(),
            StoreDiscoveryFeedbackPortImplementation.Raven => CreateRaven(),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    public EnrichmentTarget PlaylistTarget(string playlistName = "world_top_100") =>
        new EnrichmentTarget.KnownCatalogItemOperation(
            new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId.FromPlaylistName(playlistName)));

    public async Task<CatalogDiscoveryFeedbackRecordDto?> LoadAsync(EnrichmentTarget target)
    {
        cleanupDocumentIds.Add(CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier));

        if (fake is not null)
        {
            var feedback = fake.Read(target.NormalisedIdentifier);
            return feedback is null
                ? null
                : new CatalogDiscoveryFeedbackRecordDto
                {
                    Id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier),
                    TargetId = target.NormalisedIdentifier,
                    Status = feedback.Status,
                    Priority = feedback.Priority.ToString(),
                    NextEligibleAtUtc = feedback.NextEligibleAt,
                    EarliestExpectedCompletionAtUtc = feedback.EarliestExpectedCompletionAt,
                    Reason = feedback.Reason,
                    UpdatedAtUtc = feedback.UpdatedAtUtc
                };
        }

        using var session = documentStore!.OpenAsyncSession();
        return await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(
            CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier));
    }

    public async ValueTask DisposeAsync()
    {
        if (documentStore is null)
        {
            return;
        }

        foreach (var documentId in cleanupDocumentIds.Distinct(StringComparer.Ordinal))
        {
            await EmbeddedRavenTestServer.DisposeAsync(documentStore, documentId);
        }
    }

    private static StoreDiscoveryFeedbackPortContractTestEnvironment CreateFake()
    {
        var fake = new StoreDiscoveryFeedbackPortFake();
        return new StoreDiscoveryFeedbackPortContractTestEnvironment(fake, null, fake);
    }

    private static StoreDiscoveryFeedbackPortContractTestEnvironment CreateRaven()
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();
        return new StoreDiscoveryFeedbackPortContractTestEnvironment(
            new RavenStoreDiscoveryFeedbackPort(store),
            store,
            fake: null);
    }
}

public enum StoreDiscoveryFeedbackPortImplementation
{
    Fake,
    Raven
}
