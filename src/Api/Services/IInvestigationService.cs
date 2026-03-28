using Trace.Contracts;

namespace Trace.Api.Services;

public interface IInvestigationService
{
    Task<InvestigationRun> StartInvestigationAsync(CveInvestigationRequest request, string createdBy, CancellationToken ct = default);
    Task<InvestigationRun?> GetRunAsync(string runId, CancellationToken ct = default);
    Task<IReadOnlyList<InvestigationRun>> ListRecentRunsAsync(int limit = 20, CancellationToken ct = default);
    Task<IReadOnlyList<TenantInvestigationResult>> GetTenantResultsAsync(string runId, CancellationToken ct = default);
    Task<TenantInvestigationResult?> GetTenantResultAsync(string runId, string tenantId, CancellationToken ct = default);
    Task<TenantInvestigationResult> ReviewTenantResultAsync(string runId, string tenantId, ReviewDecision decision, CancellationToken ct = default);
}
