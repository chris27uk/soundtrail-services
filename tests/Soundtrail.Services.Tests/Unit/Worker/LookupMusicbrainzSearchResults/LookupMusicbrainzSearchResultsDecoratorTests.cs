using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzSearchResults;

public sealed class LookupMusicbrainzSearchResultsDecoratorTests
{
    [Fact]
    public async Task Given_Admission_Is_Acquired_When_Handling_Then_The_Inner_Handler_Is_Called_And_The_Admission_Is_Committed()
    {
        var environment = LookupMusicbrainzSearchResultsUnitTestEnvironment.Create();
        var request = environment.CreateRequest();
        var subject = environment.CreateAdmissionSubject();

        await subject.Handle(request, CancellationToken.None);

        environment.InnerHandler.Calls.Should().Be(1);
        environment.AdmissionPort.RequestedAdmission.Should().Be(
            new LookupExecutionAdmissionRequest(LookupSource.MusicBrainz, request.Id, environment.Clock.UtcNow));
        environment.AdmissionPort.CommittedCommandIds.Should().Equal(request.Id);
    }

    [Fact]
    public async Task Given_A_Previously_Completed_Search_When_Handling_Then_A_Duplicate_Result_Is_Published()
    {
        var environment = LookupMusicbrainzSearchResultsUnitTestEnvironment.Create();
        var request = environment.CreateRequest();
        environment.ReceiptStore.TryBeginResult = false;
        var subject = environment.CreateIdempotencySubject();

        await subject.Handle(request, CancellationToken.None);

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<Soundtrail.Domain.Discovery.Messages.CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Duplicate>().Subject;
        result.Reason.Should().Be("Lookup already completed.");
    }

    [Fact]
    public async Task Given_Admission_Is_Deferred_When_Handling_Then_A_Deferred_Result_Is_Published()
    {
        var environment = LookupMusicbrainzSearchResultsUnitTestEnvironment.Create();
        var request = environment.CreateRequest();
        var retryAt = environment.Clock.UtcNow.AddMinutes(2);
        environment.AdmissionPort.Result = LookupExecutionAdmissionResult.Deferred(retryAt, "Rate limited.");
        var subject = environment.CreateAdmissionSubject();

        await Assert.ThrowsAsync<LookupExecutionShortCircuitException>(() => subject.Handle(request, CancellationToken.None));

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<Soundtrail.Domain.Discovery.Messages.CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Deferred>().Subject;
        result.DeferredUntil.Should().Be(retryAt);
    }
}
