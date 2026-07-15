namespace Soundtrail.Services.Tests.Integration.Composition;

public sealed class ApiCompositionIsValidTests
{
    [Fact]
    public void Given_Production_Registrations_When_Validating_The_Api_Composition_Then_The_Service_Provider_Builds()
    {
        var act = ProductionCompositionTestEnvironment.ValidateApiComposition;

        act.Should().NotThrow();
    }
}
