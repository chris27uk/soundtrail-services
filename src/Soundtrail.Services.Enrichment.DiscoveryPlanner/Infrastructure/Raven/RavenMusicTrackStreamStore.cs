using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;

public sealed class RavenMusicTrackStreamStore(
    IAsyncDocumentSession session) : IMusicTrackEventRepository
{
    public async Task<MusicTrackStream> LoadEventsAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenMusicTrackStreamDocument.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<RavenMusicTrackStreamDocument>(documentId, cancellationToken);
        return document is null
            ? new MusicTrackStream(0, [])
            : document.ToDomain();
    }

    public async Task<AppendMusicTrackStreamResult> AppendEventsAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<IMusicTrackEvent> events,
        CancellationToken cancellationToken)
    {
        session.Advanced.UseOptimisticConcurrency = true;
        var documentId = RavenMusicTrackStreamDocument.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<RavenMusicTrackStreamDocument>(documentId, cancellationToken)
            ?? new RavenMusicTrackStreamDocument
            {
                Id = documentId,
                MusicCatalogId = musicCatalogId.Value
            };

        if (document.AppliedCommandIds.Contains(commandId.Value))
        {
            return new AppendMusicTrackStreamResult(false, document.Version, []);
        }

        if (document.Version != expectedVersion)
        {
            throw new MusicTrackStreamConcurrencyException(musicCatalogId, expectedVersion, document.Version);
        }

        document.AppliedCommandIds.Add(commandId.Value);
        document.Facts.AddRange(events.Select(x => x.ToDocument()));
        document.Version += events.Count;

        await session.StoreAsync(document, cancellationToken);

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            var applied = await WasCommandAppliedByAnotherSessionAsync(documentId, commandId, cancellationToken);
            if (applied)
            {
                return new AppendMusicTrackStreamResult(false, expectedVersion, []);
            }

            throw;
        }

        return new AppendMusicTrackStreamResult(true, document.Version, events.ToArray());
    }

    private async Task<bool> WasCommandAppliedByAnotherSessionAsync(
        string documentId,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var store = session.Advanced.DocumentStore;
        using var verificationSession = store.OpenAsyncSession();
        var document = await verificationSession.LoadAsync<RavenMusicTrackStreamDocument>(documentId, cancellationToken);
        return document?.AppliedCommandIds.Contains(commandId.Value) == true;
    }
}

public sealed class MusicTrackStreamConcurrencyException(
    MusicCatalogId musicCatalogId,
    int expectedVersion,
    int actualVersion)
    : InvalidOperationException(
        $"MusicTrack stream for '{musicCatalogId.Value}' expected version {expectedVersion} but found {actualVersion}.");
