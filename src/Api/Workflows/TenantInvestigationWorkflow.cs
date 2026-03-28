using Trace.Contracts;

namespace Trace.Api.Workflows;

public interface ITenantInvestigationWorkflow
{
    Task<TenantWorkflowResult> ExecuteAsync(InvestigationRun run, TenantInvestigationResult tenant, InvestigationContext context, CancellationToken ct = default);
}

public sealed record TenantWorkflowResult(
    IReadOnlyList<Finding> Findings,
    ExposureVerdict Verdict,
    string ProgressMessage
);

public class TenantInvestigationWorkflow : ITenantInvestigationWorkflow
{
    private readonly IReadOnlyList<IEvidenceCollector> _collectors;
    private readonly ILogger<TenantInvestigationWorkflow> _logger;

    public TenantInvestigationWorkflow(IEnumerable<IEvidenceCollector> collectors, ILogger<TenantInvestigationWorkflow> logger)
    {
        _collectors = collectors.ToList();
        _logger = logger;
    }

    public async Task<TenantWorkflowResult> ExecuteAsync(InvestigationRun run, TenantInvestigationResult tenant, InvestigationContext context, CancellationToken ct = default)
    {
        var findings = new List<Finding>();
        foreach (var collector in _collectors)
        {
            _logger.LogInformation("Collecting evidence with {CollectorName} for {RunId}/{TenantId}", collector.Name, run.Id, tenant.TenantId);
            var collected = await collector.CollectAsync(run, tenant, context, ct);
            findings.AddRange(collected);
        }

        var verdict = DetermineVerdict(findings);
        var progressMessage = findings.Count == 0
            ? "No exposure evidence collected for this tenant during MVP investigation."
            : $"Collected {findings.Count} typed findings across {_collectors.Count} collectors.";

        return new TenantWorkflowResult(findings, verdict, progressMessage);
    }

    private static ExposureVerdict DetermineVerdict(IReadOnlyList<Finding> findings)
    {
        if (findings.Any(finding => finding.Verdict == ExposureVerdict.Confirmed))
        {
            return ExposureVerdict.Confirmed;
        }

        if (findings.Any(finding => finding.Verdict == ExposureVerdict.Exposed))
        {
            return ExposureVerdict.Exposed;
        }

        if (findings.Any(finding => finding.Verdict == ExposureVerdict.PotentiallyExposed))
        {
            return ExposureVerdict.PotentiallyExposed;
        }

        return ExposureVerdict.NotExposed;
    }
}