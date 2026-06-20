using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FastJobs;
using FastJobs.SqlServer;
using FastJobs.AfterActions;
//
string connectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

var builder = Host.CreateApplicationBuilder(args);

// Register jobs with the DI container so it can be resolved when the job is executed.
builder.Services.AddJobService<ValidateOrderJob>();
builder.Services.AddJobService<ChargePaymentJob>();
builder.Services.AddJobService<SendConfirmationEmailJob>();
builder.Services.AddJobService<NotifyWarehouseJob>();

builder.Services.SetCleanupStrategy<ExpiredJobsPruningStrategy>();
//builder.Services.SetCleanupStrategy<CompletedJobsPruningStrategy>();

builder.Services.AddFastJobs(
    option => {  option.WorkerCount = 2;  option.CleanupInterval = TimeSpan.FromSeconds(35); },
     new FastJobMysqlDependencies(options => options.ConnectionString = connectionString)
);



var app = builder.Build();


app.Services.UseFastJobs();


//start host 
await app.StartAsync();

//await FastJobServer
//.EnqueueJob<FailTestJob>()
//.AddAfterAction( x => x.WithType<DeleteAfterAction>())
//.SetMaxRetryCount(5)
//.Start();

for(int i = 0; i < 10; i++)
{
    await FastJobServer.EnqueueJob<ValidateOrderJob>().SetExpiresAt(DateTime.Now.AddMinutes(1)).Start();
}


//await FastJobServer.CreateChain()
//.AddStep<ValidateOrderJob>()
//.ThenRun<ChargePaymentJob>()
//.ThenRun<SendConfirmationEmailJob>()
//.ThenRun<NotifyWarehouseJob>()
//.EnqueueAsync();


await app.WaitForShutdownAsync();

