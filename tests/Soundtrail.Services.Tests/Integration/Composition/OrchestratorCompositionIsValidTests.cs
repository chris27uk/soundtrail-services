namespace Soundtrail.Services.Tests.Integration.Composition;

public sealed class OrchestratorCompositionIsValidTests
{
    [Fact]
    public void Given_Production_Registrations_When_Validating_The_Orchestrator_Composition_Then_The_Service_Provider_Builds()
    {
        var act = ProductionCompositionTestEnvironment.ValidateOrchestratorComposition;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Given_The_Orchestrator_When_Resolving_The_Known_Music_Data_Listener_Then_The_Listener_Is_Registered()
    {
        var act = ProductionCompositionTestEnvironment.ValidateOrchestratorRegistersKnownMusicDataListenerAsync;

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Given_The_Orchestrator_When_Resolving_The_Unknown_Music_Data_Listener_Then_The_Listener_Is_Registered()
    {
        var act = ProductionCompositionTestEnvironment.ValidateOrchestratorRegistersUnknownMusicDataListenerAsync;

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Given_The_Orchestrator_When_Resolving_The_Assess_Music_Catalog_Item_Listener_Then_The_Listener_Is_Registered()
    {
        var act = ProductionCompositionTestEnvironment.ValidateOrchestratorRegistersAssessMusicCatalogItemListenerAsync;

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Given_The_Orchestrator_When_Resolving_The_Dispatch_Lookup_Work_Listener_Then_The_Listener_Is_Registered()
    {
        var act = ProductionCompositionTestEnvironment.ValidateOrchestratorRegistersDispatchLookupWorkListenerAsync;
 
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Given_The_Orchestrator_When_Resolving_The_Lookup_Completed_Listener_Then_The_Listener_Is_Registered()
    {
        var act = ProductionCompositionTestEnvironment.ValidateOrchestratorRegistersLookupCompletedListenerAsync;

        await act.Should().NotThrowAsync();
    }
}
