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
        private readonly List<Topic> lista_topic = new();
        private readonly SemaphoreSlim _logSemaphore = new(1, 1);
        private readonly SemaphoreSlim _clientSemaphore = new(1, 1);
        private readonly SemaphoreSlim _topicSemaphore = new(1, 1);

        private readonly ConcurrentDictionary<string, Cliente> _clientes = new();
        private readonly ConcurrentDictionary<string, IServerStreamWriter<Message>> _clientStreams = new();
        private readonly ConcurrentDictionary<string, IServerStreamWriter<Message>> _publidtreams = new();

        public MessageBrokerService(ILogger<MessageBrokerService> logger)
        {
            _logger = logger;

            lista_topic.AddRange(new[]
            {
                new Topic(12, "futbol", "blanco"),
                new Topic(13, "comida", "blanco"),
                new Topic(14, "inversiones", "blanco"),
                new Topic(15, "animales", "blanco"),
                new Topic(16, "carros", "blanco")
            });
        }

        //-----------------------------------------------subcripcion------------------------------------------------------------------

        public override async Task Subcribirse_Cliente(ClientRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            await _clientSemaphore.WaitAsync();
            try
            {
                if (_clientes.TryGetValue(request.Id, out var cliente))
                {
                    cliente.IngresarTopicsSubscritos(request.Topic);
                    _clientStreams[request.Id] = responseStream;
                }
                else
                {
                    cliente = new Cliente(request.Id, request.Nombre, request.Edad);
                    cliente.IngresarTopicsSubscritos(request.Topic);
                    _clientes[request.Id] = cliente;
                    _clientStreams[request.Id] = responseStream;
                }
            }
            finally
            {
                _clientSemaphore.Release();
            }
            await responseStream.WriteAsync(new Message { Content = "Tema " + request.Topic + " inscrito correctamente." });
        }

        public override async Task Publish(PublishRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            bool encontrado = false;
            bool iguales = false;

            var tema = lista_topic.FirstOrDefault(t => t.Nombre == request.Topic);
            if (tema == null)
            {
                await responseStream.WriteAsync(new Message { Content = "Tema no encontrado." });
                return;
            }

            await _clientSemaphore.WaitAsync();
            try
            {
                if (_clientes.TryGetValue(request.IdPublish, out var cliente))
                {
                    encontrado = true;
                    if (cliente.TopicsPublish.Contains(request.Topic))
                    {
                        iguales = true;
                    }
                }
            }
            finally
            {
                _clientSemaphore.Release();
            }

            if (encontrado && iguales)
            {
                await _topicSemaphore.WaitAsync();
                try
                {
                    var messages = _topics.GetOrAdd(request.Topic, new List<string>());

                    foreach (var item in _clientes)
                    {
                        if (item.Value.TopicsSubscritos.Contains(request.Topic))
                        {
                            item.Value.IngresarTema(request.Topic + " : " + request.Message);
                        }
                    }
                }
                finally
                {
                    _topicSemaphore.Release();
                }

                await LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje publicado en el tema {request.Topic}");
                await responseStream.WriteAsync(new Message { Content = "Mensaje enviado" });
            }
            else
            {
                await responseStream.WriteAsync(new Message { Content = "Aún no se ha subscrito como productor." });
            }
        }

        public override async Task<Message> Subscribe_publisher(ClientRequest request, ServerCallContext context)
        {
            var tema = lista_topic.FirstOrDefault(t => t.Nombre == request.Topic);

            if (tema != null)
            {
                await _clientSemaphore.WaitAsync();
                try
                {
                    if (_clientes.TryGetValue(request.Id, out var cliente))
                    {
                        if (cliente.TopicsPublish.Contains(tema.Nombre))
                        {
                            return new Message
                            {
                                Topic = tema.Nombre,
                                Message_ = "YA SUSCRITO COMO PUBLISHER",
                                Content = tema.Descripcion
                            };
                        }
                        else
                        {
                            cliente.IngresarTopicsPublish(tema.Nombre);
                            return new Message
                            {
                                Topic = tema.Nombre,
                                Message_ = "PUBLISHER REGISTRADO",
                                Content = tema.Descripcion
                            };
                        }
                    }
                    else
                    {
                        cliente = new Cliente(request.Id, request.Nombre, request.Edad);
                        cliente.IngresarTopicsPublish(tema.Nombre);
                        _clientes[request.Id] = cliente;

                        return new Message
                        {
                            Topic = tema.Nombre,
                            Message_ = "PUBLISHER REGISTRADO",
                            Content = tema.Descripcion
                        };
                    }
                }
                finally
                {
                    _clientSemaphore.Release();
                }
            }
            else
            {
                return new Message
                {
                    Topic = string.Empty,
                    Message_ = "TEMA NO ENCONTRADO",
                    Content = string.Empty
                };
            }
        }

        public override async Task<TopicList> listar_mensajes(ClientRequest request, ServerCallContext context)
        {
            var reply = new TopicList();
            var p = new List<string>();

            await _clientSemaphore.WaitAsync();
            try
            {
                if (_clientes.TryGetValue(request.Id, out var cliente))
                {
                    var cola = cliente.ObtenerColaDeTemas();

                    if (cola != null)
                    {
                        p.AddRange(cola);
                    }
                    else
                    {
                        p.Add("No hay mensajes");
                    }

                    reply.Topics.AddRange(p);
                    await LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Lista de temas enviada al cliente");
                    return reply;
                }
                else
                {
                    p.Add("No hay mensajes");
                    reply.Topics.AddRange(p);
                    return reply;
                }
            }
            finally
            {
                _clientSemaphore.Release();
            }
        }

        public override async Task Enviar(ClientRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            await _clientSemaphore.WaitAsync();
            try
            {
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
                        await clientStreamWriter.WriteAsync(new Message { Content = "no hay mensajes" });
                    }
                }
                else
                {
                    Console.WriteLine($"El cliente con ID {request.Id} no está conectado.");
                    await responseStream.WriteAsync(new Message { Content = "no hay mensajes" });
                }
            }
            finally
            {
                _clientSemaphore.Release();
            }
        }

        public override async Task SubscribeToTopic(ClientRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            var message = new Message { Content = request.Topic };
            await responseStream.WriteAsync(message);
            await Task.Delay(1000);
        }

        public override async Task<TopicList> GetTopics(Empty request, ServerCallContext context)
        {
            await LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Lista de temas enviada al cliente");

            var reply = new TopicList();
            var p = new List<string>();

            await _topicSemaphore.WaitAsync();
            try
            {
                p.AddRange(lista_topic.Select(tema => tema.Nombre));
            }
            finally
            {
                _topicSemaphore.Release();
            }

            reply.Topics.AddRange(p);
            return reply;
        }

        private async Task LogEvent(string logMessage)
        {
            await _logSemaphore.WaitAsync();
            try
            {
                await File.AppendAllTextAsync("log.txt", logMessage + Environment.NewLine);
            }
            finally
            {
                _logSemaphore.Release();
            }
        }
    }
}
