using Grpc.Core;
using GrpcServiceMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientGRPC
{
    public class Cliente
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public int Edad { get; set; }

        public List<string> TopicsSubscritos { get; set; } // Lista de temas a los que está suscrito el cliente
        public List<string> TopicsPublish { get; set; } // Lista de temas a los que está suscrito el cliente

        public AsyncServerStreamingCall<Message> Stream { get; set; }


        public List<string> ColaDeTemas { get; set; } 


        public Cliente()
        {
        }
            public Cliente(string id, string nombre, int edad)
        {
            Id = id;
            Nombre = nombre;
            Edad = edad;
            TopicsPublish = new List<string>();
            TopicsSubscritos = new List<string>();
            ColaDeTemas =new List<string>();


        }

        // Método para mostrar la información del cliente
        public void MostrarInformacion()
        {
            Console.WriteLine($"ID: {Id}");
            Console.WriteLine($"Nombre: {Nombre}");
            Console.WriteLine($"Edad: {Edad}");
        }




        // Método para agregar un nuevo tema a los temas suscritos
        public void IngresarTopicsSubscritos(string topic)
        {
            TopicsSubscritos.Add(topic);
        }

        public void IngresarTopicsPublish(string topic)
        {
            TopicsPublish.Add(topic);
        }

        public  void IngresarTema(string tema)
        {
            ColaDeTemas.Add(tema);
        }

        public List<String> ObtenerColaDeTemas()
        {
            return ColaDeTemas;
        }

    }
}
