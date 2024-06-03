using ClientGRPC;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static List<string> temas = new List<string>(); // Lista para almacenar los temas
    static List<string> suscripciones = new List<string>(); // Lista para almacenar las suscripciones del usuario

    static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5238");
        var client = new MessageBroker.MessageBrokerClient(channel);

        var cts = new CancellationTokenSource();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("Seleccione una opción:");
            Console.WriteLine("1. Publicar mensaje");
            Console.WriteLine("2. Suscribirse a un tema");
            Console.WriteLine("3. Salir");

            var option = Console.ReadLine();

            if (option == "1")
            {
                await PublicarMensajeMenu(client);
            }
            else if (option == "2")
            {
                await SuscripcionesMenu(client, cts.Token);
            }
            else if (option == "3")
            {
                break;
            }
        }
    }

    static async Task PublicarMensajeMenu(MessageBroker.MessageBrokerClient client)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Menú Publicar Mensaje:");
            Console.WriteLine("1. Escribir mensaje");
            Console.WriteLine("2. Ver temas existentes");
            Console.WriteLine("3. Volver al menú principal");

            var option = Console.ReadLine();

            if (option == "1")
            {
                await PublishMessage(client);
            }
            else if (option == "2")
            {
                ListarTemas();
            }
            else if (option == "3")
            {
                break;
            }
        }
    }

    static async Task PublishMessage(MessageBroker.MessageBrokerClient client)
    {
        Console.Clear();
        Console.WriteLine("Ingrese el tema:");
        var topic = Console.ReadLine();
        Console.WriteLine("Ingrese el mensaje:");
        var message = Console.ReadLine();

        var reply = await client.PublishAsync(new PublishRequest { Topic = topic, Message = message });

        if (!temas.Contains(topic))
        {
            temas.Add(topic);
        }

        Console.WriteLine("Respuesta del servidor: " + reply.Status);
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }

    static void ListarTemas()
    {
        Console.Clear();
        Console.WriteLine("Temas existentes:");
        foreach (var tema in temas)
        {
            Console.WriteLine(tema);
        }
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }

    static async Task SuscripcionesMenu(MessageBroker.MessageBrokerClient client, CancellationToken cancellationToken)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Menú Suscripciones:");
            Console.WriteLine("1. Suscribirse a un tema");
            Console.WriteLine("2. Ver mis suscripciones");
            Console.WriteLine("3. Ver suscripciones disponibles");
            Console.WriteLine("4. Volver al menú principal");

            var option = Console.ReadLine();

            if (option == "1")
            {
                await SubscribeToTopic(client, cancellationToken);
            }
            else if (option == "2")
            {
                ListarMisSuscripciones();
            }
            else if (option == "3")
            {
                ListarSuscripcionesDisponibles();
            }
            else if (option == "4")
            {
                break;
            }
        }
    }

    static async Task SubscribeToTopic(MessageBroker.MessageBrokerClient client, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("Ingrese el tema:");
        var topic = Console.ReadLine();

        if (!temas.Contains(topic))
        {
            Console.WriteLine("El tema no existe.");
            Console.WriteLine("Presione una tecla para continuar...");
            Console.ReadKey();
            return;
        }

        using var stream = client.Subscribe(new SubscribeRequest { Topic = topic }, cancellationToken: cancellationToken);

        if (!suscripciones.Contains(topic))
        {
            suscripciones.Add(topic);
        }

        Console.WriteLine("Suscrito al tema: " + topic);
        Console.WriteLine("Presione Ctrl+C para cancelar la suscripción.");

        try
        {
            await foreach (var message in stream.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                Console.WriteLine($"Mensaje recibido del tema {message.Topic}: {message.Message_}");
            }
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
        {
            Console.WriteLine("La suscripción fue cancelada.");
        }
    }

    static void ListarMisSuscripciones()
    {
        Console.Clear();
        Console.WriteLine("Mis suscripciones:");
        foreach (var suscripcion in suscripciones)
        {
            Console.WriteLine(suscripcion);
        }
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }

    static void ListarSuscripcionesDisponibles()
    {
        Console.Clear();
        Console.WriteLine("Suscripciones disponibles:");
        foreach (var tema in temas)
        {
            Console.WriteLine(tema);
        }
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }
}
