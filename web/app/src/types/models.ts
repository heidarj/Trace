export type InvestigationStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';
export type ExposureVerdict = 'Unknown' | 'NotExposed' | 'PotentiallyExposed' | 'Exposed' | 'Confirmed';
export type FindingType = 'VulnerableResourceFound' | 'MissingPatch' | 'MisconfiguredPolicy' | 'ExposedEndpoint' | 'SuspiciousActivity' | 'Informational';
export type ReviewStatus = 'Pending' | 'Approved' | 'Rejected';
export type TicketStatus = 'Draft' | 'Submitted' | 'Acknowledged' | 'Resolved';

export interface InvestigationRun {
  id: string;
  cveId: string;
  title: string;
  description?: string;
  status: InvestigationStatus;
  startedAt: string;
  completedAt?: string;
  totalTenants: number;
  tenantsCompleted: number;
  findingsCount: number;
  createdBy: string;
}

export interface TenantInvestigationResult {
  id: string;
  runId: string;
  tenantId: string;
  tenantName: string;
  verdict: ExposureVerdict;
  status: InvestigationStatus;
  reviewStatus: ReviewStatus;
  startedAt: string;
  completedAt?: string;
  findingsCount: number;
  reviewedBy?: string;
  reviewedAt?: string;
  reviewNotes?: string;
}

export interface EvidenceArtifact {
  id: string;
  title: string;
  artifactType: string;
  content: string;
  collectedAt: string;
}

export interface Finding {
  id: string;
  runId: string;
  tenantId: string;
  title: string;
  description: string;
  type: FindingType;
  verdict: ExposureVerdict;
  resourceId?: string;
  resourceType?: string;
  subscriptionId?: string;
  detectedAt: string;
  evidence: EvidenceArtifact[];
}

export interface ReviewDecision {
  runId: string;
  tenantId: string;
  decision: ReviewStatus;
  reviewedBy: string;
  notes?: string;
}

export interface TicketRecommendation {
  id: string;
  runId: string;
  tenantId: string;
  title: string;
  description: string;
  cveId: string;
  tenantName: string;
  status: TicketStatus;
  createdAt: string;
  externalTicketId?: string;
  externalSystem?: string;
}

export interface CveInvestigationRequest {
  cveId: string;
  title: string;
  description?: string;
  tenantIds: string[];
}
