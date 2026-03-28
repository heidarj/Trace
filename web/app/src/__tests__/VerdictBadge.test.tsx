import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { VerdictBadge } from '../components/VerdictBadge';

function renderWithFluent(ui: React.ReactElement) {
  return render(
    <FluentProvider theme={webLightTheme}>{ui}</FluentProvider>
  );
}

describe('VerdictBadge', () => {
  it('renders Not Exposed', () => {
    renderWithFluent(<VerdictBadge verdict="NotExposed" />);
    expect(screen.getByText('Not Exposed')).toBeInTheDocument();
  });

  it('renders Exposed', () => {
    renderWithFluent(<VerdictBadge verdict="Exposed" />);
    expect(screen.getByText('Exposed')).toBeInTheDocument();
  });

  it('renders Unknown', () => {
    renderWithFluent(<VerdictBadge verdict="Unknown" />);
    expect(screen.getByText('Unknown')).toBeInTheDocument();
  });
});
