namespace FSharpSending.Sending

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open FSharpSending.Common.Helpers.AppConfigurations.ConfigurationExtensions
open FSharpSending.Sending.Startup

module Program =
    let exitCode = 0

    let CreateHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(fun configHost ->
                 configHost.AddCommandLine(args) |> ignore
                 )
            .ConfigureSettingFiles()
            .ConfigureServices(fun hostContext services ->
                    services.AddHostedService<SendingService>()
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