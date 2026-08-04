namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

internal interface IProjectorHandler
{
    Task HandleAsync(IDomainEvent @event, CancellationToken cancellationToken);
}
