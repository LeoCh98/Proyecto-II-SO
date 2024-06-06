using ClientGRPC;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

List<string> temas = new List<string>(); // Lista para almacenar los temas
List<string> suscripciones = new List<string>(); // Lista para almacenar las suscripciones del usuario
List<string> suscripciones_Plublisher = new List<string>(); // Lista para almacenar las suscripciones del usuario

using var channel = GrpcChannel.ForAddress("http://localhost:5238");
var client = new MessageBroker.MessageBrokerClient(channel);

var cts = new CancellationTokenSource();

Cliente cliente_1 = null;

while (true)
{
    if (cliente_1 == null)
    {
        cliente_1 = new Cliente("801100990", "miriam", 20);
    }

    Console.WriteLine("Seleccione una opción:");
    Console.WriteLine("1. Publicar mensaje");
    Console.WriteLine("2. Suscribirse a un tema");
    Console.WriteLine("3. Recibir mensajes de un tema");
    Console.WriteLine("4. Salir");

    var option = Console.ReadLine();

    if (option == "1")
    {
        await Menublisher(client, cliente_1.Id);
    }
    else if (option == "2")
    {
        await SuscripcionesMenu(client, cts.Token);
    }
    else if (option == "3")
    {
        var request = new ClientRequest { Topic = "CONECTADO" };
        await ReceiveMessages(client, request);
    }
    else if (option == "4")
    {
        break;
    }
}

async Task Menublisher(MessageBroker.MessageBrokerClient client, String id)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("Menú Publicar Mensaje:");
        Console.WriteLine("1. Escribir mensaje");
        Console.WriteLine("2. Ver temas existentes");
        Console.WriteLine("3. Subcribirse como productor");
        Console.WriteLine("4. Volver al menú principal");

        var option = Console.ReadLine();

        if (option == "1")
        {
            await PublishMessage(client, id);
        }
        else if (option == "2")
        {
            await ListarTemas(client);
        }
        else if (option == "3")
        {
            await Subcribe_Publisher_Topic(client);
        }
        else if (option == "4")
        {
            break;
        }
    }
}

async Task SuscripcionesMenu(MessageBroker.MessageBrokerClient client, CancellationToken cancellationToken)
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
            Console.WriteLine("ERROR DIGITE OTRO NUMERO");
        }
    }
}

//----------------------------------------------LISTAR-------------------------------------------------

async Task ListarTemas(MessageBroker.MessageBrokerClient client)
{
    Console.Clear();
    Console.WriteLine("Temas existentes:");

    var response = await client.GetTopicsAsync(new Empty());

    temas = new List<string>(response.Topics);

    foreach (var tema in temas)
    {
        Console.WriteLine(tema);
    }
    Console.WriteLine("Presione una tecla para continuar...");
    Console.ReadKey();
}
void ListarMisSuscripciones()
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
//---------------------------------------------SUBCRIBIRSE-----------------------------------------------------


async Task SubscribeToTopic(MessageBroker.MessageBrokerClient client)
{
    await ListarTemas(client);

    Console.WriteLine("Ingrese el tema:");
    var topic = Console.ReadLine();
    Console.Clear();

    if (!temas.Contains(topic))
    {
        Console.WriteLine("El tema no existe.");
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
        return;
    }

    Console.Clear();

    var response = await client.Subcribirse_ClienteAsync(new ClientRequest
    {
        Id = cliente_1.Id,
        Nombre = cliente_1.Nombre,
        Edad = cliente_1.Edad,
        Suscritor = topic,
        SuscritorPublish = "NONE",
        Topic = topic
    });

    if (response.Message_ == "REGISTRADO")
    {
        suscripciones.Add(topic);
        Console.WriteLine("Suscrito al tema: " + topic);
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }
    else if (response.Message_ == "YA SUSCRITO")
    {
        Console.WriteLine("Ya se encuentra Suscrito al tema: " + topic);
        Console.ReadKey();
    }

    if (!suscripciones.Contains(topic))
    {
        suscripciones.Add(topic);
        Console.WriteLine("Suscrito al tema: " + topic);
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }
    else
    {
        Console.WriteLine("Ya se encuentra Suscrito al tema: " + topic);
        Console.ReadKey();
    }
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
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
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
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
    }
    else if (response.Message_ == "YA SUSCRITO COMO PUBLISHER")
    {
        Console.WriteLine("Ya se encuentra Suscrito al tema: " + topic);
        Console.ReadKey();
    }
}

//-------------------------------------------MENSAJE------------------------------------------


async Task PublishMessage(MessageBroker.MessageBrokerClient client, String id)
{
    Console.Clear();
    Console.WriteLine("Ingrese el tema:");
    var topic = Console.ReadLine();
    Console.WriteLine("Ingrese el mensaje:");
    var message = Console.ReadLine();

    var reply = await client.PublishAsync(new PublishRequest { Topic = topic, Message = message, IdPublish = id });




    var prueba = new ClientRequest { Topic = topic };

    using var call = client.SubscribeToTopic(prueba);




    Console.WriteLine("Respuesta del servidor: " + reply.Status);
    Console.WriteLine("Presione una tecla para continuar...");
    Console.ReadKey();
}



async Task ReceiveMessages(MessageBroker.MessageBrokerClient client,ClientRequest request)
{
    Console.Clear();
    Console.WriteLine("Seleccione el tema del cual desea recibir mensajes:");

    ListarMisSuscripciones();



    // Crea una solicitud para suscribirse a un tema
  

    // Establece un token de cancelación para detener la suscripción (opcional)
    var cts = new CancellationTokenSource();


    while (true)
    {
        try
        {
            await foreach (var message in client.SubscribeToTopic(request).ResponseStream.ReadAllAsync())
            {
                Console.WriteLine("Mensaje recibido: " + message.Content);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            // El flujo se ha cerrado, probablemente debido a un error del servidor o cierre de conexión.
            // Puedes volver a suscribirte al flujo para seguir escuchando.
            Console.WriteLine("El flujo se ha cerrado. Volviendo a suscribirse al flujo...");

            // Aquí puedes implementar la lógica para volver a suscribirte al flujo, por ejemplo, llamando a SubscribeToTopic nuevamente.
        }
    }


    // Espera hasta que el usuario presione una tecla para salir
    Console.WriteLine("Presione una tecla para salir...");
    Console.ReadKey();
}






//---------------------------------------------------------------------------------------

