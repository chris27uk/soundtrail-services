using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupExecutionAdmission;

public sealed class LookupExecutionAdmissionPortIntegrationTests
{
    [Fact]
    public async Task Given_Concurrent_Attempts_For_The_Same_Message_When_Acquiring_Then_Exactly_One_Attempt_Is_Admitted()
    {
        await using var environment = await LookupExecutionAdmissionPortIntegrationTestEnvironment.CreateAsync();
        var request = environment.CreateRequest("msg-same");

        var attempts = Enumerable.Range(0, 20)
            .Select(_ => environment.Subject.TryAcquireAsync(request, CancellationToken.None));

        var results = await Task.WhenAll(attempts);

        results.Count(x => x.Status == LookupExecutionAdmissionStatus.Acquired).Should().Be(1);
        results.Count(x => x.Status == LookupExecutionAdmissionStatus.Duplicate).Should().Be(19);
        results.Should().NotContain(x => x.Status == LookupExecutionAdmissionStatus.Deferred);
    }

    [Fact]
    public async Task Given_Concurrent_Attempts_For_Different_Messages_When_Provider_Budget_Is_One_Then_Only_One_Attempt_Is_Admitted()
    {
        await using var environment = await LookupExecutionAdmissionPortIntegrationTestEnvironment.CreateAsync();

        var attempts = Enumerable.Range(0, 20)
            .Select(index => environment.Subject.TryAcquireAsync(
                environment.CreateRequest($"msg-budget-{index}"),
                CancellationToken.None));

        var results = await Task.WhenAll(attempts);

        results.Count(x => x.Status == LookupExecutionAdmissionStatus.Acquired).Should().Be(1);
        results.Count(x => x.Status == LookupExecutionAdmissionStatus.Deferred).Should().Be(19);
        results.Should().NotContain(x => x.Status == LookupExecutionAdmissionStatus.Duplicate);
    }

    [Fact]
    public async Task Given_An_Acquired_Attempt_When_It_Is_Committed_Then_Redelivery_Is_Treated_As_A_Duplicate()
    {
        await using var environment = await LookupExecutionAdmissionPortIntegrationTestEnvironment.CreateAsync();
        var request = environment.CreateRequest("msg-committed");

        var acquired = await environment.Subject.TryAcquireAsync(request, CancellationToken.None);
        await environment.Subject.CommitAsync(request.MessageId, CancellationToken.None);
        var duplicate = await environment.Subject.TryAcquireAsync(request, CancellationToken.None);

        acquired.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
        duplicate.Status.Should().Be(LookupExecutionAdmissionStatus.Duplicate);
    }

    [Fact]
    public async Task Given_An_Acquired_Attempt_When_It_Is_Released_Then_A_Different_Message_Can_Consume_The_Same_Budget_Slot()
    {
        await using var environment = await LookupExecutionAdmissionPortIntegrationTestEnvironment.CreateAsync();
        var first = environment.CreateRequest("msg-first");
        var second = environment.CreateRequest("msg-second");

        var acquired = await environment.Subject.TryAcquireAsync(first, CancellationToken.None);
        await environment.Subject.ReleaseAsync(first.MessageId, CancellationToken.None);
        var reacquired = await environment.Subject.TryAcquireAsync(second, CancellationToken.None);

        acquired.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
        reacquired.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
    }
}
