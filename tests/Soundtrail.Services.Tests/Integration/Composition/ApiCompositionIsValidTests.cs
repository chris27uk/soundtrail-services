namespace Soundtrail.Services.Tests.Integration.Composition;

public sealed class ApiCompositionIsValidTests
{
    [Fact]
    public void Given_Production_Registrations_When_Validating_The_Api_Composition_Then_The_Service_Provider_Builds()
    {
        var act = ProductionCompositionTestEnvironment.ValidateApiComposition;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Given_Production_Registrations_When_Sending_A_Known_Music_Data_Request_Then_Api_Messaging_Can_Route_It()
    {
        var act = ProductionCompositionTestEnvironment.ValidateApiCanRouteKnownMusicDataMessageAsync;

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Given_Production_Registrations_When_Sending_An_Unknown_Music_Data_Request_Then_Api_Messaging_Can_Route_It()
    {
        var act = ProductionCompositionTestEnvironment.ValidateApiCanRouteUnknownMusicDataMessageAsync;

        await act.Should().NotThrowAsync();
    }
}
