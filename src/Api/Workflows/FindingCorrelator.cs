using Trace.Contracts;

namespace Trace.Api.Workflows;

public interface IFindingCorrelator
{
    Task<FindingCorrelationResult> CorrelateAsync(IReadOnlyList<TenantInvestigationResult> tenantResults, CancellationToken ct = default);
}

public sealed record FindingCorrelationResult(
    int FindingsCount,
    int TenantsCompleted,
    InvestigationStatus Status,
    WorkflowStage FinalStage,
    string ProgressMessage
);

public class FindingCorrelator : IFindingCorrelator
{
    public Task<FindingCorrelationResult> CorrelateAsync(IReadOnlyList<TenantInvestigationResult> tenantResults, CancellationToken ct = default)
    {
        var findingsCount = tenantResults.Sum(result => result.FindingsCount);
        var tenantsCompleted = tenantResults.Count(result => result.Status == InvestigationStatus.Completed);
        var failedCount = tenantResults.Count(result => result.Status == InvestigationStatus.Failed);

        if (failedCount > 0)
        {
            return Task.FromResult(new FindingCorrelationResult(
                findingsCount,
                tenantsCompleted,
                InvestigationStatus.Failed,
                WorkflowStage.Failed,
                $"Investigation finished with {failedCount} failed tenant workflows and {findingsCount} findings."));
        }

        return Task.FromResult(new FindingCorrelationResult(
            findingsCount,
            tenantsCompleted,
            InvestigationStatus.Completed,
            WorkflowStage.Completed,
            $"Investigation completed for {tenantsCompleted} tenants with {findingsCount} findings."));
    }
}