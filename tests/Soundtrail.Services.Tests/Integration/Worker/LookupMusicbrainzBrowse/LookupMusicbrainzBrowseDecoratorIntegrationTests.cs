using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Tests.Integration.Worker.Shared;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupMusicbrainzBrowse;

public sealed class LookupMusicbrainzBrowseDecoratorIntegrationTests
{
    [Fact]
    public async Task Given_A_Duplicate_Artist_Albums_Command_When_Handling_Then_The_Duplicate_Does_Not_Consume_Extra_Budget()
    {
        await using var environment = await LookupExecutionAdmissionDecoratorIntegrationTestEnvironment.CreateAsync();
        var subject = new AdmittedLookupMusicbrainzArtistAlbumsHandlerDecorator(
            new ArtistAlbumsInnerHandler(),
            environment.CommandBus,
            environment.AdmissionPort,
            environment.Clock);
        var request = new LookupMusicbrainzArtistAlbumsMessage(
            MessageId.For("lookup:musicbrainz-artist-albums:artist-a"),
            CorrelationId.From("corr:artist-a"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            ArtistId.From("artist-a"));
        var other = new LookupMusicbrainzArtistAlbumsMessage(
            MessageId.For("lookup:musicbrainz-artist-albums:artist-b"),
            CorrelationId.From("corr:artist-b"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            ArtistId.From("artist-b"));

        await AssertDuplicateDoesNotConsumeExtraBudget(
            environment,
            request,
            other,
            subject);
    }

    [Fact]
    public async Task Given_A_Duplicate_Artist_Tracks_Command_When_Handling_Then_The_Duplicate_Does_Not_Consume_Extra_Budget()
    {
        await using var environment = await LookupExecutionAdmissionDecoratorIntegrationTestEnvironment.CreateAsync();
        var subject = new AdmittedLookupMusicbrainzArtistTracksHandlerDecorator(
            new ArtistTracksInnerHandler(),
            environment.CommandBus,
            environment.AdmissionPort,
            environment.Clock);
        var request = new LookupMusicbrainzArtistTracksMessage(
            MessageId.For("lookup:musicbrainz-artist-tracks:artist-a"),
            CorrelationId.From("corr:artist-a"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            ArtistId.From("artist-a"));
        var other = new LookupMusicbrainzArtistTracksMessage(
            MessageId.For("lookup:musicbrainz-artist-tracks:artist-b"),
            CorrelationId.From("corr:artist-b"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            ArtistId.From("artist-b"));

        await AssertDuplicateDoesNotConsumeExtraBudget(
            environment,
            request,
            other,
            subject);
    }

    [Fact]
    public async Task Given_A_Duplicate_Album_Tracks_Command_When_Handling_Then_The_Duplicate_Does_Not_Consume_Extra_Budget()
    {
        await using var environment = await LookupExecutionAdmissionDecoratorIntegrationTestEnvironment.CreateAsync();
        var subject = new AdmittedLookupMusicbrainzAlbumTracksHandlerDecorator(
            new AlbumTracksInnerHandler(),
            environment.CommandBus,
            environment.AdmissionPort,
            environment.Clock);
        var request = new LookupMusicbrainzAlbumTracksMessage(
            MessageId.For("lookup:musicbrainz-album-tracks:artist-a:album-a"),
            CorrelationId.From("corr:album-a"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            AlbumId.From("artist-a", "album-a"));
        var other = new LookupMusicbrainzAlbumTracksMessage(
            MessageId.For("lookup:musicbrainz-album-tracks:artist-a:album-b"),
            CorrelationId.From("corr:album-b"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            AlbumId.From("artist-a", "album-b"));

        await AssertDuplicateDoesNotConsumeExtraBudget(
            environment,
            request,
            other,
            subject);
    }

    private static async Task AssertDuplicateDoesNotConsumeExtraBudget<TMessage>(
        LookupExecutionAdmissionDecoratorIntegrationTestEnvironment environment,
        TMessage request,
        TMessage other,
        IHandler<TMessage> subject)
        where TMessage : IMessage
    {
        await environment.AdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.MusicBrainz, request.Id, environment.Clock.UtcNow),
            CancellationToken.None);

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<LookupExecutionShortCircuitException>();
        environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>()
            .Which.Result.Should().BeOfType<LookupResult.Duplicate>();

        environment.Clock.UtcNow = environment.Clock.UtcNow.AddSeconds(1);
        var distinct = await environment.AdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.MusicBrainz, other.Id, environment.Clock.UtcNow),
            CancellationToken.None);
        distinct.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
    }

    private sealed class ArtistAlbumsInnerHandler : IHandler<LookupMusicbrainzArtistAlbumsMessage>
    {
        public Task Handle(LookupMusicbrainzArtistAlbumsMessage request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ArtistTracksInnerHandler : IHandler<LookupMusicbrainzArtistTracksMessage>
    {
        public Task Handle(LookupMusicbrainzArtistTracksMessage request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AlbumTracksInnerHandler : IHandler<LookupMusicbrainzAlbumTracksMessage>
    {
        public Task Handle(LookupMusicbrainzAlbumTracksMessage request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
