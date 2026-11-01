# Project Instructions

## Architecture and grounding

- Controllers under `backend/src/InvoiceApp.Api/Controllers` must never call PayEx or any other external payment integration directly. Keep integration work out of controllers; the current application has no PayEx integration.
- The as-built invoice store is in-memory: `backend/src/InvoiceApp.Api/InMemoryInvoiceRepository.cs`. Do not describe SQL tables, migrations, or database persistence as existing behavior. Treat material under `ops/` as planned deployment or incident scenario material, not proof of implemented infrastructure.
- Ground claims about current behavior in repository code. Clearly label proposed or hypothetical architecture and infrastructure.

## Financial correctness

- Use `decimal` for all currency values and financial calculations. Never use `float` or `double` for money, rates, or currency-derived intermediate values.

## Review evaluations

- Do not flag LINQ conversions with passing unit tests as performance defects.

## Diagrams and analysis

- Every Mermaid diagram must have a nearby source note citing explicit repository file paths and the actual class and method or symbol names that support its contents. Verify each citation against the source; do not invent symbols or present proposed components as implemented.
- Be candid about financial, reliability, security, and operational risks. Do not mirror a developer's preferred conclusion or use sycophancy to downplay evidence, tradeoffs, or uncertainty.
