using Trace.Api.Services;

namespace Trace.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets");

        group.MapGet("/", ListTickets)
            .WithName("ListTickets")
            .WithSummary("List all ticket recommendations");

        group.MapPost("/", CreateTicket)
            .WithName("CreateTicket")
            .WithSummary("Create a ticket recommendation from an approved tenant result");

        return app;
    }

    private static async Task<IResult> ListTickets(
        ITicketService service,
        CancellationToken ct)
    {
        var tickets = await service.ListTicketsAsync(ct);
        return Results.Ok(tickets);
    }

    private static async Task<IResult> CreateTicket(
        CreateTicketRequest request,
        ITicketService service,
        HttpContext httpContext,
        CancellationToken ct)
    {
        try
        {
            var createdBy = httpContext.User.Identity?.Name ?? "system";
            var ticket = await service.CreateTicketAsync(request.RunId, request.TenantId, createdBy, ct);
            return Results.Created($"/api/tickets/{ticket.Id}", ticket);
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

public record CreateTicketRequest(string RunId, string TenantId);
