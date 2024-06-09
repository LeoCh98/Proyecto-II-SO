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
                    return;
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
        Console.WriteLine("4. Ver mis suscripciones Publisher");
        Console.WriteLine("5. Volver al menú principal");
        Console.WriteLine("=======================================");
        Console.Write("Opción: ");

        if (int.TryParse(Console.ReadLine(), out int option))
        {
            switch (option)
            {
                case 1:
                    await SubscribeToTopic(client);
                    break;
                case 2:
                    ListarMisSuscripciones();
                    break;
                case 3:
                    await ListarTemasDisp(client);
                    break;
                case 4:
                    Listar_Publisher();
                    break;
                case 5:
                    return;
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

async Task ListarTemasDisp(MessageBroker.MessageBrokerClient client)
{
    Console.Clear();
    Console.WriteLine("=======================================");
    Console.WriteLine("          TEMAS DISPONIBLES");
    Console.WriteLine("=======================================");

    var response = await client.GetTopicsAsync(new Empty());

    temas = new List<string>(response.Topics);
    var temasNoSuscritos = temas.Except(suscripciones);

    foreach (var tema in temasNoSuscritos)
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
}

void Listar_Publisher()
{
    Console.Clear();
    Console.WriteLine("=======================================");
    Console.WriteLine("       MIS SUSCRIPCIONES DE PUBLISHER ");
    Console.WriteLine("=======================================");
    foreach (var suscripcion in suscripciones_Publisher)
    {
        Console.WriteLine(suscripcion);
    }
    Console.WriteLine("=======================================");
}

//---------------------------------------------SUBCRIBIRSE-----------------------------------------------------

async Task SubscribeToTopic(MessageBroker.MessageBrokerClient client)
{
    await ListarTemas(client);

    Console.WriteLine("Ingrese el tema:");
    string topic = Console.ReadLine();
    if (string.IsNullOrEmpty(topic))
    {
        Console.WriteLine("Por favor, debe ingresar un tema.");
        return;
    }
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

    if (!string.IsNullOrEmpty(response.Content) && temas.Contains(topic))
    {
        if (!suscripciones.Contains(topic))
        {
            suscripciones.Add(topic);
            Console.WriteLine(response.Content);
        }
        else 
        {
            Console.WriteLine("Ya se encuentra suscrito al tema " + topic);
        }
    }
    else
    {
        Console.WriteLine("El tema "+ topic +" no existe.");
    }
}

async Task Subcribe_Publisher_Topic(MessageBroker.MessageBrokerClient client)
{
    await ListarTemas(client);

    Console.WriteLine("Ingrese el tema:");
    var topic = Console.ReadLine();

    if (!temas.Contains(topic))
    {
        Console.WriteLine("El tema "+ topic +" no existe.");
        return;
    }
    Console.Clear();

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
        suscripciones_Publisher.Add(topic);
        Console.WriteLine("Suscrito al tema: " + topic + " como publisher.");
    }
    else if (response.Message_ == "YA SUSCRITO COMO PUBLISHER")
    {
        Console.WriteLine("Ya se encuentra Suscrito al tema: " + topic);
    }
}

//-------------------------------------------MENSAJE------------------------------------------

async Task PublishMessage(MessageBroker.MessageBrokerClient client, String id)
{
    Console.Clear();

    Console.WriteLine("=======================================");
    Console.WriteLine("       PUBLICAR MENSAJE");
    Console.WriteLine("=======================================");
    if (suscripciones_Publisher.Count > 0)
    {

        Console.WriteLine("\nTEMAS SUBCRITOS                               \n");

        foreach (var tema in suscripciones_Publisher)
        {

            Console.WriteLine(tema);
        }
    }
    else
    {
        Console.WriteLine("NO SE ENCUENTRA TEMAS REGISTRADO COMO PRODUCTOR");
        return;
    }

    Console.WriteLine("=======================================");



    Console.Write("Ingrese el tema: "); var topic = Console.ReadLine();


    if (!temas.Contains(topic) && !string.IsNullOrEmpty(topic))
    {
        Console.Clear();
        Console.WriteLine("El tema "+ topic +" no existe.");
        return;
    }

    Console.Write("Ingrese el mensaje: ");
    var message = Console.ReadLine();
    if (!string.IsNullOrEmpty(message))
    {
        var reply = await client.PublishAsync(new PublishRequest { Topic = topic, Message = message, IdPublish = id });
        Console.WriteLine("Respuesta del servidor: " + reply.Content);
    }
    else
    {
        Console.WriteLine("Mensaje sin contenido, intente nuevamente.");
    }
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