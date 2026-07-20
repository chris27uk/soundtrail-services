using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupPlaylistTracks;

public sealed class IdempotentLookupPlaylistTracksByProviderHandlerDecoratorTests
{
    [Fact]
    public async Task Given_The_Request_Has_Not_Been_Processed_When_Handling_Then_The_Inner_Handler_Is_Called_And_The_Receipt_Is_Completed()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        var subject = environment.CreateIdempotencySubject();
        var request = environment.CreateRequest();

        await subject.Handle(request);

        environment.InnerHandler.Calls.Should().Be(1);
        environment.ReceiptStore.TryBeginCommandIds.Should().Equal(request.Id);
        environment.ReceiptStore.CompletedCommandIds.Should().Equal(request.Id);
        environment.ReceiptStore.ReleasedCommandIds.Should().BeEmpty();
        environment.CommandBus.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_The_Request_Has_Already_Been_Processed_When_Handling_Then_A_Duplicate_Result_Is_Published()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        environment.ReceiptStore.TryBeginResult = false;
        var subject = environment.CreateIdempotencySubject();
        var request = environment.CreateRequest();

        await subject.Handle(request);

        environment.InnerHandler.Calls.Should().Be(0);
        environment.ReceiptStore.CompletedCommandIds.Should().BeEmpty();
        var message = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        message.RequestedAt.Should().Be(request.RequestedAt);
        message.CorrelationId.Should().Be(request.CorrelationId);
        var duplicate = message.Result.Should().BeOfType<LookupResult.Duplicate>().Subject;
        duplicate.CompletedAt.Should().Be(environment.Clock.UtcNow);
        duplicate.Reason.Should().Be("Lookup already completed.");
    }

    [Fact]
    public async Task Given_The_Inner_Handler_Short_Circuits_When_Handling_Then_The_Receipt_Is_Released_Without_Rethrowing()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        environment.InnerHandler.ExceptionToThrow = new LookupExecutionShortCircuitException();
        var subject = environment.CreateIdempotencySubject();
        var request = environment.CreateRequest();

        await subject.Handle(request);

        environment.ReceiptStore.CompletedCommandIds.Should().BeEmpty();
        environment.ReceiptStore.ReleasedCommandIds.Should().Equal(request.Id);
    }

    [Fact]
    public async Task Given_The_Inner_Handler_Fails_When_Handling_Then_The_Receipt_Is_Released_And_The_Exception_Is_Rethrown()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        environment.InnerHandler.ExceptionToThrow = new InvalidOperationException("boom");
        var subject = environment.CreateIdempotencySubject();
        var request = environment.CreateRequest();

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        environment.ReceiptStore.CompletedCommandIds.Should().BeEmpty();
        environment.ReceiptStore.ReleasedCommandIds.Should().Equal(request.Id);
    }
}
