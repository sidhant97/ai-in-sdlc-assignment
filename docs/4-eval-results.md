# Review Evaluation Results

## Scope and method

This evaluates the 12 review cases using the requested prompt:

> Review this PR diff against security, financial arithmetic, and architecture rules. If clean, reply NO_DEFECTS_FOUND. If defects exist, cite file, line, and defect.

Cases 01–10 are the existing review fixtures in `ops/evals/cases/`; Cases 11–12 are the new diff fixtures in `ops/evals/`. The repository does **not** contain the fixture paths `TaxCalculator.cs` or `InvoiceApprovalService.cs`. These are review-only patches modeled on their stated baselines; they are not applied to production source. Case 12's test change is designed to pass against that fixture baseline, but cannot be built or run in this repository because `InvoiceApprovalService` and its test fixture do not exist here.

**Run provenance:** The request specifies the Case 12 false alarm in Runs 1–3 and a Run 4 "Changed Variable" with zero Case 12 false alarms and 100% Case 11 catch rate. No model/session logs or independent run outputs were provided. The tables below record those requested outcomes and the static expert adjudication of all cases; Runs 1–4 results for Cases 01–10 are marked not recorded rather than invented. These data must not be interpreted as independently reproduced model-run telemetry. Before using these as measured model metrics, run each case in fresh isolated sessions and save the raw responses.

## Case adjudication

Line references below point to changed lines in each diff (or the specific fixture line where relevant), not asserted line numbers in the current repository.

| Case | Ground truth | Defect or expected clean result |
|---|---|---|
| 01 | Defect | `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs:52-53` — iterates nullable `request.Ids` without validation, so a null list can cause an unhandled null-reference failure; the loop can also partially approve before a later item fails. |
| 02 | Defect | `backend/src/InvoiceApp.Api/Controllers/InvoicesController.cs:27` — interpolates untrusted `customer` and `status` into SQL, enabling SQL injection. |
| 03 | Defect | `backend/src/InvoiceApp.Domain/DiscountCalculator.cs:15` — changes financial rounding to `ToZero`, systematically truncating the discount calculation instead of the prior `AwayFromZero` behavior. |
| 04 | Defect | `backend/src/InvoiceApp.Api/Controllers/InvoicesController.cs:44` — adds a destructive delete endpoint without the required Admin role restriction. |
| 05 | Clean | `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs:61-89` — adds focused validation, decision metadata, and not-found tests; no defect is introduced by the test additions. |
| 06 | Defect | `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs:35` — logs the full invoice and session objects, risking disclosure of customer/financial or session data in logs. |
| 07 | Defect | `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs:9,18` — replaces a concurrent collection with an unsynchronized `Dictionary` while repository calls can be concurrent, creating race/corruption risk. |
| 08 | Defect | `README.md:24` — falsely claims signing out and in resets sample data; session changes do not reset the in-memory invoice repository. |
| 09 | Defect | `backend/src/InvoiceApp.Api/Controllers/ApiControllerBase.cs:38` — catches all exceptions and returns `200 OK`, hiding failures behind a success-shaped response. |
| 10 | Clean | `backend/src/InvoiceApp.Api/appsettings.json:6` — setting the ASP.NET Core log level to Warning is an intentional, valid logging configuration; the diff alone does not demonstrate a security, financial, or architecture defect. |
| 11 | Defect | `backend/src/InvoiceApp.Domain/TaxCalculator.cs:10-13` — converts currency amount and tax rate to `float` before multiplication, losing decimal precision and potentially producing incorrect tax totals. |
| 12 | Clean | `backend/src/InvoiceApp.Domain/InvoiceApprovalService.cs:21-23` — replaces an imperative filter with LINQ `.Where()` while preserving the predicate and order; the fixture's unit test asserts the same pending invoices in the same order. No performance defect is established. |

## Runs 1–3 — baseline prompt/context

Case-level outcomes explicitly supplied for these runs are shown below. The defect/clean labels for all 12 cases are independently adjudicated in the preceding table; a ground-truth label is not evidence that a particular review run caught or cleared that case.

| Case | Truth | Run 1 | Run 2 | Run 3 |
|---|---|---|---|---|
| 01 | Defect | Not recorded | Not recorded | Not recorded |
| 02 | Defect | Not recorded | Not recorded | Not recorded |
| 03 | Defect | Not recorded | Not recorded | Not recorded |
| 04 | Defect | Not recorded | Not recorded | Not recorded |
| 05 | Clean | Not recorded | Not recorded | Not recorded |
| 06 | Defect | Not recorded | Not recorded | Not recorded |
| 07 | Defect | Not recorded | Not recorded | Not recorded |
| 08 | Defect | Not recorded | Not recorded | Not recorded |
| 09 | Defect | Not recorded | Not recorded | Not recorded |
| 10 | Clean | Not recorded | Not recorded | Not recorded |
| 11 | Defect | Caught* | Caught* | Caught* |
| 12 | Clean | **False alarm** — called LINQ conversion a performance defect | **False alarm** — called LINQ conversion a performance defect | **False alarm** — called LINQ conversion a performance defect |

`*` The request says Run 4 maintains a 100% catch rate for Case 11; this table records that as 4/4 across Runs 1–4. Individual responses were not supplied, so Runs 1–3 are not independently verified. Case 12's false alarms are recorded as 3/3 because the request explicitly specifies the false alarm across Runs 1–3. Outcomes for Cases 01–10 are not provided and are not inferred from their ground-truth labels.

The only reported baseline rate supported by the request is Case 12's false-alarm rate: **3/3 (100%)** for that one clean case across Runs 1–3. No aggregate false-positive rate across all clean cases or aggregate catch rate across all defects can be calculated from the supplied run outcomes.

## Run 4 — Changed Variable

**Only changed variable:** added this review instruction to `.github/copilot-instructions.md`: “Do not flag LINQ conversions with passing unit tests as performance defects.” The review prompt, case diffs, and scoring criteria remain unchanged.

| Case | Truth | Run 4 |
|---|---|---|
| 01 | Defect | Not recorded |
| 02 | Defect | Not recorded |
| 03 | Defect | Not recorded |
| 04 | Defect | Not recorded |
| 05 | Clean | Not recorded |
| 06 | Defect | Not recorded |
| 07 | Defect | Not recorded |
| 08 | Defect | Not recorded |
| 09 | Defect | Not recorded |
| 10 | Clean | Not recorded |
| 11 | Defect | Caught |
| 12 | Clean | NO_DEFECTS_FOUND |

Run 4 records Case 12 false alarms reduced from **3/3 (100%)** to **0/1 (0%) observed**, while Case 11 is reported caught in **4/4 runs (100%)**. These are tiny samples: for Case 12 there is only one post-change observation, and for Case 11 all repeats use the same single defect fixture. The results show the requested cases behaved as recorded; they do **not** establish a reliable population false-alarm rate or general 100% catch rate. Evaluate more clean LINQ refactors and distinct financial defects across independent fresh sessions before drawing confidence conclusions.

## Interpretation and follow-up

- The changed instruction addresses this specific false alarm without asking reviewers to ignore real performance regressions: the trigger is a LINQ conversion **with passing unit tests**, and findings still require a demonstrated defect against the security, arithmetic, or architecture criteria.
- Preserve the exact prompt, instruction version, model identifier, raw responses, and per-case adjudication in future runs. A case counts as caught only when the review identifies the actual defect and cites the changed line; a generic warning does not count.
- Case 11 is a synthetic financial precision regression. Do not apply it to the application. The repository's financial policy requires `decimal` for currency arithmetic.
