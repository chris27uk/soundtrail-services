using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.Scheduler;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Hosting;

namespace Soundtrail.Services.Tests.Unit.Scheduler.Infrastructure;

public sealed class SchedulerHostedServiceTests
{
    [Fact]
    public async Task Given_Multiple_Scheduler_Handlers_When_A_Tick_Runs_Then_All_Handlers_Are_Executed()
    {
        var first = new SchedulerHandlerSpy();
        var second = new SchedulerHandlerSpy();
        var provider = new ServiceCollection()
            .AddSingleton<ISchedulerHandler>(first)
            .AddSingleton<ISchedulerHandler>(second)
            .BuildServiceProvider();
        var hostedService = new TestSchedulerHostedService(
            new TestServiceScopeFactory(provider),
            Options.Create(new SchedulerOptions
            {
                RunIntervalSeconds = 60
            }),
            NullLogger<SchedulerHostedService>.Instance);

        await hostedService.RunOnce(CancellationToken.None);

        first.HandleCalls.Should().Be(1);
        second.HandleCalls.Should().Be(1);
    }

    private sealed class SchedulerHandlerSpy : ISchedulerHandler
    {
        public int HandleCalls { get; private set; }

        public Task Handle(CancellationToken cancellationToken = default)
        {
            HandleCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SchedulerOptions> options,
        ILogger<SchedulerHostedService> logger)
        : SchedulerHostedService(scopeFactory, options, logger)
    {
        public Task RunOnce(CancellationToken cancellationToken) => ExecuteOneIteration(cancellationToken);
    }

    private sealed class TestServiceScopeFactory(IServiceProvider provider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestServiceScope(provider);
    }

    private sealed class TestServiceScope(IServiceProvider provider) : IServiceScope, IAsyncDisposable
    {
        public IServiceProvider ServiceProvider => provider;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
