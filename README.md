# Hangfire.AspNetCore.Plus 
An improved version of Hangfire.AspNetCore package based on my experience in recent years with following features:

- Default setup with some of best community extensions
- Added Hangfire.Console with Serilog Sink output for better logging and troubleshooting
- Allows dashboard with various authorization options (local requests only, user with role authentication, no authentication)
- Adds Hangfire Health check to project
- Allows custom keys for jobs and retrieve jobs using custom keys

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

