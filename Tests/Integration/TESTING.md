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
  `WorkerRepositoryTest<T>`, etc.) hold the actual `[Fact]` methods, and live in the
  test project itself.
- **Each provider gets a concrete subclass per repository** (e.g.
  `MsSql_Jobs_repositoryTest : JobRepositoryTest<MsSqlFastJobsHostFixture>`), tagged
  with `[Collection("<Provider>HostFixture_Collection")]` and
  `[Trait("Provider", "<Provider>")]`.
- **One container per provider, shared across all its repository test classes.**
  Classes tagged with the same `[Collection(...)]` name share a single
  `FastJobsHostFixtureBase` instance (one Testcontainer, one `IHost`, one FastJobs
  runtime) — not one per class.

### Fixture code lives in a separate project: `DBHostFixtureProviders`

All Testcontainers/fixture wiring has been pulled out of the test project into its
own class library, referenced via:

```xml
<ItemGroup>
  <ProjectReference Include="..\DBHostFixtureProviders\DBHostFixtureProvider.csproj" />
</ItemGroup>
```

Layout:

```
DBHostFixtureProviders/
├── DBHostFixtureProvider.csproj
├── FastjobsHostFixture.cs        # FastJobsHostFixtureBase (abstract, shared IHost bootstrap)
├── DB-Providers/
│   ├── IDatabaseFixture.cs       # IAsyncLifetime + ConnectionString contract
│   ├── MariaDBFixture.cs         # Testcontainers.MariaDb wrapper
│   └── MSSQLFixture.cs           # Testcontainers MSSQL wrapper
└── HostFixtures/
    ├── MariaDBHostFixture.cs     # FastJobsHostFixtureBase subclass, wires MariaDB into FastJobs
    └── MSSQLHostFixture.cs       # FastJobsHostFixtureBase subclass, wires MSSQL into FastJobs
```

- `DB-Providers/` — raw container lifecycle. One `IDatabaseFixture` implementation per
  provider; knows nothing about FastJobs, only how to start/stop a container and hand
  back a connection string.
- `HostFixtures/` — one `FastJobsHostFixtureBase` subclass per provider; wires the raw
  `IDatabaseFixture` into an `IHost` running FastJobs (schema name, provider-specific
  `FastJobXyzDependencies`, etc.). This is also where any provider-specific "custom
  data" (schema names, options) is set — see note below.

### `CollectionDefinition` stays in the test project

Even though the fixture *classes* now live in `DBHostFixtureProviders`, the
`[CollectionDefinition("<Provider>HostFixture_Collection")]` /
`ICollectionFixture<T>` marker classes must remain in the **test project**, not the
fixture library. xUnit only discovers `[CollectionDefinition]` attributes by
reflecting over the assembly it's currently executing — it does not scan referenced
assemblies. Putting the definition in `DBHostFixtureProviders` produces:

```
The following constructor parameters did not have matching fixture data: MsSqlFastJobsHostFixture fixture
```

```csharp
// In the TEST project
using  HostFixtureProviders;

[CollectionDefinition("MSSQLHostFixture_Collection")]
public class MSSQLCollectionDefinition : ICollectionFixture<MsSqlFastJobsHostFixture> { }

[CollectionDefinition("MariaDBHostFixture_Collection")]
public class MariaDBCollectionDefinition : ICollectionFixture<MariaDbFastJobsHostFixture> { }
```

### Provider-specific config

xUnit constructs fixture types itself and won't accept custom constructor arguments
on `ICollectionFixture<T>`/`CollectionDefinition`. Per-provider config (schema names,
etc.) instead lives inside each `HostFixtures/*HostFixture.cs` subclass — either as a
literal or read from env/config in its parameterless constructor / `ConfigureFastJobs`
override.

### Adding a new provider

1. New `IDatabaseFixture` implementation under `DB-Providers/` (container
   startup/teardown) in `DBHostFixtureProviders`.
2. New `FastJobsHostFixtureBase` subclass under `HostFixtures/` in
   `DBHostFixtureProviders` (wires the provider into FastJobs).
3. New `[CollectionDefinition("<Provider>HostFixture_Collection")]` /
   `ICollectionFixture<T>` pair — **in the test project**.
4. New repository test subclasses in the test project, tagged with
   `[Collection(...)]` and `[Trait("Provider", "...")]`.

## Known gotchas

- **`CollectionDefinition` classes must live in the test assembly**, not in
  `DBHostFixtureProviders`. See above — this is the #1 cause of "did not have
  matching fixture data" errors after the project split.
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