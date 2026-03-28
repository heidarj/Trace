import type {
  InvestigationRun,
  TenantInvestigationResult,
  TicketRecommendation,
  CveInvestigationRequest,
  ReviewDecision,
} from '../types/models';

const BASE_URL = '/api';

async function fetchJson<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });
  if (!response.ok) {
    throw new Error(`API error ${response.status}: ${await response.text()}`);
  }
  return response.json() as Promise<T>;
}

export const api = {
  // Investigations
  listInvestigations: (limit = 20) =>
    fetchJson<InvestigationRun[]>(`/investigations?limit=${limit}`),

  getInvestigation: (runId: string) =>
    fetchJson<InvestigationRun>(`/investigations/${runId}`),

  createInvestigation: (request: CveInvestigationRequest) =>
    fetchJson<InvestigationRun>('/investigations', {
      method: 'POST',
      body: JSON.stringify(request),
    }),

  // Tenant results
  getTenantResults: (runId: string) =>
    fetchJson<TenantInvestigationResult[]>(`/investigations/${runId}/tenants`),

  getTenantResult: (runId: string, tenantId: string) =>
    fetchJson<TenantInvestigationResult>(`/investigations/${runId}/tenants/${tenantId}`),

  reviewTenantResult: (runId: string, tenantId: string, decision: ReviewDecision) =>
    fetchJson<TenantInvestigationResult>(`/investigations/${runId}/tenants/${tenantId}/review`, {
      method: 'POST',
      body: JSON.stringify(decision),
    }),

  // Tickets
  listTickets: () => fetchJson<TicketRecommendation[]>('/tickets'),

  createTicket: (runId: string, tenantId: string) =>
    fetchJson<TicketRecommendation>('/tickets', {
      method: 'POST',
      body: JSON.stringify({ runId, tenantId }),
    }),
};
