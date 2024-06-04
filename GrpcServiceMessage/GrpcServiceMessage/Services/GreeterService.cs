using Grpc.Core;
using GrpcServiceMessage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using GrpcServiceMessage.Class;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.Formatters;
using ClientGRPC;
using Google.Protobuf;

namespace GrpcServiceMessage.Services
{
    public class MessageBrokerService : MessageBroker.MessageBrokerBase
    {
        private readonly ILogger<MessageBrokerService> _logger;
        private readonly ConcurrentDictionary<string, List<string>> _topics = new();
       
        
        List<string> topics = new List<string>();
        List<Cliente> clients = new List<Cliente>();
        List<Topic> lista_topic = new List<Topic>();


        

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
            var p = new List<String>();

            foreach (var tema in lista_topic)
            {
                p.Add(tema.Nombre);
            }

            reply.Topics.AddRange(p);


            return Task.FromResult(reply);


        }









        public override Task<Message> Subscribe(SubscribeRequest request, ServerCallContext context)
        {
           
            bool encontrado = false;
           
            Topic t1 = null;

            
            foreach (var tema in lista_topic)
            {
                if (request.Topic == tema.Nombre) 
                {
                    encontrado = true; 
                    t1 = tema; 
                    break; 
                }
            }

            
            if (encontrado)
            {
               
                return Task.FromResult(new Message { Topic = t1.Nombre, Message_ = "REGISTRADO", Content = t1.Descripcion });
            }
            else
            {
                // Se devuelve un mensaje vac�o indicando que el tema no fue encontrado
                return Task.FromResult(new Message { Topic = "", Message_ = "", Content = "" });
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

            topics.Add(request.Topic);

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

