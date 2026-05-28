using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Features.Search;

namespace Soundtrail.Services.Tests.Integration.Features.Health;

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
        // Given

        // When
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().ContainKey("status").WhoseValue.Should().Be("alive");
    }

    [Fact]
    public async Task Ready_Endpoint_Returns_Ready_When_Dependencies_Are_Healthy()
    {
        // Given
        _factory.TrackLookup.Ready = true;
        _factory.TrackSearch.Ready = true;

        // When
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().ContainKey("status").WhoseValue.Should().Be("ready");
    }
}
