using FluentAssertions;
using Trace.Api.Workflows;
using Trace.Contracts;
using Xunit;

namespace Trace.Api.Tests;

public class FindingCorrelatorTests
{
    [Fact]
    public async Task CorrelateAsync_ReturnsCompletedWhenAllTenantsSucceed()
    {
        var correlator = new FindingCorrelator();
        var tenantResults = new List<TenantInvestigationResult>
        {
            new(
                "run-1:tenant-1:summary", "run-1", "tenant-1", "Tenant 1",
                ExposureVerdict.Exposed, InvestigationStatus.Completed, ReviewStatus.Pending,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 2,
                null, null, null,
                WorkflowStage.Completed, "Completed."),
            new(
                "run-1:tenant-2:summary", "run-1", "tenant-2", "Tenant 2",
                ExposureVerdict.NotExposed, InvestigationStatus.Completed, ReviewStatus.Pending,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0,
                null, null, null,
                WorkflowStage.Completed, "Completed.")
        };

        var result = await correlator.CorrelateAsync(tenantResults);

        result.Status.Should().Be(InvestigationStatus.Completed);
        result.FinalStage.Should().Be(WorkflowStage.Completed);
        result.FindingsCount.Should().Be(2);
        result.TenantsCompleted.Should().Be(2);
    }

    [Fact]
    public async Task CorrelateAsync_ReturnsFailedWhenAnyTenantFails()
    {
        var correlator = new FindingCorrelator();
        var tenantResults = new List<TenantInvestigationResult>
        {
            new(
                "run-1:tenant-1:summary", "run-1", "tenant-1", "Tenant 1",
                ExposureVerdict.Unknown, InvestigationStatus.Failed, ReviewStatus.Pending,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0,
                null, null, null,
                WorkflowStage.Failed, "Failed."),
            new(
                "run-1:tenant-2:summary", "run-1", "tenant-2", "Tenant 2",
                ExposureVerdict.Exposed, InvestigationStatus.Completed, ReviewStatus.Pending,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1,
                null, null, null,
                WorkflowStage.Completed, "Completed.")
        };

        var result = await correlator.CorrelateAsync(tenantResults);

        result.Status.Should().Be(InvestigationStatus.Failed);
        result.FinalStage.Should().Be(WorkflowStage.Failed);
        result.FindingsCount.Should().Be(1);
        result.TenantsCompleted.Should().Be(1);
    }
}