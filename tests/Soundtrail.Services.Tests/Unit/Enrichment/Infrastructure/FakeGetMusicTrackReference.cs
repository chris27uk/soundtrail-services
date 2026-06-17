using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class FakeGetMusicTrackReference : IGetMusicTrackReference
{
    private readonly Dictionary<MusicSearchTerm, IReadOnlyList<ExternalReference>> responses = [];

    public List<MusicSearchTerm> SearchTerms { get; } = [];

    public Task<IReadOnlyList<ExternalReference>> GetReferenceToMusicTrack(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken)
    {
        SearchTerms.Add(searchTerm);
        return Task.FromResult(
            responses.TryGetValue(searchTerm, out var references)
                ? references
                : (IReadOnlyList<ExternalReference>)[]);
    }

    public void Seed(MusicSearchTerm lookupKey, params ExternalReference[] references) =>
        responses[lookupKey] = references;
}
