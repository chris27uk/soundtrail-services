using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Soundtrail.Services.AppHost;

namespace Soundtrail.Services.Tests.Unit.AppHost;

public sealed class AppHostStartupValidatorTests
{
    [Fact]
    public void Given_A_Placeholder_Service_Bus_Connection_String_When_Validating_Startup_Then_An_Exception_Is_Thrown()
    {
        var configuration = BuildConfiguration(
            serviceBusConnectionString: "Endpoint=sb://replace-me.servicebus.windows.net/;SharedAccessKeyName=replace-me;SharedAccessKey=replace-me",
            useProviderStubs: false);

        var act = () => AppHostStartupValidator.Validate(configuration, "/tmp");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:servicebus*");
    }

    [Fact]
    public void Given_A_Service_Bus_Emulator_Connection_String_When_Validating_Startup_Then_No_Exception_Is_Thrown()
    {
        var configuration = BuildConfiguration(
            serviceBusConnectionString: "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            useProviderStubs: false);

        var act = () => AppHostStartupValidator.Validate(configuration, "/tmp");

        act.Should().NotThrow();
    }

    [Fact]
    public void Given_Provider_Stubs_Enabled_When_The_Mappings_Directory_Is_Missing_Then_An_Exception_Is_Thrown()
    {
        var configuration = BuildConfiguration(
            serviceBusConnectionString: "Endpoint=sb://real.servicebus.windows.net/;SharedAccessKeyName=name;SharedAccessKey=key",
            useProviderStubs: true);

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var act = () => AppHostStartupValidator.Validate(configuration, tempRoot);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WireMock mappings directory*");
    }

    [Fact]
    public void Given_Provider_Stubs_Enabled_When_The_Mappings_Directory_Has_No_Files_Then_An_Exception_Is_Thrown()
    {
        var configuration = BuildConfiguration(
            serviceBusConnectionString: "Endpoint=sb://real.servicebus.windows.net/;SharedAccessKeyName=name;SharedAccessKey=key",
            useProviderStubs: true);

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "wiremock", "mappings"));

        try
        {
            var act = () => AppHostStartupValidator.Validate(configuration, tempRoot);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*no WireMock mapping files*");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Given_Valid_Local_Development_Configuration_When_Validating_Startup_Then_No_Exception_Is_Thrown()
    {
        var configuration = BuildConfiguration(
            serviceBusConnectionString: "Endpoint=sb://real.servicebus.windows.net/;SharedAccessKeyName=name;SharedAccessKey=key",
            useProviderStubs: true);

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var mappingsPath = Path.Combine(tempRoot, "wiremock", "mappings");
        Directory.CreateDirectory(mappingsPath);
        File.WriteAllText(Path.Combine(mappingsPath, "sample.json"), "{}");

        try
        {
            var act = () => AppHostStartupValidator.Validate(configuration, tempRoot);

            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static IConfiguration BuildConfiguration(string serviceBusConnectionString, bool useProviderStubs)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = serviceBusConnectionString,
                ["LocalDevelopment:UseProviderStubs"] = useProviderStubs.ToString()
            })
            .Build();
    }
}
