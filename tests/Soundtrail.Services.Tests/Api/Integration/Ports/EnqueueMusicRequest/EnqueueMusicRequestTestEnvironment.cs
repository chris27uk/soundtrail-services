using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Shared;
using Wolverine;
using Wolverine.Attributes;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest.WolverineLocal;

internal sealed class EnqueueMusicRequestTestEnvironment : IAsyncDisposable
{
    private readonly IHost host;
    private readonly LookupMusicRequestCapture capture;

    private EnqueueMusicRequestTestEnvironment(
        IHost host,
        LookupMusicRequestCapture capture)
    {
        this.host = host;
        this.capture = capture;
        EnqueueMusicRequest = host.Services.GetRequiredService<IEnqueueMusicRequest>();
    }

    public IEnqueueMusicRequest EnqueueMusicRequest { get; }

    public static async Task<EnqueueMusicRequestTestEnvironment> WithConfiguredRouteAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        var capture = new LookupMusicRequestCapture();

        builder.Services.AddSingleton(capture);
        builder.Services.AddSingleton<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        builder.UseWolverine(opts =>
        {
            opts.Discovery.DisableConventionalDiscovery();
            opts.Discovery.IncludeType<LookupMusicRequestCaptureHandler>();
        });

        var host = builder.Build();
        await host.StartAsync();
        return new EnqueueMusicRequestTestEnvironment(host, capture);
    }

    public static async Task<EnqueueMusicRequestTestEnvironment> WithoutConfiguredRouteAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        var capture = new LookupMusicRequestCapture();

        builder.Services.AddSingleton(capture);
        builder.Services.AddSingleton<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        builder.UseWolverine(opts =>
        {
            opts.Discovery.DisableConventionalDiscovery();
        });

        var host = builder.Build();
        await host.StartAsync();
        return new EnqueueMusicRequestTestEnvironment(host, capture);
    }

    public Task<LookupMusicRequest> WaitForCapturedRequestAsync(TimeSpan timeout) =>
        this.capture.WaitAsync(timeout);

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
