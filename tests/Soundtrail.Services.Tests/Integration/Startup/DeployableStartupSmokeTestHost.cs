using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Startup;

internal sealed class DeployableStartupSmokeTestHost : IAsyncDisposable
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private readonly WebApplication app;
    private readonly RavenEmbeddedTestDatabase? raven;

    private DeployableStartupSmokeTestHost(
        WebApplication app,
        HttpClient client,
        RavenEmbeddedTestDatabase? raven)
    {
        this.app = app;
        Client = client;
        this.raven = raven;
    }

    public HttpClient Client { get; }

    public static async Task<DeployableStartupSmokeTestHost> StartAsync(
        string relativeProjectPath,
        Action<WebApplicationBuilder> configure,
        Action<WebApplication> map,
        bool useEmbeddedRaven)
    {
        RavenEmbeddedTestDatabase? raven = null;

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = Path.Combine(SolutionRoot, relativeProjectPath),
                EnvironmentName = Environments.Development
            });

            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;"
            });

            if (useEmbeddedRaven)
            {
                raven = RavenEmbeddedTestDatabase.Create();
                builder.Services.AddSingleton<IDocumentStore>(raven.Store);
                builder.Services.AddScoped<IAsyncDocumentSession>(_ => raven.Store.OpenAsyncSession());
            }

            configure(builder);

            var app = builder.Build();
            map(app);
            await app.StartAsync();

            return new DeployableStartupSmokeTestHost(app, app.GetTestClient(), raven);
        }
        catch
        {
            raven?.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
        raven?.Dispose();
    }
}
