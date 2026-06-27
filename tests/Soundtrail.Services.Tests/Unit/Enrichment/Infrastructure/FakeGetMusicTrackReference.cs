using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class FakeGetMusicTrackReference : IGetMusicTrackReference
{
    private readonly Dictionary<MusicSearchCriteria, IReadOnlyList<ExternalReference>> responses = [];
    private Exception? exception;

    public List<MusicSearchCriteria> SearchTerms { get; } = [];

    public Task<IReadOnlyList<ExternalReference>> GetReferenceToMusicTrack(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        if (exception is not null)
        {
            throw exception;
        }

        SearchTerms.Add(searchCriteria);
        return Task.FromResult(
            responses.TryGetValue(searchCriteria, out var references)
                ? references
                : (IReadOnlyList<ExternalReference>)[]);
    }

    public void Seed(MusicSearchCriteria lookupKey, params ExternalReference[] references) =>
        responses[lookupKey] = references;

    public void Throw(Exception ex) => exception = ex;
}
