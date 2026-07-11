## ADR-002: Multiple Concurrent FastJobs Instances Within a Single Process

**Status:** Proposed (future consideration — not scheduled)
**Date:** 2026-07-11

## Context

`FastJobServer` is currently a static singleton exposing one global `IServiceScopeFactory`:

```csharp
public static class FastJobServer
{
    private static IServiceScopeFactory _ScopeFactory;
    static internal void BuildInstance(IServiceScopeFactory scopeFactory) { ... }
}
```

This gives developers a static, instance-free API (`FastJobServer.EnqueueJob<T>()`), which is a deliberate ergonomic choice. The tradeoff: **one process can only ever be wired to one job store (one DB, one provider) at a time**, because `BuildInstance()` overwrites the only slot that exists. Calling it twice in the same process (as surfaced during integration testing, where two provider fixtures both call `BuildInstance()`) causes the second call to silently redirect *all* callers — including ones mid-flight — to the new store.

Today this is a non-issue in production because FastJobs is explicitly single-process, single-database. It became visible only in tests, where multiple provider fixtures share the same static.

Anticipated future cases where "one store per process" stops being sufficient:
- Multi-tenant deployments where different tenants must be isolated to different databases.
- Sharded/regional job stores in one process (e.g. data-residency requirements).
- Running logically separate job domains (e.g. "critical" vs "bulk") with different storage profiles in one host, without paying for separate processes.

## Two distinct patterns worth separating

These get conflated easily because both involve "more than one FastJobs instance," but they solve different problems and would be implemented differently. Worth keeping them as separate future paths rather than one option.

### Pattern A — Multiple independent instances, each with its own DB (isolation)

N FastJobs instances, each single-process/single-DB, fully unaware of each other. A supervising "Server" abstraction owns all N and routes a given call to the correct one based on caller context (tenant ID, job domain, region, etc.). No two instances ever touch the same data — isolation is the point. This was the original framing above.

- **Coordination needed:** none between instances. The only coordination problem is routing — making sure a call from tenant A's flow resolves to tenant A's instance, not tenant B's. `AsyncLocal` (option 2 below) is a good fit for this because it's a per-flow *selection* problem, not a shared-state problem.
- **Failure mode if done wrong:** exactly the bug already found — one instance's setup silently overwrites another's, and calls get routed to the wrong DB.

### Pattern B — Multiple instances pointing at the *same* DB, synchronized (this message's addition)

N FastJobs instances/nodes, potentially across processes or machines, all reading and writing the **same** job store, needing to agree on things like "which node claims this job so it doesn't run twice." This is the pattern Quartz.NET's clustering actually implements (see prior correction) — not a non-goal after all, if this is the direction you want. It's a genuinely different engineering problem from Pattern A:

- **Coordination needed:** real distributed coordination. At minimum, a claim/lease mechanism so two nodes don't both pick up and execute the same due job — this is what Quartz solves with DB-level row locking (`UPDATE ... SET locked_by = @nodeId WHERE locked_by IS NULL`, or a dedicated lock table) rather than anything in-process. `AsyncLocal` is irrelevant here — it only scopes state within one process's logical call flow; it cannot coordinate across processes or machines.
- **What "FastJobServer synchronizes" would actually require:**
  - A unique, stable node/instance identity per running FastJobs instance (Quartz's `instanceId`).
  - A claim mechanism at the schema level — an owner/lease column with a heartbeat, or a `SELECT ... FOR UPDATE`/equivalent per-provider locking primitive, so `PruneByExpiry`-style and job-dispatch operations don't double-execute. This is schema and provider-library work, not just a change to the static `FastJobServer` class.
  - Failure/recovery handling: what happens when a node dies mid-job (Quartz re-queues jobs flagged "requests recovery" once the dead node's lease expires) — needs an equivalent decision here.
  - Clock-skew awareness if nodes run on separate machines, since lease expiry is time-based (Quartz explicitly warns about this).
- **Failure mode if done wrong:** the exact bug class Quartz users hit in production — two nodes both acquire the same trigger and the same job runs twice (see Quartz issue #397, still an open sharp edge in their own ecosystem even with locking in place).
- **Relationship to Pattern A:** these aren't mutually exclusive long-term. A single "isolated instance" in Pattern A could itself later be a Pattern-B cluster (e.g. tenant A's instance is 3 clustered nodes against tenant A's DB). But building both at once is a lot of surface area — worth sequencing rather than designing simultaneously.

## Considered options

1. **Do nothing** — keep single static global. Simplest; correct as long as FastJobs' scope stays single-store-per-process. Current tests worked around this via `[Collection]`-based serialization.
2. **`AsyncLocal<IServiceScopeFactory>` override on top of the existing static global** — logical-flow-scoped instance selection, falling back to the process-wide default. Solves **Pattern A** (routing, not coordination). Keeps the static call site unchanged for existing consumers; adds an internal seam for flow-scoped overrides (used already to fix the test cross-contamination issue). Low overhead (context fork on override-set, not on every call). Does nothing for Pattern B — no cross-process reach.
3. **A supervising "Server" abstraction managing multiple independent FastJobs Core deployments** — the Pattern A architecture. A higher-level project owns N single-process/single-DB FastJobs instances and routes work to the correct one. `AsyncLocal` (option 2) is the likely in-process mechanism underneath it.
4. **DB-level claim/lease locking per provider, plus a node-identity concept** — the Pattern B architecture. Substantially larger scope: schema changes (lock/owner columns), per-provider locking primitive implementations (SQL Server vs MariaDB row locking semantics will differ, same class of divergence the multi-provider contract test suite already exists to catch), and recovery/heartbeat logic. Should not be started opportunistically alongside Pattern A work — it's a separate initiative.

## Decision

No decision yet — this is deferred. FastJobs remains single-process, single-database for now. If Pattern A (isolation) is needed first, option 2 (`AsyncLocal`) is the validated, low-risk stepping stone since it doesn't change the public static API. Pattern B (shared-DB synchronization) is a materially larger undertaking — schema, per-provider locking, and recovery semantics — and should get its own ADR and design pass rather than being folded into whichever gets built first.

## Consequences of deferring

- No API surface committed to yet — free to design the "Server" abstraction properly later rather than retrofitting it.
- Risk: if multi-instance support is bolted on reactively under deadline pressure, it's likely to reproduce the same static-mutation hazard this ADR originates from. Revisit this document before starting that work rather than re-deriving it.
- Test suite already carries the workaround (`[Collection]` grouping / `AsyncLocal` override) needed to safely exercise multiple provider fixtures in one run — that pattern is reusable scaffolding if option 3 is picked up later.

## Follow-ups for whoever picks this up

- Investigate how the "Server" abstraction should route a call to the correct FastJobs instance (explicit handle vs. ambient context vs. `AsyncLocal`-based transparency).
- Decide whether independent instances share a process or whether "concurrent instance" should just mean "separate process" (much simpler, avoids all of the above, at the cost of resource overhead per instance).
- Benchmark `AsyncLocal` fork cost under FastJobs' actual enqueue hot path before assuming it's negligible at scale.