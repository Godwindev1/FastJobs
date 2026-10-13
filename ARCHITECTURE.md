# FastJobs Architecture

This document covers the design decisions behind FastJobs. For installation and usage, see the [README](README.md).

## Overview

FastJobs is a decoupled background job engine for ASP.NET Core:

- **Core engine** (`FastJobs`): scheduling, workers, retries, misfire handling and cleanup. It has no database dependency.
- **Persistence abstractions** (`FastJobs.Persistence`): shared models and interfaces used by the core and every provider.
- **Providers** (`FastJobs.SqlServer`, `FastJobs.MariaDB`, `FastJobs.PostgreSQL`): swappable storage implementations with automatic schema initialization.
- **Dashboard** (`FastJobs.Dashboard`): optional Blazor monitoring UI.

## Reliability

- **Exponential backoff with jitter:** configurable base delay (default 10s), max delay cap (default 300s) and jitter (default 5s), so failing jobs don't retry in lockstep.
- **Misfire detection:** a background detector finds jobs that missed their scheduled time and applies the `Skip`, `FireOnce` or `Smart` policy.
- **Recurring jobs:** the next occurrence is scheduled from the scheduled time and missed runs are skipped, which avoids drift.
- **Job chaining:** steps execute without overlapping, verified in tests.
- **Per-job-type concurrency limits:** one job type can't saturate the worker pool.
- **UTC-consistent timestamps:** `DateTimeOffset` across storage and scheduling.

## Cleanup strategies

- A pluggable `ICleanupStrategy` runs on an interval inside a `BackgroundService`.
- Built-in strategies: `CompletedJobsPruningStrategy`, `ExpiredJobsPruningStrategy` and `NullStrategy`.
- **Custom strategies:** implement `ICleanupStrategy` with full DI access to encode your own retention, archival or compliance rules.
- `CancellationToken` is supported end to end. A faulty strategy is caught and logged, so it can't crash the host.

## Logging and observability

- Structured logging through `ILogger`. Sinks are configured by hooking up Serilog, NLog, etc.
- Metrics are covered by tests using `Microsoft.Extensions.Diagnostics.Testing`.
- The Blazor dashboard uses path-rewriting middleware, interactive server rendering, and static assets embedded in a Razor class library.

## Decoupled storage

The core has no database dependency. Each provider implements the persistence abstractions for one database (SQL Server, MariaDB / MySQL, PostgreSQL), so adding a database means adding a package, not changing the engine.

## Testing

- **xUnit** integration suite running against real databases via **Testcontainers**.
- **Contract tests:** one abstract base suite runs against every provider, guaranteeing identical behavior.
- **Fixture design:** `IClassFixture`, `ICollectionFixture` and serialized collections for shared static state.
- Pruning, misfire detection and job chaining are covered by tests. See `TESTING.md` for how to run them.

## Delivery

- Independently versioned NuGet packages (`.nupkg` plus `.snupkg` symbols).
- GitHub Actions: CI on every push to `main`, and NuGet publish on version tags with automatic semver extraction.
- Docs and demo site: [fastjobs.tech](https://fastjobs.tech).
