namespace Soundtrail.Services.Features.Resolve.Contracts;

public interface ITrackLookupPort
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
