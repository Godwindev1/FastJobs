
using HostFixtureProviders;

[CollectionDefinition("MariaDBHostFixture_Collection")]
public class MariaDBCollectionDefinition : ICollectionFixture<MariaDbFastJobsHostFixture>
{
    
}


[CollectionDefinition("MSSQLHostFixture_Collection")]
public class MSSQLCollectionDefinition : ICollectionFixture<MsSqlFastJobsHostFixture>
{
    
}

[CollectionDefinition("PostgresHostFixture_Collection")]
public class PostgresCollectionDefinition : ICollectionFixture<PostgresFastJobsHostFixture>
{
    
}