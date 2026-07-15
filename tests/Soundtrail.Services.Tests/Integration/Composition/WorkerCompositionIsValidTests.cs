namespace Soundtrail.Services.Tests.Integration.Composition;

public sealed class WorkerCompositionIsValidTests
{
    [Fact]
    public void Given_Production_Registrations_When_Validating_The_Worker_Composition_Then_The_Service_Provider_Builds()
    {
        var act = ProductionCompositionTestEnvironment.ValidateWorkerComposition;

        act.Should().NotThrow();
    }
}
