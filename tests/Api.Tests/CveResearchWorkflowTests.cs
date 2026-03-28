using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Trace.Api.Services;
using Trace.Api.Workflows;
using Trace.Contracts;
using Xunit;

namespace Trace.Api.Tests;

public class CveResearchWorkflowTests
{
    [Fact]
    public async Task ExecuteAsync_BuildsTypedInvestigationContext()
    {
        var workflow = new CveResearchWorkflow(
            [
                new RequestNormalizationResearchTool(new MockResearchModelClient()),
                new KeywordEnrichmentResearchTool()
            ],
            Mock.Of<ILogger<CveResearchWorkflow>>());

        var run = new InvestigationRun(
            "run-1",
            "cve-2024-12345",
            "Microsoft Outlook Remote Code Execution",
            "Investigate Outlook clients that may require an urgent update.",
            InvestigationStatus.Running,
            DateTimeOffset.UtcNow,
            null,
            2,
            0,
            0,
            "tester");

        var result = await workflow.ExecuteAsync(run, ["tenant-1", "tenant-2"]);

        result.RunId.Should().Be("run-1");
        result.NormalizedCveIds.Should().Contain("CVE-2024-12345");
        result.AffectedProducts.Should().Contain(product => product.Name == "Microsoft Outlook");
        result.CollectorQueries.Should().NotBeEmpty();
        result.ResearchSummary.Should().NotBeNullOrWhiteSpace();
    }
}