using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Wolverine;
using Wolverine.Logging;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest;

internal sealed class EnqueueMusicRequestTestEnvironment : IAsyncDisposable
{
    private readonly IHost host;
    private readonly IServiceScope? scope;
    private readonly Func<TimeSpan, Task<object>> waitForCapturedRequest;

    private EnqueueMusicRequestTestEnvironment(
        IHost host,
        IServiceScope? scope,
        IEnqueueMusicRequest enqueueMusicRequest,
        Func<TimeSpan, Task<object>> waitForCapturedRequest)
    {
        this.host = host;
        this.scope = scope;
        this.waitForCapturedRequest = waitForCapturedRequest;
        EnqueueMusicRequest = enqueueMusicRequest;
    }

    public IEnqueueMusicRequest EnqueueMusicRequest { get; }

    public static Task<EnqueueMusicRequestTestEnvironment> CreateAsync(
        EnqueueMusicRequestPortMode mode,
        bool configuredRoute)
    {
        return mode switch
        {
            EnqueueMusicRequestPortMode.InMemoryFake => Task.FromResult(CreateFake(configuredRoute)),
            EnqueueMusicRequestPortMode.WolverineLocal => CreateWolverineAsync(configuredRoute),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static EnqueueMusicRequestTestEnvironment CreateFake(bool configuredRoute)
    {
        var host = Host.CreateApplicationBuilder().Build();
        if (!configuredRoute)
        {
            return new EnqueueMusicRequestTestEnvironment(
                host,
                scope: null,
                new ThrowingEnqueueMusicRequest(),
                _ => Task.FromException<object>(new InvalidOperationException("No fake route configured.")));
        }

        var queue = new InMemoryEnqueueMusicRequest();
        return new EnqueueMusicRequestTestEnvironment(
            host,
            scope: null,
            queue,
            timeout => WaitForCapturedRequestAsync(queue, timeout));
    }

    private static async Task<EnqueueMusicRequestTestEnvironment> CreateWolverineAsync(bool configuredRoute)
    {
        var builder = Host.CreateApplicationBuilder();
        var capture = new LookupMusicRequestCapture();
        builder.Services.AddSingleton(capture);
        builder.Services.AddSingleton<IMessageTracker>(capture);

        if (configuredRoute)
        {
            builder.Environment.EnvironmentName = Environments.Development;
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
                ["ServiceBus:LookupMusicRequestsQueueName"] = "lookup-music-requests"
            });
            builder.Services.AddLookupMusicRequestQueue(builder.Configuration);
        }
        else
        {
            builder.Services.AddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        }

        builder.UseWolverine(opts =>
        {
            if (configuredRoute)
            {
                opts.UseApiServiceBusMessaging(builder.Configuration, builder.Environment);
                opts.LocalQueueFor<LookupMusicRequestDto>();
                if (builder.Environment.IsDevelopment())
                {
                    opts.StubAllExternalTransports();
                }
            }
            else
            {
                builder.Services.AddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
                opts.UseRuntimeCompilation();
            }

            opts.Discovery.DisableConventionalDiscovery();
            if (configuredRoute)
            {
                opts.Discovery.IncludeType<LookupMusicRequestCaptureHandler>();
            }
        });

        var host = builder.Build();
        await host.StartAsync();
        var scope = host.Services.CreateScope();
        return new EnqueueMusicRequestTestEnvironment(
            host,
            scope,
            scope.ServiceProvider.GetRequiredService<IEnqueueMusicRequest>(),
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

    public static LookupMusicRequest Request(string query) =>
        new(
            Query: query,
            TrustLevel: 2,
            RiskScore: 10,
            OccurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            CorrelationId: CorrelationId.New());

    private static async Task<object> WaitForCapturedRequestAsync(
        InMemoryEnqueueMusicRequest queue,
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

        throw new TimeoutException("LookupMusicRequest was not captured.");
    }
}
