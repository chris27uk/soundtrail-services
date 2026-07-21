using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupPlaylistTracks;

public sealed class IdempotentLookupPlaylistTracksByProviderHandlerDecoratorIntegrationTests
{
    [Fact]
    public async Task Given_The_Raven_Receipt_Store_When_The_Message_Is_New_Then_The_Receipt_Is_Completed()
    {
        await using var environment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForIdempotencyAsync();
        var subject = await environment.CreateIdempotencySubjectAsync();
        var request = environment.CreateRequest();

        await subject.Handle(request);
        await environment.SaveReceiptChangesAsync();

        environment.InnerHandler.Calls.Should().Be(1);
        environment.CommandBus.Messages.Should().BeEmpty();
        var receipt = await environment.LoadReceiptAsync(request.Id);
        receipt.Should().NotBeNull();
        receipt!.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_The_Raven_Receipt_Store_When_The_Message_Was_Already_Completed_Then_A_Duplicate_Result_Is_Published()
    {
        var requestId = MessageId.For("msg-raven-duplicate");

        await using var firstEnvironment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForIdempotencyAsync();
        var firstSubject = await firstEnvironment.CreateIdempotencySubjectAsync();
        await firstSubject.Handle(firstEnvironment.CreateRequest(requestId.Value));
        await firstEnvironment.SaveReceiptChangesAsync();

        await using var environment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForIdempotencyAsync();
        var subject = await environment.CreateIdempotencySubjectAsync();
        var request = environment.CreateRequest(requestId.Value);

        await subject.Handle(request);

        environment.InnerHandler.Calls.Should().Be(0);
        var message = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        message.RequestedAt.Should().Be(request.RequestedAt);
        message.CorrelationId.Should().Be(request.CorrelationId);
        message.Result.Should().BeOfType<LookupResult.Duplicate>();
    }

    [Fact]
    public async Task Given_The_Raven_Receipt_Store_When_The_Inner_Handler_Fails_Then_The_Receipt_Is_Released()
    {
        await using var environment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForIdempotencyAsync();
        environment.InnerHandler.ExceptionToThrow = new InvalidOperationException("boom");
        var subject = await environment.CreateIdempotencySubjectAsync();
        var request = environment.CreateRequest();

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        await environment.SaveReceiptChangesAsync();
        var receipt = await environment.LoadReceiptAsync(request.Id);
        receipt.Should().BeNull();
    }
}
