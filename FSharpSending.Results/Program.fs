namespace FSharpSending.Results

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open FSharpSending.Common.Helpers.AppConfigurations.ConfigurationExtensions
open FSharpSending.Results.Startup

module Program =
    let exitCode = 0

    let CreateHostBuilder args =
        let dllPath = System.Reflection.Assembly.GetEntryAssembly().Location 
        let contentRoot =  dllPath.Substring(0, dllPath.LastIndexOf(@"\") + 1)
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(contentRoot)
            .ConfigureHostConfiguration(fun configHost ->
                 configHost.AddCommandLine(args) |> ignore
                 )
            .ConfigureSettingFiles()
            .ConfigureServices(fun hostContext services ->
                    services.AddHostedService<ResultsService>()
                            .ConfigureServices hostContext.Configuration |> ignore
                )
            .ConfigureLogging(fun context logging -> 
                              logging.AddConfiguration(context.Configuration.GetSection("Logging"))
                                     .AddConsole() |> ignore
                              );

    [<EntryPoint>]
    let main args =
        CreateHostBuilder(args).Build().Run()

        exitCode