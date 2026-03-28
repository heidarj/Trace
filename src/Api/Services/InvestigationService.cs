using Trace.Api.Repositories;
using Trace.Contracts;

namespace Trace.Api.Services;

public class InvestigationService : IInvestigationService
{
    private readonly IInvestigationRepository _runRepo;
    private readonly ITenantResultRepository _tenantRepo;
    private readonly ILogger<InvestigationService> _logger;

    public InvestigationService(
        IInvestigationRepository runRepo,
        ITenantResultRepository tenantRepo,
        ILogger<InvestigationService> logger)
    {
        _runRepo = runRepo;
        _tenantRepo = tenantRepo;
        _logger = logger;
    }

    public async Task<InvestigationRun> StartInvestigationAsync(CveInvestigationRequest request, string createdBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting investigation for CVE {CveId} across {TenantCount} tenants",
            request.CveId, request.TenantIds.Count);

        var runId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        var run = new InvestigationRun(
            runId,
            request.CveId,
            request.Title,
            request.Description,
            InvestigationStatus.Running,
            now,
            null,
            request.TenantIds.Count,
            0,
            0,
            createdBy
        );

        var created = await _runRepo.CreateAsync(run, ct);

        var tenantTasks = request.TenantIds.Select(tenantId =>
            _tenantRepo.CreateAsync(new TenantInvestigationResult(
                $"{runId}:{tenantId}:summary",
                runId,
                tenantId,
                $"Tenant {tenantId[..Math.Min(8, tenantId.Length)]}",
                ExposureVerdict.Unknown,
                InvestigationStatus.Pending,
                ReviewStatus.Pending,
                now,
                null,
                0,
                null,
                null,
                null
            ), ct));

        await Task.WhenAll(tenantTasks);

        return created;
    }

    public async Task<InvestigationRun?> GetRunAsync(string runId, CancellationToken ct = default) =>
        await _runRepo.GetByIdAsync(runId, ct);

    public async Task<IReadOnlyList<InvestigationRun>> ListRecentRunsAsync(int limit = 20, CancellationToken ct = default) =>
        await _runRepo.ListRecentAsync(limit, ct);

    public async Task<IReadOnlyList<TenantInvestigationResult>> GetTenantResultsAsync(string runId, CancellationToken ct = default) =>
        await _tenantRepo.ListByRunAsync(runId, ct);

    public async Task<TenantInvestigationResult?> GetTenantResultAsync(string runId, string tenantId, CancellationToken ct = default) =>
        await _tenantRepo.GetAsync(runId, tenantId, ct);

    public async Task<TenantInvestigationResult> ReviewTenantResultAsync(string runId, string tenantId, ReviewDecision decision, CancellationToken ct = default)
    {
        _logger.LogInformation("Reviewing tenant result {RunId}/{TenantId} as {Decision}", runId, tenantId, decision.Decision);

        var existing = await _tenantRepo.GetAsync(runId, tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant result not found: {runId}/{tenantId}");

        var updated = existing with
        {
            ReviewStatus = decision.Decision,
            ReviewedBy = decision.ReviewedBy,
            ReviewedAt = DateTimeOffset.UtcNow,
            ReviewNotes = decision.Notes
        };

        return await _tenantRepo.UpdateAsync(updated, ct);
    }
}
