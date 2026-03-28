using Trace.Contracts;

namespace Trace.Api.Services;

public interface IResearchModelClient
{
    Task<ResearchModelSuggestion> GenerateAsync(string cveId, string title, string? description, CancellationToken ct = default);
}

public sealed record ResearchModelSuggestion(
    string NormalizedCveId,
    string ExploitationStatus,
    IReadOnlyList<string> DetectionHints,
    string ResearchSummary
);