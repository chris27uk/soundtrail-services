namespace MusicResolver.Application.Ports;

public interface ITrackLookupPort
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
