namespace FSharpSending.Common.Helpers.AppConfigurations

open Microsoft.Extensions.Configuration
open System

module ConfigurationExtensions =
    let private addSettingFiles environment receiver (config : IConfigurationBuilder)  =
        let receiverEnv = 
            match environment <> "Production" with
            | true -> String.Empty
            | false -> $".{environment}"
        config.AddJsonFile("Settings/appsettings.json")
            .AddJsonFile($"Settings/appsettings.{environment}.json", true)
            .AddJsonFile($"Settings/appsettings.{receiver}{receiverEnv}.json")    
    
 
    type Microsoft.Extensions.Hosting.IHostBuilder with
        member __.ConfigureSettingFiles()=
            __.ConfigureAppConfiguration(fun context config ->
                let env = context.HostingEnvironment
                let receiver = context.Configuration.["receiver"]
                addSettingFiles env.EnvironmentName receiver config |> ignore
            )