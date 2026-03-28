import { useEffect, useState } from 'react';
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
  Badge,
  makeStyles,
  tokens,
  Card,
  CardHeader,
} from '@fluentui/react-components';
import type { TicketRecommendation, TicketStatus } from '../types/models';
import { api } from '../api/client';

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

type BadgeColor = 'brand' | 'danger' | 'important' | 'informative' | 'severe' | 'subtle' | 'success' | 'warning';

function TicketStatusBadge({ status }: { status: TicketStatus }) {
  const config: Record<TicketStatus, { color: BadgeColor; label: string }> = {
    Draft: { color: 'subtle', label: 'Draft' },
    Submitted: { color: 'brand', label: 'Submitted' },
    Acknowledged: { color: 'informative', label: 'Acknowledged' },
    Resolved: { color: 'success', label: 'Resolved' },
  };
  const { color, label } = config[status] ?? { color: 'subtle' as BadgeColor, label: status };
  return <Badge color={color} appearance="filled">{label}</Badge>;
}

export function TicketsPage() {
  const styles = useStyles();
  const [tickets, setTickets] = useState<TicketRecommendation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.listTickets()
      .then(setTickets)
      .catch(() => setError('Failed to load tickets.'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Title1>Ticket Recommendations</Title1>
        <Body1>Outbound case recommendations from approved investigations</Body1>
      </div>

      {loading ? (
        <Spinner label="Loading tickets..." />
      ) : error ? (
        <Body1>{error}</Body1>
      ) : tickets.length === 0 ? (
        <Card>
          <CardHeader header={<Text weight="semibold">No tickets yet</Text>} />
          <Body1>Ticket recommendations will appear here once approved tenant results are processed.</Body1>
        </Card>
      ) : (
        <Table aria-label="Ticket recommendations">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Title</TableHeaderCell>
              <TableHeaderCell>CVE</TableHeaderCell>
              <TableHeaderCell>Tenant</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Created</TableHeaderCell>
              <TableHeaderCell>External ID</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {tickets.map(ticket => (
              <TableRow key={ticket.id}>
                <TableCell><Text weight="semibold">{ticket.title}</Text></TableCell>
                <TableCell><Text style={{ fontFamily: 'monospace' }}>{ticket.cveId}</Text></TableCell>
                <TableCell>{ticket.tenantName}</TableCell>
                <TableCell><TicketStatusBadge status={ticket.status} /></TableCell>
                <TableCell>{new Date(ticket.createdAt).toLocaleString()}</TableCell>
                <TableCell>{ticket.externalTicketId ?? '—'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
