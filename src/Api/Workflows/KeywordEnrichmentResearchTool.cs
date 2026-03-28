using Trace.Contracts;

namespace Trace.Api.Workflows;

public class KeywordEnrichmentResearchTool : ICveResearchTool
{
    public string Name => nameof(KeywordEnrichmentResearchTool);

    public Task<CveResearchToolOutput> ExecuteAsync(CveResearchToolInput input, CancellationToken ct = default)
    {
        var combined = $"{input.Run.Title} {input.Run.Description}".ToLowerInvariant();
        var products = new List<AffectedProduct>();
        var fixes = new List<FixReference>();
        var detectionHints = new List<string>();
        var collectorQueries = new List<CollectorQueryHint>();

        if (combined.Contains("outlook"))
        {
            products.Add(new AffectedProduct("Microsoft Outlook", ["Review installed build and update channel."]));
            fixes.Add(new FixReference($"{input.Run.CveId}-outlook-guidance", "Review Microsoft Outlook security guidance for the affected build train."));
            detectionHints.Add("Compare Outlook versions against the vulnerable release family.");
            collectorQueries.Add(new CollectorQueryHint("Defender", "software=Outlook", "Find endpoints with Outlook installations that require review."));
        }

        if (combined.Contains("windows"))
        {
            products.Add(new AffectedProduct("Microsoft Windows", ["Review monthly cumulative update posture."]));
            fixes.Add(new FixReference($"{input.Run.CveId}-windows-guidance", "Review Microsoft Windows update guidance for the affected servicing baseline."));
            detectionHints.Add("Correlate the vulnerable CVE with Windows endpoint patch status.");
            collectorQueries.Add(new CollectorQueryHint("Azure", "resourceType =~ 'microsoft.compute/virtualmachines'", "Inspect Windows guest inventory and extension state."));
        }

        if (combined.Contains("partner center"))
        {
            products.Add(new AffectedProduct("Microsoft Partner Center", ["Review affected administrative surface and identity controls."]));
            detectionHints.Add("Inspect privileged administrative access tied to the affected Partner Center workflow.");
            collectorQueries.Add(new CollectorQueryHint("Azure", "resourceType =~ 'microsoft.authorization/roleAssignments'", "Review administrative scope and elevated access patterns."));
        }

        if (products.Count == 0)
        {
            detectionHints.Add("Inspect software inventory and patch posture for the named CVE across onboarded tenants.");
        }

        collectorQueries.Add(new CollectorQueryHint("Defender", $"cveId={input.Run.CveId}", "Search vulnerability inventory for the submitted CVE."));

        return Task.FromResult(new CveResearchToolOutput(
            AffectedProducts: products,
            Fixes: fixes,
            DetectionHints: detectionHints,
            CollectorQueries: collectorQueries,
            Summary: $"Derived {products.Count} affected product hints and {collectorQueries.Count} collector queries from request keywords."));
    }
}