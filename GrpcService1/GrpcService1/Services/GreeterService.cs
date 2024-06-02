using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GrpcService1.Services
{
    public class ClienteServiciosImpl : ClienteServicios.ClienteServiciosBase
    {
        private readonly ILogger<ClienteServiciosImpl> _logger;
        private readonly ConcurrentDictionary<int, ConcurrentQueue<string>> _topics = new ConcurrentDictionary<int, ConcurrentQueue<string>>();

        public ClienteServiciosImpl(ILogger<ClienteServiciosImpl> logger)
        {
            _logger = logger;
        }

        public override Task<estadoMSJ> enviarMensaje(Mensaje request, ServerCallContext context)
        {
            var queue = _topics.GetOrAdd(request.Tema, new ConcurrentQueue<string>());
            queue.Enqueue(request.Contenido);

            _logger.LogInformation($"{DateTime.Now:dd/MM/yyyy:HH:mm:ss} Mensaje recibido en el tema {request.Tema}");

            return Task.FromResult(new estadoMSJ { Recibido = true });
        }

        public override Task<Mensaje> recibirMSJServer(solicitudMSJ request, ServerCallContext context)
        {
            if (_topics.TryGetValue(request.Tema, out var queue))
            {
                if (queue.TryDequeue(out var message))
                {
                    return Task.FromResult(new Mensaje { Tema = request.Tema, Contenido = message });
                }
            }

            return Task.FromResult(new Mensaje { Tema = request.Tema, Contenido = string.Empty });
        }
    }
}
