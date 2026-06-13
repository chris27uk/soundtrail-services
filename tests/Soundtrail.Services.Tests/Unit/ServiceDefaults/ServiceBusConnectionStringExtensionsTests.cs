using FluentAssertions;
using Soundtrail.Services.ServiceDefaults;

namespace Soundtrail.Services.Tests.Unit.ServiceDefaults;

public sealed class ServiceBusConnectionStringExtensionsTests
{
    [Fact]
    public void Given_service_bus_emulator_connection_string_when_checking_emulator_mode_then_it_returns_true()
    {
        var connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        connectionString.IsDevelopmentEmulatorConnectionString().Should().BeTrue();
    }

    [Fact]
    public void Given_regular_service_bus_connection_string_when_checking_emulator_mode_then_it_returns_false()
    {
        var connectionString = "Endpoint=sb://soundtrail.servicebus.windows.net/;SharedAccessKeyName=name;SharedAccessKey=key;";

        connectionString.IsDevelopmentEmulatorConnectionString().Should().BeFalse();
    }
}
