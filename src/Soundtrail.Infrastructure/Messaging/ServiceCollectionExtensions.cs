using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine;

namespace Soundtrail.Adapters.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWolverineCommandBus(this IServiceCollection services)
    {
        services.TryAddScoped<Soundtrail.Domain.Abstractions.ICommandBus>(
            sp => new WolverineCommandBus(sp.GetRequiredService<IMessageBus>()));
        return services;
    }
}
