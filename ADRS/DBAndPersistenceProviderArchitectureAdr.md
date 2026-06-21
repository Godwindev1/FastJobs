# ADR: Provider-Package Architecture for Persistence (Inspired by Apache Airflow Providers)

**Status:** Proposed — Not Yet Implemented, Work in Progress
**Date:** 2026-06-21
**Component:** Persistence layer (`FastJobs.SqlServer`, future `FastJobs.Postgres`, `FastJobs.MySql`, third-party providers)

## Context

FastJobs currently has providers as separate projects within the same solution. As more persistence backends are added (Postgres, MySQL, and potentially third-party providers such as a community-built SQLite implementation), we want a consistent, strict pattern for how providers relate to the core contract — rather than each provider growing organically with its own conventions.

Researched precedent: Apache Airflow's "providers" model. Airflow Core defines the scheduling engine; each integration (Postgres, MySQL, Redis, etc.) ships as its own independently versioned package implementing Airflow's hook/operator contracts. Community and third-party providers have identical standing — no special treatment for "official" packages, as long as they implement the same contract. This is a strong model for the separation of concerns FastJobs is aiming for.

## Decision

Rename the current persistence contract project to **`FastJobs.Persistence`** (from `FastJobs.SqlServerCore`), establishing it as the single, backend-agnostic contract surface (`IJobRepository`, related persistence interfaces) that every provider implements.

Each persistence backend — `FastJobs.SqlServer`, `FastJobs.Postgres`, `FastJobs.MySql`, and any third-party provider — depends on `FastJobs.Persistence` and implements its contracts independently. No provider depends on another provider. The pattern is strict: a provider may only depend on `FastJobs.Persistence` (and its own DB client library), never reach into another provider's project, and never assume FastJobs.Core has backend-specific knowledge.

This is a forward-looking architectural decision, not an implementation yet. `FastJobs.SqlServer` continues as the only working provider for now; the rename and strict separation rule apply as new providers are added.

## Consequences

**Positive**
- Clean seam for third-party providers (e.g. a community SQLite implementation) to plug in with zero special-casing, matching Airflow's "providers have the same capacity, official or not" principle.
- Each provider can evolve and release independently once `FastJobs.Persistence` stabilizes.
- Naming (`FastJobs.Persistence`) accurately reflects its role as a contract, not a SQL-Server-specific core.

**Negative / Risks**
- `FastJobs.Persistence` becomes a real public contract the moment a second provider depends on it — breaking changes there ripple across every provider, including third-party ones not under our control.
- Rename from `FastJobs.SqlServerCore` is a breaking change for any existing consumer/reference.
- Strictness (no cross-provider dependencies) must be enforced by convention/review for now; nothing currently prevents a provider from violating it.

## Future Considerations
- Enforce the "no cross-provider dependency" rule via analyzer or CI check once more than one provider exists.
- Revisit independent versioning/release cadence per provider once `FastJobs.Persistence` is stable (see prior provider-package discussion).