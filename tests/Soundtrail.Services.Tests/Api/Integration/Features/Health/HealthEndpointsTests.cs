using FluentAssertions;
using Soundtrail.Services.Tests.Api.Integration.Features.Search;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Api.Integration.Features.Health;

public sealed class HealthEndpointsTests(SoundtrailServicesApiFactory factory) : IClassFixture<SoundtrailServicesApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Given_The_Live_Endpoint_When_It_Is_Called_Then_It_Returns_Alive()
    {
        var response = await this.client.GetAsync("/health/live");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().ContainKey("status").WhoseValue.Should().Be("alive");
    }

    [Fact]
    public async Task Given_Healthy_Dependencies_When_The_Ready_Endpoint_Is_Called_Then_It_Returns_Ready()
    {
        factory.CatalogLookup.Ready = true;
        factory.TrackSearch.Ready = true;

        var response = await this.client.GetAsync("/health/ready");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().ContainKey("status").WhoseValue.Should().Be("ready");
    }

    [Fact]
    public async Task Given_An_Unhealthy_Dependency_When_The_Ready_Endpoint_Is_Called_Then_It_Returns_Service_Unavailable()
    {
        factory.CatalogLookup.Ready = false;
        factory.TrackSearch.Ready = true;

        var response = await this.client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
