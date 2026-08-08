using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Orchestrator;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Projection;

public sealed class HandlerCollectionRegistrationTests
{
    [Fact]
    public void Given_Discovered_Message_Types_Without_Di_Registrations_When_Resolving_Then_Throws_For_Empty_Collection()
    {
        var services = new ServiceCollection();
        HandlerCollection.AddMessageHandlersFromAssemblies(services, typeof(OrchestratorAssemblyMarker));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<HandlerCollection>();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*zero registered handlers*");
    }

    [Fact]
    public async Task Given_Unregistered_Discovered_Types_When_Building_Then_Soft_Skips_And_Keeps_Registered_Handlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<IHandler<RegisteredMessage>, RegisteredHandler>();
        using var provider = services.BuildServiceProvider();

        var collection = new HandlerCollection();
        HandlerCollectionRegistrar.RegisterMessageHandler<MissingMessage>(collection, provider);
        HandlerCollectionRegistrar.RegisterMessageHandler<RegisteredMessage>(collection, provider);

        var missing = () => collection.HandleAsync(new MissingMessage(), CancellationToken.None);
        await missing.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*MissingMessage*");

        await collection.HandleAsync(new RegisteredMessage(), CancellationToken.None);
    }

    [Fact]
    public async Task Given_Message_Types_Added_After_Initial_Registration_When_Resolving_Then_Accumulated_Types_Remain_Visible()
    {
        var services = new ServiceCollection();
        services.AddScoped<IHandler<RegisteredMessage>, RegisteredHandler>();
        HandlerCollection.AddMessageHandlersFromAssemblies(services, typeof(OrchestratorAssemblyMarker));

        var state = services
            .Select(static descriptor => descriptor.ImplementationInstance)
            .OfType<HandlerCollectionRegistrationState>()
            .Single();
        state.AddMessageTypes([typeof(RegisteredMessage)]);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var collection = scope.ServiceProvider.GetRequiredService<HandlerCollection>();

        await collection.HandleAsync(new RegisteredMessage(), CancellationToken.None);
    }

    private sealed record RegisteredMessage;

    private sealed record MissingMessage;

    private sealed class RegisteredHandler : IHandler<RegisteredMessage>
    {
        public Task Handle(IncomingMessage<RegisteredMessage> context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
