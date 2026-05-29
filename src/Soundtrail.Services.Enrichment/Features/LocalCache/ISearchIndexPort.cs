namespace Soundtrail.Services.Enrichment.Features.LocalCache;

public interface ISearchIndexPort
{
    Task UpsertAsync(
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
