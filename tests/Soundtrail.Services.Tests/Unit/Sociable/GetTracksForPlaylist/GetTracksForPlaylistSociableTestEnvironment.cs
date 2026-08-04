using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

internal sealed class HandlerCollection
{
    private readonly IEnumerable<IProjectorHandler> handlers;

    public HandlerCollection(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        handlers = serviceProvider.GetServices<IProjectorHandler>();
    }

    public async Task HandleAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
