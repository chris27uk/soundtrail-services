using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.EventSourcing;

public sealed class EventHandlersTests
{
    [Fact]
    public void Given_Multiple_Sync_Handlers_For_The_Same_Event_When_Handling_Then_All_Handlers_Run()
    {
        var handlers = new EventHandlers();
        var executions = new List<string>();

        handlers.Register<TestDomainEvent>(_ => executions.Add("first"));
        handlers.Register<TestDomainEvent>(_ => executions.Add("second"));

        handlers.Handle(new TestDomainEvent());

        executions.Should().Equal("first", "second");
    }

    [Fact]
    public async Task Given_Sync_And_Async_Handlers_For_The_Same_Event_When_Handling_Asynchronously_Then_All_Handlers_Run()
    {
        var handlers = new EventHandlers();
        var executions = new List<string>();

        handlers.Register<TestDomainEvent>(_ => executions.Add("sync"));
        handlers.RegisterAsync<TestDomainEvent>((_, _) =>
        {
            executions.Add("async");
            return Task.CompletedTask;
        });

        await handlers.HandleAsync(new TestDomainEvent());

        executions.Should().Equal("sync", "async");
    }

    [Fact]
    public void Given_Async_Handlers_For_An_Event_When_Handling_Synchronously_Then_An_Error_Is_Thrown()
    {
        var handlers = new EventHandlers();
        handlers.RegisterAsync<TestDomainEvent>((_, _) => Task.CompletedTask);

        var act = () => handlers.Handle(new TestDomainEvent());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*async handlers are registered*");
    }

    private sealed record TestDomainEvent : IDomainEvent;
}
