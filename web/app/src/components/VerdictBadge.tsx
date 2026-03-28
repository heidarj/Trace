import { Badge } from '@fluentui/react-components';
import type { ExposureVerdict } from '../types/models';

interface Props {
  verdict: ExposureVerdict;
}

type BadgeColor = 'brand' | 'danger' | 'important' | 'informative' | 'severe' | 'subtle' | 'success' | 'warning';

export function VerdictBadge({ verdict }: Props) {
  const config: Record<ExposureVerdict, { color: BadgeColor; label: string }> = {
    Unknown: { color: 'subtle', label: 'Unknown' },
    NotExposed: { color: 'success', label: 'Not Exposed' },
    PotentiallyExposed: { color: 'warning', label: 'Possibly Exposed' },
    Exposed: { color: 'danger', label: 'Exposed' },
    Confirmed: { color: 'important', label: 'Confirmed' },
  };
  const { color, label } = config[verdict] ?? { color: 'subtle' as BadgeColor, label: verdict };
  return <Badge color={color} appearance="filled">{label}</Badge>;
}
