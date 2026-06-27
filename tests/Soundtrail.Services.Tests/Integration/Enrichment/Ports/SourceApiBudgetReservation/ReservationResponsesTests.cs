using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.SourceApiBudgetReservation;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class ReservationResponsesTests
{
    [Theory]
    [MemberData(nameof(SourceApiBudgetReservationContractModes.All), MemberType = typeof(SourceApiBudgetReservationContractModes))]
    public async Task Given_A_Source_With_Remaining_Budget_When_Reserving_Then_The_Reservation_Is_Accepted(SourceApiBudgetReservationMode mode)
    {
        using var env = SourceApiBudgetReservationTestEnvironment.Create(mode);

        var result = await env.Port.TryReserveAsync(
            new SourceApiBudgetReservationRequest(
                LookupSource.Odesli,
                new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        result.Accepted.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SourceApiBudgetReservationContractModes.All), MemberType = typeof(SourceApiBudgetReservationContractModes))]
    public async Task Given_A_Window_That_Is_Full_When_Reserving_Then_The_Reservation_Is_Rejected_With_A_Retry_Time(SourceApiBudgetReservationMode mode)
    {
        using var env = SourceApiBudgetReservationTestEnvironment.Create(mode);
        var now = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

        await env.Port.TryReserveAsync(new SourceApiBudgetReservationRequest(LookupSource.Odesli, now), CancellationToken.None);
        await env.Port.TryReserveAsync(new SourceApiBudgetReservationRequest(LookupSource.Odesli, now.AddSeconds(1)), CancellationToken.None);

        var result = await env.Port.TryReserveAsync(
            new SourceApiBudgetReservationRequest(LookupSource.Odesli, now.AddSeconds(2)),
            CancellationToken.None);

        result.Accepted.Should().BeFalse();
        result.RetryAt.Should().Be(new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero));
        result.Reason.Should().Be("Odesli budget temporarily unavailable");
    }

    [Theory]
    [MemberData(nameof(SourceApiBudgetReservationContractModes.All), MemberType = typeof(SourceApiBudgetReservationContractModes))]
    public async Task Given_MusicBrainz_Minimum_Spacing_When_Reserving_Twice_In_The_Same_Second_Then_The_Second_Reservation_Is_Rejected(SourceApiBudgetReservationMode mode)
    {
        using var env = SourceApiBudgetReservationTestEnvironment.Create(mode);
        var now = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

        var first = await env.Port.TryReserveAsync(
            new SourceApiBudgetReservationRequest(LookupSource.MusicBrainz, now),
            CancellationToken.None);
        var second = await env.Port.TryReserveAsync(
            new SourceApiBudgetReservationRequest(LookupSource.MusicBrainz, now.AddMilliseconds(250)),
            CancellationToken.None);
        var nextSecond = await env.Port.TryReserveAsync(
            new SourceApiBudgetReservationRequest(LookupSource.MusicBrainz, now.AddSeconds(1)),
            CancellationToken.None);

        first.Accepted.Should().BeTrue();
        second.Accepted.Should().BeFalse();
        second.RetryAt.Should().Be(new DateTimeOffset(2026, 6, 16, 12, 0, 1, TimeSpan.Zero));
        nextSecond.Accepted.Should().BeTrue();
    }
}
