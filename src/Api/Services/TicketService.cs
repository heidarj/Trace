using Trace.Api.Repositories;
using Trace.Contracts;

namespace Trace.Api.Services;

public class TicketService : ITicketService
{
    private readonly ITenantResultRepository _tenantRepo;
    private readonly IInvestigationRepository _runRepo;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITenantResultRepository tenantRepo,
        IInvestigationRepository runRepo,
        ILogger<TicketService> logger)
    {
        _tenantRepo = tenantRepo;
        _runRepo = runRepo;
        _logger = logger;
    }

    public async Task<TicketRecommendation> CreateTicketAsync(string runId, string tenantId, string createdBy, CancellationToken ct = default)
    {
        var tenantResult = await _tenantRepo.GetAsync(runId, tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant result not found: {runId}/{tenantId}");

        if (tenantResult.ReviewStatus != ReviewStatus.Approved)
            throw new InvalidOperationException("Only approved tenant results can generate ticket recommendations.");

        var run = await _runRepo.GetByIdAsync(runId, ct)
            ?? throw new KeyNotFoundException($"Investigation run not found: {runId}");

        _logger.LogInformation("Creating ticket for run {RunId}, tenant {TenantId}", runId, tenantId);

        var ticket = new TicketRecommendation(
            Guid.NewGuid().ToString(),
            runId,
            tenantId,
            $"[{run.CveId}] Exposure remediation – {tenantResult.TenantName}",
            $"Tenant {tenantResult.TenantName} was found to be exposed to {run.CveId}. " +
            $"Verdict: {tenantResult.Verdict}. Findings: {tenantResult.FindingsCount}. " +
            $"Reviewed by: {tenantResult.ReviewedBy}.",
            run.CveId,
            tenantResult.TenantName,
            TicketStatus.Draft,
            DateTimeOffset.UtcNow,
            null,
            null
        );

        return await _tenantRepo.CreateTicketAsync(ticket, ct);
    }

    public async Task<IReadOnlyList<TicketRecommendation>> ListTicketsAsync(CancellationToken ct = default) =>
        await _tenantRepo.ListTicketsAsync(ct);
}
