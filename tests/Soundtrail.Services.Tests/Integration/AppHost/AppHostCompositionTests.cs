using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.AppHost;

namespace Soundtrail.Services.Tests.Integration.AppHost;

public sealed class AppHostCompositionTests
{
    private static readonly string AppHostProjectDirectory =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Soundtrail.Services.AppHost"));

    [Fact]
    public void Given_local_development_mode_when_composing_apphost_then_all_deployables_expose_an_http_endpoint()
    {
        using var app = BuildDistributedApplication();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var deployables = model.Resources
            .OfType<ProjectResource>()
            .Where(resource => resource.Name.StartsWith("soundtrail-services-", StringComparison.Ordinal))
            .ToArray();

        deployables.Should().NotBeEmpty();
        deployables.Should().OnlyContain(resource => HasEndpointNamed(resource, "http"));
    }

    [Fact]
    public void Given_local_development_mode_when_composing_apphost_then_local_infrastructure_resources_are_present()
    {
        using var app = BuildDistributedApplication();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resourceNames = model.Resources.Select(resource => resource.Name).ToArray();

        resourceNames.Should().Contain("ravendb");
        resourceNames.Should().Contain("mssql");
        resourceNames.Should().Contain("servicebus-emulator");
        resourceNames.Should().Contain("provider-stubs");
    }

    [Fact]
    public void Given_apphost_composition_source_when_reviewing_queue_topology_then_required_queue_names_are_injected_into_projects()
    {
        var source = File.ReadAllText(Path.Combine(AppHostProjectDirectory, "AppHostComposition.cs"));

        source.Should().Contain("ServiceBus__CatalogSearchAttemptsQueueName");
        source.Should().Contain("ServiceBus__DiscoveryBacklogSchedulingQueueName");
        source.Should().Contain("ServiceBus__MusicBrainzLookupQueueName");
        source.Should().Contain("ServiceBus__PlaybackReferencesLookupQueueName");
        source.Should().Contain("ServiceBus__EnrichmentResponsesQueueName");
        source.Should().Contain("ServiceBus__MusicTrackEventsQueueName");
    }

    private static DistributedApplication BuildDistributedApplication()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            DisableDashboard = true,
            ProjectDirectory = AppHostProjectDirectory,
            AssemblyName = typeof(AppHostComposition).Assembly.GetName().Name
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            ["LocalDevelopment:UseProviderStubs"] = "true",
            ["LocalDevelopment:UseServiceBusEmulator"] = "true",
            ["ServiceBusEmulator:SqlPassword"] = "Soundtrail_Sql_Dev_123!"
        });

        AppHostComposition.Configure(builder, AppHostProjectDirectory);
        return builder.Build();
    }

    private static bool HasEndpointNamed(ProjectResource resource, string endpointName)
    {
        foreach (var annotation in GetAnnotations(resource))
        {
            var annotationType = annotation.GetType();
            if (!annotationType.Name.Contains("EndpointAnnotation", StringComparison.Ordinal))
            {
                continue;
            }

            var name = annotationType.GetProperty("Name")?.GetValue(annotation) as string
                       ?? annotationType.GetProperty("EndpointName")?.GetValue(annotation) as string;
            if (string.Equals(name, endpointName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<object> GetAnnotations(ProjectResource resource)
    {
        var annotationsProperty = resource.GetType().GetProperty("Annotations");
        var annotations = annotationsProperty?.GetValue(resource) as System.Collections.IEnumerable;
        if (annotations is null)
        {
            return [];
        }

        return annotations.Cast<object>();
    }
}
