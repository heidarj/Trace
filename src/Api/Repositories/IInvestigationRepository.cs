using Trace.Contracts;

namespace Trace.Api.Repositories;

public interface IInvestigationRepository
{
    Task<InvestigationRun> CreateAsync(InvestigationRun run, CancellationToken ct = default);
    Task<InvestigationRun?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<InvestigationRun>> ListRecentAsync(int limit = 20, CancellationToken ct = default);
    Task<InvestigationRun> UpdateAsync(InvestigationRun run, CancellationToken ct = default);
}
