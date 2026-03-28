import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Button,
  Text,
  Title1,
  Title2,
  Body1,
  Spinner,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogContent,
  DialogActions,
  DialogBody,
  Field,
  Textarea,
  makeStyles,
  tokens,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbButton,
  BreadcrumbDivider,
  Divider,
} from '@fluentui/react-components';
import { CheckmarkCircleRegular, DismissCircleRegular } from '@fluentui/react-icons';
import type { TenantInvestigationResult } from '../types/models';
import { api } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';
import { VerdictBadge } from '../components/VerdictBadge';
import { ReviewBadge } from '../components/ReviewBadge';

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXL,
    maxWidth: '1100px',
    margin: '0 auto',
  },
  metaGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
    marginBottom: tokens.spacingVerticalL,
  },
  metaItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  reviewActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
});

export function TenantResultPage() {
  const styles = useStyles();
  const { runId, tenantId } = useParams<{ runId: string; tenantId: string }>();
  const navigate = useNavigate();
  const [result, setResult] = useState<TenantInvestigationResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reviewDialogOpen, setReviewDialogOpen] = useState(false);
  const [reviewAction, setReviewAction] = useState<'Approved' | 'Rejected'>('Approved');
  const [reviewNotes, setReviewNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [ticketSubmitting, setTicketSubmitting] = useState(false);

  const load = async () => {
    if (!runId || !tenantId) return;
    try {
      setLoading(true);
      const r = await api.getTenantResult(runId, tenantId);
      setResult(r);
      setError(null);
    } catch {
      setError('Failed to load tenant result.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [runId, tenantId]);

  const handleReview = async () => {
    if (!runId || !tenantId) return;
    try {
      setSubmitting(true);
      const updated = await api.reviewTenantResult(runId, tenantId, {
        runId,
        tenantId,
        decision: reviewAction,
        reviewedBy: 'current-user@example.com',
        notes: reviewNotes || undefined,
      });
      setResult(updated);
      setReviewDialogOpen(false);
    } catch {
      setError('Review action failed.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateTicket = async () => {
    if (!runId || !tenantId) return;
    try {
      setTicketSubmitting(true);
      await api.createTicket(runId, tenantId);
      navigate('/tickets');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to create ticket.');
    } finally {
      setTicketSubmitting(false);
    }
  };

  if (loading) return <div style={{ padding: 40 }}><Spinner label="Loading..." /></div>;
  if (error || !result) return <div style={{ padding: 40 }}><Body1>{error ?? 'Result not found.'}</Body1></div>;

  const canReview = result.status === 'Completed' && result.reviewStatus === 'Pending';
  const canCreateTicket = result.reviewStatus === 'Approved';

  return (
    <div className={styles.root}>
      <Breadcrumb aria-label="breadcrumb">
        <BreadcrumbItem>
          <BreadcrumbButton onClick={() => navigate('/')}>Investigations</BreadcrumbButton>
        </BreadcrumbItem>
        <BreadcrumbDivider />
        <BreadcrumbItem>
          <BreadcrumbButton onClick={() => navigate(`/investigations/${runId}`)}>{runId?.slice(0, 8)}…</BreadcrumbButton>
        </BreadcrumbItem>
        <BreadcrumbDivider />
        <BreadcrumbItem>
          <BreadcrumbButton current>{result.tenantName}</BreadcrumbButton>
        </BreadcrumbItem>
      </Breadcrumb>

      <Title1 style={{ marginBottom: tokens.spacingVerticalS }}>{result.tenantName}</Title1>

      <div className={styles.metaGrid}>
        <div className={styles.metaItem}>
          <Text weight="semibold">Tenant ID</Text>
          <Text style={{ fontFamily: 'monospace' }}>{result.tenantId}</Text>
        </div>
        <div className={styles.metaItem}>
          <Text weight="semibold">Status</Text>
          <StatusBadge status={result.status} />
        </div>
        <div className={styles.metaItem}>
          <Text weight="semibold">Verdict</Text>
          <VerdictBadge verdict={result.verdict} />
        </div>
        <div className={styles.metaItem}>
          <Text weight="semibold">Review Status</Text>
          <ReviewBadge status={result.reviewStatus} />
        </div>
        <div className={styles.metaItem}>
          <Text weight="semibold">Findings</Text>
          <Text>{result.findingsCount}</Text>
        </div>
        {result.reviewedBy && (
          <div className={styles.metaItem}>
            <Text weight="semibold">Reviewed By</Text>
            <Text>{result.reviewedBy}</Text>
            {result.reviewedAt && <Text size={200}>{new Date(result.reviewedAt).toLocaleString()}</Text>}
          </div>
        )}
        {result.reviewNotes && (
          <div className={styles.metaItem} style={{ gridColumn: '1 / -1' }}>
            <Text weight="semibold">Review Notes</Text>
            <Body1>{result.reviewNotes}</Body1>
          </div>
        )}
      </div>

      {/* Review actions */}
      <div className={styles.reviewActions}>
        {canReview && (
          <>
            <Button
              appearance="primary"
              icon={<CheckmarkCircleRegular />}
              onClick={() => { setReviewAction('Approved'); setReviewDialogOpen(true); }}
            >Approve</Button>

            <Button
              appearance="secondary"
              icon={<DismissCircleRegular />}
              onClick={() => { setReviewAction('Rejected'); setReviewDialogOpen(true); }}
            >Reject</Button>
          </>
        )}
        {canCreateTicket && (
          <Button
            appearance="secondary"
            onClick={handleCreateTicket}
            disabled={ticketSubmitting}
          >
            {ticketSubmitting ? <Spinner size="tiny" /> : 'Create Ticket Recommendation'}
          </Button>
        )}
      </div>

      {/* Review dialog */}
      <Dialog open={reviewDialogOpen} onOpenChange={(_, d) => setReviewDialogOpen(d.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{reviewAction === 'Approved' ? 'Approve Result' : 'Reject Result'}</DialogTitle>
            <DialogContent>
              <Field label="Notes (optional)">
                <Textarea
                  placeholder="Optional review notes..."
                  value={reviewNotes}
                  onChange={(_, d) => setReviewNotes(d.value)}
                />
              </Field>
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">Cancel</Button>
              </DialogTrigger>
              <Button
                appearance="primary"
                onClick={handleReview}
                disabled={submitting}
              >
                {submitting ? <Spinner size="tiny" /> : 'Confirm'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Divider style={{ margin: `${tokens.spacingVerticalL} 0` }} />

      {/* Findings */}
      <Title2>Findings ({result.findingsCount})</Title2>
      {result.findingsCount === 0 ? (
        <Body1 style={{ color: tokens.colorNeutralForeground3 }}>No findings recorded for this tenant.</Body1>
      ) : (
        <Body1 style={{ color: tokens.colorNeutralForeground3, marginTop: tokens.spacingVerticalS }}>
          {result.findingsCount} finding(s) detected. Detailed findings view will show resource IDs, evidence artifacts, and remediation guidance.
        </Body1>
      )}
    </div>
  );
}
