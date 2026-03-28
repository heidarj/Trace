namespace Trace.Contracts;

public record InvestigationRun(
    string Id,
    string CveId,
    string Title,
    string? Description,
    InvestigationStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int TotalTenants,
    int TenantsCompleted,
    int FindingsCount,
    string CreatedBy
);

public record InvestigationRunSummary(
    string Id,
    string CveId,
    string Title,
    InvestigationStatus Status,
    DateTimeOffset StartedAt,
    int TotalTenants,
    int FindingsCount
);

public record InvestigationContext(
    string RunId,
    string CveId,
    string Title,
    IReadOnlyList<string> TenantIds,
    DateTimeOffset StartedAt
);

public record TenantInvestigationResult(
    string Id,
    string RunId,
    string TenantId,
    string TenantName,
    ExposureVerdict Verdict,
    InvestigationStatus Status,
    ReviewStatus ReviewStatus,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int FindingsCount,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    string? ReviewNotes
);

public record Finding(
    string Id,
    string RunId,
    string TenantId,
    string Title,
    string Description,
    FindingType Type,
    ExposureVerdict Verdict,
    string? ResourceId,
    string? ResourceType,
    string? SubscriptionId,
    DateTimeOffset DetectedAt,
    IReadOnlyList<EvidenceArtifact> Evidence
);

public record EvidenceArtifact(
    string Id,
    string Title,
    string ArtifactType,
    string Content,
    DateTimeOffset CollectedAt
);

public record ReviewDecision(
    string RunId,
    string TenantId,
    ReviewStatus Decision,
    string ReviewedBy,
    string? Notes
);

public record TicketRecommendation(
    string Id,
    string RunId,
    string TenantId,
    string Title,
    string Description,
    string CveId,
    string TenantName,
    TicketStatus Status,
    DateTimeOffset CreatedAt,
    string? ExternalTicketId,
    string? ExternalSystem
);
