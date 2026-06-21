# ADR: Extensible Custom Cleanup Strategies

**Status:** Accepted
**Date:** 2026-06-21
**Component:** FastJobs.Core (Cleanup subsystem)

## Context

FastJobs ships with built-in `ICleanupStrategy` implementations (`CompletedJobsPruningStrategy`, `ExpiredJobsPruningStrategy`, `NullStrategy`) executed on a fixed interval by `JobCleanupManager`, a `BackgroundService`.

These built-ins cover the generic case: prune rows by terminal status and age. They do not — and cannot — cover business-specific retention logic. Real-world consumers (e.g. trading systems, audit-sensitive workflows) need rules such as:

- Different retention windows per job type or tag
- Asymmetric handling of failed vs. succeeded jobs
- Archival to cold storage before deletion
- Metrics-driven decisions (e.g. only prune if disk usage or queue depth crosses a threshold)
- Compliance/regulatory holds that override age-based deletion entirely

This logic is inherently consumer-specific and cannot be reasonably generalized into FastJobs' core strategies without turning the library into a rules engine.

## Decision

Expose `ICleanupStrategy` as a first-class, public extension point. Library consumers can implement their own strategy class, inject any FastJobs core service (job repository, metrics, configuration) or any of their own application services via standard DI, and register it in place of (or alongside) the built-in strategies.

`JobCleanupManager` remains strategy-agnostic: it resolves `ICleanupStrategy` from DI and invokes `Clean()` on a timer, with errors caught and logged so a faulty custom strategy cannot crash the host. All business logic, decision-making, and side effects (archiving, alerting, conditional pruning, etc.) live entirely inside the consumer's `Clean()` implementation — FastJobs imposes no contract beyond "do your cleanup work, async, idempotently, and respect cancellation."

Composing multiple strategies (running several cleanup routines per tick, concurrently or sequentially) is explicitly out of scope for FastJobs. `JobCleanupManager` resolves and runs exactly one `ICleanupStrategy`. If a consumer needs multiple cleanup routines, composing them (e.g. a wrapper strategy that internally calls several inner ones) is the strategy developer's responsibility, not FastJobs'.

### Cancellation contract

`ICleanupStrategy.Clean` accepts a `CancellationToken`, forwarded by `JobCleanupManager` from its own host `CancellationToken`:

```csharp
public interface ICleanupStrategy
{
    Task Clean(CancellationToken cancellationToken);
}
```

Rules for strategy implementers:

- Pass the token into every async call that accepts one (DB calls, HTTP calls, `Task.Delay`). If a dependency doesn't support a token, check `cancellationToken.IsCancellationRequested` manually at reasonable points.
- For multi-step or batched work (e.g. archiving in chunks), check the token between steps/batches — not just once at the start — so shutdown isn't blocked behind a large backlog.
- Do not catch and swallow `OperationCanceledException` inside `Clean()`. Let it propagate. `JobCleanupManager` already treats it as an expected shutdown signal; swallowing it and logging as an error misrepresents a clean shutdown as a failure.
- Built-in strategies (`CompletedJobsPruningStrategy`, `ExpiredJobsPruningStrategy`, `NullStrategy`) and `IJobRepository` pruning methods are updated to accept and forward the token down to the underlying DB call, so the contract is meaningful end-to-end rather than cosmetic at the top level.

## Consequences

**Positive**
- Consumers get full control over retention/archival policy without forking FastJobs.
- Core engine stays small; no bespoke rules DSL to design, document, or maintain.
- Consistent with existing pattern (`NullStrategy` already proves the seam works).

**Negative / Risks**
- No enforced idempotency on custom strategies — a poorly written `Clean()` could double-process records across ticks. Mitigated only by documentation, not the type system.
- Composing multiple cleanup routines is entirely the strategy developer's responsibility; FastJobs provides no built-in fan-out or orchestration across strategies.
- No built-in retry/backoff per strategy — a transient failure (e.g. a brief DB connection blip) is logged and the system simply waits for the next tick, which may be too coarse-grained for consumers who need faster recovery from transient errors, and offers no escalation path if failures persist across many consecutive ticks.

## Future Considerations

- Retry/backoff handling around `Clean()` invocations (e.g. exponential backoff on transient failure, or a consecutive-failure threshold that surfaces a distinct "cleanup degraded" signal) rather than relying solely on the fixed interval to retry.