namespace Trace.Contracts;

public enum InvestigationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum ExposureVerdict
{
    Unknown,
    NotExposed,
    PotentiallyExposed,
    Exposed,
    Confirmed
}

public enum FindingType
{
    VulnerableResourceFound,
    MissingPatch,
    MisconfiguredPolicy,
    ExposedEndpoint,
    SuspiciousActivity,
    Informational
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}

public enum TicketStatus
{
    Draft,
    Submitted,
    Acknowledged,
    Resolved
}

public enum WorkflowStage
{
    Queued,
    Research,
    TenantFanOut,
    Aggregation,
    Completed,
    Failed
}

public enum WorkItemStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public enum WorkItemType
{
    Research,
    TenantInvestigation,
    Aggregation
}
