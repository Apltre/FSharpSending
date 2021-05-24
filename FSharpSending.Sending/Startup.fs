namespace FSharpSending.Sending

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open FSharpSending.Common.Helpers.RabbitMQ.DependencyConfiguration
open System
open Logger
open Microsoft.Extensions.Logging
open FSharpSending.Sending.Stores.JobMessageBus
open RabbitMQ.Client
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Sending.RabbitMQ
open FSharpSending.Sending.Controllers
open System.Net.Http

module Startup =
    type IServiceCollection with
        member __.ConfigureServices (config : IConfiguration) =
            let services = __

            configureRabbit services config

            services.AddSingleton<MessageBusStore>(fun serviceProvider ->
                let rabbitConnection = serviceProvider.GetRequiredService<IConnection>()
                let loggerStore = serviceProvider.GetRequiredService<LoggerStore>()
                let workflowId = WorkflowId (Convert.ToInt32 config.["Id"])
                RabbitJobStore.createRabbitJobStore rabbitConnection workflowId loggerStore.logError
            ) |> ignore

            services.AddSingleton<Logger.LoggerStore>(fun serviceProvider ->
                let logger = serviceProvider.GetRequiredService<ILogger>()
                Logger.createLogger logger
               ) |> ignore

            services.AddHttpClient<AgentsController>(fun serviceProvider (client : HttpClient) -> 
                client.BaseAddress <- new Uri(config.["Agents:Url"])        
            )