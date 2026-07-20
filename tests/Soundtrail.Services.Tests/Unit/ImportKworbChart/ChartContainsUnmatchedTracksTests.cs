namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

public sealed class ChartContainsUnmatchedTracksTests
{
    [Fact]
    public async Task Given_Two_Triggers_In_The_Same_Hour_When_Handling_Them_Then_They_Produce_The_Same_Command_Scope()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();

        var first = environment.CreateRequest(new DateTimeOffset(2026, 7, 19, 10, 1, 0, TimeSpan.Zero));
        var second = environment.CreateRequest(new DateTimeOffset(2026, 7, 19, 10, 59, 0, TimeSpan.Zero));

        await environment.CreateSubjectUnderTest().Handle(first);
        await environment.CreateSubjectUnderTest().Handle(second);

        environment.CommandBus.Commands.Select(x => x.Id.Value).Should().OnlyContain(x => x == "kworb:worldwidesongchart:2026071910");
    }

    [Fact]
    public async Task Given_Two_Triggers_In_Different_Hours_When_Handling_Them_Then_They_Produce_Different_Command_Scopes()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest(new DateTimeOffset(2026, 7, 19, 10, 59, 0, TimeSpan.Zero)));
        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest(new DateTimeOffset(2026, 7, 19, 11, 0, 0, TimeSpan.Zero)));

        environment.CommandBus.Commands.Select(x => x.Id.Value)
            .Should()
            .Equal("kworb:worldwidesongchart:2026071910", "kworb:worldwidesongchart:2026071911");
    }
}
