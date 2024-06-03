using ClientGRPC;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

List<string> temas = new List<string>(); // Lista para almacenar los temas
List<string> suscripciones = new List<string>(); // Lista para almacenar las suscripciones del usuario

using var channel = GrpcChannel.ForAddress("http://localhost:5238");
var client = new MessageBroker.MessageBrokerClient(channel);

var cts = new CancellationTokenSource();

Cliente c = null;

while (true)

{

    if (c == null)
    {
        /*
        Console.WriteLine("-------------------------Bienvenido---------------------------: ");
        Console.WriteLine();
        Console.WriteLine("Ingrese su identificacion: ");
        string Identificacion = Console.ReadLine();

        Console.WriteLine("Ingrese su nombre: ");
        string Nombre = Console.ReadLine();

        Console.WriteLine("Ingrese su edad: ");
        string edadInput = Console.ReadLine();
        int Edad;

        // Intentar convertir la edad a un número entero
        while (!int.TryParse(edadInput, out Edad))
        {
            Console.WriteLine("Por favor, ingrese una edad válida.");
            edadInput = Console.ReadLine();
        }
        */
        c = new Cliente("801100976", "miriam", 20);
        /*
        Console.Clear();
        c.MostrarInformacion(); // Muestra la información del cliente después de crearlo
       
        Console.WriteLine("Informacion ingresada con exito.");
        Console.WriteLine("Presione una tecla para continuar...");
        Console.ReadKey();
        Console.Clear();
        */
    }




    //Console.Clear();
    Console.WriteLine("Seleccione una opción:");
    Console.WriteLine("1. Publicar mensaje");
    Console.WriteLine("2. Suscribirse a un tema");
    Console.WriteLine("3. Salir");

    var option = Console.ReadLine();

    if (option == "1")
    {
        await PublicarMensajeMenu(client,c.Id);
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












async Task PublicarMensajeMenu(MessageBroker.MessageBrokerClient client,String id)
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
            await PublishMessage(client ,id);
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




async Task PublishMessage(MessageBroker.MessageBrokerClient client , String id)
{
    Console.Clear();
    Console.WriteLine("Ingrese el tema:");
    var topic = Console.ReadLine();
    Console.WriteLine("Ingrese el mensaje:");
    var message = Console.ReadLine();

    var reply = await client.PublishAsync(new PublishRequest { Topic = topic, Message = message , IdPublish = id});

  

    Console.WriteLine("Respuesta del servidor: " + reply.Status);
    Console.WriteLine("Presione una tecla para continuar...");
    Console.ReadKey();
}










void ListarTemas()
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

async Task SubscribeToTopic(MessageBroker.MessageBrokerClient client, CancellationToken cancellationToken)
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

void ListarSuscripcionesDisponibles()
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
