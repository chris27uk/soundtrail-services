using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzBrowse;

public sealed class LookupMusicbrainzBrowseDecoratorTests
{
    [Fact]
    public async Task Given_Artist_Albums_Admission_Is_Acquired_When_Handling_Then_The_Inner_Handler_Is_Called()
    {
        var environment = LookupMusicbrainzBrowseUnitTestEnvironment.Create();
        var request = environment.CreateArtistAlbumsRequest();
        var subject = environment.CreateArtistAlbumsAdmissionSubject();

        await subject.Handle(request, CancellationToken.None);

        environment.ArtistAlbumsInnerHandler.Calls.Should().Be(1);
        environment.AdmissionPort.RequestedAdmission.Should().Be(
            new LookupExecutionAdmissionRequest(LookupSource.MusicBrainz, request.Id, environment.Clock.UtcNow));
    }

    [Fact]
    public async Task Given_Artist_Tracks_Admission_Is_Duplicate_When_Handling_Then_A_Duplicate_Result_Is_Published()
    {
        var environment = LookupMusicbrainzBrowseUnitTestEnvironment.Create();
        var request = environment.CreateArtistTracksRequest();
        environment.AdmissionPort.Result = LookupExecutionAdmissionResult.Duplicate();
        var subject = environment.CreateArtistTracksAdmissionSubject();

        await Assert.ThrowsAsync<LookupExecutionShortCircuitException>(() => subject.Handle(request, CancellationToken.None));

        environment.CommandBus.Messages.Single()
            .Should().BeOfType<Soundtrail.Domain.Discovery.Messages.CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Duplicate>();
    }

    [Fact]
    public async Task Given_Album_Tracks_Were_Previously_Completed_When_Handling_Then_A_Duplicate_Result_Is_Published()
    {
        var environment = LookupMusicbrainzBrowseUnitTestEnvironment.Create();
        var request = environment.CreateAlbumTracksRequest();
        environment.ReceiptStore.TryBeginResult = false;
        var subject = environment.CreateAlbumTracksIdempotencySubject();

        await subject.Handle(request, CancellationToken.None);

        environment.CommandBus.Messages.Single()
            .Should().BeOfType<Soundtrail.Domain.Discovery.Messages.CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Duplicate>();
    }
}
