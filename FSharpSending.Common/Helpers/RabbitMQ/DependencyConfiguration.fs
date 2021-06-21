namespace FSharpSending.Common.Helpers.RabbitMQ

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open RabbitMQ.Client
open Microsoft.Extensions.Options

module DependencyConfiguration =
    let configureRabbit (services : IServiceCollection) (configuration : IConfiguration) =
        let rabbitConfiguration = configuration.GetSection("Rabbit")
        services.Configure<ConnectionFactory>(rabbitConfiguration) |> ignore

        services.AddTransient<IConnection>(fun serviceProvider ->
            let factory = serviceProvider.GetRequiredService<IOptions<ConnectionFactory>>().Value
            factory.AutomaticRecoveryEnabled <- true
            factory.TopologyRecoveryEnabled <- true
            factory.CreateConnection()
         ) |> ignore