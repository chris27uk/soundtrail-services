using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupPlaylistTracks;

public sealed class AdmittedLookupPlaylistTracksByProviderHandlerDecoratorIntegrationTests
{
    [Fact]
    public async Task Given_The_Redis_Admission_Port_When_Admission_Is_Acquired_Then_The_Inner_Handler_Is_Called()
    {
        await using var environment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForAdmissionAsync();
        var subject = environment.CreateAdmissionSubject();
        var request = environment.CreateRequest();

        await subject.Handle(request);

        environment.InnerHandler.Calls.Should().Be(1);
        environment.CommandBus.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_The_Redis_Admission_Port_When_The_Message_Is_Already_Active_Then_A_Duplicate_Result_Is_Published()
    {
        await using var environment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForAdmissionAsync();
        var subject = environment.CreateAdmissionSubject();
        var request = environment.CreateRequest();

        await environment.AdmissionPort!.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.Kworb, request.Id, environment.Clock.UtcNow),
            CancellationToken.None);

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<LookupExecutionShortCircuitException>();
        var message = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        message.RequestedAt.Should().Be(request.RequestedAt);
        message.CorrelationId.Should().Be(request.CorrelationId);
        message.Result.Should().BeOfType<LookupResult.Duplicate>();
    }

    [Fact]
    public async Task Given_The_Redis_Admission_Port_When_The_Source_Budget_Is_Exhausted_Then_A_Deferred_Result_Is_Published()
    {
        await using var environment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForAdmissionAsync();
        var subject = environment.CreateAdmissionSubject();
        var existing = environment.CreateRequest("msg-existing");
        var request = environment.CreateRequest("msg-deferred");

        await environment.AdmissionPort!.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.Kworb, existing.Id, environment.Clock.UtcNow),
            CancellationToken.None);

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<LookupExecutionShortCircuitException>();
        var message = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        var deferred = message.Result.Should().BeOfType<LookupResult.Deferred>().Subject;
        deferred.Reason.Should().Contain("budget temporarily unavailable");
    }

    [Fact]
    public async Task Given_The_Redis_Admission_Port_When_The_Inner_Handler_Fails_Then_The_Lease_Is_Released()
    {
        await using var environment = await LookupPlaylistTracksDecoratorIntegrationTestEnvironment.CreateForAdmissionAsync();
        environment.InnerHandler.ExceptionToThrow = new InvalidOperationException("boom");
        var subject = environment.CreateAdmissionSubject();
        var request = environment.CreateRequest();

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        var reacquire = await environment.AdmissionPort!.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.Kworb, request.Id, environment.Clock.UtcNow),
            CancellationToken.None);
        reacquire.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
    }
}
