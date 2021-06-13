namespace FSharpSending.Results

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open FSharpSending.Common.Helpers.RabbitMQ.DependencyConfiguration
open System
open Logger
open Microsoft.Extensions.Logging
open FSharpSending.Results.Stores.JobMessageBus
open RabbitMQ.Client
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Results.RabbitMQ
open FSharpSending.Results.Controllers
open System.Net.Http

module Startup =
    type IServiceCollection with
        member __.ConfigureServices (config : IConfiguration) =
            let services = __

            configureRabbit services config

            services.AddSingleton<MessageBusStore>(fun serviceProvider ->
                let rabbitConnection = serviceProvider.GetRequiredService<IConnection>()
                let loggerStore = serviceProvider.GetRequiredService<LoggerStore>()
                let workflowId = WorkflowId (Convert.ToInt32 config.["WorkflowId"])
                RabbitJobStore.createRabbitJobStore rabbitConnection workflowId loggerStore.logError
            ) |> ignore

            services.AddSingleton<Logger.LoggerStore>(fun serviceProvider ->
                let logger = serviceProvider.GetRequiredService<ILogger<LoggerStore>>()
                Logger.createLogger logger
               ) |> ignore

            services.AddHttpClient<AgentsController>(fun serviceProvider (client : HttpClient) ->
                client.BaseAddress <- new Uri(config.["Notifications:Url"])      
            )