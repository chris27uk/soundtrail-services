using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Api;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Wolverine;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest;

internal sealed class EnqueueMusicRequestTestEnvironment : IAsyncDisposable
{
    private readonly IHost host;
    private readonly IServiceScope? scope;
    private readonly Func<TimeSpan, Task<LookupMusicRequest>> waitForCapturedRequest;

    private EnqueueMusicRequestTestEnvironment(
        IHost host,
        IServiceScope? scope,
        IEnqueueMusicRequest enqueueMusicRequest,
        Func<TimeSpan, Task<LookupMusicRequest>> waitForCapturedRequest)
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
                _ => Task.FromException<LookupMusicRequest>(new InvalidOperationException("No fake route configured.")));
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
        builder.Services.AddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        builder.UseWolverine(opts =>
        {
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
            capture.WaitAsync);
    }

    public Task<LookupMusicRequest> WaitForCapturedRequestAsync(TimeSpan timeout) =>
        this.waitForCapturedRequest(timeout);

    public async ValueTask DisposeAsync()
    {
        this.scope?.Dispose();
        await this.host.StopAsync();
        this.host.Dispose();
    }

    public static LookupMusicRequest Request(string query) =>
        new(
            Query: NormalizedSearchQuery.FromText(query),
            TrustLevel: 2,
            RiskScore: 10,
            OccurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            CorrelationId: CorrelationId.New());

    private static async Task<LookupMusicRequest> WaitForCapturedRequestAsync(
        InMemoryEnqueueMusicRequest queue,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            var request = await queue.DequeueAsync(cts.Token);
            if (request is not null)
            {
                return request;
            }

            await Task.Delay(10, cts.Token);
        }

        throw new TimeoutException("LookupMusicRequest was not captured.");
    }
}
