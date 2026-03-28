using System.Security.Cryptography;
using System.Text;
using Trace.Contracts;

namespace Trace.Api.Workflows;

public interface IEvidenceCollector
{
    string Name { get; }
    Task<IReadOnlyList<Finding>> CollectAsync(InvestigationRun run, TenantInvestigationResult tenant, InvestigationContext context, CancellationToken ct = default);
}

public class AzureMockedCollector : IEvidenceCollector
{
    public string Name => "Azure";

    public Task<IReadOnlyList<Finding>> CollectAsync(InvestigationRun run, TenantInvestigationResult tenant, InvestigationContext context, CancellationToken ct = default)
    {
        var score = DeterministicInvestigationSignals.Score(run.Id, tenant.TenantId, context.CveId, Name);
        if (score % 4 == 0)
        {
            return Task.FromResult<IReadOnlyList<Finding>>([]);
        }

        var query = context.CollectorQueries?.FirstOrDefault(hint => hint.Collector.Equals(Name, StringComparison.OrdinalIgnoreCase));
        var verdict = score % 5 == 0 ? ExposureVerdict.PotentiallyExposed : ExposureVerdict.Exposed;
        var finding = new Finding(
            $"{run.Id}:{tenant.TenantId}:azure:0",
            run.Id,
            tenant.TenantId,
            $"Azure inventory indicates exposure to {context.CveId}",
            $"Control-plane inventory for {tenant.TenantName} surfaced assets that should be reviewed for {context.CveId}.",
            score % 2 == 0 ? FindingType.MissingPatch : FindingType.MisconfiguredPolicy,
            verdict,
            $"/subscriptions/sub-{score % 10:00}/resourceGroups/rg-{tenant.TenantId}/providers/Microsoft.Compute/virtualMachines/vm-{score % 7:00}",
            "Microsoft.Compute/virtualMachines",
            $"sub-{score % 10:00}",
            DateTimeOffset.UtcNow,
            [
                new EvidenceArtifact(
                    $"{run.Id}:{tenant.TenantId}:azure:evidence",
                    "Azure mocked collector result",
                    "AzureResourceGraph",
                    query is null ? "No Azure query hint available for this CVE." : $"Executed query hint: {query.Query}",
                    DateTimeOffset.UtcNow)
            ]);

        return Task.FromResult<IReadOnlyList<Finding>>([finding]);
    }
}

public class DefenderMockedCollector : IEvidenceCollector
{
    public string Name => "Defender";

    public Task<IReadOnlyList<Finding>> CollectAsync(InvestigationRun run, TenantInvestigationResult tenant, InvestigationContext context, CancellationToken ct = default)
    {
        var score = DeterministicInvestigationSignals.Score(run.Id, tenant.TenantId, context.CveId, Name);
        if (score % 3 != 0)
        {
            return Task.FromResult<IReadOnlyList<Finding>>([]);
        }

        var query = context.CollectorQueries?.FirstOrDefault(hint => hint.Collector.Equals(Name, StringComparison.OrdinalIgnoreCase));
        var finding = new Finding(
            $"{run.Id}:{tenant.TenantId}:defender:0",
            run.Id,
            tenant.TenantId,
            $"Defender inventory found vulnerable software for {context.CveId}",
            $"Device vulnerability posture for {tenant.TenantName} includes software requiring remediation for {context.CveId}.",
            FindingType.VulnerableResourceFound,
            ExposureVerdict.Confirmed,
            $"device-{score % 1000:000}",
            "Microsoft Defender for Endpoint device",
            null,
            DateTimeOffset.UtcNow,
            [
                new EvidenceArtifact(
                    $"{run.Id}:{tenant.TenantId}:defender:evidence",
                    "Defender mocked collector result",
                    "DefenderVulnerabilityInventory",
                    query is null ? "No Defender query hint available for this CVE." : $"Executed query hint: {query.Query}",
                    DateTimeOffset.UtcNow)
            ]);

        return Task.FromResult<IReadOnlyList<Finding>>([finding]);
    }
}

internal static class DeterministicInvestigationSignals
{
    public static int Score(params string?[] values)
    {
        var input = string.Join('|', values.Where(value => !string.IsNullOrWhiteSpace(value)));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt32(bytes, 0) & int.MaxValue;
    }
}