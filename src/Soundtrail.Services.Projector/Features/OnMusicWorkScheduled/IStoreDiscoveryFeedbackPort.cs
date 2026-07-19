using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicWorkScheduled;

public interface IStoreDiscoveryFeedbackPort
{
    Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken);
}
