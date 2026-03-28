using Trace.Api.Services;
using Trace.Contracts;

namespace Trace.Api.Workflows;

public class RequestNormalizationResearchTool : ICveResearchTool
{
    private readonly IResearchModelClient _modelClient;

    public RequestNormalizationResearchTool(IResearchModelClient modelClient)
    {
        _modelClient = modelClient;
    }

    public string Name => nameof(RequestNormalizationResearchTool);

    public async Task<CveResearchToolOutput> ExecuteAsync(CveResearchToolInput input, CancellationToken ct = default)
    {
        var suggestion = await _modelClient.GenerateAsync(input.Run.CveId, input.Run.Title, input.Run.Description, ct);
        return new CveResearchToolOutput(
            NormalizedCveIds: [suggestion.NormalizedCveId],
            ExploitationStatus: suggestion.ExploitationStatus,
            DetectionHints: suggestion.DetectionHints,
            Summary: suggestion.ResearchSummary);
    }
}