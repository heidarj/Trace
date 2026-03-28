using Trace.Api.Services;
using Trace.Contracts;

namespace Trace.Api.Workflows;

public interface ICveResearchWorkflow
{
    Task<InvestigationContext> ExecuteAsync(InvestigationRun run, IReadOnlyList<string> tenantIds, CancellationToken ct = default);
}

public interface ICveResearchTool
{
    string Name { get; }
    Task<CveResearchToolOutput> ExecuteAsync(CveResearchToolInput input, CancellationToken ct = default);
}

public sealed record CveResearchToolInput(
    InvestigationRun Run,
    IReadOnlyList<string> TenantIds
);

public sealed record CveResearchToolOutput(
    IReadOnlyList<string>? NormalizedCveIds = null,
    IReadOnlyList<AffectedProduct>? AffectedProducts = null,
    IReadOnlyList<FixReference>? Fixes = null,
    string? ExploitationStatus = null,
    IReadOnlyList<string>? DetectionHints = null,
    IReadOnlyList<CollectorQueryHint>? CollectorQueries = null,
    string? Summary = null
);

public class CveResearchWorkflow : ICveResearchWorkflow
{
    private readonly IReadOnlyList<ICveResearchTool> _tools;
    private readonly ILogger<CveResearchWorkflow> _logger;

    public CveResearchWorkflow(IEnumerable<ICveResearchTool> tools, ILogger<CveResearchWorkflow> logger)
    {
        _tools = tools.ToList();
        _logger = logger;
    }

    public async Task<InvestigationContext> ExecuteAsync(InvestigationRun run, IReadOnlyList<string> tenantIds, CancellationToken ct = default)
    {
        _logger.LogInformation("Running research workflow for {RunId} with {ToolCount} tools", run.Id, _tools.Count);

        var input = new CveResearchToolInput(run, tenantIds);
        var outputs = new List<CveResearchToolOutput>(_tools.Count);

        foreach (var tool in _tools)
        {
            _logger.LogInformation("Executing research tool {ToolName} for {RunId}", tool.Name, run.Id);
            outputs.Add(await tool.ExecuteAsync(input, ct));
        }

        var normalizedCveIds = outputs.SelectMany(output => output.NormalizedCveIds ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (normalizedCveIds.Count == 0)
        {
            normalizedCveIds.Add(run.CveId.Trim().ToUpperInvariant());
        }

        var affectedProducts = outputs
            .SelectMany(output => output.AffectedProducts ?? [])
            .GroupBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AffectedProduct(
                group.First().Name,
                group.SelectMany(product => product.VersionRanges ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();

        var fixes = outputs
            .SelectMany(output => output.Fixes ?? [])
            .GroupBy(fix => fix.ArticleId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var detectionHints = outputs.SelectMany(output => output.DetectionHints ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var collectorQueries = outputs
            .SelectMany(output => output.CollectorQueries ?? [])
            .GroupBy(query => $"{query.Collector}:{query.Query}:{query.Purpose}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var researchSummary = string.Join(" ", outputs.Select(output => output.Summary).Where(summary => !string.IsNullOrWhiteSpace(summary)));
        if (string.IsNullOrWhiteSpace(researchSummary))
        {
            researchSummary = $"Prepared investigation context for {run.CveId}.";
        }

        var exploitationStatus = outputs
            .Select(output => output.ExploitationStatus)
            .FirstOrDefault(status => !string.IsNullOrWhiteSpace(status))
            ?? "Unknown";

        return new InvestigationContext(
            run.Id,
            run.CveId,
            run.Title,
            tenantIds,
            run.StartedAt,
            normalizedCveIds,
            affectedProducts,
            fixes,
            exploitationStatus,
            detectionHints,
            collectorQueries,
            researchSummary);
    }
}