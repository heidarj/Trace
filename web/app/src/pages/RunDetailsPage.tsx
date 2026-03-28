import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Text,
  Title1,
  Title2,
  Body1,
  Badge,
  Spinner,
  ProgressBar,
  Table,
  TableHeader,
  TableHeaderCell,
  TableBody,
  TableRow,
  TableCell,
  TableCellLayout,
  makeStyles,
  tokens,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbButton,
  BreadcrumbDivider,
  Card,
} from '@fluentui/react-components';
import type { InvestigationRun, TenantInvestigationResult } from '../types/models';
import { api } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';
import { VerdictBadge } from '../components/VerdictBadge';
import { ReviewBadge } from '../components/ReviewBadge';

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
  metaRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
    flexWrap: 'wrap',
  },
  summaryCards: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalXL,
  },
  summaryCard: {
    padding: tokens.spacingHorizontalL,
    textAlign: 'center',
  },
  summaryNumber: {
    fontSize: tokens.fontSizeHero700,
    fontWeight: tokens.fontWeightBold,
    color: tokens.colorBrandForeground1,
  },
  progressSection: {
    marginBottom: tokens.spacingVerticalL,
  },
  progressMeta: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
  clickableRow: {
    cursor: 'pointer',
    ':hover': { backgroundColor: tokens.colorNeutralBackground1Hover },
  },
});

export function RunDetailsPage() {
  const styles = useStyles();
  const { runId } = useParams<{ runId: string }>();
  const navigate = useNavigate();
  const [run, setRun] = useState<InvestigationRun | null>(null);
  const [tenants, setTenants] = useState<TenantInvestigationResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDetails = async (showLoader = false) => {
    if (!runId) return;

    try {
      if (showLoader) {
        setLoading(true);
      }

      const [r, t] = await Promise.all([api.getInvestigation(runId), api.getTenantResults(runId)]);
      setRun(r);
      setTenants(t);
      setError(null);
    } catch {
      setError('Failed to load run details.');
    } finally {
      if (showLoader) {
        setLoading(false);
      }
    }
  };

  useEffect(() => {
    void loadDetails(true);
  }, [runId]);

  useEffect(() => {
    if (!runId || !run || (run.status !== 'Pending' && run.status !== 'Running')) {
      return;
    }

    const timer = window.setInterval(() => {
      void loadDetails();
    }, 5000);

    return () => window.clearInterval(timer);
  }, [runId, run?.status]);

  if (loading) return <div style={{ padding: 40 }}><Spinner label="Loading..." /></div>;
  if (error || !run) return <div style={{ padding: 40 }}><Body1>{error ?? 'Run not found.'}</Body1></div>;

  const exposedCount = tenants.filter(t => t.verdict === 'Exposed' || t.verdict === 'Confirmed').length;
  const notExposedCount = tenants.filter(t => t.verdict === 'NotExposed').length;
  const pendingReviewCount = tenants.filter(t => t.reviewStatus === 'Pending' && t.status === 'Completed').length;
  const progressValue = run.totalTenants > 0 ? run.tenantsCompleted / run.totalTenants : 0;

  return (
    <div className={styles.root}>
      <Breadcrumb aria-label="breadcrumb">
        <BreadcrumbItem>
          <BreadcrumbButton onClick={() => navigate('/')}>Investigations</BreadcrumbButton>
        </BreadcrumbItem>
        <BreadcrumbDivider />
        <BreadcrumbItem>
          <BreadcrumbButton current>{run.cveId}</BreadcrumbButton>
        </BreadcrumbItem>
      </Breadcrumb>

      <div className={styles.header}>
        <Title1 style={{ fontFamily: 'monospace' }}>{run.cveId}</Title1>
        <Title2>{run.title}</Title2>
        <div className={styles.metaRow}>
          <StatusBadge status={run.status} />
          <Text>Started: {new Date(run.startedAt).toLocaleString()}</Text>
          {run.completedAt && <Text>Completed: {new Date(run.completedAt).toLocaleString()}</Text>}
          <Text>By: {run.createdBy}</Text>
        </div>
        {run.description && <Body1>{run.description}</Body1>}
      </div>

      {/* Progress */}
      <div className={styles.progressSection}>
        <Text>Tenant progress: {run.tenantsCompleted} / {run.totalTenants}</Text>
        <ProgressBar value={progressValue} style={{ marginTop: 8 }} />
        <div className={styles.progressMeta}>
          <Badge appearance="outline" color="informative">{run.currentStage ?? 'Queued'}</Badge>
          {run.progressMessage && <Text>{run.progressMessage}</Text>}
          {run.lastCheckpointAt && <Text>Last checkpoint: {new Date(run.lastCheckpointAt).toLocaleString()}</Text>}
        </div>
      </div>

      {/* Summary */}
      <div className={styles.summaryCards}>
        {[
          { label: 'Total Tenants', value: run.totalTenants, color: undefined },
          { label: 'Exposed', value: exposedCount, color: exposedCount > 0 ? tokens.colorPaletteRedForeground1 : undefined },
          { label: 'Not Exposed', value: notExposedCount, color: tokens.colorPaletteGreenForeground1 },
          { label: 'Pending Review', value: pendingReviewCount, color: pendingReviewCount > 0 ? tokens.colorPaletteYellowForeground1 : undefined },
          { label: 'Total Findings', value: run.findingsCount },
        ].map(({ label, value, color }) => (
          <Card key={label} className={styles.summaryCard}>
            <div className={styles.summaryNumber} style={{ color }}>{value}</div>
            <Body1>{label}</Body1>
          </Card>
        ))}
      </div>

      {/* Tenant results table */}
      <Title2>Tenant Results</Title2>
      <Table aria-label="Tenant results" style={{ marginTop: 12 }}>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Tenant</TableHeaderCell>
            <TableHeaderCell>Status</TableHeaderCell>
            <TableHeaderCell>Verdict</TableHeaderCell>
            <TableHeaderCell>Review</TableHeaderCell>
            <TableHeaderCell>Findings</TableHeaderCell>
            <TableHeaderCell>Reviewed By</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {tenants.map(t => (
            <TableRow
              key={t.id}
              className={styles.clickableRow}
              onClick={() => navigate(`/investigations/${runId}/tenants/${t.tenantId}`)}
            >
              <TableCell>
                <TableCellLayout>
                  <Text weight="semibold">{t.tenantName}</Text>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>{t.tenantId}</Text>
                </TableCellLayout>
              </TableCell>
              <TableCell><StatusBadge status={t.status} /></TableCell>
              <TableCell><VerdictBadge verdict={t.verdict} /></TableCell>
              <TableCell><ReviewBadge status={t.reviewStatus} /></TableCell>
              <TableCell>
                {t.findingsCount > 0
                  ? <Badge color="danger" appearance="filled">{t.findingsCount}</Badge>
                  : <Text>—</Text>
                }
              </TableCell>
              <TableCell>{t.reviewedBy ?? '—'}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
