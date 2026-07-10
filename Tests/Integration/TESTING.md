# Running the Integration Test Suite

FastJobs' integration tests run the same contract tests against every supported
DB provider (MSSQL, MariaDB, ...) using Testcontainers.

## Prerequisites

- Docker reachable via `DOCKER_HOST` (this project targets a remote Docker host —
  set `DOCKER_HOST=tcp://<vm-ip>:2375` in your `.env` file at the test project root)
- A `.env` file loaded via `DotNetEnv` — missing/misconfigured values currently fail
  with a `NullReferenceException` rather than a clear error, so double-check this first
  if setup blows up immediately

## Running tests

```bash
# run everything, all providers
dotnet test

# run a single provider
dotnet test --filter "Provider=MSSQL"
dotnet test --filter "Provider=MariaDB"

# run multiple providers
dotnet test --filter "Provider=MSSQL|Provider=MariaDB"
```

`dotnet test` with no filter is what CI runs — it's the source of truth that every
provider passes. The `--filter` flag is for fast local iteration when you only care
about one provider.

## Architecture

- **Contract tests are written once.** Abstract generic bases (`JobRepositoryTest<T>`,
  `WorkerRepositoryTest<T>`, etc.) hold the actual `[Fact]` methods.
- **Each provider gets a concrete subclass per repository** (e.g.
  `MsSql_Jobs_repositoryTest : JobRepositoryTest<MsSqlFastJobsHostFixture>`), tagged
  with `[Collection("<Provider>HostFixture_Collection")]` and
  `[Trait("Provider", "<Provider>")]`.
- **One container per provider, shared across all its repository test classes.**
  Classes tagged with the same `[Collection(...)]` name share a single
  `FastJobsHostFixtureBase` instance (one Testcontainer, one `IHost`, one FastJobs
  runtime) — not one per class.
- **Adding a new provider** means:
  1. A new `IDatabaseFixture` implementation (container startup/teardown)
  2. A new `FastJobsHostFixtureBase` subclass (wires the provider into FastJobs)
  3. A new `[CollectionDefinition("<Provider>HostFixture_Collection")]` /
     `ICollectionFixture<T>` pair
  4. New repository test subclasses tagged with `[Collection(...)]` and
     `[Trait("Provider", "...")]`

## Known gotchas

- **Generic test bases must be `abstract`.** If not, xUnit tries to discover and
  construct the open generic type directly, producing a
  `Class fixture type '' may only define a single public constructor` error.
- **Fixture classes must never contain `[Fact]` methods.** A fixture with a `[Fact]`
  gets discovered as its own independent test and self-triggers, spinning up a
  container outside of any collection.
- **Don't mix `IClassFixture<T>` and `[Collection]`/`ICollectionFixture<T>`** on the
  same test hierarchy. `IClassFixture<T>` takes priority and silently defeats
  collection-level sharing — each class ends up with its own container again.
- **Tests within a collection run serially, not in parallel**, by design — they share
  one database instance, so this avoids race conditions between test classes.
  Collections for *different* providers still run in parallel with each other.