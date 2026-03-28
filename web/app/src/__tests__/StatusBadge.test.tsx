import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { StatusBadge } from '../components/StatusBadge';

function renderWithFluent(ui: React.ReactElement) {
  return render(
    <FluentProvider theme={webLightTheme}>{ui}</FluentProvider>
  );
}

describe('StatusBadge', () => {
  it('renders Pending status', () => {
    renderWithFluent(<StatusBadge status="Pending" />);
    expect(screen.getByText('Pending')).toBeInTheDocument();
  });

  it('renders Running status', () => {
    renderWithFluent(<StatusBadge status="Running" />);
    expect(screen.getByText('Running')).toBeInTheDocument();
  });

  it('renders Completed status', () => {
    renderWithFluent(<StatusBadge status="Completed" />);
    expect(screen.getByText('Completed')).toBeInTheDocument();
  });

  it('renders Failed status', () => {
    renderWithFluent(<StatusBadge status="Failed" />);
    expect(screen.getByText('Failed')).toBeInTheDocument();
  });
});
