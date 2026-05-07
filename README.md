# Fastjobs
![CI](https://github.com/Godwindev1/FastJobs/actions/workflows/ValidatePackages.yml/badge.svg)

Welcome to the Quick Start Guide for Getting up and running with **FastJobs** .

FastJobs is a lightweight .NET background job processing library built for simplicity and speed.
As you read this guide, expect to see details of:
- Fastjobs Installation
- Configuration & Setup


## Fastjobs Installation
You can install Fastjobs via the .NET CLI or the NuGet Package Manager.

### .NET CLI

```bash
dotnet add package FastJobs
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package FastJobs
```



## NuGet Packages

FastJobs is split into focused packages so you only install what you need.


`FastJobs`  Core engine already discussed above and required for all setups 

`FastJobs.SqlServer`  Sql Server  Required for Persistance persistence for recurring jobs *Currently Supports only My Sql* 

```bash
dotnet add package FastJobs.SqlServer
```
`FastJobs.Dashboard` Optional RCL dashboard for monitoring and  observability 

```bash
dotnet add package FastJobs.Dashboard
```


---

## Configuration & Setup
Fastjobs Is Very Easy To Setup And Get Going. The main job scheduling services you will be interacting with live in `Fastjobs` namespace and the persistence layer for sql in `Fastjobs.sqlServer` namespace.

To use Fastjobs Add the following using statements 
``` csharp
using FastJobs;
using FastJobs.SqlServer;
```

Next Call `builder.Services.AddFastJobs()` with Options for extra config info like so 

```csharp 
string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Services.AddFastJobs(
    option => {  option.WorkerCount = 4; },

    //Fastjobs.sqlServer only has mysql / mariadb provider as of april 2026
     new FastJobs.SqlServer.FastJobMysqlDependencies(
        options => options.ConnectionString =  connectionString
    )
);

//TO INCLUDE THE WEB DASHBOARD
builder.Services.AddFastjobsDashboard();

```

to Finish up configuration call use FastJobs()
```csharp

var app = builder.Build();

app.Services.UseFastJobs();
```
---
if you would like to include the Web Dashboard NB: This Wont work if your application does not use a Web host

```csharp
//ADD USING STATEMENT 
using FastJobs.Dashboard;

/*{
    DI And Services Setup
}*/

var app = builder.Build();

app.UseFastJobsDashboard("/Dashboard"); //should come before routing is done to allow rewriting path to internal Dashboard path
app.UseStaticFiles();   
app.UseRouting();
app.UseAntiforgery();   

//Expose Dashboard Components
app.MapFastJobsDashboard();


```
