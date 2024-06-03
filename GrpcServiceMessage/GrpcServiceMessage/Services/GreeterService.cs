using Grpc.Core;
using GrpcServiceMessage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

using System.Collections.Generic;


namespace GrpcServiceMessage.Services
{
    public class MessageBrokerService : MessageBroker.MessageBrokerBase
    {
        private readonly ILogger<MessageBrokerService> _logger;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _topics = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<IServerStreamWriter<Message>>> _subscribers = new();
        private readonly object _logLock = new();

        public MessageBrokerService(ILogger<MessageBrokerService> logger)
        {
            _logger = logger;
        }


        public override Task<PublishReply> Publish(PublishRequest request, ServerCallContext context)
        {
            /*
            var queue = _topics.GetOrAdd(request.Topic, new ConcurrentQueue<string>());
            queue.Enqueue(request.Message);

            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje publicado en el tema {request.Topic}");

            NotifySubscribers(request.Topic, request.Message);
            */
             Console.WriteLine(request.Message);
            Console.WriteLine(request.Topic);
            Console.WriteLine(request.IdPublish);



             LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje publicado en el tema {request.Topic}");

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
                          
                            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje enviado al subscriptor del tema {topic}");
                        }
                        catch (Exception ex)
                        {
                            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Error enviando mensaje al subscriptor del tema {topic}: {ex.Message}");
                        }
                    });
                }
            }
        }


        public override async Task Subscribe(SubscribeRequest request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            var subscribers = _subscribers.GetOrAdd(request.Topic, new ConcurrentBag<IServerStreamWriter<Message>>());
            subscribers.Add(responseStream);

            LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Cliente suscrito al tema {request.Topic}");

            try
            {
                await Task.Delay(Timeout.Infinite, context.CancellationToken); // Keep the subscription alive
            }
            catch (TaskCanceledException)
            {
                LogEvent($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Cliente desconectado del tema {request.Topic}");
            }
            finally
            {
                // Remove the subscriber when the connection is closed
                subscribers.TryTake(out responseStream);
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
