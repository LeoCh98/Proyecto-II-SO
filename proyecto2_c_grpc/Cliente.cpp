#include <iostream>
#include <sstream>
#include <grpcpp/grpcpp.h>
#include "Cliente_Proto.pb.h"
#include "Cliente_Proto.grpc.pb.h"

using grpc::Channel;
using grpc::ClientContext;
using grpc::Status;
using grpc::ServerContext;

//espacio de nombres y utilizacion de funciones del .proto
using Cliente_Productor::ClienteServicios;

class ClienteServiciosImpl{
private:
//instancia para las llamadas al server
std::unique_ptr<ClienteServicios::Stub> canalPropio;
public:

ClienteServiciosImpl(std::shared_ptr<Channel> canal)
:canalPropio(ClienteServicios::NewStub(canal)){ }

//funcion para publicar un tema
//No es necesario especificar idMensaje porque ese se lo asigna el server
bool enviarMensaje(int32_t Ptema, std::string Pcontenido){
    Cliente_Productor::Mensaje msj;
    msj.set_idmensaje(-1);
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
std::string RecibirMensaje(int32_t tema, int32_t PidMensaje) {
    Cliente_Productor::solicitudMSJ solicitud;
    solicitud.set_tema(tema);
    solicitud.set_idmensaje(PidMensaje);

    Cliente_Productor::Mensaje mensaje;
    ClientContext context;

    Status status = canalPropio->recibirMSJServer(&context, solicitud, &mensaje);

    if (status.ok()) {
      return mensaje.contenido();
    } else {
      std::cerr << "Hola gRPC call failed: " << status.error_message() << std::endl;
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
    ClienteServiciosImpl client(grpc::CreateChannel("192.168.100.68:6050", grpc::InsecureChannelCredentials()));

  //probando conexion
  bool result = client.enviarMensaje(0, "Hola servidor!");
  if (result) {
    std::cout << "Se pudo completar la conexion cliente-servidor" << std::endl;
    std::string response = client.RecibirMensaje(0, 0);
    std::cout << "Mensaje recibido del server: " << response << std::endl;
  }else{
    std::cout << "No pudo completar la conexion cliente-servidor asegurese que la ip sea la correcta\n";
    std::cout << "O asegurese que el server este encendido\n";
    return -1;
  }




  return 0;

}