using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.ServiceDefaults;

namespace Soundtrail.Adapters.Persistence;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddRavenDocumentStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));

        services.TryAddSingleton<IDocumentStore>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RavenDbOptions>>().Value;
                var store = new DocumentStore
                {
                    Urls = options.Urls,
                    Database = options.Database,
                    Conventions = new DocumentConventions
                    {
                        FindCollectionName = type => type.Name
                    }
                };

                return store.Initialize();
            });
        services.TryAddScoped<IAsyncDocumentSession>(sp => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RavenDatabaseHostedService>());
        services.AddStartupValidation(
            "raven-document-store",
            (serviceProvider, cancellationToken) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RavenDbOptions>>().Value;
                if (options.Urls.Length == 0 || options.Urls.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidOperationException("RavenDb:Urls must contain at least one URL.");
                }

                if (string.IsNullOrWhiteSpace(options.Database))
                {
                    throw new InvalidOperationException("RavenDb:Database is not configured.");
                }

                _ = serviceProvider.GetRequiredService<IDocumentStore>();

                using var scope = serviceProvider.CreateScope();
                _ = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
                return Task.CompletedTask;
            });
        return services;
    }
}
