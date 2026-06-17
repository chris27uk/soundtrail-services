using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

internal static class EmbeddedRavenServiceCollectionExtensions
{
    public static IServiceCollection AddEmbeddedRavenForTesting(
        this IServiceCollection services,
        IDocumentStore documentStore)
    {
        services.TryAddSingleton(documentStore);
        services.TryAddScoped<IAsyncDocumentSession>(_ => documentStore.OpenAsyncSession());
        return services;
    }
}
