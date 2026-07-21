using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupPlaylistTracks;

public sealed class AdmittedLookupPlaylistTracksByProviderHandlerDecoratorTests
{
    [Fact]
    public async Task Given_Admission_Is_Acquired_When_Handling_Then_The_Inner_Handler_Is_Called_And_The_Admission_Is_Committed()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        var subject = environment.CreateAdmissionSubject();
        var request = environment.CreateRequest();

        await subject.Handle(request);

        environment.InnerHandler.Calls.Should().Be(1);
        environment.AdmissionPort.RequestedAdmission.Should().Be(
            new LookupExecutionAdmissionRequest(LookupSource.Kworb, request.Id, environment.Clock.UtcNow));
        environment.AdmissionPort.CommittedCommandIds.Should().Equal(request.Id);
        environment.AdmissionPort.ReleasedCommandIds.Should().BeEmpty();
        environment.CommandBus.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Admission_Is_Duplicate_When_Handling_Then_A_Duplicate_Result_Is_Published_And_Execution_Is_Short_Circuited()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        environment.AdmissionPort.Result = LookupExecutionAdmissionResult.Duplicate();
        var subject = environment.CreateAdmissionSubject();
        var request = environment.CreateRequest();

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<LookupExecutionShortCircuitException>();
        environment.InnerHandler.Calls.Should().Be(0);
        var message = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        message.RequestedAt.Should().Be(request.RequestedAt);
        message.CorrelationId.Should().Be(request.CorrelationId);
        var duplicate = message.Result.Should().BeOfType<LookupResult.Duplicate>().Subject;
        duplicate.CompletedAt.Should().Be(environment.Clock.UtcNow);
        duplicate.Reason.Should().Be("Lookup already executing.");
    }

    [Fact]
    public async Task Given_Admission_Is_Deferred_When_Handling_Then_A_Deferred_Result_Is_Published_And_Execution_Is_Short_Circuited()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        var retryAt = environment.Clock.UtcNow.AddMinutes(5);
        environment.AdmissionPort.Result = LookupExecutionAdmissionResult.Deferred(retryAt, "Rate limited.");
        var subject = environment.CreateAdmissionSubject();
        var request = environment.CreateRequest();

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<LookupExecutionShortCircuitException>();
        environment.InnerHandler.Calls.Should().Be(0);
        var message = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        message.RequestedAt.Should().Be(request.RequestedAt);
        message.CorrelationId.Should().Be(request.CorrelationId);
        var deferred = message.Result.Should().BeOfType<LookupResult.Deferred>().Subject;
        deferred.CompletedAt.Should().Be(environment.Clock.UtcNow);
        deferred.DeferredUntil.Should().Be(retryAt);
        deferred.Reason.Should().Be("Rate limited.");
    }

    [Fact]
    public async Task Given_The_Inner_Handler_Fails_When_Handling_Then_The_Admission_Is_Released_And_The_Exception_Is_Rethrown()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        environment.InnerHandler.ExceptionToThrow = new InvalidOperationException("boom");
        var subject = environment.CreateAdmissionSubject();
        var request = environment.CreateRequest();

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        environment.AdmissionPort.CommittedCommandIds.Should().BeEmpty();
        environment.AdmissionPort.ReleasedCommandIds.Should().Equal(request.Id);
    }
}
