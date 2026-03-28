using Trace.Api.Services;
using Trace.Contracts;

namespace Trace.Api.Endpoints;

public static class InvestigationEndpoints
{
    public static IEndpointRouteBuilder MapInvestigationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/investigations")
            .WithTags("Investigations");

        group.MapPost("/", CreateInvestigation)
            .WithName("CreateInvestigation")
            .WithSummary("Start a new CVE investigation");

        group.MapGet("/", ListInvestigations)
            .WithName("ListInvestigations")
            .WithSummary("List recent investigation runs");

        group.MapGet("/{runId}", GetInvestigation)
            .WithName("GetInvestigation")
            .WithSummary("Get investigation run details");

        return app;
    }

    private static async Task<IResult> CreateInvestigation(
        CveInvestigationRequest request,
        IInvestigationService service,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var createdBy = httpContext.User.Identity?.Name ?? "system";
        var run = await service.StartInvestigationAsync(request, createdBy, ct);
        return Results.Created($"/api/investigations/{run.Id}", run);
    }

    private static async Task<IResult> ListInvestigations(
        IInvestigationService service,
        int limit = 20,
        CancellationToken ct = default)
    {
        var runs = await service.ListRecentRunsAsync(limit, ct);
        return Results.Ok(runs);
    }

    private static async Task<IResult> GetInvestigation(
        string runId,
        IInvestigationService service,
        CancellationToken ct)
    {
        var run = await service.GetRunAsync(runId, ct);
        return run is null ? Results.NotFound() : Results.Ok(run);
    }
}
