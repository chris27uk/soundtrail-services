using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Soundtrail.Services.Tests.Features.Search;

namespace Soundtrail.Services.Tests.Features.Health;

public sealed class HealthEndpointsTests : IClassFixture<SoundtrailServicesApiFactory>
{
    private readonly HttpClient _client;
    private readonly SoundtrailServicesApiFactory _factory;

    public HealthEndpointsTests(SoundtrailServicesApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Live_Endpoint_Returns_Alive()
    {
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().ContainKey("status").WhoseValue.Should().Be("alive");
    }

    [Fact]
    public async Task Ready_Endpoint_Returns_Ready_When_Dependencies_Are_Healthy()
    {
        _factory.TrackLookup.Ready = true;
        _factory.TrackSearch.Ready = true;

        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().ContainKey("status").WhoseValue.Should().Be("ready");
    }
}
