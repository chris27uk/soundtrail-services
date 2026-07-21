using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupStreamingLocations;

public sealed class IdempotentLookupStreamingLocationDecoratorsTests
{
    [Fact]
    public async Task Given_Isrc_Lookup_Was_Previously_Completed_When_Handling_Then_A_Duplicate_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateIsrcRequest();
        environment.ReceiptStore.TryBeginResult = false;
        var subject = environment.CreateIsrcIdempotencySubject();

        await subject.Handle(request, CancellationToken.None);

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<Soundtrail.Domain.Discovery.Messages.CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Duplicate>().Subject;
        result.Reason.Should().Be("Lookup already completed.");
        environment.IsrcInnerHandler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Given_Metadata_Lookup_Throws_When_Handling_Then_The_Receipt_Is_Released()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateMetadataRequest();
        environment.MetadataInnerHandler.ExceptionToThrow = new InvalidOperationException("Boom");
        var subject = environment.CreateMetadataIdempotencySubject();

        var action = () => subject.Handle(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Boom");
        environment.ReceiptStore.ReleasedCommandIds.Should().Equal(request.Id);
        environment.ReceiptStore.CompletedCommandIds.Should().BeEmpty();
    }
}
