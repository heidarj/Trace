using Trace.Contracts;

namespace Trace.Api.Repositories;

public interface ITenantResultRepository
{
    Task<TenantInvestigationResult> CreateAsync(TenantInvestigationResult result, CancellationToken ct = default);
    Task<TenantInvestigationResult?> GetAsync(string runId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantInvestigationResult>> ListByRunAsync(string runId, CancellationToken ct = default);
    Task<TenantInvestigationResult> UpdateAsync(TenantInvestigationResult result, CancellationToken ct = default);
    Task<Finding> AddFindingAsync(Finding finding, CancellationToken ct = default);
    Task<IReadOnlyList<Finding>> GetFindingsAsync(string runId, string tenantId, CancellationToken ct = default);
    Task<TicketRecommendation> CreateTicketAsync(TicketRecommendation ticket, CancellationToken ct = default);
    Task<IReadOnlyList<TicketRecommendation>> ListTicketsAsync(CancellationToken ct = default);
}
