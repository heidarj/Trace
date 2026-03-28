import { Badge } from '@fluentui/react-components';
import type { ReviewStatus } from '../types/models';

interface Props {
  status: ReviewStatus;
}

type BadgeColor = 'brand' | 'danger' | 'important' | 'informative' | 'severe' | 'subtle' | 'success' | 'warning';

export function ReviewBadge({ status }: Props) {
  const config: Record<ReviewStatus, { color: BadgeColor; label: string }> = {
    Pending: { color: 'warning', label: 'Pending Review' },
    Approved: { color: 'success', label: 'Approved' },
    Rejected: { color: 'danger', label: 'Rejected' },
  };
  const { color, label } = config[status] ?? { color: 'subtle' as BadgeColor, label: status };
  return <Badge color={color} appearance="filled">{label}</Badge>;
}
