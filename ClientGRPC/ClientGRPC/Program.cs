using ClientGRPC;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static ClientGRPC.MessageBroker;
using System.ComponentModel.DataAnnotations;

List<string> temas = new List<string>(); // Lista para almacenar los temas
List<string> suscripciones = new List<string>(); // Lista para almacenar las suscripciones del usuario
List<string> suscripciones_Publisher = new List<string>(); // Lista para almacenar las suscripciones del usuario

using var channel = GrpcChannel.ForAddress("http://localhost:5238");
var client = new MessageBroker.MessageBrokerClient(channel);

var cts = new CancellationTokenSource();

Cliente cliente_1 = ObtenerDatosUsuario();

bool salir = false;
while (!salir)
{
    Console.Clear();
    Console.WriteLine("=======================================");
    Console.WriteLine("          MENÚ PRINCIPAL");
    Console.WriteLine("=======================================");
    Console.WriteLine("Seleccione una opción:");
    Console.WriteLine("1. Publicar mensaje");
    Console.WriteLine("2. Suscribirse a un tema");
    Console.WriteLine("3. Recibir mensajes de un tema");
    Console.WriteLine("4. Salir");
    Console.WriteLine("=======================================");
    Console.Write("Opción: ");

    if (int.TryParse(Console.ReadLine(), out int option))
    {
        switch (option)
        {
            case 1:
                await Menublisher(client, cliente_1.Id);
                break;
            case 2:
                await SuscripcionesMenu(client, cts.Token);
                break;
            case 3:
                var request = new ClientRequest { Topic = "CONECTADO" };
                await Listar_mensajes_Recibidos(client);
                break;
            case 4:
                salir = true;
                continue;
            default:
                Console.WriteLine("Opción no válida. Intente de nuevo.");
                break;
        }
    }
    else
    {
        Console.WriteLine("Opción no válida. Intente de nuevo.");
    }
    Console.WriteLine("Presione una tecla para continuar...");
    Console.ReadKey();
}

Cliente ObtenerDatosUsuario()
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=======================================");
        Console.WriteLine("       INGRESO DE DATOS DEL USUARIO");
        Console.WriteLine("=======================================");

        Console.Write("Ingrese el ID del cliente: ");
        var id = Console.ReadLine();

        Console.Write("Ingrese el nombre del cliente: ");
        var nombre = Console.ReadLine();

        Console.Write("Ingrese la edad del cliente: ");
        if (int.TryParse(Console.ReadLine(), out int edad) && !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nombre))
        {
            return new Cliente(id, nombre, edad);
        }
        else
        {
            Console.WriteLine("\nFormato inválido. Intente de nuevo.");
            Console.ReadKey();
        }
    }
}

async Task Menublisher(MessageBroker.MessageBrokerClient client, String id)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=======================================");
        Console.WriteLine("          MENÚ PUBLICAR MENSAJE");
        Console.WriteLine("=======================================");
        Console.WriteLine("1. Escribir mensaje");
        Console.WriteLine("2. Ver temas existentes");
        Console.WriteLine("3. Subscribirse como productor");
        Console.WriteLine("4. Volver al menú principal");
        Console.WriteLine("=======================================");
        Console.Write("Opción: ");

        if (int.TryParse(Console.ReadLine(), out int option))
        {
            switch (option)
            {
                case 1:
                    await PublishMessage(client, id);
                    break;
                case 2:
                    await ListarTemas(client);
                    break;
                case 3:
                    await Subcribe_Publisher_Topic(client);
                    break;
                case 4:
                    continue;
                default:
                    Console.WriteLine("Opción no válida. Intente de nuevo.");
                    break;
            }
        }
        else
        {
            Console.WriteLine("Opción no válida. Intente de nuevo.");
        }
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }
}

async Task SuscripcionesMenu(MessageBroker.MessageBrokerClient client, CancellationToken cancellationToken)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=======================================");
        Console.WriteLine("          MENÚ SUSCRIPCIONES");
        Console.WriteLine("=======================================");
        Console.WriteLine("1. Suscribirse a un tema");
        Console.WriteLine("2. Ver mis suscripciones");
        Console.WriteLine("3. Ver suscripciones disponibles");
        Console.WriteLine("4. Volver al menú principal");
        Console.WriteLine("=======================================");
        Console.Write("Opción: ");

        var option = Console.ReadLine();

        if (option == "1")
        {
            await SubscribeToTopic(client);
        }
        else if (option == "2")
        {
            ListarMisSuscripciones();
        }
        else if (option == "3")
        {
            await ListarTemas(client);
        }
        else if (option == "4")
        {
            break;
        }
        else
        {
            Console.WriteLine("ERROR: Por favor, digite otro número");
            Console.ReadKey();
        }
    }
}

//----------------------------------------------LISTAR-------------------------------------------------

async Task ListarTemas(MessageBroker.MessageBrokerClient client)
{
    Console.Clear();
    Console.WriteLine("=======================================");
    Console.WriteLine("          TEMAS EXISTENTES");
    Console.WriteLine("=======================================");

    var response = await client.GetTopicsAsync(new Empty());

    temas = new List<string>(response.Topics);

    foreach (var tema in temas)
    {
        Console.WriteLine(tema);
    }
    Console.WriteLine("=======================================");
}

async Task Listar_mensajes_Recibidos(MessageBroker.MessageBrokerClient client)
{
    Console.Clear();
    Console.WriteLine("=======================================");
    Console.WriteLine("       MENSAJES RECIBIDOS");
    Console.WriteLine("=======================================");

    var request = new ClientRequest
    {
        Id = cliente_1.Id,
        Nombre = cliente_1.Nombre,
        Edad = cliente_1.Edad,
    };
    var response = await client.listar_mensajesAsync(request);

    temas = new List<string>(response.Topics);

    foreach (var tema in temas)
    {
        Console.WriteLine(tema);
    }
    Console.WriteLine("=======================================");
}

void ListarMisSuscripciones()
{
    Console.Clear();
    Console.WriteLine("=======================================");
    Console.WriteLine("       MIS SUSCRIPCIONES");
    Console.WriteLine("=======================================");
    foreach (var suscripcion in suscripciones)
    {
        Console.WriteLine(suscripcion);
    }
    Console.WriteLine("=======================================");
    Console.WriteLine("Presione una tecla para continuar...");
    Console.ReadKey();
}

//---------------------------------------------SUBCRIBIRSE-----------------------------------------------------

async Task SubscribeToTopic(MessageBroker.MessageBrokerClient client)
{
    await ListarTemas(client);

    Console.WriteLine("Ingrese el tema:");
    var topic = Console.ReadLine();
    Console.Clear();

    var request = new ClientRequest
    {
        Id = cliente_1.Id,
        Nombre = cliente_1.Nombre,
        Edad = cliente_1.Edad,
        Suscritor = topic,
        SuscritorPublish = topic,
        Topic = topic
    };

    var response = await client.Subcribirse_ClienteAsync(request);

    Console.WriteLine(response.Content);
}

async Task Subcribe_Publisher_Topic(MessageBroker.MessageBrokerClient client)
{
    await ListarTemas(client);

    Console.WriteLine("Ingrese el tema:");
    var topic = Console.ReadLine();
    Console.Clear();

    if (!temas.Contains(topic))
    {
        Console.WriteLine("El tema no existe.");
        return;
    }

    var response = await client.Subscribe_publisherAsync(new ClientRequest
    {
        Id = cliente_1.Id,
        Nombre = cliente_1.Nombre,
        Edad = cliente_1.Edad,
        Suscritor = topic,
        SuscritorPublish = topic,
        Topic = topic
    });

    if (response.Message_ == "PUBLISHER REGISTRADO")
    {
        suscripciones.Add(topic);
        Console.WriteLine("Suscrito al tema: " + topic);
    }
    else if (response.Message_ == "YA SUSCRITO COMO PUBLISHER")
    {
        Console.WriteLine("Ya se encuentra Suscrito al tema: " + topic);
    }
}

//-------------------------------------------MENSAJE------------------------------------------

async Task PublishMessage(MessageBroker.MessageBrokerClient client, String id)
{
    await ListarTemas(client);
    Console.Clear();
    Console.WriteLine("=======================================");
    Console.WriteLine("       PUBLICAR MENSAJE");
    Console.WriteLine("=======================================");
    Console.Write("Ingrese el tema: ");
    var topic = Console.ReadLine();

    if (!temas.Contains(topic))
    {
        Console.Clear();
        Console.WriteLine("El tema no existe.");
        return;
    }

    Console.Write("Ingrese el mensaje: ");
    var message = Console.ReadLine();

    var reply = await client.PublishAsync(new PublishRequest { Topic = topic, Message = message, IdPublish = id });

    Console.WriteLine("Respuesta del servidor: " + reply.Content);
    Console.WriteLine("=======================================");
}

async Task RecibirMensajes(string client_ID, MessageBroker.MessageBrokerClient client)
{
    var request = new ClientRequest { Id = client_ID };

    try
    {
        using var call = client.Enviar(request);

        await foreach (var message in call.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine($"Mensaje recibido: {message.Content}");
        }
    }
    catch (RpcException ex)
    {
        Console.WriteLine($"Error al recibir mensajes del servidor: {ex.Status.Detail}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al recibir mensajes del servidor: {ex}");
    }
}


