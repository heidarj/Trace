using Trace.Contracts;

namespace Trace.Api.Services;

public interface ITicketService
{
    Task<TicketRecommendation> CreateTicketAsync(string runId, string tenantId, string createdBy, CancellationToken ct = default);
    Task<IReadOnlyList<TicketRecommendation>> ListTicketsAsync(CancellationToken ct = default);
}
