namespace Soundtrail.Services.Tests.Integration.Composition;

public sealed class ProjectorCompositionIsValidTests
{
    [Fact]
    public void Given_Production_Registrations_When_Validating_The_Projector_Composition_Then_The_Service_Provider_Builds()
    {
        var act = ProductionCompositionTestEnvironment.ValidateProjectorComposition;

        act.Should().NotThrow();
    }
}
