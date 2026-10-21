using System;
using System.Threading.Tasks;
using System.Net.Http;

using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace NBomberTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var httpClient = Http.CreateDefaultClient();

            var scenario = Scenario.Create("http_scenario", async context =>
            {
                var request =
                    Http.CreateRequest("GET", "https://nbomber.com")
                        .WithHeader("Accept", "text/html");
                        // .WithHeader("Accept", "application/json")
                        // .WithBody(new StringContent("{ id: 1 }", Encoding.UTF8, "application/json");
                        // .WithBody(new ByteArrayContent(new [] {1,2,3}))                        

                var response = await Http.Send(httpClient, request);

                return response;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 100, 
                                  interval: TimeSpan.FromSeconds(1),
                                  during: TimeSpan.FromSeconds(30))
            );

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
            }
    }
}