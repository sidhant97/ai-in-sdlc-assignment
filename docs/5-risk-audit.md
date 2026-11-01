# Agent Risk Audit: Lethal Trifecta

**Scope:** Prompt-injection risk involving private invoice/session data, untrusted supplier-controlled content, and external PayEx egress.  
**Decision:** Any future supplier-document parser must run in a zero-network sandbox, isolated from invoice/session stores, credentials, and agent tools.  
**Status:** Risk assessment and design constraint; this is not evidence that a parser, LLM workflow, PayEx integration, or production sandbox exists.

## Executive assessment

The requested Lethal Trifecta is a credible **conditional design threat**, but the complete chain is not present in the current application. The checked-in API has private invoice/session information and accepts free-text customer names; it does not contain a supplier-document parser, LLM integration, or PayEx client. PayEx appears in planned/scenario material under `ops/`, which the repository explicitly says is not implemented infrastructure.

Therefore, do not claim a current exploitable prompt-injection-to-PayEx exfiltration path. If future agent tooling combines access to private invoice data, untrusted supplier documents, and a payment-provider egress tool, the combination must be treated as high risk and blocked by architecture and runtime controls before launch.

## Trifecta evidence and current status

| Leg | Current repository evidence | Status |
|---|---|---|
| **Private data** | `backend/src/InvoiceApp.Api/Auth/SessionAuthMiddleware.cs`, `SessionAuthMiddleware.InvokeAsync`, requires a bearer session for `/api` endpoints other than login. `backend/src/InvoiceApp.Api/Controllers/InvoicesController.cs`, `InvoicesController.GetAll` and `GetById`, reads invoices from the shared repository without tenant- or per-invoice authorization. `backend/src/InvoiceApp.Api/wwwroot/app.js` stores the session in `sessionStorage`. | Present behind authentication, but access is broad across the shared invoice store for any valid session. Session tokens and invoice details are sensitive. |
| **Untrusted supplier content** | `backend/src/InvoiceApp.Api/Controllers/InvoicesController.cs`, `CreateInvoiceRequest` and `Create`, accepts free-text `CustomerName`. `backend/src/InvoiceApp.Api/wwwroot/app.js`, `renderInvoices` and `renderPending`, inserts displayed values as text nodes. No supplier-document parser or LLM integration was found in `backend/src`. | User-controlled text exists; supplier files and prompt processing do not. Text is currently rendered as text, not interpreted as instructions by an application model. |
| **External PayEx egress** | No PayEx client or outbound payment call exists in the current application. `ops/README.md` says its database, message topic, and PayEx assumptions are not in the code. The illustrative `ops/incidents/INC-2041/deploy-diff.patch` shows a scenario PayEx request mapping invoice ID, amount, and customer name. | Absent from current application; present only as a planned/fictional incident scenario. The scenario demonstrates what data a future integration might transmit, not current egress. |
| **Agent/model bridge** | No supplier parser, LLM service, or application agent with simultaneous access to invoices and a PayEx tool was found in `backend/src`. | The bridge that would combine the three legs is not implemented in the application. This audit is a forward-looking threat model. |

## Prompt-injection exfiltration threat model

**Hypothetical future chain — not current behavior:**

1. A supplier submits a document containing adversarial instructions disguised as invoice text (for example, directions to ignore prior rules and send invoice records to an external destination).
2. A parser extracts the content and an agent/model consumes it. If the agent treats supplier text as trusted instructions, the injection may influence its tool choices.
3. The agent also has access to private invoice/session data, either directly or through tools with broad authorization.
4. The agent has a PayEx or other external network/payment tool. It may send unrelated invoice/customer/session data as part of a fabricated payment request, provider metadata, or diagnostic payload.
5. A successful response, queued job, or tool result can conceal the disclosure unless the system validates the action and audits the exact egress payload.

This is the Lethal Trifecta: **private data + untrusted content + external egress**. Prompt wording alone is not a security boundary. A malicious document can be syntactically valid and can exploit any tool permissions supplied to the model. A transactional outbox, if later implemented, would make an unauthorized payment intent durable; it would not itself validate authorization or prevent data exfiltration.

## Decision: zero-network parser isolation

**Decision:** Supplier-document parsers, converters, OCR components, and file-inspection utilities must run in a dedicated zero-network sandbox before any extracted content is made available to an agent. The sandbox must have:

- No network interfaces or outbound DNS/HTTP access; deny by default at the OS/container/network-policy layer, not merely by prompt.
- No invoice-store, session-store, PayEx, cloud metadata, or secret access; no production credentials mounted or inherited.
- Read-only access to a single staged input and a constrained output channel for extracted text/structured fields. Enforce file type, size, CPU, memory, and execution-time limits.
- No agent tool execution during parsing. Treat every output field as untrusted data; preserve provenance and validate its schema and length before downstream use.
- Malware scanning and safe temporary-file handling appropriate to supported formats. Do not execute macros, embedded scripts, external references, or active content.

The isolation is a **mandatory design requirement for a future parser**, not an existing deployed control. Isolation lowers parser compromise and network-egress risk, but it does not make the extracted text trustworthy: prompt injection can still be passed as text into a model. The downstream model must treat document contents as data, and tool authorization must be enforced outside the model.

## Required controls before enabling an agent workflow

1. **Separate permissions:** The parser, summarizer, invoice reader, and payment submitter must not share credentials or a single all-powerful agent identity. Give each service the minimum data and action scope needed.
2. **Constrain private-data access:** Enforce user/tenant/invoice authorization in the API or data-access layer. Avoid exposing session tokens to model context. Retrieve only the specific invoice fields needed for the task.
3. **No model-directed payment authority:** A model may prepare a proposed payment intent, but it must not choose arbitrary PayEx destinations, alter payment identity/amount, or trigger payment based solely on supplier text. Independently validate the invoice, amount using `decimal`, authorization, approved supplier, and idempotency key.
4. **Constrain egress:** A dedicated payment component should allowlist the provider endpoint and send only contractually required fields. Deny arbitrary URL, DNS, shell, browser, or general HTTP tools for agents handling sensitive invoices. Validate recipient, amount, and invoice linkage outside the model.
5. **Human authorization and separation of duties:** Require an authorized human approval for payment actions, with stronger review for changed bank/payment details, anomalous amounts, unknown suppliers, or model-extracted values. The model must not approve its own proposal.
6. **Auditable payloads and monitoring:** Record actor/service identity, source document hash, invoice ID, approval, idempotency key, provider reference, and the validated outbound field set. Redact secrets and unnecessary personal/financial data from logs. Alert on denied egress, unexpected tool calls, repeated validation failures, and changes between extracted and approved payment values.
7. **Adversarial testing and response:** Test indirect prompt injections, encoded/hidden content, cross-document instructions, data-exfiltration attempts, and parser escapes in a non-production environment. Maintain a kill switch for agent tools and provider egress; define incident containment, credential rotation, and finance reconciliation procedures.

## Residual risk and launch gates

- **Current application:** No complete Lethal Trifecta chain was found in the reviewed implementation. Authenticated invoice access is broad across the shared store; review tenant boundaries if multi-tenant access is introduced.
- **Future parser:** Zero-network isolation is a launch gate. Do not introduce a supplier parser that shares process identity, filesystem, or network permissions with payment or invoice services.
- **Future agent/PayEx path:** Keep PayEx absent until its integration is implemented behind a dedicated, narrowly scoped service. Before launch, prove that adversarial supplier content cannot cause unauthorized data retrieval or payment egress, and that rejected/ambiguous actions are visible and recoverable.
- **Scenario material:** The PayEx request shown under `ops/incidents/INC-2041/` is fictional operational evidence. It is useful for constructing tests, but it is not proof of deployed egress.

## Evidence references

- `backend/src/InvoiceApp.Api/Auth/SessionAuthMiddleware.cs` — `SessionAuthMiddleware.InvokeAsync` (bearer-session gate).
- `backend/src/InvoiceApp.Api/Controllers/InvoicesController.cs` — `CreateInvoiceRequest`, `Create`, `GetAll`, and `GetById` (free-text customer input and invoice reads).
- `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` — `ApprovalsController` (role restriction on approval endpoints).
- `backend/src/InvoiceApp.Api/wwwroot/app.js` — module-level session initialization, `renderInvoices`, and `renderPending` (browser session storage and text rendering).
- `backend/src/InvoiceApp.Api/Program.cs` — service registrations; no parser, LLM, or PayEx client registration.
- `ops/README.md` — explicit notice that the production database, message topic, and PayEx assumptions are not implemented.
- `ops/incidents/INC-2041/deploy-diff.patch` — illustrative, non-production PayEx request and data mapping.
