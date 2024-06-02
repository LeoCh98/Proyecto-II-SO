
using ClientGRPC;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress(" http://localhost:5238");
        var client = new MessageBroker.MessageBrokerClient(channel);

        var cts = new CancellationTokenSource();

        Console.WriteLine("Seleccione una opción:");
        Console.WriteLine("1. Publicar mensaje");
        Console.WriteLine("2. Suscribirse a un tema");
        var option = Console.ReadLine();

        if (option == "1")
        {
            await PublishMessage(client);
        }
        else if (option == "2")
        {
            await SubscribeToTopic(client, cts.Token);
        }
    }

    static async Task PublishMessage(MessageBroker.MessageBrokerClient client)
    {
        Console.WriteLine("Ingrese el tema:");
        var topic = Console.ReadLine();
        Console.WriteLine("Ingrese el mensaje:");
        var message = Console.ReadLine();

        var reply = await client.PublishAsync(new PublishRequest { Topic = topic, Message = message });
        Console.WriteLine("Respuesta del servidor: " + reply.Status);
    }

    static async Task SubscribeToTopic(MessageBroker.MessageBrokerClient client, CancellationToken cancellationToken)
    {
        Console.WriteLine("Ingrese el tema:");
        var topic = Console.ReadLine();

        using var stream = client.Subscribe(new SubscribeRequest { Topic = topic }, cancellationToken: cancellationToken);

        Console.WriteLine("Suscrito al tema: " + topic);
        try
        {
            await foreach (var message in stream.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
               
            }
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
        {
            Console.WriteLine("La suscripción fue cancelada.");
        }
    }
}
