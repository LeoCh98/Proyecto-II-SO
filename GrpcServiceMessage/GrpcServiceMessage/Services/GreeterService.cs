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

namespace GrpcServiceMessage.Services
{
    public class MessageBrokerService : MessageBroker.MessageBrokerBase
    {
        private readonly ILogger<MessageBrokerService> _logger;
        private readonly ConcurrentDictionary<string, List<string>> _topics = new();
        private readonly ConcurrentDictionary<string, Cliente> listaClientes = new();
        private readonly List<Topic> lista_topic = new List<Topic>();
        private readonly ConcurrentDictionary<string, ConcurrentBag<IServerStreamWriter<Message>>> _subscribers = new();
        private readonly object _logLock = new();

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

        public override Task<Message> Subscribe(ClientRequest request, ServerCallContext context)
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


        public override Task<Message> Subscribe_publisher(ClientRequest request, ServerCallContext context)
        {


            var tema = lista_topic.FirstOrDefault(t => t.Nombre == request.Topic);

            if (tema != null)
            {
                // Buscar el cliente en la lista de clientes suscritos
                if (listaClientes.TryGetValue(request.Id, out var cliente))
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
                    listaClientes.TryAdd(request.Id, cliente);

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


        public override Task<PublishReply> Publish(PublishRequest request, ServerCallContext context)
        {
            var messages = _topics.GetOrAdd(request.Topic, new List<string>());

            lock (messages)
            {
                messages.Add(request.Message);
            }

            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje publicado en el tema {request.Topic}");

            //topics.Add(request.Topic);

            NotifySubscribers(request.Topic, request.Message);

            return Task.FromResult(new PublishReply { Status = "publicacion guardada" });
        }

        private void NotifySubscribers(string topic, string message)
        {
            if (_subscribers.TryGetValue(topic, out var subscribers))
            {
                foreach (var subscriber in subscribers)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await subscriber.WriteAsync(new Message { Topic = topic, Message_ = message });
                            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje enviado al suscriptor del tema {topic}");
                        }
                        catch (Exception ex)
                        {
                            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Error enviando mensaje al suscriptor del tema {topic}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void LogEvent(string logMessage)
        {
            lock (_logLock)
            {
                File.AppendAllText("log.txt", logMessage + Environment.NewLine);
            }
        }






    }
}


