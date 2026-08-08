using System.Text.Json;
using Soundtrail.Adapters.Messaging.Asb;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class ServiceBusQueuesEmulatorConfigTests
{
    [Fact]
    public void Given_The_AppHost_Emulator_Config_When_Reading_Queue_Names_Then_They_Match_ServiceBusQueues()
    {
        var configPath = Path.Combine(
            AppContext.BaseDirectory,
            "EndToEnd",
            "Shared",
            "servicebus-emulator",
            "Config.json");

        File.Exists(configPath).Should().BeTrue($"emulator config should be copied to test output at {configPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var queues = document.RootElement
            .GetProperty("UserConfig")
            .GetProperty("Namespaces")[0]
            .GetProperty("Queues")
            .EnumerateArray()
            .Select(queue => queue.GetProperty("Name").GetString()!)
            .ToArray();

        queues.Should().BeEquivalentTo(ServiceBusQueues.All);
    }
}
