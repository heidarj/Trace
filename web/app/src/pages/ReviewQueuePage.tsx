import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Text,
  Title1,
  Body1,
  Spinner,
  Table,
  TableHeader,
  TableHeaderCell,
  TableBody,
  TableRow,
  TableCell,
  TableCellLayout,
  Badge,
  Button,
  makeStyles,
  tokens,
  Card,
  CardHeader,
} from '@fluentui/react-components';
import { OpenRegular } from '@fluentui/react-icons';
import type { InvestigationRun, TenantInvestigationResult } from '../types/models';
import { api } from '../api/client';
import { VerdictBadge } from '../components/VerdictBadge';

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
});

interface ReviewItem {
  run: InvestigationRun;
  tenant: TenantInvestigationResult;
}

export function ReviewQueuePage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [items, setItems] = useState<ReviewItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadQueue = async () => {
      try {
        const runs = await api.listInvestigations(100);
        const completed = runs.filter(r => r.status === 'Completed' || r.status === 'Running');
        const tenantArrays = await Promise.all(completed.map(r => api.getTenantResults(r.id)));
        const queue: ReviewItem[] = [];
        completed.forEach((run, i) => {
          const pending = tenantArrays[i].filter(t => t.reviewStatus === 'Pending' && t.status === 'Completed');
          pending.forEach(tenant => queue.push({ run, tenant }));
        });
        setItems(queue);
      } catch {
        setError('Failed to load review queue.');
      } finally {
        setLoading(false);
      }
    };
    loadQueue();
  }, []);

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Title1>Review Queue</Title1>
        <Body1>Completed tenant investigations awaiting review</Body1>
      </div>

      {loading ? (
        <Spinner label="Loading review queue..." />
      ) : error ? (
        <Body1>{error}</Body1>
      ) : items.length === 0 ? (
        <Card>
          <CardHeader header={<Text weight="semibold">All caught up!</Text>} />
          <Body1>No tenant results are currently pending review.</Body1>
        </Card>
      ) : (
        <Table aria-label="Review queue">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>CVE</TableHeaderCell>
              <TableHeaderCell>Tenant</TableHeaderCell>
              <TableHeaderCell>Verdict</TableHeaderCell>
              <TableHeaderCell>Findings</TableHeaderCell>
              <TableHeaderCell>Completed</TableHeaderCell>
              <TableHeaderCell>Action</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map(({ run, tenant }) => (
              <TableRow key={`${run.id}:${tenant.tenantId}`}>
                <TableCell>
                  <Text weight="semibold" style={{ fontFamily: 'monospace' }}>{run.cveId}</Text>
                </TableCell>
                <TableCell>
                  <TableCellLayout>
                    <Text weight="semibold">{tenant.tenantName}</Text>
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>{tenant.tenantId}</Text>
                  </TableCellLayout>
                </TableCell>
                <TableCell><VerdictBadge verdict={tenant.verdict} /></TableCell>
                <TableCell>
                  {tenant.findingsCount > 0
                    ? <Badge color="danger" appearance="filled">{tenant.findingsCount}</Badge>
                    : <Text>0</Text>
                  }
                </TableCell>
                <TableCell>{tenant.completedAt ? new Date(tenant.completedAt).toLocaleString() : '—'}</TableCell>
                <TableCell>
                  <Button
                    size="small"
                    icon={<OpenRegular />}
                    onClick={() => navigate(`/investigations/${run.id}/tenants/${tenant.tenantId}`)}
                  >Review</Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
