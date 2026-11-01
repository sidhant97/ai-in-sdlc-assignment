# Operational Autonomy Policy

**Scope:** Month-End Log Triage and Tier-2 Incident Diagnostics  
**Applies to:** Six-engineer team, including two engineers new to this codebase  
**Rotation:** One-week on-call/triage handoff; designate a primary and a senior escalation contact each week  
**Status:** Proposed operating policy. The staffing and 60% month-end volume are planning inputs, not verified production telemetry.

## Autonomy Levels Matrix

| Level | Authority | Permitted work | Approval and limits |
|---|---|---|---|
| **1 — Read-only** | Inspect and report | Review approved logs, metrics, traces, dashboards, and deployment history; correlate evidence; document hypotheses and uncertainty. | No state-changing commands, production edits, replay, restart, scaling, rollback, or configuration changes. Protect customer data and secrets; use approved read-only access. |
| **2 — Propose** | Diagnose and recommend | Produce a concise evidence-backed incident summary; rank hypotheses; propose mitigations with impact, risk, verification, and rollback steps; prepare a change for human review. | No production action. A human incident lead/senior engineer decides whether to seek approval and execute. Label assumptions; do not present scenario material as live-system evidence. |
| **3 — Act with senior approval** | Execute a specifically approved action | Carry out only the named, scoped production action after a senior engineer (or incident commander out of hours) approves it. Verify the result, record commands and timestamps, and stop/escalate if conditions differ from the approved plan. | Approval must precede action and identify action, target, limits, and rollback/stop condition. No scope expansion, destructive data operations, or payment replay without explicit approval from the accountable service/finance owner. |
| **4 — Fully autonomous** | Unsupervised production authority | **Strictly prohibited.** | No agent or engineer may independently change production, approve its own change, replay payments, or override the approval process. No emergency exception; use the incident commander and established human approval path. |

## Team, rotation, and month-end rules

- The team has six engineers; two are brand new to this codebase. Keep a named weekly primary and a named senior escalation contact. Handoffs include open incidents, current evidence, pending approvals, and next actions.
- New hires start at **Level 2 maximum on Day 1**. They may perform Level 1 observation and Level 2 diagnosis/proposals, but may not execute production changes. The cap remains until formal promotion to Level 3.
- During the month-end crunch, when the expected workload is approximately **60% of volume**, any production diagnostic action or mitigation is performed with a senior engineer paired in the incident channel/session. Pairing does not replace the required explicit senior approval for Level 3 actions.
- If no authorized senior or incident commander is available, remain read-only/propose, preserve evidence, and escalate through the on-call chain. Do not infer approval from silence or urgency.
- Use least-privilege accounts and approved tools. Do not copy secrets or unnecessary personal/payment data into notes, chat, or audit records. Preserve relevant evidence and log the actor, approver, rationale, action, result, and time for each approved change.
- A production change requires a named human approver. Prefer reversible, narrowly scoped mitigations; validate the rollback before execution. Stop and re-escalate on unexpected impact, uncertain payment/financial state, or evidence that contradicts the plan.

## Promotion to Level 3

A new hire (or other Level 2 engineer) is eligible for Level 3 consideration only after all of the following:

1. **At least 60 days on the rotation.**
2. **At least five verified live triages**, reviewed by a senior engineer for evidence quality, uncertainty handling, escalation judgment, data handling, and handoff quality. Simulations do not count as live triages.
3. **Explicit architect approval**, recorded with the promotion decision and any scope limitations.

Until all criteria are documented and approved, the engineer remains capped at Level 2. Level 3 grants only approved, bounded execution authority; it never grants Level 4 autonomy. Reassess access and authorization when role, rotation, or incident responsibilities change.
