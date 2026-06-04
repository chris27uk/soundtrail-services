using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Shared;
using Wolverine;
using Wolverine.Attributes;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest.Contract;

public enum EnqueueMusicRequestPortMode
{
    InMemoryFake,
    WolverineLocal
}

internal sealed class EnqueueMusicRequestTestEnvironment : IAsyncDisposable
{
    private readonly IHost host;
    private readonly Func<TimeSpan, Task<LookupMusicRequest>> waitForCapturedRequest;

    private EnqueueMusicRequestTestEnvironment(
        IHost host,
        IEnqueueMusicRequest enqueueMusicRequest,
        Func<TimeSpan, Task<LookupMusicRequest>> waitForCapturedRequest)
    {
        this.host = host;
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
                new ThrowingEnqueueMusicRequest(),
                _ => Task.FromException<LookupMusicRequest>(new InvalidOperationException("No fake route configured.")));
        }

        var queue = new InMemoryEnqueueMusicRequest();
        return new EnqueueMusicRequestTestEnvironment(
            host,
            queue,
            timeout => WaitForCapturedRequestAsync(queue, timeout));
    }

    private static async Task<EnqueueMusicRequestTestEnvironment> CreateWolverineAsync(bool configuredRoute)
    {
        var builder = Host.CreateApplicationBuilder();
        var capture = new LookupMusicRequestCapture();

        builder.Services.AddSingleton(capture);
        builder.Services.AddSingleton<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
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
        return new EnqueueMusicRequestTestEnvironment(
            host,
            host.Services.GetRequiredService<IEnqueueMusicRequest>(),
            capture.WaitAsync);
    }

    public Task<LookupMusicRequest> WaitForCapturedRequestAsync(TimeSpan timeout) =>
        this.waitForCapturedRequest(timeout);

    public async ValueTask DisposeAsync()
    {
        await host.StopAsync();
        host.Dispose();
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

public sealed class LookupMusicRequestCapture
{
    private readonly TaskCompletionSource<LookupMusicRequest> received = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Record(LookupMusicRequest request) => this.received.TrySetResult(request);

    public async Task<LookupMusicRequest> WaitAsync(TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(this.received.Task, timeoutTask);
        if (completed == timeoutTask)
        {
            throw new TimeoutException("LookupMusicRequest was not captured.");
        }

        return await this.received.Task;
    }
}

public sealed class LookupMusicRequestCaptureHandler(LookupMusicRequestCapture capture)
{
    [WolverineHandler]
    public Task Handle(LookupMusicRequest request, CancellationToken cancellationToken)
    {
        capture.Record(request);
        return Task.CompletedTask;
    }
}

internal sealed class ThrowingEnqueueMusicRequest : IEnqueueMusicRequest
{
    public Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException("No route configured."));
}
