namespace Trace.Contracts;

public record CveInvestigationRequest(
    string CveId,
    string Title,
    string? Description,
    IReadOnlyList<string> TenantIds
);
