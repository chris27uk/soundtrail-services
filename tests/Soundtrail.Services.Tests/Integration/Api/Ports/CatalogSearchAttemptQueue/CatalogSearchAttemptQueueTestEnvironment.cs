using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Translators.Registry;
using Wolverine;
using Wolverine.Logging;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue;

internal sealed class CatalogSearchAttemptQueueTestEnvironment : IAsyncDisposable
{
    private readonly IHost host;
    private readonly IServiceScope? scope;
    private readonly Func<TimeSpan, Task<object>> waitForCapturedRequest;

    private CatalogSearchAttemptQueueTestEnvironment(
        IHost host,
        IServiceScope? scope,
        ICommandBus commandBus,
        Func<TimeSpan, Task<object>> waitForCapturedRequest)
    {
        this.host = host;
        this.scope = scope;
        this.waitForCapturedRequest = waitForCapturedRequest;
        CommandBus = commandBus;
    }

    public ICommandBus CommandBus { get; }

    public static Task<CatalogSearchAttemptQueueTestEnvironment> CreateAsync(
        CatalogSearchAttemptQueuePortMode mode,
        bool configuredRoute)
    {
        return mode switch
        {
            CatalogSearchAttemptQueuePortMode.InMemoryFake => Task.FromResult(CreateFake(configuredRoute)),
            CatalogSearchAttemptQueuePortMode.WolverineLocal => CreateWolverineAsync(configuredRoute),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static CatalogSearchAttemptQueueTestEnvironment CreateFake(bool configuredRoute)
    {
        var host = Host.CreateApplicationBuilder().Build();
        if (!configuredRoute)
        {
            return new CatalogSearchAttemptQueueTestEnvironment(
                host,
                scope: null,
                new ThrowingCatalogSearchAttemptQueue(),
                _ => Task.FromException<object>(new InvalidOperationException("No fake route configured.")));
        }

        var queue = new TestInMemoryCommandBus();
        return new CatalogSearchAttemptQueueTestEnvironment(
            host,
            scope: null,
            queue,
            timeout => WaitForCapturedRequestAsync(queue, timeout));
    }

    private static async Task<CatalogSearchAttemptQueueTestEnvironment> CreateWolverineAsync(bool configuredRoute)
    {
        var builder = Host.CreateApplicationBuilder();
        var capture = new CatalogSearchAttemptCapture();
        builder.Services.AddSingleton(capture);
        builder.Services.AddSingleton<IMessageTracker>(capture);

        if (configuredRoute)
        {
            builder.Environment.EnvironmentName = Environments.Development;
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
                ["ServiceBus:CatalogSearchAttemptsQueueName"] = "lookup-music-requests"
            });
            builder.Services.AddCatalogSearchAttemptQueue(builder.Configuration);
        }
        else
        {
            builder.Services.AddScoped<ICommandBus, WolverineCommandBus>();
        }

        builder.UseWolverine(opts =>
        {
            if (configuredRoute)
            {
                opts.UseApiServiceBusMessaging(builder.Configuration, builder.Environment);
                opts.LocalQueueFor<CatalogSearchAttemptDto>();
                if (builder.Environment.IsDevelopment())
                {
                    opts.StubAllExternalTransports();
                }
            }
            else
            {
                opts.UseRuntimeCompilation();
            }

            opts.Discovery.DisableConventionalDiscovery();
            if (configuredRoute)
            {
                opts.Discovery.IncludeType<CatalogSearchAttemptCaptureHandler>();
            }
        });

        var host = builder.Build();
        await host.StartAsync();
        var scope = host.Services.CreateScope();
        return new CatalogSearchAttemptQueueTestEnvironment(
            host,
            scope,
            scope.ServiceProvider.GetRequiredService<ICommandBus>(),
            async timeout => await capture.WaitAsync(timeout));
    }

    public Task<object> WaitForCapturedRequestAsync(TimeSpan timeout) =>
        this.waitForCapturedRequest(timeout);

    public async ValueTask DisposeAsync()
    {
        this.scope?.Dispose();
        await this.host.StopAsync();
        this.host.Dispose();
    }

    public static SearchCatalogRequested Request(string query) =>
        new(
            SearchCriteria: MusicSearchCriteria.ByQuery(query, SearchTypesFilter.Tracks),
            Playback: PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            TrustLevel: 2,
            RiskScore: 10,
            OccurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            CorrelationId: CorrelationId.New());

    private static async Task<object> WaitForCapturedRequestAsync(
        TestInMemoryCommandBus queue,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            var request = queue.Requests.SingleOrDefault();
            if (request is not null)
            {
                return request;
            }

            await Task.Delay(10, cts.Token);
        }

        throw new TimeoutException("CatalogSearchAttempt was not captured.");
    }

    private sealed class TestInMemoryCommandBus : ICommandBus
    {
        private readonly Queue<CatalogSearchAttemptDto> requests = new();

        public List<CatalogSearchAttemptDto> Requests => requests.ToList();

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            requests.Enqueue(TypeTranslationRegistry.Default.Translate<CatalogSearchAttemptDto>((SearchCatalogRequested)command));
            return Task.CompletedTask;
        }
    }
}
