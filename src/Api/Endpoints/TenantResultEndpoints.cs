using Trace.Api.Services;
using Trace.Contracts;

namespace Trace.Api.Endpoints;

public static class TenantResultEndpoints
{
    public static IEndpointRouteBuilder MapTenantResultEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/investigations/{runId}/tenants")
            .WithTags("TenantResults");

        group.MapGet("/", GetTenantResults)
            .WithName("GetTenantResults")
            .WithSummary("Get all tenant results for a run");

        group.MapGet("/{tenantId}", GetTenantResult)
            .WithName("GetTenantResult")
            .WithSummary("Get a single tenant result");

        group.MapPost("/{tenantId}/review", ReviewTenantResult)
            .WithName("ReviewTenantResult")
            .WithSummary("Approve or reject a tenant result");

        return app;
    }

    private static async Task<IResult> GetTenantResults(
        string runId,
        IInvestigationService service,
        CancellationToken ct)
    {
        var results = await service.GetTenantResultsAsync(runId, ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> GetTenantResult(
        string runId,
        string tenantId,
        IInvestigationService service,
        CancellationToken ct)
    {
        var result = await service.GetTenantResultAsync(runId, tenantId, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ReviewTenantResult(
        string runId,
        string tenantId,
        ReviewDecision decision,
        IInvestigationService service,
        HttpContext httpContext,
        CancellationToken ct)
    {
        try
        {
            var reviewer = httpContext.User.Identity?.Name ?? decision.ReviewedBy;
            var actualDecision = decision with { ReviewedBy = reviewer };
            var result = await service.ReviewTenantResultAsync(runId, tenantId, actualDecision, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { ex.Message });
        }
    }
}
