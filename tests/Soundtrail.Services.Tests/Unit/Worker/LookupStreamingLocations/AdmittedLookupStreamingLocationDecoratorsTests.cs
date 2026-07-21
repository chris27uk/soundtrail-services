using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupStreamingLocations;

public sealed class AdmittedLookupStreamingLocationDecoratorsTests
{
    [Fact]
    public async Task Given_Isrc_Admission_Is_Acquired_When_Handling_Then_The_Inner_Handler_Is_Called_And_The_Admission_Is_Committed()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateIsrcRequest();
        var subject = environment.CreateIsrcAdmissionSubject();

        await subject.Handle(request, CancellationToken.None);

        environment.IsrcInnerHandler.Calls.Should().Be(1);
        environment.AdmissionPort.RequestedAdmission.Should().Be(
            new LookupExecutionAdmissionRequest(LookupSource.Odesli, request.Id, environment.Clock.UtcNow));
        environment.AdmissionPort.CommittedCommandIds.Should().Equal(request.Id);
    }

    [Fact]
    public async Task Given_Metadata_Admission_Is_Duplicate_When_Handling_Then_A_Duplicate_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateMetadataRequest();
        environment.AdmissionPort.Result = LookupExecutionAdmissionResult.Duplicate();
        var subject = environment.CreateMetadataAdmissionSubject();

        await Assert.ThrowsAsync<LookupExecutionShortCircuitException>(() => subject.Handle(request, CancellationToken.None));

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<Soundtrail.Domain.Discovery.Messages.CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Duplicate>().Subject;
        result.Reason.Should().Be("Lookup already executing.");
        environment.MetadataInnerHandler.Calls.Should().Be(0);
    }
}
