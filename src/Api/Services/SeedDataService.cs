using Trace.Api.Repositories;
using Trace.Contracts;

namespace Trace.Api.Services;

// Provides believable fake seed data for local development demos.
// Seeding only runs when the InvestigationRuns container is empty,
// so it is safe to restart without duplicates.
public static class SeedDataService
{
    public static async Task SeedAsync(
        IInvestigationRepository runRepo,
        ITenantResultRepository tenantRepo,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existing = await runRepo.ListRecentAsync(1, ct);
        if (existing.Count > 0)
        {
            logger.LogInformation("Seed data already present, skipping.");
            return;
        }

        logger.LogInformation("Seeding local demo data...");

        var now = DateTimeOffset.UtcNow;
        var tenants = new[] { "tenant-alpha", "tenant-beta", "tenant-gamma", "tenant-delta" };

        var runs = new[]
        {
            ("CVE-2024-21413", "Microsoft Outlook MHTML Vulnerability", InvestigationStatus.Completed, -5),
            ("CVE-2024-30103", "Microsoft Outlook Remote Code Execution", InvestigationStatus.Completed, -3),
            ("CVE-2024-38021", "Microsoft Outlook Elevation of Privilege", InvestigationStatus.Running, -1),
            ("CVE-2024-49035", "Microsoft Partner Center Privilege Escalation", InvestigationStatus.Pending, 0),
        };

        foreach (var (cveId, title, status, daysAgo) in runs)
        {
            var runId = Guid.NewGuid().ToString();
            var started = now.AddDays(daysAgo).AddHours(-2);

            var run = new InvestigationRun(
                runId, cveId, title,
                $"Investigating {cveId} across managed tenants.",
                status,
                started,
                status == InvestigationStatus.Completed ? started.AddHours(1) : null,
                tenants.Length,
                status == InvestigationStatus.Completed ? tenants.Length : status == InvestigationStatus.Running ? 2 : 0,
                status == InvestigationStatus.Completed ? new Random(runId.GetHashCode()).Next(2, 12) : 0,
                "seed-admin"
            );

            await runRepo.CreateAsync(run, ct);

            var verdicts = new[] { ExposureVerdict.Exposed, ExposureVerdict.NotExposed, ExposureVerdict.PotentiallyExposed, ExposureVerdict.NotExposed };
            var reviews = new[] { ReviewStatus.Approved, ReviewStatus.Approved, ReviewStatus.Pending, ReviewStatus.Pending };

            for (int i = 0; i < tenants.Length; i++)
            {
                var tenantId = tenants[i];
                var tenantStatus = status == InvestigationStatus.Completed ? InvestigationStatus.Completed
                    : (i < 2 && status == InvestigationStatus.Running) ? InvestigationStatus.Completed
                    : InvestigationStatus.Pending;

                var verdict = tenantStatus == InvestigationStatus.Completed ? verdicts[i] : ExposureVerdict.Unknown;
                var review = tenantStatus == InvestigationStatus.Completed ? reviews[i] : ReviewStatus.Pending;

                var findings = verdict is ExposureVerdict.Exposed or ExposureVerdict.PotentiallyExposed
                    ? new Random((runId + tenantId).GetHashCode()).Next(1, 5) : 0;

                var tenantResult = new TenantInvestigationResult(
                    $"{runId}:{tenantId}:summary",
                    runId, tenantId,
                    tenantId switch
                    {
                        "tenant-alpha" => "Contoso Corp",
                        "tenant-beta" => "Fabrikam Inc",
                        "tenant-gamma" => "Northwind Traders",
                        _ => "Adventure Works"
                    },
                    verdict,
                    tenantStatus,
                    review,
                    started.AddMinutes(i * 10),
                    tenantStatus == InvestigationStatus.Completed ? started.AddMinutes(i * 10 + 30) : null,
                    findings,
                    review == ReviewStatus.Approved ? "alice@contoso.com" : null,
                    review == ReviewStatus.Approved ? started.AddHours(3) : null,
                    review == ReviewStatus.Approved ? "Reviewed and confirmed." : null
                );

                await tenantRepo.CreateAsync(tenantResult, ct);

                for (int f = 0; f < findings; f++)
                {
                    var finding = new Finding(
                        Guid.NewGuid().ToString(),
                        runId, tenantId,
                        $"Vulnerable resource detected – {cveId}",
                        $"Resource in subscription sub-{f:00} is running a vulnerable version affected by {cveId}.",
                        FindingType.VulnerableResourceFound,
                        ExposureVerdict.Exposed,
                        $"/subscriptions/sub-{f:00}/resourceGroups/rg-prod/providers/Microsoft.Compute/virtualMachines/vm-{f:00}",
                        "Microsoft.Compute/virtualMachines",
                        $"sub-{f:00}",
                        started.AddMinutes(i * 10 + f * 2),
                        [
                            new EvidenceArtifact(
                                Guid.NewGuid().ToString(),
                                "Resource Graph Query Result",
                                "ResourceGraphQuery",
                                $"{{ \"resourceId\": \"/subscriptions/sub-{f:00}/...\", \"version\": \"1.0.0-vulnerable\" }}",
                                started.AddMinutes(i * 10 + f * 2)
                            )
                        ]
                    );
                    await tenantRepo.AddFindingAsync(finding, ct);
                }
            }
        }

        logger.LogInformation("Seed data complete.");
    }
}
