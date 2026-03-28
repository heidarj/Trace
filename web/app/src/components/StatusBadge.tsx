import { Badge } from '@fluentui/react-components';
import type { InvestigationStatus } from '../types/models';

interface Props {
  status: InvestigationStatus;
}

type BadgeColor = 'brand' | 'danger' | 'important' | 'informative' | 'severe' | 'subtle' | 'success' | 'warning';

export function StatusBadge({ status }: Props) {
  const config: Record<InvestigationStatus, { color: BadgeColor; label: string }> = {
    Pending: { color: 'subtle', label: 'Pending' },
    Running: { color: 'brand', label: 'Running' },
    Completed: { color: 'success', label: 'Completed' },
    Failed: { color: 'danger', label: 'Failed' },
    Cancelled: { color: 'subtle', label: 'Cancelled' },
  };
  const { color, label } = config[status] ?? { color: 'subtle' as BadgeColor, label: status };
  return <Badge color={color} appearance="filled">{label}</Badge>;
}
