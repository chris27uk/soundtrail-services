using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.LookupExecutionReceiptStore;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class LookupExecutionReceiptStoreResponsesTests
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_New_CommandId_When_Beginning_Then_It_Is_Acquired_Once(ReceiptStoreMode mode)
    {
        await using var env = ReceiptStoreTestEnvironment.Create(mode);
        var commandId = CommandId.For("LookupStreamingLocations:mc_track_1");

        var first = await env.TryBeginAsync(commandId);
        var second = await env.TryBeginAsync(commandId);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Begun_CommandId_When_Marked_Completed_Then_It_Is_Recorded_As_Completed(ReceiptStoreMode mode)
    {
        await using var env = ReceiptStoreTestEnvironment.Create(mode);
        var commandId = CommandId.For("LookupStreamingLocations:mc_track_1");

        await env.TryBeginAsync(commandId);
        await env.MarkCompletedAsync(commandId);

        (await env.WasCompletedAsync(commandId)).Should().BeTrue();
    }

    public static IEnumerable<object[]> AllModes()
    {
        yield return [ReceiptStoreMode.InProcessFake];
        yield return [ReceiptStoreMode.RavenEmbedded];
    }

    public enum ReceiptStoreMode
    {
        InProcessFake,
        RavenEmbedded
    }

    private sealed class ReceiptStoreTestEnvironment : IAsyncDisposable
    {
        private readonly LookupExecutionReceiptStoreFake.State? fakeState;
        private readonly RavenEmbeddedTestDatabase? raven;

        private ReceiptStoreTestEnvironment(
            LookupExecutionReceiptStoreFake.State? fakeState,
            RavenEmbeddedTestDatabase? raven)
        {
            this.fakeState = fakeState;
            this.raven = raven;
        }

        public static ReceiptStoreTestEnvironment Create(ReceiptStoreMode mode) =>
            mode switch
            {
                ReceiptStoreMode.InProcessFake => new(new LookupExecutionReceiptStoreFake.State(), null),
                ReceiptStoreMode.RavenEmbedded => new(null, RavenEmbeddedTestDatabase.Create()),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

        public async Task<bool> TryBeginAsync(CommandId commandId)
        {
            if (fakeState is not null)
            {
                return await new LookupExecutionReceiptStoreFake(fakeState).TryBeginAsync(commandId, CancellationToken.None);
            }

            using var session = raven!.Store.OpenAsyncSession();
            var store = new RavenLookupExecutionReceiptStore(session);
            var acquired = await store.TryBeginAsync(commandId, CancellationToken.None);
            await session.SaveChangesAsync();
            return acquired;
        }

        public async Task MarkCompletedAsync(CommandId commandId)
        {
            if (fakeState is not null)
            {
                await new LookupExecutionReceiptStoreFake(fakeState).MarkCompletedAsync(commandId, CancellationToken.None);
                return;
            }

            using var session = raven!.Store.OpenAsyncSession();
            var store = new RavenLookupExecutionReceiptStore(session);
            await store.MarkCompletedAsync(commandId, CancellationToken.None);
            await session.SaveChangesAsync();
        }

        public async Task<bool> WasCompletedAsync(CommandId commandId)
        {
            if (fakeState is not null)
            {
                return fakeState.CompletedReceipts.Contains(commandId.Value);
            }

            using var session = raven!.Store.OpenAsyncSession();
            var document = await session.LoadAsync<RavenLookupExecutionReceiptDto>(
                RavenLookupExecutionReceiptDto.GetDocumentId(commandId.Value));
            return document?.Completed == true;
        }

        public ValueTask DisposeAsync()
        {
            raven?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
