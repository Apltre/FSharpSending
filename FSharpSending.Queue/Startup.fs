namespace FSharpSending.Queue

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open FSharpSending.Common.Helpers.RabbitMQ.DependencyConfiguration
open MongoDB.Driver
open FSharpSending.Queue.Mongo
open FSharpSending.Queue.Stores
open System
open Logger
open Microsoft.Extensions.Logging
open FSharpSending.Queue.Stores.JobMessageBus
open RabbitMQ.Client
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.RabbitMQ

module Startup =
    type IServiceCollection with
        member __.ConfigureServices (config : IConfiguration) =
            let services = __

            configureRabbit services config

            services.AddSingleton(new MongoClient(config.GetConnectionString("Mongo"))) |> ignore

            services.AddSingleton<IMongoDatabase>(fun serviceProvider ->
                serviceProvider.GetRequiredService<MongoClient>()
                               .GetDatabase("Sending")
            ) |> ignore

            services.AddSingleton<IMongoCollection<MongoJob>>(fun serviceProvider ->
                let collection = serviceProvider.GetRequiredService<IMongoDatabase>()
                                                .GetCollection<MongoJob>("FSJobs")
                let keysDefinition = Builders<MongoJob>.IndexKeys.Ascending(fun j -> j.StartTime :> Object)
                let indexModel = new CreateIndexModel<MongoJob>(keysDefinition)
                collection.Indexes.CreateOneAsync(indexModel) |> Async.AwaitTask |> Async.RunSynchronously |> ignore
                collection
            ) |> ignore

            services.AddSingleton<DbJobStore>(fun serviceProvider ->
                let collection = serviceProvider.GetRequiredService<IMongoCollection<MongoJob>>()
                MongoJobStore.createMongoJobStore collection
            ) |> ignore

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