namespace Soundtrail.Services.Tests.Integration.Composition;

public sealed class OrchestratorCompositionIsValidTests
{
    [Fact]
    public void Given_Production_Registrations_When_Validating_The_Orchestrator_Composition_Then_The_Service_Provider_Builds()
    {
        var act = ProductionCompositionTestEnvironment.ValidateOrchestratorComposition;

        act.Should().NotThrow();
    }
}
