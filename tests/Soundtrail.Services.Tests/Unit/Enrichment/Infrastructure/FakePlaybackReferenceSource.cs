using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class FakePlaybackReferenceSource : IPlaybackReferenceSource
{
    private readonly Dictionary<PlaybackReferenceLookupKey, IReadOnlyList<ExternalReference>> responses = [];

    public List<PlaybackReferenceLookupKey> Lookups { get; } = [];

    public Task<IReadOnlyList<ExternalReference>> GetPlaybackReferencesAsync(
        PlaybackReferenceLookupKey lookupKey,
        CancellationToken cancellationToken)
    {
        Lookups.Add(lookupKey);
        return Task.FromResult(
            responses.TryGetValue(lookupKey, out var references)
                ? references
                : (IReadOnlyList<ExternalReference>)[]);
    }

    public void Seed(PlaybackReferenceLookupKey lookupKey, params ExternalReference[] references) =>
        responses[lookupKey] = references;
}
