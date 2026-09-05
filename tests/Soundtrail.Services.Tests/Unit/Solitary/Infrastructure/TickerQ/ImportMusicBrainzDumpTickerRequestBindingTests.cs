using System.Text;
using System.Text.Json;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Adapters;
using TickerQ.Utilities;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.TickerQ;

public sealed class ImportMusicBrainzDumpTickerRequestBindingTests
{
    [Fact]
    public void Given_Dashboard_CamelCase_Json_When_Deserialized_With_Ticker_Options_Then_DumpVersion_Binds()
    {
        var previous = TickerHelper.RequestJsonSerializerOptions;
        try
        {
            TickerHelper.RequestJsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var request = TickerHelper.ReadTickerRequest<ImportMusicBrainzDumpTickerRequest>(
                Encoding.UTF8.GetBytes("""{"dumpVersion":"20260815-001001"}"""));

            request.Should().NotBeNull();
            request!.DumpVersion.Should().Be("20260815-001001");
        }
        finally
        {
            TickerHelper.RequestJsonSerializerOptions = previous;
        }
    }

    [Fact]
    public void Given_Dashboard_CamelCase_Json_When_Deserialized_With_Default_Ticker_Options_Then_Attribute_Still_Binds()
    {
        var previous = TickerHelper.RequestJsonSerializerOptions;
        try
        {
            TickerHelper.RequestJsonSerializerOptions = new JsonSerializerOptions();

            var request = TickerHelper.ReadTickerRequest<ImportMusicBrainzDumpTickerRequest>(
                Encoding.UTF8.GetBytes("""{"dumpVersion":"20260815-001001"}"""));

            request.Should().NotBeNull();
            request!.DumpVersion.Should().Be("20260815-001001");
        }
        finally
        {
            TickerHelper.RequestJsonSerializerOptions = previous;
        }
    }
}
