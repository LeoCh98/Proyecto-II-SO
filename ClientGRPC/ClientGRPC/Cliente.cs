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

        
        public Cliente(string id, string nombre, int edad)
        {
            Id = id;
            Nombre = nombre;
            Edad = edad;
        }

        // Método para mostrar la información del cliente
        public void MostrarInformacion()
        {
            Console.WriteLine($"ID: {Id}");
            Console.WriteLine($"Nombre: {Nombre}");
            Console.WriteLine($"Edad: {Edad}");
        }
    }
}
