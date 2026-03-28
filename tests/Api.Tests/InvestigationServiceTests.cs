using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Trace.Api.Repositories;
using Trace.Api.Services;
using Trace.Contracts;

namespace Trace.Api.Tests;

public class InvestigationServiceTests
{
    private readonly Mock<IInvestigationRepository> _runRepoMock = new();
    private readonly Mock<ITenantResultRepository> _tenantRepoMock = new();
    private readonly Mock<IWorkQueueService> _workQueueMock = new();
    private readonly Mock<IWorkDispatcher> _dispatcherMock = new();
    private readonly Mock<ILogger<InvestigationService>> _loggerMock = new();
    private readonly InvestigationService _service;

    public InvestigationServiceTests()
    {
        _service = new InvestigationService(
            _runRepoMock.Object,
            _tenantRepoMock.Object,
            _workQueueMock.Object,
            _dispatcherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task StartInvestigationAsync_CreatesRunAndTenantResults()
    {
        var request = new CveInvestigationRequest(
            "CVE-2024-12345",
            "Test CVE",
            "Test description",
            ["tenant-1", "tenant-2"]
        );

        var expectedRun = new InvestigationRun(
            "run-1", "CVE-2024-12345", "Test CVE", "Test description",
            InvestigationStatus.Pending, DateTimeOffset.UtcNow, null, 2, 0, 0, "test-user",
            WorkflowStage.Queued, "Queued CVE research.", DateTimeOffset.UtcNow
        );

        var queuedWork = new WorkQueueItem(
            "research",
            "run-1",
            WorkItemType.Research,
            WorkItemStatus.Pending,
            DateTimeOffset.UtcNow,
            ProgressMessage: "Queued CVE research.",
            LastUpdatedAt: DateTimeOffset.UtcNow);

        _runRepoMock.Setup(r => r.CreateAsync(It.IsAny<InvestigationRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRun);

        _tenantRepoMock.Setup(r => r.CreateAsync(It.IsAny<TenantInvestigationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvestigationResult r, CancellationToken _) => r);

        _workQueueMock.Setup(q => q.QueueResearchAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queuedWork);

        _dispatcherMock.Setup(d => d.QueueAsync(It.IsAny<QueuedWorkReference>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var result = await _service.StartInvestigationAsync(request, "test-user");

        result.Should().BeEquivalentTo(expectedRun);
        _runRepoMock.Verify(r => r.CreateAsync(
            It.Is<InvestigationRun>(run => run.Status == InvestigationStatus.Pending && run.CurrentStage == WorkflowStage.Queued),
            It.IsAny<CancellationToken>()), Times.Once);
        _tenantRepoMock.Verify(r => r.CreateAsync(
            It.Is<TenantInvestigationResult>(tenant => tenant.CurrentStage == WorkflowStage.Queued && tenant.Status == InvestigationStatus.Pending),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _workQueueMock.Verify(q => q.QueueResearchAsync("run-1", It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.QueueAsync(
            It.Is<QueuedWorkReference>(work => work.RunId == "run-1" && work.WorkItemId == "research"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewTenantResultAsync_ApprovesResult()
    {
        var runId = "run-1";
        var tenantId = "tenant-1";
        var existing = new TenantInvestigationResult(
            $"{runId}:{tenantId}:summary", runId, tenantId, "Test Tenant",
            ExposureVerdict.Exposed, InvestigationStatus.Completed,
            ReviewStatus.Pending, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            3, null, null, null
        );

        var decision = new ReviewDecision(runId, tenantId, ReviewStatus.Approved, "reviewer@test.com", "Confirmed exposure");

        _tenantRepoMock.Setup(r => r.GetAsync(runId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _tenantRepoMock.Setup(r => r.UpdateAsync(It.IsAny<TenantInvestigationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvestigationResult r, CancellationToken _) => r);

        var result = await _service.ReviewTenantResultAsync(runId, tenantId, decision);

        result.ReviewStatus.Should().Be(ReviewStatus.Approved);
        result.ReviewedBy.Should().Be("reviewer@test.com");
        result.ReviewNotes.Should().Be("Confirmed exposure");
    }

    [Fact]
    public async Task ReviewTenantResultAsync_ThrowsWhenNotFound()
    {
        _tenantRepoMock.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvestigationResult?)null);

        var decision = new ReviewDecision("run-1", "tenant-1", ReviewStatus.Approved, "reviewer", null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ReviewTenantResultAsync("run-1", "tenant-1", decision));
    }

    [Fact]
    public async Task GetRunAsync_ReturnsNullWhenNotFound()
    {
        _runRepoMock.Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvestigationRun?)null);

        var result = await _service.GetRunAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRunAsync_HydratesTenantProgress()
    {
        var run = new InvestigationRun(
            "run-1", "CVE-2024-12345", "Test CVE", null,
            InvestigationStatus.Running, DateTimeOffset.UtcNow, null,
            2, 0, 0, "test-user",
            WorkflowStage.TenantFanOut, "Research complete.", DateTimeOffset.UtcNow);

        var tenants = new List<TenantInvestigationResult>
        {
            new(
                "run-1:tenant-1:summary", "run-1", "tenant-1", "Tenant 1",
                ExposureVerdict.Exposed, InvestigationStatus.Completed, ReviewStatus.Pending,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 2,
                null, null, null,
                WorkflowStage.Completed, "Tenant complete."),
            new(
                "run-1:tenant-2:summary", "run-1", "tenant-2", "Tenant 2",
                ExposureVerdict.Unknown, InvestigationStatus.Pending, ReviewStatus.Pending,
                DateTimeOffset.UtcNow, null, 0,
                null, null, null,
                WorkflowStage.Queued, "Queued.")
        };

        _runRepoMock.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        _tenantRepoMock.Setup(r => r.ListByRunAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(tenants);

        var result = await _service.GetRunAsync("run-1");

        result.Should().NotBeNull();
        result!.TenantsCompleted.Should().Be(1);
        result.FindingsCount.Should().Be(2);
        result.ProgressMessage.Should().Contain("1 of 2");
    }
}
