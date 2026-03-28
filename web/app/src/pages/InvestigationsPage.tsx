import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  CardHeader,
  Text,
  Title1,
  Body1,
  Badge,
  Spinner,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogContent,
  DialogActions,
  DialogBody,
  Field,
  Input,
  Textarea,
  Table,
  TableHeader,
  TableHeaderCell,
  TableBody,
  TableRow,
  TableCell,
  TableCellLayout,
  makeStyles,
  tokens,
  Toolbar,
  ToolbarButton,
} from '@fluentui/react-components';
import { AddRegular, ArrowClockwiseRegular } from '@fluentui/react-icons';
import type { InvestigationRun, CveInvestigationRequest } from '../types/models';
import { api } from '../api/client';
import { StatusBadge } from '../components/StatusBadge';

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  statsRow: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalXL,
  },
  statCard: {
    padding: tokens.spacingHorizontalL,
    textAlign: 'center',
  },
  statNumber: {
    fontSize: tokens.fontSizeHero700,
    fontWeight: tokens.fontWeightBold,
    color: tokens.colorBrandForeground1,
  },
  table: {
    marginTop: tokens.spacingVerticalM,
  },
  clickableRow: {
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
});

export function InvestigationsPage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [runs, setRuns] = useState<InvestigationRun[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<CveInvestigationRequest>({
    cveId: '',
    title: '',
    description: '',
    tenantIds: [],
  });
  const [tenantIdsInput, setTenantIdsInput] = useState('');

  const loadRuns = async () => {
    try {
      setLoading(true);
      const data = await api.listInvestigations();
      setRuns(data);
      setError(null);
    } catch {
      setError('Failed to load investigations. Make sure the API is running.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadRuns(); }, []);

  const handleSubmit = async () => {
    try {
      setSubmitting(true);
      const request: CveInvestigationRequest = {
        ...form,
        tenantIds: tenantIdsInput.split(',').map(s => s.trim()).filter(Boolean),
      };
      const run = await api.createInvestigation(request);
      setDialogOpen(false);
      setRuns(prev => [run, ...prev]);
      navigate(`/investigations/${run.id}`);
    } catch {
      setError('Failed to start investigation.');
    } finally {
      setSubmitting(false);
    }
  };

  const completedCount = runs.filter(r => r.status === 'Completed').length;
  const runningCount = runs.filter(r => r.status === 'Running').length;
  const totalFindings = runs.reduce((sum, r) => sum + r.findingsCount, 0);
  const uniqueCves = new Set(runs.map(r => r.cveId)).size;

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title1>Investigations</Title1>
          <Body1>CVE exposure investigations across managed tenants</Body1>
        </div>
        <Toolbar>
          <ToolbarButton icon={<ArrowClockwiseRegular />} onClick={loadRuns}>Refresh</ToolbarButton>
          <Dialog open={dialogOpen} onOpenChange={(_, d) => setDialogOpen(d.open)}>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="primary" icon={<AddRegular />}>New Investigation</Button>
            </DialogTrigger>
            <DialogSurface>
              <DialogBody>
                <DialogTitle>Start CVE Investigation</DialogTitle>
                <DialogContent>
                  <Field label="CVE ID" required>
                    <Input
                      placeholder="CVE-2024-12345"
                      value={form.cveId}
                      onChange={(_, d) => setForm(f => ({ ...f, cveId: d.value }))}
                    />
                  </Field>
                  <Field label="Title" required>
                    <Input
                      placeholder="Brief description of the vulnerability"
                      value={form.title}
                      onChange={(_, d) => setForm(f => ({ ...f, title: d.value }))}
                    />
                  </Field>
                  <Field label="Description">
                    <Textarea
                      placeholder="Additional context..."
                      value={form.description}
                      onChange={(_, d) => setForm(f => ({ ...f, description: d.value }))}
                    />
                  </Field>
                  <Field label="Tenant IDs (comma-separated)" required>
                    <Input
                      placeholder="tenant-alpha, tenant-beta"
                      value={tenantIdsInput}
                      onChange={(_, d) => setTenantIdsInput(d.value)}
                    />
                  </Field>
                </DialogContent>
                <DialogActions>
                  <DialogTrigger disableButtonEnhancement>
                    <Button appearance="secondary">Cancel</Button>
                  </DialogTrigger>
                  <Button
                    appearance="primary"
                    onClick={handleSubmit}
                    disabled={submitting || !form.cveId || !form.title || !tenantIdsInput}
                  >
                    {submitting ? <Spinner size="tiny" /> : 'Start Investigation'}
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        </Toolbar>
      </div>

      {/* Stats */}
      <div className={styles.statsRow}>
        {[
          { label: 'Total Investigations', value: runs.length },
          { label: 'Completed', value: completedCount },
          { label: 'In Progress', value: runningCount },
          { label: 'Unique CVEs', value: uniqueCves },
          { label: 'Total Findings', value: totalFindings },
        ].map(({ label, value }) => (
          <Card key={label} className={styles.statCard}>
            <div className={styles.statNumber}>{value}</div>
            <Body1>{label}</Body1>
          </Card>
        ))}
      </div>

      {/* Table */}
      {loading ? (
        <Spinner label="Loading investigations..." />
      ) : error ? (
        <Card>
          <CardHeader header={<Text weight="semibold">Error</Text>} />
          <Body1>{error}</Body1>
        </Card>
      ) : (
        <Table aria-label="Investigation runs" className={styles.table}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>CVE ID</TableHeaderCell>
              <TableHeaderCell>Title</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Tenants</TableHeaderCell>
              <TableHeaderCell>Findings</TableHeaderCell>
              <TableHeaderCell>Started</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {runs.map(run => (
              <TableRow
                key={run.id}
                className={styles.clickableRow}
                onClick={() => navigate(`/investigations/${run.id}`)}
              >
                <TableCell>
                  <TableCellLayout>
                    <Text weight="semibold" style={{ fontFamily: 'monospace' }}>{run.cveId}</Text>
                  </TableCellLayout>
                </TableCell>
                <TableCell>{run.title}</TableCell>
                <TableCell><StatusBadge status={run.status} /></TableCell>
                <TableCell>{run.tenantsCompleted}/{run.totalTenants}</TableCell>
                <TableCell>
                  {run.findingsCount > 0
                    ? <Badge color="danger" appearance="filled">{run.findingsCount}</Badge>
                    : <Badge color="subtle" appearance="outline">0</Badge>
                  }
                </TableCell>
                <TableCell>{new Date(run.startedAt).toLocaleString()}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
