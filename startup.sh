# StartUp file

PORT=5238
PID=$(lsof -t -i:$PORT)
if [ ! -z "$PID" ]; then
 echo "Liberando el puerto $PORT (PID: $PID)"
 kill -9 $PID
fi

# Compilar y ejecutar el servidor gRPC
cd ./GrpcServiceMessage/GrpcServiceMessage
dotnet build
echo "Iniciando el servidor gRPC..."
dotnet run &

# Compilar y ejecutar el cliente gRPC
cd ../../ClientGRPC/ClientGRPC
dotnet build
echo "Iniciando el cliente gRPC..."
dotnet run