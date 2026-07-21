using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Enrichment.Worker.Shared.Execution;

public static class LookupHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddLookupHandlerPipeline<TMessage, TMetadata>(
        this IServiceCollection services,
        Func<IServiceProvider, IHandler<TMessage>> businessFactory)
        where TMessage : class, IMessage
        where TMetadata : class, ILookupDecoratorMetadata<TMessage>
    {
        services.AddScoped<IHandler<TMessage>>(businessFactory);
        services.AddScoped<ILookupDecoratorMetadata<TMessage>, TMetadata>();
        services.Decorate<IHandler<TMessage>, AdmittedLookupHandlerDecorator<TMessage>>();
        services.Decorate<IHandler<TMessage>, IdempotentLookupHandlerDecorator<TMessage>>();
        return services;
    }
}
