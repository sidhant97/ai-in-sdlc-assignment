---
name: adr-stress-test
description: "Challenge architectural decision records and assumptions about messaging. Use to stress-test bias against queues, model 3:00 AM month-end failures with 30-second timeouts, and estimate team operating costs."
---

# ADR Stress Test

Use this workflow when reviewing an architecture decision record (ADR), especially one that rejects messaging or favors a synchronous design. The goal is evidence-based scrutiny, not to argue for messaging by default.

## Procedure

1. **Separate evidence from assumptions.** Summarize the decision, constraints, workload, failure modes, and supporting evidence. Mark unknowns explicitly. Treat planned infrastructure and incident scenarios under `ops/` as scenarios, not as proof that those components exist in the application today.
2. **Challenge bias about messaging.** State the strongest evidence-backed case for and against a message broker. Test whether objections (for example, complexity, cost, or eventual consistency) are measured or assumed; apply the same scrutiny to synchronous calls. Compare alternatives against the same requirements and do not force a broker if the evidence does not justify one.
3. **Run the 3:00 AM month-end failure simulation.** Assume a month-end peak and a downstream dependency that takes 30 seconds to time out. Trace a request or work item from initiation through timeout, retries, user-visible outcome, and recovery. Identify which components and operators are affected, how backlog or request pile-up grows, what happens to duplicate or partially completed work, and how the system recovers. Explicitly label the scenario and do not claim these components exist unless verified in source.
4. **Stress the controls.** For each proposed design, identify timeout budgets, retry limits and backoff, concurrency or queue limits, idempotency, dead-letter or reconciliation handling where applicable, observability, alert ownership, and a safe recovery or rollback procedure. Flag controls that are unspecified rather than assuming they are present.
5. **Compute team costs transparently.** Estimate implementation, testing, deployment, on-call, incident response, and ongoing maintenance effort for each viable option. Show inputs and arithmetic (for example, hours per week × loaded hourly cost × weeks), separate one-time from recurring costs, and include uncertainty or ranges. Use user-provided or cited rates; if unavailable, leave monetary totals as variables and report hours separately instead of inventing salary figures.
6. **Report a balanced recommendation.** List evidence, assumptions, quantified tradeoffs, unresolved questions, and risks to finance and operations. Recommend the simplest option that meets the stated requirements, with confidence and conditions that would change the recommendation.
