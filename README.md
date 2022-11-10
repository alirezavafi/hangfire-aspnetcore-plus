# Hangfire.AspNetCore.Plus 
An improved version of Hangfire.AspNetCore package based on my experience in recent years with following features:

- Default setup with some of best community extensions
- Added Hangfire.Console with Serilog Sink output for better logging and troubleshooting
- Allows dashboard with various authorization options (local requests only, user with role authentication, no authentication)
- Adds Hangfire Health check to project
- Allows custom keys for jobs and retrieve jobs using custom keys
- Supports Dependency Injection for Job filters (Inherit JobFilterWithServiceProviderAttribute)
- Job filters on success or failure states with dependency injection (Inherit JobFailureFilterAttribute or JobSuccessFilterAttribute)
 
 
### Used Hangfire Extensions
- Hangfire.Console (https://github.com/pieceofsummer/Hangfire.Console)
- Hangfire.Correlate (https://github.com/skwasjer/Hangfire.Correlate)
- Hangfire.MissionControl (https://github.com/ahydrax/Hangfire.MissionControl)
- Hangfire.Console.Extensions.Serilog (https://github.com/AnderssonPeter/Hangfire.Console.Extensions)
- Bonura.Hangfire.PerformContextAccessor (https://github.com/meriturva/Hangfire.PerformContextAccessor)


### Instructions

**First**, install the _Hangfire.AspNetCore.Plus_ [NuGet package](https://www.nuget.org/packages/Hangfire.AspNetCore.Plus) into your app.

```shell
dotnet add package Hangfire.AspNetCore.Plus --prerelease
```

In your application's _Startup.cs_, add the middleware like below:

```csharp
 public void ConfigureServices(IServiceCollection services)
 {
     ...

     var connectionString = ...
     var storage = new SqlServerStorage(connectionString, new SqlServerStorageOptions());
     
     services.AddHangfirePlus(storage);
     services.AddHangfireServer();

     // or
     // services.AddHangfireServerPlus(storage);

     ...
 }
 
 public void Configure(IApplicationBuilder app, IHostingEnvironment env)
 {
     ...
 
     app.UseEndpoints(e =>
     {
         e.MapHangfireDashboardPlus(app.ApplicationServices, new HangfireDashboardOptions()
                {
                    AuthorizationMode = HangfireDashboardAuthorizationMode.NoAuthorize
                });
     });
 
     ...
 }
```

###Sample Job Filter on Failure

```
public class TestFilter : JobFailureFilterAttribute
{
    private ILogger _logger;

    public TestFilter()
    {
        _logger = ServiceProvider.GetRequiredService<ILogger>();
    }
    
    
    protected override Task OnJobFailure(ElectStateContext context, FailedState state)
    {
        _logger.Error("job failure");
        return Task.CompletedTask;
    }
}

public class TestJob
{
    [TestFilter]
    public void Test()
    {
        throw new InvalidOperationException();
    }
}
