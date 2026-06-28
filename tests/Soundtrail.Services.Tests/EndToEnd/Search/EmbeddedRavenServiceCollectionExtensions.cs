using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Adapters.Registry.CompositionRoot;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

internal static class EmbeddedRavenServiceCollectionExtensions
{
    public static IServiceCollection AddEmbeddedRavenForTesting(
        this IServiceCollection services,
        IDocumentStore documentStore)
    {
        services.AddTypeTranslationsFromAssemblies(typeof(TypeTranslationRegistry).Assembly);
        services.TryAddSingleton(documentStore);
        services.TryAddScoped<IAsyncDocumentSession>(_ => documentStore.OpenAsyncSession());
        return services;
    }
}
