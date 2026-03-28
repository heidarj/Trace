using Microsoft.Azure.Cosmos;
using Trace.Contracts;

namespace Trace.Api.Repositories;

public class TenantResultRepository : ITenantResultRepository
{
    private readonly CosmosClient _client;
    private readonly ILogger<TenantResultRepository> _logger;
    private Container? _container;

    private const string DatabaseId = "tracedb";
    private const string ContainerName = "RunTenantData";

    public TenantResultRepository(CosmosClient client, ILogger<TenantResultRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    private Container Container =>
        _container ??= _client.GetContainer(DatabaseId, ContainerName);

    public async Task<TenantInvestigationResult> CreateAsync(TenantInvestigationResult result, CancellationToken ct = default)
    {
        var doc = ToSummaryDocument(result);
        var response = await Container.CreateItemAsync(doc, new PartitionKey(doc.RunId), cancellationToken: ct);
        return FromSummaryDocument(response.Resource);
    }

    public async Task<TenantInvestigationResult?> GetAsync(string runId, string tenantId, CancellationToken ct = default)
    {
        var docId = TenantSummaryId(runId, tenantId);
        try
        {
            var response = await Container.ReadItemAsync<TenantSummaryDocument>(docId, new PartitionKey(runId), cancellationToken: ct);
            return FromSummaryDocument(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TenantInvestigationResult>> ListByRunAsync(string runId, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.runId = @runId AND c.documentType = 'tenantSummary'")
            .WithParameter("@runId", runId);

        var results = new List<TenantInvestigationResult>();
        using var feed = Container.GetItemQueryIterator<TenantSummaryDocument>(query, requestOptions: new() { PartitionKey = new PartitionKey(runId) });
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            results.AddRange(page.Select(FromSummaryDocument));
        }
        return results;
    }

    public async Task<TenantInvestigationResult> UpdateAsync(TenantInvestigationResult result, CancellationToken ct = default)
    {
        var docId = TenantSummaryId(result.RunId, result.TenantId);
        var existing = await Container.ReadItemAsync<TenantSummaryDocument>(docId, new PartitionKey(result.RunId), cancellationToken: ct);

        var doc = ToSummaryDocument(result);
        doc.ETag = existing.Resource.ETag;

        var options = new ItemRequestOptions { IfMatchEtag = doc.ETag };
        var response = await Container.ReplaceItemAsync(doc, docId, new PartitionKey(result.RunId), options, ct);
        return FromSummaryDocument(response.Resource);
    }

    public async Task SaveContextAsync(InvestigationContext context, CancellationToken ct = default)
    {
        var docId = ContextDocumentId(context.RunId);

        try
        {
            var existing = await Container.ReadItemAsync<InvestigationContextDocument>(docId, new PartitionKey(context.RunId), cancellationToken: ct);
            var replacement = ToContextDocument(context);
            replacement.ETag = existing.Resource.ETag;

            var options = new ItemRequestOptions { IfMatchEtag = replacement.ETag };
            await Container.ReplaceItemAsync(replacement, docId, new PartitionKey(context.RunId), options, ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await Container.CreateItemAsync(ToContextDocument(context), new PartitionKey(context.RunId), cancellationToken: ct);
        }
    }

    public async Task<InvestigationContext?> GetContextAsync(string runId, CancellationToken ct = default)
    {
        var docId = ContextDocumentId(runId);

        try
        {
            var response = await Container.ReadItemAsync<InvestigationContextDocument>(docId, new PartitionKey(runId), cancellationToken: ct);
            return FromContextDocument(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Finding> AddFindingAsync(Finding finding, CancellationToken ct = default)
    {
        var doc = ToFindingDocument(finding);
        var response = await Container.UpsertItemAsync(doc, new PartitionKey(finding.RunId), cancellationToken: ct);
        return FromFindingDocument(response.Resource);
    }

    public async Task<IReadOnlyList<Finding>> GetFindingsAsync(string runId, string tenantId, CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.runId = @runId AND c.tenantId = @tenantId AND c.documentType = 'finding'")
            .WithParameter("@runId", runId)
            .WithParameter("@tenantId", tenantId);

        var results = new List<Finding>();
        using var feed = Container.GetItemQueryIterator<FindingDocument>(query, requestOptions: new() { PartitionKey = new PartitionKey(runId) });
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            results.AddRange(page.Select(FromFindingDocument));
        }
        return results;
    }

    public async Task<TicketRecommendation> CreateTicketAsync(TicketRecommendation ticket, CancellationToken ct = default)
    {
        var doc = ToTicketDocument(ticket);
        var response = await Container.CreateItemAsync(doc, new PartitionKey(ticket.RunId), cancellationToken: ct);
        return FromTicketDocument(response.Resource);
    }

    public async Task<IReadOnlyList<TicketRecommendation>> ListTicketsAsync(CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.documentType = 'ticket' ORDER BY c._ts DESC");

        var results = new List<TicketRecommendation>();
        using var feed = Container.GetItemQueryIterator<TicketDocument>(query);
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            results.AddRange(page.Select(FromTicketDocument));
        }
        return results;
    }

    private static string TenantSummaryId(string runId, string tenantId) => $"{runId}:{tenantId}:summary";

    private static string ContextDocumentId(string runId) => $"{runId}:context";

    private static TenantSummaryDocument ToSummaryDocument(TenantInvestigationResult r) => new()
    {
        Id = TenantSummaryId(r.RunId, r.TenantId),
        RunId = r.RunId,
        TenantId = r.TenantId,
        TenantName = r.TenantName,
        Verdict = r.Verdict.ToString(),
        Status = r.Status.ToString(),
        ReviewStatus = r.ReviewStatus.ToString(),
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        FindingsCount = r.FindingsCount,
        ReviewedBy = r.ReviewedBy,
        ReviewedAt = r.ReviewedAt,
        ReviewNotes = r.ReviewNotes,
        CurrentStage = r.CurrentStage.ToString(),
        ProgressMessage = r.ProgressMessage
    };

    private static TenantInvestigationResult FromSummaryDocument(TenantSummaryDocument d) => new(
        d.Id,
        d.RunId,
        d.TenantId,
        d.TenantName,
        Enum.Parse<ExposureVerdict>(d.Verdict),
        Enum.Parse<InvestigationStatus>(d.Status),
        Enum.Parse<ReviewStatus>(d.ReviewStatus),
        d.StartedAt,
        d.CompletedAt,
        d.FindingsCount,
        d.ReviewedBy,
        d.ReviewedAt,
        d.ReviewNotes,
        string.IsNullOrWhiteSpace(d.CurrentStage) ? WorkflowStage.Queued : Enum.Parse<WorkflowStage>(d.CurrentStage),
        d.ProgressMessage
    );

    private static InvestigationContextDocument ToContextDocument(InvestigationContext context) => new()
    {
        Id = ContextDocumentId(context.RunId),
        RunId = context.RunId,
        CveId = context.CveId,
        Title = context.Title,
        TenantIds = context.TenantIds.ToList(),
        StartedAt = context.StartedAt,
        NormalizedCveIds = context.NormalizedCveIds?.ToList() ?? [],
        AffectedProducts = context.AffectedProducts?.Select(product => new AffectedProductDoc
        {
            Name = product.Name,
            VersionRanges = product.VersionRanges?.ToList() ?? []
        }).ToList() ?? [],
        Fixes = context.Fixes?.Select(fix => new FixReferenceDoc
        {
            ArticleId = fix.ArticleId,
            Description = fix.Description,
            Url = fix.Url
        }).ToList() ?? [],
        ExploitationStatus = context.ExploitationStatus,
        DetectionHints = context.DetectionHints?.ToList() ?? [],
        CollectorQueries = context.CollectorQueries?.Select(query => new CollectorQueryHintDoc
        {
            Collector = query.Collector,
            Query = query.Query,
            Purpose = query.Purpose
        }).ToList() ?? [],
        ResearchSummary = context.ResearchSummary
    };

    private static InvestigationContext FromContextDocument(InvestigationContextDocument d) => new(
        d.RunId,
        d.CveId,
        d.Title,
        d.TenantIds,
        d.StartedAt,
        d.NormalizedCveIds,
        d.AffectedProducts.Select(product => new AffectedProduct(product.Name, product.VersionRanges)).ToList(),
        d.Fixes.Select(fix => new FixReference(fix.ArticleId, fix.Description, fix.Url)).ToList(),
        d.ExploitationStatus,
        d.DetectionHints,
        d.CollectorQueries.Select(query => new CollectorQueryHint(query.Collector, query.Query, query.Purpose)).ToList(),
        d.ResearchSummary
    );

    private static FindingDocument ToFindingDocument(Finding f) => new()
    {
        Id = f.Id,
        RunId = f.RunId,
        TenantId = f.TenantId,
        Title = f.Title,
        Description = f.Description,
        Type = f.Type.ToString(),
        Verdict = f.Verdict.ToString(),
        ResourceId = f.ResourceId,
        ResourceType = f.ResourceType,
        SubscriptionId = f.SubscriptionId,
        DetectedAt = f.DetectedAt,
        Evidence = f.Evidence.Select(e => new EvidenceDoc { Id = e.Id, Title = e.Title, ArtifactType = e.ArtifactType, Content = e.Content, CollectedAt = e.CollectedAt }).ToList()
    };

    private static Finding FromFindingDocument(FindingDocument d) => new(
        d.Id, d.RunId, d.TenantId, d.Title, d.Description,
        Enum.Parse<FindingType>(d.Type), Enum.Parse<ExposureVerdict>(d.Verdict),
        d.ResourceId, d.ResourceType, d.SubscriptionId, d.DetectedAt,
        d.Evidence.Select(e => new EvidenceArtifact(e.Id, e.Title, e.ArtifactType, e.Content, e.CollectedAt)).ToList()
    );

    private static TicketDocument ToTicketDocument(TicketRecommendation t) => new()
    {
        Id = t.Id,
        RunId = t.RunId,
        TenantId = t.TenantId,
        Title = t.Title,
        Description = t.Description,
        CveId = t.CveId,
        TenantName = t.TenantName,
        Status = t.Status.ToString(),
        CreatedAt = t.CreatedAt,
        ExternalTicketId = t.ExternalTicketId,
        ExternalSystem = t.ExternalSystem
    };

    private static TicketRecommendation FromTicketDocument(TicketDocument d) => new(
        d.Id, d.RunId, d.TenantId, d.Title, d.Description,
        d.CveId, d.TenantName, Enum.Parse<TicketStatus>(d.Status),
        d.CreatedAt, d.ExternalTicketId, d.ExternalSystem
    );
}

internal class TenantSummaryDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "tenantSummary";
    public string RunId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int FindingsCount { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public string? CurrentStage { get; set; }
    public string? ProgressMessage { get; set; }
    public string? ETag { get; set; }
}

internal class InvestigationContextDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "investigationContext";
    public string RunId { get; set; } = string.Empty;
    public string CveId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> TenantIds { get; set; } = [];
    public DateTimeOffset StartedAt { get; set; }
    public List<string> NormalizedCveIds { get; set; } = [];
    public List<AffectedProductDoc> AffectedProducts { get; set; } = [];
    public List<FixReferenceDoc> Fixes { get; set; } = [];
    public string? ExploitationStatus { get; set; }
    public List<string> DetectionHints { get; set; } = [];
    public List<CollectorQueryHintDoc> CollectorQueries { get; set; } = [];
    public string? ResearchSummary { get; set; }
    public string? ETag { get; set; }
}

internal class AffectedProductDoc
{
    public string Name { get; set; } = string.Empty;
    public List<string> VersionRanges { get; set; } = [];
}

internal class FixReferenceDoc
{
    public string ArticleId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Url { get; set; }
}

internal class CollectorQueryHintDoc
{
    public string Collector { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}

internal class FindingDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "finding";
    public string RunId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public string? SubscriptionId { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
    public List<EvidenceDoc> Evidence { get; set; } = [];
}

internal class EvidenceDoc
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ArtifactType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CollectedAt { get; set; }
}

internal class TicketDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "ticket";
    public string RunId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CveId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? ExternalTicketId { get; set; }
    public string? ExternalSystem { get; set; }
}
