namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.SourceApiBudgetReservation;

public static class SourceApiBudgetReservationContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [SourceApiBudgetReservationMode.InProcessFake],
        [SourceApiBudgetReservationMode.RavenCompareExchange]
    ];
}
