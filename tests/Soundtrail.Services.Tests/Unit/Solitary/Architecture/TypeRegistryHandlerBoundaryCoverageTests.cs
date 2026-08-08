using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested.Composition;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Composition;
using Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Composition;

namespace Soundtrail.Services.Tests.Unit.Solitary.Architecture;

public sealed class TypeRegistryHandlerBoundaryCoverageTests
{
    [Fact]
    public void Handler_boundary_types_have_bidirectional_type_registry_pairs()
    {
        var registry = TypeTranslationRegistry.CreateFromAssemblies(
            typeof(TypeTranslationRegistry).Assembly,
            typeof(AppTypeRegistry).Assembly);

        var required = HandlerBoundaryTypeDiscovery.DiscoverRequiredTypes(
            orchestratorAssembly: typeof(OnKnownMusicDataRequestedFeature).Assembly,
            schedulerAssembly: typeof(ImportKworbChartFeature).Assembly,
            projectorAssembly: typeof(OnWorkScheduledFeature).Assembly,
            apiAssembly: typeof(AppTypeRegistry).Assembly);

        required.Should().NotBeEmpty();

        var failures = new List<string>();
        foreach (var discovered in required)
        {
            var domainType = discovered.DomainType;
            if (!registry.TryGetDtoTypeForDomain(domainType, out var dtoType) || dtoType is null)
            {
                failures.Add($"{domainType.FullName} ({discovered.Source}): missing RegisterPair / RegisterStoredEventPair metadata");
                continue;
            }

            if (!registry.HasTranslation(domainType, dtoType))
            {
                failures.Add($"{domainType.FullName} ({discovered.Source}): missing Domain→DTO translator to {dtoType.FullName}");
            }

            if (!registry.HasTranslation(dtoType, domainType))
            {
                failures.Add($"{domainType.FullName} ({discovered.Source}): missing DTO→Domain translator from {dtoType.FullName}");
            }
        }

        failures.Should().BeEmpty(
            "every handler boundary type must have bidirectional Domain↔DTO registration so ToDto/ToDomainObject cannot fail. Missing:{0}{1}",
            Environment.NewLine,
            string.Join(Environment.NewLine, failures));
    }
}
