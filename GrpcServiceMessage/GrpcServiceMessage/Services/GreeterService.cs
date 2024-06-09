using System.Collections.Concurrent;
using Grpc.Core;
using GrpcServiceMessage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using GrpcServiceMessage.Class;
using ClientGRPC;
using System.Net.NetworkInformation;
using Google.Protobuf;

namespace GrpcServiceMessage.Services
{
    public class MessageBrokerService : MessageBroker.MessageBrokerBase
    {
        private readonly ILogger<MessageBrokerService> _logger;
        private readonly ConcurrentDictionary<string, List<string>> _topics = new();
        private readonly ConcurrentDictionary<string, Cliente> listaClientes = new();
        private readonly List<Topic> lista_topic = new List<Topic>();
        private readonly object _logLock = new();
        

        private readonly ConcurrentDictionary<string, List<string>> _temas = new ConcurrentDictionary<string, List<string>>();
        public MessageBrokerService(ILogger<MessageBrokerService> logger)
        {
            _logger = logger;

            Topic topic1 = new Topic(12, "carro", "blanco");
            Topic topic2 = new Topic(13, "cerdo", "blanco");
            Topic topic3 = new Topic(14, "dinero", "blanco");
            Topic topic4 = new Topic(15, "gatos", "blanco");

            lista_topic.Add(topic1);
            lista_topic.Add(topic2);
            lista_topic.Add(topic3);
            lista_topic.Add(topic4);
        }













        private readonly ConcurrentDictionary<string, Cliente> _clientes = new ConcurrentDictionary<string, Cliente>();
        private readonly ConcurrentDictionary<string, IServerStreamWriter<Message>> _clientStreams = new ConcurrentDictionary<string, IServerStreamWriter<Message>>();
        private readonly ConcurrentDictionary<string, IServerStreamWriter<Message>> _publidtreams = new ConcurrentDictionary<string, IServerStreamWriter<Message>>();





        //-----------------------------------------------subcripcion------------------------------------------------------------------

        public override async Task Subcribirse_Cliente(ClientRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            
            
            
            var cliente = new Cliente(request.Id, request.Nombre, request.Edad);
            cliente.IngresarTopicsSubscritos(request.Topic);

            



             _clientes.TryAdd(request.Id, cliente);
             _clientStreams.TryAdd(request.Id, responseStream);

            await responseStream.WriteAsync(new Message { Content = "TEMA GUARDADO GUARDADO" });
           // await EnviarMensajeACliente(request.Id, new Message { Content = "Cliente suscrito correctamente" });

        }





      

        public override async Task Publish(PublishRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {

            bool iguales = false;

            bool Encontrado = false;
            var tema = lista_topic.FirstOrDefault(t => t.Nombre == request.Topic);



            if (_clientes.TryGetValue(request.IdPublish, out var cliente))
            {
                Encontrado = true;
                if (cliente.TopicsPublish.Contains(request.Topic))
                {
                    iguales = true;
                }
            }



            if (tema != null && Encontrado && iguales)
            {
                var messages = _topics.GetOrAdd(request.Topic, new List<string>());


                foreach (var item in _clientes)
                {
                    foreach(var message in item.Value.TopicsSubscritos)
                    {
                        if (message==tema.Nombre)
                        {
                            item.Value.IngresarTema(request.Message);
                        }
                    }
                }










                LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje publicado en el tema {request.Topic}");
               
               
               

             

                await responseStream.WriteAsync(new Message { Content = "Mensaje enviado" });

               
            }
            else
            {
                await responseStream.WriteAsync(new Message { Content = "ERROR NO SE ENCUENTRA SUBCRITO COMO PUBLISHER" });
            }

        }





        public override Task<Message> Subscribe_publisher(ClientRequest request, ServerCallContext context)
        {



            var tema = lista_topic.FirstOrDefault(t => t.Nombre == request.Topic);

            if (tema != null)
            {
                // Buscar el cliente en la lista de clientes suscritos
                if (_clientes.TryGetValue(request.Id, out var cliente))
                {
                    // Verificar si el cliente ya está suscrito al tema
                    if (cliente.TopicsPublish.Contains(tema.Nombre))
                    {
                        return Task.FromResult(new Message
                        {
                            Topic = tema.Nombre,
                            Message_ = "YA SUSCRITO COMO PUBLISHER",
                            Content = tema.Descripcion
                        });
                    }
                    else
                    {
                        // Agregar el tema a la lista de temas suscritos del cliente
                        cliente.IngresarTopicsPublish(tema.Nombre);

                        Console.WriteLine($"Cliente actualizado: {cliente.Nombre}, Topics: {string.Join(", ", cliente.TopicsSubscritos)}");

                        return Task.FromResult(new Message
                        {
                            Topic = tema.Nombre,
                            Message_ = "PUBLISHER REGISTRADO",
                            Content = tema.Descripcion
                        });
                    }
                }
                else
                {
                    // Crear un nuevo cliente y registrar el tema al que se suscribe
                    cliente = new Cliente(request.Id, request.Nombre, request.Edad);
                    cliente.IngresarTopicsPublish(tema.Nombre);


                    _clientes.TryAdd(request.Id, cliente);

                    Console.WriteLine($"Nuevo cliente agregado: {cliente.Nombre}, Topics: {string.Join(", ", cliente.TopicsPublish)}");

                    return Task.FromResult(new Message
                    {
                        Topic = tema.Nombre,
                        Message_ = "PUBLISHER REGISTRADO",
                        Content = tema.Descripcion
                    });
                }
            }
            else
            {
                // Retornar un mensaje vacío indicando que el tema no fue encontrado
                return Task.FromResult(new Message
                {
                    Topic = string.Empty,
                    Message_ = "TEMA NO ENCONTRADO",
                    Content = string.Empty
                });
            }



        }





        public override Task<TopicList> listar_mensajes(ClientRequest request, ServerCallContext context)
        {
            var reply = new TopicList();
            var p = new List<string>();

            if (_clientStreams.TryGetValue(request.Id, out var clientStreamWriter))
            {
                

                var clien = _clientes.FirstOrDefault(t => t.Value.Id == request.Id);
                var cola = clien.Value.ObtenerColaDeTemas();

                if (cola != null)
                {
                    foreach (var pi in cola)
                    {
                      p.Add(pi.ToString());
                    }
                }
                else
                {
                    p.Add("No hay mensajes");
                   
                }

                

                reply.Topics.AddRange(p);
                LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Lista de temas enviada al cliente");
                return Task.FromResult(reply);
            }
            else
            {

                p.Add("No hay mensajes");
                reply.Topics.AddRange(p);
                return Task.FromResult(reply);
            }

        }




        public override async Task Enviar(ClientRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            // Verifica si el cliente está en el diccionario
            if (_clientStreams.TryGetValue(request.Id, out var clientStreamWriter))
            {
                var clien = _clientes.FirstOrDefault(t => t.Value.Id == request.Id);

                var cola = clien.Value.ObtenerColaDeTemas();


                if (cola != null)
                {
                    foreach (var p in cola)
                    {
                        await clientStreamWriter.WriteAsync(new Message { Content = p });
                    }
                }
                else
                {
                    // Envía el mensaje al cliente
                    await clientStreamWriter.WriteAsync(new Message { Content = "no hay mensajes" });
                }


            }
            else
            {
                // El cliente no está conectado o no existe
                Console.WriteLine($"El cliente con ID {request.Id} no está conectado.");
                if (clientStreamWriter != null)
                {
                    await clientStreamWriter.WriteAsync(new Message { Content = "no hay mensajes" });
                }
                else
                {
                    // Manejar el caso en el que clientStreamWriter es null
                    Console.WriteLine("Error: clientStreamWriter es null.");
                }
            }
        }





        public override async Task SubscribeToTopic(ClientRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            // Aquí puedes implementar la lógica para enviar mensajes continuamente al cliente

            // Supongamos que generamos un mensaje de ejemplo
            var message = new Message { Content = request.Topic };

            // Enviamos el mensaje al cliente
            await responseStream.WriteAsync(message);

            // Simulamos un intervalo de tiempo entre mensajes
            await Task.Delay(1000); // Espera 1 segundo antes de enviar el próximo mensaje

        }







        //--------------------------------------------------------------------------------------------------------------





        public override Task<TopicList> GetTopics(Empty request, ServerCallContext context)
        {


            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Lista de temas enviada al cliente");

            var reply = new TopicList();
            var p = new List<string>();

            foreach (var tema in lista_topic)
            {
                p.Add(tema.Nombre);
            }

            reply.Topics.AddRange(p);

            return Task.FromResult(reply);
        }







        /*
        public override Task<Message> Subcribirse_Cliente(ClientRequest request, ServerCallContext context)
        {
          

            // Buscar el tema solicitado en la lista de temas
            var tema = lista_topic.FirstOrDefault(t => t.Nombre == request.Topic);

            if (tema != null)
            {
                // Buscar el cliente en la lista de clientes suscritos
                if (listaClientes.TryGetValue(request.Id, out var cliente))
                {
                    // Verificar si el cliente ya está suscrito al tema
                    if (cliente.TopicsSubscritos.Contains(tema.Nombre))
                    {
                        return Task.FromResult(new Message
                        {
                            Topic = tema.Nombre,
                            Message_ = "YA SUSCRITO",
                            Content = tema.Descripcion
                        });
                    }
                    else
                    {
                        // Agregar el tema a la lista de temas suscritos del cliente
                        cliente.IngresarTopicsSubscritos(tema.Nombre);

                        Console.WriteLine($"Cliente actualizado: {cliente.Nombre}, Topics: {string.Join(", ", cliente.TopicsSubscritos)}");

                        return Task.FromResult(new Message
                        {
                            Topic = tema.Nombre,
                            Message_ = "REGISTRADO",
                            Content = tema.Descripcion
                        });
                    }
                }
                else
                {
                    // Crear un nuevo cliente y registrar el tema al que se suscribe
                    cliente = new Cliente(request.Id, request.Nombre, request.Edad);
                    cliente.IngresarTopicsSubscritos(tema.Nombre);
                    listaClientes.TryAdd(request.Id, cliente);



                    


                    Console.WriteLine($"Nuevo cliente agregado: {cliente.Nombre}, Topics: {string.Join(", ", cliente.TopicsSubscritos)}");

                    return Task.FromResult(new Message
                    {
                        Topic = tema.Nombre,
                        Message_ = "REGISTRADO",
                        Content = tema.Descripcion
                    });
                }
            }
            else
            {
                // Retornar un mensaje vacío indicando que el tema no fue encontrado
                return Task.FromResult(new Message
                {
                    Topic = string.Empty,
                    Message_ = "TEMA NO ENCONTRADO",
                    Content = string.Empty
                });
            }
        }


        public override async Task Subscribe_prueba(ClientRequest request, IServerStreamWriter<ClientRequest> responseStream, ServerCallContext context)
        {
            _clientStreams.TryAdd(request.Id, responseStream);

        }



        */


        //-----------------------------------------------------------------------------








        private async Task NotifySubscribers(string topic, string message)
        {
            foreach (var cliente in listaClientes.Values)
            {
                if (cliente.TopicsSubscritos.Contains(topic))
                {
                    // Verificar si el cliente tiene un flujo de respuesta asociado
                    if (listaClientes.TryGetValue(cliente.Id, out var clientStream))
                    {
                        try
                        {
                            // Crear el mensaje con el contenido y enviarlo al cliente
                            var msg = new Message { Topic = topic, Content = message };
                          

                            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje enviado al cliente {cliente.Nombre} suscrito al tema {topic}");
                        }
                        catch (Exception ex)
                        {
                            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Error enviando mensaje al cliente {cliente.Nombre} suscrito al tema {topic}: {ex.Message}");
                        }
                    }
                    else
                    {
                        LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} El cliente {cliente.Nombre} suscrito al tema {topic} no tiene un flujo de respuesta asociado.");
                    }
                }
            }
        }







        //------------------------------------------------------------------------------













        private void LogEvent(string logMessage)
        {
            lock (_logLock)
            {
                File.AppendAllText("log.txt", logMessage + Environment.NewLine);
            }
        }



      

    }
}
