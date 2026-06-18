using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Wolverine;
using Wolverine.Logging;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue;

internal sealed class CatalogSearchAttemptQueueTestEnvironment : IAsyncDisposable
{
    private readonly IHost host;
    private readonly IServiceScope? scope;
    private readonly Func<TimeSpan, Task<object>> waitForCapturedRequest;

    private CatalogSearchAttemptQueueTestEnvironment(
        IHost host,
        IServiceScope? scope,
        IEnqueueCatalogSearchAttempt enqueueMusicRequest,
        Func<TimeSpan, Task<object>> waitForCapturedRequest)
    {
        this.host = host;
        this.scope = scope;
        this.waitForCapturedRequest = waitForCapturedRequest;
        CatalogSearchAttemptQueue = enqueueMusicRequest;
    }

    public IEnqueueCatalogSearchAttempt CatalogSearchAttemptQueue { get; }

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

        var queue = new TestInMemoryEnqueueCatalogSearchAttempt();
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
            builder.Services.AddScoped<IEnqueueCatalogSearchAttempt, WolverineEnqueueCatalogSearchAttempt>();
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
                builder.Services.AddScoped<IEnqueueCatalogSearchAttempt, WolverineEnqueueCatalogSearchAttempt>();
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
            scope.ServiceProvider.GetRequiredService<IEnqueueCatalogSearchAttempt>(),
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

    public static CatalogSearchAttempt Request(string query) =>
        new(
            Criteria: CatalogSearchCriteria.Search("track", NormalizedSearchQuery.FromText(query).Value),
            Query: query,
            TrustLevel: 2,
            RiskScore: 10,
            OccurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            CorrelationId: CorrelationId.New());

    private static async Task<object> WaitForCapturedRequestAsync(
        TestInMemoryEnqueueCatalogSearchAttempt queue,
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

    private sealed class TestInMemoryEnqueueCatalogSearchAttempt : IEnqueueCatalogSearchAttempt
    {
        private readonly Queue<CatalogSearchAttemptDto> requests = new();

        public List<CatalogSearchAttemptDto> Requests => requests.ToList();

        public Task EnqueueAsync(CatalogSearchAttempt request, CancellationToken cancellationToken)
        {
            requests.Enqueue(CatalogSearchAttemptMapper.ToDto(request));
            return Task.CompletedTask;
        }
    }
}
