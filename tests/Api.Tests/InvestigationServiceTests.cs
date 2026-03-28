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
    private readonly Mock<ILogger<InvestigationService>> _loggerMock = new();
    private readonly InvestigationService _service;

    public InvestigationServiceTests()
    {
        _service = new InvestigationService(_runRepoMock.Object, _tenantRepoMock.Object, _loggerMock.Object);
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
            InvestigationStatus.Running, DateTimeOffset.UtcNow, null, 2, 0, 0, "test-user"
        );

        _runRepoMock.Setup(r => r.CreateAsync(It.IsAny<InvestigationRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRun);

        _tenantRepoMock.Setup(r => r.CreateAsync(It.IsAny<TenantInvestigationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvestigationResult r, CancellationToken _) => r);

        var result = await _service.StartInvestigationAsync(request, "test-user");

        result.Should().BeEquivalentTo(expectedRun);
        _runRepoMock.Verify(r => r.CreateAsync(It.IsAny<InvestigationRun>(), It.IsAny<CancellationToken>()), Times.Once);
        _tenantRepoMock.Verify(r => r.CreateAsync(It.IsAny<TenantInvestigationResult>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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
}
