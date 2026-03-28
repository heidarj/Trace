Initialize this repository as a clean, production-minded starter for a web-first Azure vulnerability investigation platform.

Primary product goal
Build a web application where an operator can submit a CVE ID, kick off an investigation workflow across many customer tenants/subscriptions, persist findings, review the results, and later create downstream tickets or expose the same capabilities through MCP tools.

Current product stance
- Primary UI is a normal web client.
- Microsoft Teams support should be possible later by hosting the same web UI in a Teams tab.
- Do not make Teams the primary shell or central architectural assumption.
- MCP support is a later phase, but the backend should be shaped so it can cleanly expose MCP tools.
- ServiceNow and Slack are later adapters, not core app dependencies.

Tech stack
- Backend: .NET 10
- Distributed app orchestration / local dev: .NET Aspire
- Frontend: React + TypeScript
- UI: Fluent UI React
- Database: Azure Cosmos DB for NoSQL
- Local database on macOS: Aspire + Azure Cosmos DB Linux-based preview emulator
- Auth: Microsoft Entra ID / OIDC for the web app
- Tests: backend + frontend tests
- Logging: structured logging with correlation IDs
- API contract style: strongly typed DTOs only

Important constraints
1. Keep the repo simple.
   For v1, prefer a straightforward structure:
   - API
   - Web frontend
   - Shared contracts if useful
   - Tests
   - Aspire AppHost / ServiceDefaults

   Do not force an over-layered enterprise architecture with many projects unless there is a real reason.

2. Use Aspire.
   - Create AppHost and ServiceDefaults.
   - Wire up API and web app through Aspire.
   - Configure Azure Cosmos DB through Aspire.
   - For local macOS development, configure Cosmos using the Linux-based preview emulator pattern.
   - Expose Data Explorer locally if practical.
   - Keep emulator-specific code isolated and clearly commented.
   - Do not make cloud resources required for local startup.

3. Web-first, Teams-compatible.
   - Build the UI as a standard web app first.
   - Keep routing, auth, and layout suitable for normal browser use.
   - Structure it so the app can later be hosted inside a Teams tab with minimal changes.
   - Add notes/placeholders for Teams tab support and Teams SSO integration later.
   - Do not generate a Teams-first shell or manifest-heavy setup yet.

4. Auth design
   - Implement normal Microsoft Entra / OIDC web authentication for the primary web app.
   - Backend should validate tokens and establish user context.
   - Keep auth abstractions clean so later the same backend can support:
     - browser-based OIDC for the web app
     - Teams SSO hosted-tab scenarios
     - MCP authorization-compatible surface
   - Design the MCP-facing surface later to be OAuth/OIDC-friendly and easy to front with APIM if desired, but do not require APIM in v1.

5. Domain boundaries
   The app should center on these concepts:
   - investigation run
   - tenant result
   - finding
   - evidence artifact
   - review decision
   - ticket recommendation / outbound case request

   Create strongly typed contracts for at least:
   - CveInvestigationRequest
   - InvestigationRun
   - InvestigationRunSummary
   - InvestigationContext
   - TenantInvestigationResult
   - Finding
   - EvidenceArtifact
   - ReviewDecision
   - TicketRecommendation
   - InvestigationStatus
   - ExposureVerdict
   - FindingType

6. Workflow philosophy
   - Deterministic evidence collection and persistence first.
   - LLM/agent features may later help with enrichment or summarization.
   - Never make the system rely on freeform LLM output for core findings.
   - Shape the code so a later orchestration layer can expose:
     - start CVE investigation
     - query workflow status
     - list recent investigations
   - These should later map well to MCP tools.

7. Initial repo shape
   Prefer something close to:

   /src
     /AppHost
     /ServiceDefaults
     /Api
     /Contracts
   /web
     /app
   /tests
     /Api.Tests
     /Web.Tests

   Keep it lean.
   If you introduce additional folders, justify them through actual use.

8. API scope for v1
   Create compileable starter endpoints for:
   - create investigation run from CVE
   - list recent runs
   - get run details
   - get all tenant results for a run
   - get a single tenant result for a run
   - approve/reject a result for review
   - create a placeholder outbound ticket request from an approved result

   Keep business logic out of controllers/endpoints.
   Use services and repositories.

9. Persistence design
   Start with a Cosmos-oriented design that supports these hot paths:
   - landing page shows latest runs / recent CVEs / recent investigations
   - selecting a run shows aggregate tenant results for that run
   - selecting a tenant result shows that tenant’s detailed findings for that run

   Model at least two persistence shapes:
   A. InvestigationRuns
      - one document per run
      - optimized for recent-run and summary views

   B. RunTenantData
      - if storing multiple record types per tenant per run, use a hierarchical partitioning-friendly design around runId + tenantId
      - if only one document per tenant per run is actually implemented in v1, it is acceptable to simplify and document why

   Do not create a separate Cosmos container per result type unless there is a real retention/indexing/access-pattern need.

   Use explicit document types or discriminators where helpful, such as:
   - tenantSummary
   - finding
   - evidence
   - review
   - ticket

   Add comments or architecture notes discussing alternate query patterns:
   - month-based landing page
   - by-CVE summaries
   - recent runs
   Mention that these can later be handled by a projection or GSI, but do not make preview GSI support a hard local-dev dependency.

10. Cosmos implementation guidance
   - Use repositories or a thin data access layer over the Cosmos SDK.
   - Include optimistic concurrency where review or approval actions could race.
   - Use indexing defaults unless a clear change is warranted.
   - Keep documents reasonably sized.
   - Seed fake data for local demos.

11. Frontend guidance
   Build a polished web UI with React + TypeScript + Fluent UI React.
   Pages should include:
   - Investigations landing page
   - Run details page
   - Tenant result details page
   - Review queue
   - Outbound ticket history / queue

   UI expectations
   - No raw JSON in the normal user flow.
   - Render purpose-built UI for each major result type.
   - Use nice tables, cards, badges, tabs, dialogs, progress indicators, toolbars, etc.
   - Build domain-specific components rather than a generic object viewer.

12. Fluent UI guidance
   Use Fluent UI React heavily and thoughtfully.
   Also specifically study and apply the Fluent UI “Media Object” recipe where appropriate for result cards, evidence summaries, reviewer/action rows, and list items.
   Use rich, readable layout patterns instead of plain stacked text.
   Use person/persona-style presentation where ownership or reviewers are shown.
   Prefer well-structured visual hierarchy over developer-centric debug UI.

13. Landing page expectations
   The landing page should feel useful immediately.
   Show:
   - recent investigations
   - recent CVEs investigated
   - status badges
   - aggregate counts
   - started/updated timestamps
   - easy navigation into a run

   Seed believable fake data so the app looks alive on first run.

14. Review workflow
   Add a simple but real review flow:
   - pending review
   - approved
   - rejected

   Review actions should be visible in the UI and persisted.
   Approved results can later become candidates for ticket creation.

15. Ticketing design
   Do not implement full ServiceNow yet.
   Instead, create a clean internal abstraction and placeholder API for outbound case creation from an approved tenant result.
   This should make later ServiceNow support straightforward without coupling the domain model to ServiceNow specifics.

16. Future MCP support
   Shape backend services so they can later expose MCP tools like:
   - start_cve_investigation
   - get_investigation_status
   - list_recent_investigations
   - get_tenant_result

   Do not implement a full MCP server now unless it is cheap and clean.
   Instead, add a short architecture note and organize the application services so the later MCP surface is thin.

17. Future Teams support
   Add a short architecture note for hosting the same web app inside a Teams tab later.
   Mention that:
   - the web client remains primary
   - Teams support is an additional host surface
   - auth should be designed so Teams SSO can be added later without rewriting the app

   Do not overbuild Teams-specific code now.

18. Developer experience
   - The whole solution must build.
   - The whole solution must run locally through Aspire.
   - README must contain exact setup steps.
   - Local startup should not require real Azure credentials for the happy path.
   - Seed data should make the UI useful immediately.
   - Add TODO comments only where they are meaningful.

19. Quality bar
   - Favor explicitness over magic.
   - Favor simplicity over speculative architecture.
   - Do not scaffold dead files or fake abstractions with no use.
   - Make the generated code coherent and runnable.
   - Make the README accurate.
   - Keep the app easy to evolve into:
     - real Azure/Lighthouse scanning
     - Defender integrations
     - Teams support
     - ServiceNow adapter
     - Slack adapter
     - MCP tool exposure

20. Deliverables
   Please create:
   - solution and project structure
   - Aspire AppHost and ServiceDefaults
   - API starter with typed contracts and services
   - React + TypeScript web app using Fluent UI React
   - Cosmos integration wired for local dev through Aspire
   - fake seeded data
   - tests
   - README
   - short architecture note describing:
     - why the repo is intentionally simple
     - how Cosmos persistence is shaped around the access patterns
     - how Teams support can be added later
     - how MCP can be added later
     - where a projection or GSI may later help for landing-page/month/CVE query patterns

21. Non-goals for v1
   - no real Azure Resource Graph collector yet
   - no real Defender integration yet
   - no real ServiceNow integration yet
   - no real Slack integration yet
   - no mandatory APIM dependency
   - no Teams-first shell
   - no raw payload/debug-centric UI
   - no premature microservices split