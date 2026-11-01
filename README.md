# AI-in-SDLC Assignment --- Copilot Agent Setup Guide


> **Important:** Run the steps in order. Copilot must inspect the real
> repository and assignment instructions before creating or changing
> files. Do not claim that tests, metrics, or evaluations passed unless
> they were actually executed and verified.

## Prerequisites

-   Windows with PowerShell
-   Git installed and configured
-   .NET SDK installed
-   Visual Studio Code
-   GitHub Copilot Chat access
-   Access to the assignment repository:
    <https://github.com/NITIN1670/ai-in-sdlc-assignment>

Check the tools:

``` powershell
git --version
& "C:\Program Files\dotnet\dotnet.exe" --list-sdks
```

If `dotnet` is not recognized, add the installation directory to the
current PowerShell session:

``` powershell
$env:PATH = "C:\Program Files\dotnet;" + $env:PATH
```

## Step 0 --- Clone the repository and create a branch

Open the VS Code terminal (\`Ctrl + \`\`) and run:

``` powershell
git clone https://github.com/NITIN1670/ai-in-sdlc-assignment.git
cd ai-in-sdlc-assignment
git checkout -b feature/assignment-deliverables
```

Find the project and solution files:

``` powershell
Get-ChildItem -Path . -Recurse -File -Include *.csproj,*.sln,*.slnx
```

The repository may contain multiple projects. Do not assume the
repository root contains a solution file. Based on the currently
identified structure, the API and test projects are:

-   `backend/src/InvoiceApp.Api/InvoiceApp.Api.csproj`
-   `backend/src/InvoiceApp.Domain/InvoiceApp.Domain.csproj`
-   `backend/tests/InvoiceApp.Tests/InvoiceApp.Tests.csproj`

Restore and build the API project from the repository root:

``` powershell
& "C:\Program Files\dotnet\dotnet.exe" restore ".\backend\src\InvoiceApp.Api\InvoiceApp.Api.csproj"
& "C:\Program Files\dotnet\dotnet.exe" build ".\backend\src\InvoiceApp.Api\InvoiceApp.Api.csproj"
```

Run the tests:

``` powershell
& "C:\Program Files\dotnet\dotnet.exe" test ".\backend\tests\InvoiceApp.Tests\InvoiceApp.Tests.csproj"
```

Run the API:

``` powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project ".\backend\src\InvoiceApp.Api\InvoiceApp.Api.csproj"
```

If the build fails, resolve the actual errors before proceeding. Record
the command and genuine output; do not fabricate a successful build.

## Step 1 --- Enable GitHub Copilot Agent Mode

1.  Open the cloned repository folder in VS Code.
2.  Open GitHub Copilot Chat (`Ctrl + Alt + I`, if configured).
3.  Select **Agent** mode from the mode selector.
4.  Ensure the workspace is the cloned `ai-in-sdlc-assignment`
    repository.
5.  Review proposed file changes and terminal commands before accepting
    them.

Agent Mode can perform multi-step work, but it does not remove the need
to review changes, grant permissions, or validate results.

## Step 2 --- Repository context, skill, MCP configuration, and directories

Paste this prompt into Copilot Agent:

``` text
@workspace Set up this repository for our SDLC assignment in Agent mode:

1. Inspect the repository and assignment instructions first.
2. Create directories: '.github', 'docs', 'tests', 'ops/evals', and
   '.agents/skills/adr-stress-test'.
3. Create '.github/copilot-instructions.md' (repository context) enforcing:
   - Controllers in Api.Controllers must never call external payment
     integrations (PayEx) directly.
   - Financial precision: use decimal for currency calculations; never
     use float or double for monetary values.
   - Mermaid diagrams must cite exact file paths and symbols (classes
     and methods) supporting the as-built flow.
   - Current storage is in-memory
     ('InMemoryInvoiceRepository.cs') if confirmed by repository inspection.
     Do not invent SQL tables or persistence infrastructure.
   - Challenge developer assumptions and report financial and operational
     risks honestly; do not agree merely to please the user.
4. Create
   '.agents/skills/adr-stress-test/SKILL.md' describing how to challenge
   assumptions about messaging, simulate a 3:00 AM month-end incident
   involving 30-second timeouts, and estimate team costs.
5. Create '.mcp.json' configuring 'mcp-mermaid'. Verify the correct server
   package, command, and configuration format before writing it. Do not
   invent an MCP package name or credentials.
6. Initialize an empty root-level 'audit.jsonl'.

Do not overwrite existing files without inspecting and preserving useful
content. Report all created files and any assumptions.
```

Review the changes and select **Keep/Accept** only after checking them.

## Step 3 --- Architecture audit and architecture test (Deliverable 1)

``` text
@workspace Execute Deliverable 1:

1. Inspect the actual invoice approval workflow in the repository.
2. Create 'docs/1-architecture.md' containing:
   - An as-built C4 Container diagram in Mermaid.
   - An as-built sequence diagram in Mermaid.
   - Exact file paths and class/method symbols supporting each diagram.
   - Explicit identification of InMemoryInvoiceRepository.cs, if confirmed.
   - An 'Agent Inferences' section listing assumptions that were checked
     and corrected against the actual code.
   - A target-state sequence diagram showing Azure SQL and PayEx through
     a Transactional Outbox. Mark every unimplemented element
     '[DOES NOT EXIST YET]'.
3. Create or update
   'backend/tests/InvoiceApp.Tests/ArchitectureTests.cs' with a NetArchTest
   test verifying that Api.Controllers does not depend on PayEx or
   Infrastructure, consistent with the actual namespaces and references.
4. Check whether NetArchTest is already referenced. Add a package only
   if necessary and appropriate.
5. Run:
   dotnet test --filter "FullyQualifiedName~ArchitectureTests" --verbosity normal
   from the correct test-project directory or pass the test project path.
6. Append the exact command and genuine terminal output to the document.
   If the test cannot run, document the error instead of claiming success.
```

Preview `docs/1-architecture.md` in VS Code with `Ctrl + Shift + V`
where supported.

## Step 4 --- PayEx exact-once ADR and agent stress tests (Deliverable 2)

``` text
@workspace Execute Deliverable 2:

Create 'docs/2-adr-exact-once.md' addressing safe payment submission to
PayEx for 40,000 monthly invoices, a peak rate of 80 submissions per
minute, 30-second PayEx timeouts, and SOC 2 considerations.

Evaluate these options:
1. Idempotency key plus deduplication table.
2. Transactional Outbox plus background worker (proposed choice).
3. Optimistic submission plus nightly reconciliation.

For each option explain consistency, duplicate-payment risk, failure
recovery, operational complexity, and auditability.

Include:
- Stress Test 1 (sycophancy check): analyze a scenario where a developer
  advocates Option 3 and an agent agrees despite double-payment risk.
  Explain why a senior engineer should challenge that recommendation.
  Treat this as a hypothetical exercise unless actual session evidence
  exists; do not present invented events as historical facts.
- Stress Test 2: recovery from a 3:00 AM month-end incident involving a
  30-second connection timeout. Explain ambiguous outcomes, idempotent
  retries, reconciliation, alerting, and operator actions.
- Stress Test 3: estimate implementation and operating costs for a
  six-engineer team. State assumptions and show the calculation.
- Revisit triggers, including the proposed threshold of more than
  500 approvals/second and a possible move to Azure Service Bus.
- The mandatory section:
  '## Where the Agent Was Wrong or Useless'

Do not claim that the system currently has PayEx integration, an outbox,
or a deduplication table unless the code proves it. Clearly separate
current implementation from the proposed design.
```

## Step 5 --- Operational autonomy policy (Deliverable 3)

``` text
@workspace Execute Deliverable 3:

Create 'docs/3-autonomy-policy.md', a concise operational autonomy policy
for 'Month-End Log Triage and Tier-2 Incident Diagnostics'.

Include an Autonomy Levels Matrix:
- Level 1: Read-only.
- Level 2: Propose actions; no execution.
- Level 3: Act only with senior approval.
- Level 4: Fully autonomous; strictly prohibited for this workflow.

Document team constraints:
- Six engineers.
- Two engineers are new to the codebase.
- One-week rotation.

Day-1 rules:
- New hires are capped at Level 2.
- Senior engineer pairing is mandatory during month-end crunch.
- Month-end crunch represents 60% of volume, as a stated assignment
  assumption unless verified operational data is available.

Promotion criteria for Level 3:
- At least 60 days on rotation.
- Five verified live triages.
- Architect approval.

Include audit logging, escalation, rollback, access control, and
prohibited actions. Make the policy operational and unambiguous.
```

## Step 6 --- Evaluation cases and false-alarm analysis (Deliverables 4 and 5)

``` text
@workspace Execute Deliverables 4 and 5:

1. Inspect the actual TaxCalculator.cs and create
   'ops/evals/case-11-currency-overflow.diff' showing a controlled
   float-precision defect. The diff must apply cleanly to the actual
   source file. Keep it as an evaluation artifact; do not silently
   introduce the defect into the production implementation.

2. Inspect InvoiceApprovalService.cs and create
   'ops/evals/case-12-clean-approval-filter.diff' showing a behavior-
   preserving refactor from an imperative loop to LINQ .Where().
   Include or identify passing tests that demonstrate equivalent behavior.
   The diff must apply cleanly to the actual source.

3. Create 'docs/4-eval-results.md' evaluating all 12 cases against:
   "Review this PR diff against security, financial arithmetic, and
   architecture rules. If clean, reply NO_DEFECTS_FOUND. If defects
   exist, cite file, line, and defect."

4. Record results for Runs 1 to 3 only where actual run evidence exists.
   Explain the Case 12 false alarm if observed. Do not invent results
   for the other cases or claim runs that were not performed.

5. Update '.github/copilot-instructions.md' with:
   "Do not flag LINQ conversions with passing unit tests as performance
   defects."
   Apply this as a guideline, not as an unconditional exemption:
   still flag measured or demonstrable performance regressions.

6. If Run 4 (Changed Variable) is part of the required exercise, execute
   it and record actual results. Discuss whether the Case 12 false alarm
   was reduced and whether Case 11 was still detected. Include sample
   size and confidence limitations. Do not claim 0% false alarms or
   100% catch rate without sufficient observed cases.
```

## Step 7 --- Lethal Trifecta risk audit and measurement plan (Deliverables 6 and 7)

``` text
@workspace Execute Deliverables 6 and 7:

1. Create 'docs/5-risk-audit.md' auditing the agent against the Lethal
   Trifecta:
   - Private data.
   - Untrusted supplier content.
   - External PayEx egress.

   Model a prompt-injection attempt that tries to exfiltrate private data
   through an external payment integration. Document trust boundaries,
   attack paths, mitigations, logging, and incident response.

   Include the decision to isolate parsers in zero-network sandboxes.
   Explain the implementation requirements and verify whether this
   isolation exists today. Do not describe a proposed control as already
   implemented.

2. Create 'docs/6-measurement-plan.md'.

   The assignment provides these proposed baseline metrics:
   - PR Review Lead Time = 26.5 hours (closed PR history).
   - Change Failure Rate = 18.2% (CI build failure history).

   Verify these values against available Git and CI history where possible.
   If the underlying data is unavailable, label them 'assignment-provided,
   not independently verified'.

   Include a 60-day target:
   - PR review lead time below 12 hours.
   - Change failure rate below 5%.

   Define metric formulas, data sources, owners, and a bi-weekly review
   cadence. Distinguish build failures from production change failures;
   do not use them interchangeably without an explicit definition.
```

## Step 8 --- Telemetry, demo agenda, commit, and push (Deliverable 8)

``` text
@workspace Finalize the deliverables:

1. Append structured JSONL telemetry entries to root-level 'audit.jsonl'
   for tasks actually executed in this session. Follow the workshop
   schema if it exists in the repository. If no schema is supplied,
   propose a minimal documented schema and use it consistently.
   Do not include secrets, tokens, personal data, or fabricated events.

2. Run relevant build and test commands and record genuine results.

3. Review 'git status' and inspect the staged diff. Do not stage secrets,
   generated binaries, local settings, or unrelated files.

4. Stage the intended deliverables and commit with:
   feat: complete SDLC workshop assignment using autonomous Copilot agent workflow

## Final verification checklist

Before considering the assignment complete, verify each item:

-   [ ] Repository cloned and assignment branch created.
-   [ ] Correct API and test project paths identified.
-   [ ] Restore and build executed successfully.
-   [ ] Unit tests executed and results recorded.
-   [ ] Repository Copilot instructions reviewed.
-   [ ] ADR stress-test skill created and reviewed.
-   [ ] MCP configuration validated.
-   [ ] `audit.jsonl` follows a documented schema.
-   [ ] `docs/1-architecture.md` contains code-grounded diagrams.
-   [ ] `docs/2-adr-exact-once.md` distinguishes current state from
    target state.
-   [ ] `docs/3-autonomy-policy.md` contains actionable approval rules.
-   [ ] Evaluation diffs apply to real source files.
-   [ ] Evaluation results are based on actual runs.
-   [ ] Risk audit documents trust boundaries and mitigations.
-   [ ] Measurement plan identifies verified versus assumed baselines.
-   [ ] Final Git diff reviewed; commit and push completed if
    authorized.

## Known project paths

The following paths were found during the initial repository inspection.
Re-check them after cloning because the repository may have changed:

``` text
backend/src/InvoiceApp.Api/InvoiceApp.Api.csproj
backend/src/InvoiceApp.Domain/InvoiceApp.Domain.csproj
backend/tests/InvoiceApp.Tests/InvoiceApp.Tests.csproj
```

When PowerShell cannot resolve `dotnet`, use the full executable path:

``` powershell
& "C:\Program Files\dotnet\dotnet.exe" build ".\backend\src\InvoiceApp.Api\InvoiceApp.Api.csproj"
```
