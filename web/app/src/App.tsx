import { BrowserRouter, Routes, Route, useNavigate, useLocation } from 'react-router-dom';
import {
  FluentProvider,
  webLightTheme,
  makeStyles,
  tokens,
  TabList,
  Tab,
} from '@fluentui/react-components';
import { ShieldCheckmarkRegular } from '@fluentui/react-icons';
import { InvestigationsPage } from './pages/InvestigationsPage';
import { RunDetailsPage } from './pages/RunDetailsPage';
import { TenantResultPage } from './pages/TenantResultPage';
import { ReviewQueuePage } from './pages/ReviewQueuePage';
import { TicketsPage } from './pages/TicketsPage';

// TODO (Teams tab): When hosting inside a Teams tab, wrap with the Teams provider
// from @microsoft/teams-js and swap FluentProvider for TeamsFluentProvider.
// Auth should use Teams SSO (msal-browser with Teams context) instead of OIDC redirect.
// The routing and page components remain unchanged.
// See ARCHITECTURE.md for the full Teams integration plan.

const useStyles = makeStyles({
  shell: {
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground2,
    display: 'flex',
    flexDirection: 'column',
  },
  navbar: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalXL}`,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    boxShadow: tokens.shadow4,
  },
  navTitle: {
    color: tokens.colorNeutralForegroundOnBrand,
    fontWeight: tokens.fontWeightBold,
    fontSize: tokens.fontSizeBase500,
    letterSpacing: '0.05em',
  },
  navSubtitle: {
    color: tokens.colorNeutralForegroundOnBrand,
    opacity: 0.8,
    fontSize: tokens.fontSizeBase200,
  },
  navTabs: {
    marginLeft: tokens.spacingHorizontalXXXL,
    flex: 1,
  },
  content: {
    flex: 1,
    padding: tokens.spacingVerticalM,
  },
});

function NavBar() {
  const styles = useStyles();
  const navigate = useNavigate();
  const location = useLocation();

  const getActiveTab = () => {
    if (location.pathname.startsWith('/review')) return 'review';
    if (location.pathname.startsWith('/tickets')) return 'tickets';
    return 'investigations';
  };

  return (
    <div className={styles.navbar}>
      <ShieldCheckmarkRegular fontSize={28} />
      <div>
        <div className={styles.navTitle}>Trace</div>
        <div className={styles.navSubtitle}>CVE Investigation Platform</div>
      </div>
      <TabList
        className={styles.navTabs}
        selectedValue={getActiveTab()}
        onTabSelect={(_, d) => {
          if (d.value === 'investigations') navigate('/');
          else if (d.value === 'review') navigate('/review');
          else if (d.value === 'tickets') navigate('/tickets');
        }}
        appearance="subtle"
      >
        <Tab value="investigations" style={{ color: tokens.colorNeutralForegroundOnBrand }}>Investigations</Tab>
        <Tab value="review" style={{ color: tokens.colorNeutralForegroundOnBrand }}>Review Queue</Tab>
        <Tab value="tickets" style={{ color: tokens.colorNeutralForegroundOnBrand }}>Tickets</Tab>
      </TabList>
    </div>
  );
}

function AppShell() {
  const styles = useStyles();
  return (
    <div className={styles.shell}>
      <NavBar />
      <div className={styles.content}>
        <Routes>
          <Route path="/" element={<InvestigationsPage />} />
          <Route path="/investigations/:runId" element={<RunDetailsPage />} />
          <Route path="/investigations/:runId/tenants/:tenantId" element={<TenantResultPage />} />
          <Route path="/review" element={<ReviewQueuePage />} />
          <Route path="/tickets" element={<TicketsPage />} />
        </Routes>
      </div>
    </div>
  );
}

export default function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <AppShell />
      </BrowserRouter>
    </FluentProvider>
  );
}
