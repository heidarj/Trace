namespace Trace.Api.Services;

public class MockResearchModelClient : IResearchModelClient
{
    public Task<ResearchModelSuggestion> GenerateAsync(string cveId, string title, string? description, CancellationToken ct = default)
    {
        var normalizedCve = cveId.Trim().ToUpperInvariant();
        var combined = $"{title} {description}".ToLowerInvariant();

        var exploitationStatus = combined.Contains("remote code execution") || combined.Contains("rce")
            ? "ActiveInvestigation"
            : combined.Contains("elevation of privilege")
                ? "HighPriority"
                : "Unknown";

        var detectionHints = new List<string>
        {
            $"Normalize affected software inventory for {normalizedCve}.",
            $"Review security update posture for assets linked to {normalizedCve}."
        };

        if (combined.Contains("outlook"))
        {
            detectionHints.Add("Inspect Outlook installation versions and patch baselines.");
        }

        if (combined.Contains("windows"))
        {
            detectionHints.Add("Inspect Windows endpoint patch level and vulnerable package inventory.");
        }

        var summary = $"Prepared typed research context for {normalizedCve} using request metadata and MVP heuristics.";
        return Task.FromResult(new ResearchModelSuggestion(normalizedCve, exploitationStatus, detectionHints, summary));
    }
}