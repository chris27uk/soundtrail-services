using Soundtrail.Services.ServiceDefaults;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure;

public class ServiceVersionTests
{
    [Fact]
    public void Given_Otel_Service_Version_Is_Set_When_Resolve_Then_Returns_Trimmed_Env_Value()
    {
        var version = ServiceVersion.Resolve(name =>
            name == ServiceVersion.EnvironmentVariableName ? " 1.2.3+Sha.abc " : null);

        version.Should().Be("1.2.3+Sha.abc");
    }

    [Fact]
    public void Given_Otel_Service_Version_Is_Missing_When_Resolve_Then_Returns_Fallback()
    {
        var version = ServiceVersion.Resolve(_ => null);

        version.Should().Be(ServiceVersion.Fallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_Otel_Service_Version_Is_Blank_When_Resolve_Then_Returns_Fallback(string? envValue)
    {
        var version = ServiceVersion.Resolve(name =>
            name == ServiceVersion.EnvironmentVariableName ? envValue : null);

        version.Should().Be(ServiceVersion.Fallback);
    }
}
