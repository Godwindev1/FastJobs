# ADR: Aggregate Completion Tracking for Recurring Job Instances

## Status
Proposed

## Context
Recurring jobs currently track state per-instance only (`Completed`, `Failed`, etc.). 
Series-level state (`Scheduled`, `Expired`) reflects scheduler lifecycle, not execution outcomes.
There is no way to answer "how did this recurring series perform overall?" 
(e.g., N succeeded / N failed) without querying and aggregating all instance history manually.

## Decision
Not yet decided. Options to evaluate:

1. Add a separate aggregate/summary record per `RecurringJob` id, tallying instance outcomes 
   (success/failure counts), updated on each instance completion.
2. Compute aggregation on-demand via a query/view over state history instead of maintaining a stored summary.
3. Leave as-is; rely on state history + reporting layer for aggregation.

## Consequences
- Aggregate tracking must stay decoupled from series lifecycle state (`Expired`/`Scheduled`) 
  per prior decision that expiry ≠ completion outcome.
- Stored aggregate (Option 1) adds write overhead per instance; on-demand query (Option 2) adds read cost.

## Open Questions
- Is aggregate state needed in real time, or is reporting-time aggregation sufficient?
- Where should this live: `RecurringJob` entity, separate table, or reporting layer?