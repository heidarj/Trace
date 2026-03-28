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
    string CreatedBy,
    WorkflowStage CurrentStage = WorkflowStage.Queued,
    string? ProgressMessage = null,
    DateTimeOffset? LastCheckpointAt = null
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
    DateTimeOffset StartedAt,
    IReadOnlyList<string>? NormalizedCveIds = null,
    IReadOnlyList<AffectedProduct>? AffectedProducts = null,
    IReadOnlyList<FixReference>? Fixes = null,
    string? ExploitationStatus = null,
    IReadOnlyList<string>? DetectionHints = null,
    IReadOnlyList<CollectorQueryHint>? CollectorQueries = null,
    string? ResearchSummary = null
);

public record AffectedProduct(
    string Name,
    IReadOnlyList<string>? VersionRanges = null
);

public record FixReference(
    string ArticleId,
    string Description,
    string? Url = null
);

public record CollectorQueryHint(
    string Collector,
    string Query,
    string Purpose
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
    string? ReviewNotes,
    WorkflowStage CurrentStage = WorkflowStage.Queued,
    string? ProgressMessage = null
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

public record WorkQueueItem(
    string Id,
    string RunId,
    WorkItemType WorkType,
    WorkItemStatus Status,
    DateTimeOffset CreatedAt,
    string? TenantId = null,
    string? ParentWorkItemId = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    DateTimeOffset? LeaseExpiresAt = null,
    string? WorkerId = null,
    int AttemptCount = 0,
    string? ProgressMessage = null,
    string? ErrorMessage = null,
    DateTimeOffset? LastUpdatedAt = null
);
