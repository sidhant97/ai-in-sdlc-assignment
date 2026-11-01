# ADR: Reliable PayEx submission for approved invoices

- **Status:** Proposed decision for the production scenario; not implemented in this repository
- **Decision:** Option 2, Transactional Outbox + Background Worker, with a stable PayEx idempotency key and explicit handling of ambiguous outcomes
- **Scope:** Submitting an approved invoice as a payment request to PayEx
- **Workload supplied for this decision:** 40,000 invoices/month, peak 80 submissions/minute, and a PayEx connection/request timeout of up to 30 seconds
- **Compliance constraint:** Design and evidence should support the organization's SOC 2 control environment. This ADR does not claim that the application or organization is SOC 2 compliant.

## Context and exact-once boundary

The figures above are planning inputs, not measured production telemetry from this repository. 40,000 submissions/month is about 480,000/year. A peak of 80/minute is about 1.33 submissions/second. For comparison, the monthly average is about 0.015 submissions/second if spread evenly over a 30-day month; designing to that average would hide peak and burst risk.

**Current code reality:** `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`, class `InMemoryInvoiceRepository`, stores invoices in a process-local `List<Invoice>`. `backend/src/InvoiceApp.Domain/ApprovalService.cs`, method `Approve`, changes approval state and calls `IInvoiceRepository.Update`. `backend/src/InvoiceApp.Api/Program.cs` registers the in-memory repository. There is no PayEx client, durable database, outbox, broker, or background worker in the application today. See `ops/README.md` for the repository's explicit warning that its production infrastructure material is a scenario, not implemented application infrastructure.

True exactly-once execution across our database and an independent PayEx service cannot be guaranteed by our database transaction alone. A connection can fail after PayEx has accepted a payment but before our caller receives the response. The practical target is **effectively-once payment intent**:

1. Persist one durable payment intent per approved invoice, with a unique business key and auditable state.
2. Send the same stable idempotency key for every attempt for that intent.
3. Require PayEx to honor that key atomically and for a documented retention period, and provide a way to look up an ambiguous submission by key or merchant reference.
4. Reconcile unresolved outcomes before creating a new intent or issuing a new key.

If PayEx cannot provide the required idempotency and lookup semantics, no option below can honestly promise that an ambiguous timeout will never result in a duplicate payment. That is a launch-blocking vendor/finance risk, not something retries or an outbox solve by themselves.

## Options considered

| Option | Delivery and timeout behavior | Duplicate-payment and recovery risk | SOC 2 / operations considerations | Assessment |
|---|---|---|---|---|
| **1. Idempotency Key + Dedupe Table** | The API writes a unique payment-intent/dedupe row, then submits to PayEx synchronously. A 30-second timeout can hold the API request and its resources open. | A unique key prevents duplicate local intents. It only prevents duplicate PayEx effects if PayEx honors the same stable key and ambiguous submissions can be resolved. A lost response leaves the request outcome unknown. | A durable database, unique constraint, audit trail, key management, monitoring, reconciliation, and tested incident procedures are still required. Synchronous request logs and access controls must be retained as evidence. | Viable if synchronous UX is essential and provider idempotency/lookup is proven; at this peak, the timeout coupling and request-path failure handling are unnecessary operational exposure. |
| **2. Transactional Outbox + Background Worker (Chosen)** | In one local database transaction, persist the approval/payment intent and its outbox event. Return an accepted/pending result; a worker submits independently with a bounded timeout, concurrency, and backoff. | The outbox makes the state change and intent durable together, but dispatch is at-least-once. A worker can crash after PayEx accepts but before recording success. Stable provider idempotency and reconciliation are still mandatory to avoid double effects. | Durable state transitions, correlation IDs, operator-visible backlog, attempt history, access control, and auditable recovery are explicit. Worker identity and PayEx credentials should be least-privilege and managed outside source control. | **Chosen.** Better isolates 30-second provider stalls from approver requests, preserves work through process restarts, and supports controlled recovery. This is not a claim of exactly-once delivery. |
| **3. Optimistic Submission + Nightly Reconciliation** | The application reports success/approval before the payment outcome is known; a later job compares local records with PayEx. | A timeout after PayEx acceptance but before local recording can be mistaken for failure. Resubmitting can double-pay; marking it paid without evidence can leave an unpaid invoice. A nightly process leaves an extended uncertainty window and may find discrepancies after finance acts on them. | Reconciliation reports and human review create evidence, but delayed detection, manual exception handling, and unclear ownership increase control burden. Nightly reconciliation is not an idempotency mechanism. | **Rejected as the primary control.** Reconciliation remains a necessary secondary control for Option 2, not a substitute for safe submission semantics. |

### Decision and required design

Choose **Option 2** for the stated volume and 30-second dependency timeout. The outbox and worker must be implemented alongside, not instead of, provider-side idempotency:

- Give each invoice payment intent a durable unique ID and a stable idempotency key (for example, derived from invoice ID plus an explicit payment-intent version). Do not create a new key just because an attempt timed out.
- In one Azure SQL transaction, record the approved/payment-pending state and insert the corresponding outbox event. Enforce uniqueness in the database; do not rely on an in-process check.
- Use a worker with bounded parallelism, explicit leases/visibility, a per-attempt timeout no greater than the provider's 30-second timeout, exponential backoff with jitter, and a maximum-attempt policy. Do not retry an ambiguous result with a new key.
- Record each attempt, provider reference, correlation ID, timestamps, terminal/ambiguous state, and operator action. Keep sensitive payment data out of logs; protect any necessary payload at rest and in transit.
- On ambiguous timeout, query PayEx using the same idempotency key/merchant reference. If lookup is unavailable or inconclusive, move the intent to an operator-visible `Unknown/NeedsReconciliation` state and do not automatically create a second payment.
- Provide a controlled replay path that preserves the intent/key, audited override approval for exceptional actions, and a reconciliation process comparing local intents to PayEx outcomes.
- Return a truthful user-visible pending/accepted response while the payment is unresolved. Do not represent a queued or unknown result as a completed payment.

Azure SQL and the outbox/worker are proposed components, not existing code. Azure Service Bus is not required for the initial 1.33/sec peak: a durable SQL outbox with a worker can be the simpler starting point. At higher throughput, Service Bus can carry dispatched events, but the outbox remains the atomic bridge from the database, and consumers still need idempotent handling.

## SOC 2 controls and evidence

SOC 2 is an organizational audit against a defined scope and control environment, not a property conferred by choosing SQL or a queue. Before launch, the owners should map the design to applicable controls and retain evidence, including:

- **Change management:** reviewed and approved changes, CI results, deployment records, rollback plans, and evidence that schema/worker changes were tested.
- **Logical access:** least-privilege service identities, restricted production access, MFA for human access, access reviews, and auditable operator replay/override approvals.
- **Confidentiality and secrets:** TLS, encryption at rest, managed secret storage and rotation, no API keys or sensitive payment payloads in logs, and defined retention/deletion.
- **Processing integrity:** uniqueness constraints, stable idempotency key, append-only attempt/audit history, reconciliation reports, alerts on stuck/unknown intents, and evidence of exception resolution.
- **Availability and recovery:** tested backup/restore and disaster recovery, outbox-age/backlog alerting, dead-letter/exception handling, documented incident ownership, and periodic recovery exercises.
- **Monitoring:** dashboards and alerts for queue/outbox age, attempts, ambiguous outcomes, provider latency/timeouts, duplicates/conflicts, and reconciliation differences. Logs must correlate approval, intent, attempt, and provider reference without exposing secrets.

The control owner, retention periods, recovery objectives, PayEx contract semantics, and audit evidence platform are not specified in the request and must be confirmed before production. Do not claim compliance until the responsible compliance function validates implementation and evidence.

## Stress Test 1 — Sycophancy check

**Tabletop scenario, not a recorded repository event:** A developer urges Option 3, arguing that optimistic approval plus a nightly comparison is simpler. The agent agrees and calls it safe without testing the timeout case. That agreement is wrong: a PayEx acceptance followed by a lost 30-second response leaves local state uncertain. If the nightly job treats the missing response as failure and submits again, the same invoice can be paid twice; if it assumes success, an unpaid invoice can be treated as paid. Nightly detection also delays finance's ability to contain the discrepancy.

A senior engineer overrules the agent's recommendation. The senior engineer requires a durable payment intent, stable provider idempotency, lookup/reconciliation for unknown outcomes, and a truthful pending state; they select Option 2 as the primary processing pattern. Nightly reconciliation is retained only as a secondary detection control. This decision still depends on proving PayEx's idempotency and lookup behavior; if PayEx cannot support them, the risk must be accepted explicitly by accountable finance/business owners or the integration must not automatically resubmit uncertain payments.

**What the agent should have done:** challenge the preferred conclusion symmetrically, trace the "accepted by PayEx, response lost" case, distinguish delivery guarantees from side-effect guarantees, and state uncertainty rather than echoing the developer's preference.

## Stress Test 2 — 3:00 AM month-end, 30-second timeout

**Scenario assumptions:** At the supplied peak, 80 submissions/minute continue during month-end. PayEx connections take the full 30 seconds and time out without a definitive response. The outage is simulated; it is not evidence that the current application has PayEx or a worker.

1. **Request and durable intent:** An approver completes an approval. The API commits the approval/payment intent and outbox event atomically. It returns an accepted/pending state rather than keeping the browser request open for PayEx. If the database transaction fails, it returns an error and there is no committed event to dispatch.
2. **Backlog grows, but the API is isolated:** 80 events/minute can accumulate while PayEx is unavailable. The worker's configured concurrency and attempt limit cap outbound pressure; it must not spin immediate retries. For scale intuition, if all requests were instead synchronous and each occupied a request for 30 seconds, Little's Law gives about `80/60 × 30 = 40` requests concurrently waiting at the stated steady peak, before burstiness or retries. Actual API saturation depends on worker/request capacity and has to be load-tested.
3. **Timeout is ambiguous, not failure:** A worker timeout does not prove PayEx rejected the payment. The attempt is recorded with its correlation ID and same idempotency key; the intent becomes retryable only under the provider contract or moves to `Unknown/NeedsReconciliation`. Never issue a new key just to clear the backlog.
4. **Containment:** Alert the on-call operator on provider latency/timeouts, oldest outbox age, growing pending/unknown counts, and any reconciliation mismatch. Page the named payment integration owner and notify finance through the incident process. Pause or throttle dispatch if retries are increasing risk. Do not bulk replay or mark payments complete without provider evidence and an authorized decision.
5. **Recovery:** When PayEx recovers, first query unknown intents by the same key/reference. Where PayEx confirms acceptance, record its reference and complete the intent without resubmission. Where PayEx confirms no acceptance, retry with the same key under bounded exponential backoff/jitter. Keep unresolved cases quarantined for controlled manual review. Drain the backlog at a rate below tested provider and worker limits while monitoring duplicate/conflict responses, backlog age, and finance reconciliation.
6. **After the incident:** reconcile all affected intents and provider records, preserve logs/audit evidence, document affected invoices and operator actions, investigate the timeout cause, and test the recovery path before returning to normal throughput. An outbox prevents losing committed work; it does not itself prevent duplicate PayEx effects.

**Controls still to specify and validate:** exact worker concurrency, maximum attempts/backoff, backlog capacity and alert thresholds, PayEx idempotency retention and lookup SLA, business hours/finance escalation, RTO/RPO, and the maximum acceptable pending-payment duration. Unknown or absent provider idempotency/lookup is a stop condition for automatic retries of ambiguous submissions.

## Stress Test 3 — Six-engineer build and operating cost

These are **planning estimates**, not measured project data or vendor quotes. They assume six engineers share implementation, review, testing, security/control evidence, deployment, and on-call responsibilities; one year of operation; and that Azure SQL and PayEx access are available. Estimates exclude implementation delays caused by procurement/vendor contract changes, formal external audit fees, cloud/service charges, and PayEx transaction fees. Operational hours include ongoing engineering/support and control evidence work, but not a major prolonged provider incident. Ranges reflect uncertainty; refine them with a short design spike and team estimates.

Let `R` be the team's fully loaded hourly labor rate in the relevant currency, `A` annual Azure SQL/hosting/monitoring cost, `B` annual Service Bus cost (zero for the initial SQL-worker design), `P` PayEx fees, and `E` audit/external assurance cost. These values were not supplied, so dollar totals cannot responsibly be calculated. **Year-one total cost = (build hours + first-year operational hours) × R + A + B + P + E.** Do not substitute an invented salary or cloud rate.

| Option | Build estimate (person-hours) | First-year operations (person-hours) | Year-one labor hours | Year-one labor cost formula | Relative effort and uncertainty |
|---|---:|---:|---:|---|---|
| **1. Idempotency key + dedupe table** | 160–240 | 180–300 | **340–540** | `340R–540R` | Less asynchronous machinery, but provider contract validation, synchronous timeout handling, ambiguous-result recovery, and audit evidence remain. Add `A + P + E`. |
| **2. Transactional Outbox + Background Worker (chosen)** | 300–460 | 140–240 | **440–700** | `440R–700R` | Highest initial build effort; lower recurring manual/incident effort if backlog controls and provider idempotency work as designed. Add `A + P + E`; initial SQL worker has `B = 0`. |
| **3. Optimistic submission + nightly reconciliation** | 120–190 | 320–520 | **440–710** | `440R–710R` | Lowest initial build estimate, highest human exception/reconciliation burden and financial exposure. Add `A + P + E`; an unsafe design can also incur unpriced duplicate-payment losses. |

At 40 hours/week per engineer, six engineers provide a nominal 240 team person-hours/week. The option 2 build estimate of 300–460 hours is about 1.25–1.92 team-capacity weeks, not a delivery-date promise; parallel work, review, availability, and vendor dependencies affect calendar time. Its first-year operational estimate is about 2.7–4.6 total team person-hours/week across all six engineers. The estimate does not mean every engineer is continuously assigned.

The labor ranges overlap, so they do not prove Option 2 is the cheapest. Its selection is based on reducing synchronous coupling and enabling durable, observable recovery while preserving correctness controls. Option 3's lower initial cost is not a fair comparison if expected duplicate-payment loss, finance effort, audit remediation, and incident impact are omitted; those exposures need real incident/probability and labor data before a monetary expected-loss figure can be claimed.

**Cost owners should replace these assumptions before approval:** loaded rates, build-day estimates, support/on-call rotation and alert volume, hosting/monitoring budget, PayEx fees and idempotency contract, audit scope, manual reconciliation volume, and expected incident cost. Re-estimate if a broker is added or the throughput profile changes.

## Revisit triggers

- If measured approval/submission throughput is **>500 approvals/second**, trigger a topology review and plan to move outbox-event transport/dispatch to Azure Service Bus, subject to load testing and a reviewed migration plan. This is more than 375 times the stated 80/minute peak; instrument real peak/burst rates and benchmark the database, worker, PayEx quotas, and downstream finance process before the cutover. Azure Service Bus is not implemented today.
- A Service Bus migration does not replace the transactional outbox or PayEx idempotency. Keep the durable SQL transaction as the source of truth, publish with an outbox relay, assume at-least-once delivery, and retain consumer dedupe, stable provider keys, and reconciliation. Review ordering, partitioning, dead-letter retention, replay, and SOC 2 evidence controls as part of the change.
- Reopen the decision if PayEx changes its idempotency retention/lookup contract, fails an end-to-end ambiguous-timeout test, or cannot support stable-key retries; if the 30-second timeout behavior changes; if payment volume or burstiness changes materially; if the business requires a stricter recovery objective; or if audit/control owners reject the proposed evidence/retention model.

## Where the Agent Was Wrong or Useless

- Agreeing with the developer's Option 3 preference without modeling "PayEx accepted; response timed out" would be sycophancy and could expose finance to duplicate payments. The agent should surface that risk and require senior/finance review.
- Saying "exactly once" without qualification would overstate what the outbox can provide. The outbox supports durable, at-least-once dispatch; effectively-once external effects depend on PayEx idempotency and authoritative lookup/reconciliation.
- Presenting SQL, PayEx, Azure Service Bus, or a worker as current infrastructure would be false. The current repository uses `InMemoryInvoiceRepository`; the production scenario is planned material.
- Claiming SOC 2 compliance from this design, or giving a precise dollar total without loaded rates, cloud pricing, service scope, and operational data, would be unsupported. Provide evidence requirements and transparent hour/rate formulas instead.
- A recommendation is useless if it fails to name the unknowns that block safe retries. PayEx idempotency semantics, retention, lookup behavior, and incident ownership must be verified before implementation approval.
