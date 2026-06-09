using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class PlaybackReferencesLookupExecutionListenerTestEnvironment
{
    private static readonly ResolvePlaybackReferencesCommandDto DefaultCommand =
        new(
            CommandId.For("ResolvePlaybackReferences:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            new PlaybackReferenceLookupKeyDto(PlaybackReferenceLookupModeDto.Isrc, "isrc-1", null, null));

    private PlaybackReferencesLookupExecutionListenerTestEnvironment(LookupExecutionReceiptStoreFake.State state)
    {
        PlaybackReferenceSource = new FakePlaybackReferenceSource();
        Listener = new PlaybackReferencesLookupExecutionListener(
            new ExecutePlaybackReferencesLookupHandler(
                new LookupExecutionReceiptStoreFake(state),
                PlaybackReferenceSource));
    }

    public PlaybackReferencesLookupExecutionListener Listener { get; }

    public FakePlaybackReferenceSource PlaybackReferenceSource { get; }

    public static PlaybackReferencesLookupExecutionListenerTestEnvironment WithANewExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public static PlaybackReferencesLookupExecutionListenerTestEnvironment WithADuplicateExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public void Seed(PlaybackReferenceLookupKey lookupKey, params ExternalReference[] references) =>
        PlaybackReferenceSource.Seed(lookupKey, references);

    public Task<object[]> HandleNewExecutionCommand() =>
        Listener.Handle(DefaultCommand, null!);

    public Task<object[]> HandleNewExecutionCommand(ResolvePlaybackReferencesCommandDto command) =>
        Listener.Handle(command, null!);

    public async Task<object[]> HandleDuplicateExecutionCommand()
    {
        await Listener.Handle(DefaultCommand, null!);
        return await Listener.Handle(DefaultCommand, null!);
    }
}
