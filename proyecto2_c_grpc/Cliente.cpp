#include <iostream>
#include <sstream>
#include <grpcpp/grpcpp.h>
#include "Cliente_Proto.pb.h"
#include "Cliente_Proto.grpc.pb.h"

using grpc::Channel;
using grpc::ClientContext;
using grpc::Status;

//espacio de nombres y utilizacion de funciones del .proto
using Cliente_Productor::ClienteServicios;

class ClienteServiciosCl{
private:
//instancia para las llamadas al server
std::unique_ptr<ClienteServicios::Stub> canalPropio;
public:

ClienteServiciosCl(std::shared_ptr<Channel> canal)
:canalPropio(ClienteServicios::NewStub(canal)){ }

//funcion para publicar un tema
bool enviarMensaje(int32_t Ptema, std::string Pcontenido){
    Cliente_Productor::Mensaje msj;
    msj.set_tema(Ptema);
    msj.set_contenido(Pcontenido);

    Cliente_Productor::estadoMSJ msjEstado;
    msjEstado.set_recibido(false);
    ClientContext context;
    
    //enviando el menaje
    grpc::Status operacion = canalPropio->enviarMensaje(&context, msj, &msjEstado);  
    ////Recordar en el server poner como msjEstado.recibido() == true exitoso o false msj dañado
    if(operacion.ok()){
        return true;
    }
    std::cout << "El mensaje no se ha enviado correctamente por favor vuelva a intentarlo\n";
    return msjEstado.recibido();
}

//funcion para solicitar un mensaje de un tema en especifico
std::string RecibirMensaje(int32_t tema) {
    Cliente_Productor::solicitudMSJ solicitud;
    solicitud.set_tema(tema);

    Cliente_Productor::Mensaje mensaje;
    ClientContext context;

    Status status = canalPropio->recibirMSJServer(&context, solicitud, &mensaje);

    if (status.ok()) {
      return mensaje.contenido();
    } else {
      std::cerr << "gRPC call failed: " << status.error_message() << std::endl;
      return "";
    }
  }

};

//192.168.100.68

int main(int argc, char** argv){
if(argc == 1){
    std::cout << "Por favor especifique la dir ip del server\n";
    return -1;
}
   // std::string puerto = "6050";
    ClienteServiciosCl client(grpc::CreateChannel("localhost:6050", grpc::InsecureChannelCredentials()));

  bool result = client.enviarMensaje(1, "Hello, Server!");
  if (result) {
    std::cout << "Message sent successfully!" << std::endl;
  } else {
    std::cout << "Failed to send message." << std::endl;
  }

  std::string response = client.RecibirMensaje(1);
  std::cout << "Received message: " << response << std::endl;

  return 0;

}