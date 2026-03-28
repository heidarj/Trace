using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Trace.Api.Repositories;
using Trace.Api.Services;
using Trace.Contracts;

namespace Trace.Api.Tests;

public class TicketServiceTests
{
    private readonly Mock<ITenantResultRepository> _tenantRepoMock = new();
    private readonly Mock<IInvestigationRepository> _runRepoMock = new();
    private readonly Mock<ILogger<TicketService>> _loggerMock = new();
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _service = new TicketService(_tenantRepoMock.Object, _runRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateTicketAsync_CreatesTicketForApprovedResult()
    {
        var runId = "run-1";
        var tenantId = "tenant-1";

        var approvedResult = new TenantInvestigationResult(
            $"{runId}:{tenantId}:summary", runId, tenantId, "Test Tenant",
            ExposureVerdict.Exposed, InvestigationStatus.Completed,
            ReviewStatus.Approved, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            3, "reviewer@test.com", DateTimeOffset.UtcNow, "Confirmed"
        );

        var run = new InvestigationRun(
            runId, "CVE-2024-12345", "Test CVE", null,
            InvestigationStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            1, 1, 3, "admin"
        );

        _tenantRepoMock.Setup(r => r.GetAsync(runId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedResult);
        _runRepoMock.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _tenantRepoMock.Setup(r => r.CreateTicketAsync(It.IsAny<TicketRecommendation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketRecommendation t, CancellationToken _) => t);

        var ticket = await _service.CreateTicketAsync(runId, tenantId, "admin");

        ticket.RunId.Should().Be(runId);
        ticket.TenantId.Should().Be(tenantId);
        ticket.CveId.Should().Be("CVE-2024-12345");
        ticket.Status.Should().Be(TicketStatus.Draft);
    }

    [Fact]
    public async Task CreateTicketAsync_ThrowsForUnapprovedResult()
    {
        var pendingResult = new TenantInvestigationResult(
            "run-1:tenant-1:summary", "run-1", "tenant-1", "Test",
            ExposureVerdict.Exposed, InvestigationStatus.Completed,
            ReviewStatus.Pending, DateTimeOffset.UtcNow, null,
            1, null, null, null
        );

        _tenantRepoMock.Setup(r => r.GetAsync("run-1", "tenant-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingResult);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateTicketAsync("run-1", "tenant-1", "admin"));
    }
}
