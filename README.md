# Proyecto-II-SO

El proyecto consiste en desarrollar  una versión simplificada de un “message broker” utilizando  el 
modelo  cliente/servidor  –  es  decir,  la  aplicación  deberá  permitir  la  comunicación  en  la  red.  La 
aplicación utilizará el estilo de mensajes “publisher/subscriber”, en el cuál un proceso productor 
publica un mensaje en un determinado tema, mientras que uno o varios subscriptores a ese tema lo 
consumen.

## Requerimientos
    - .NET Core versión 8.0.301 (Lastest)
    - WSL version: 2.1.5.0 o superior.

## Compilación del Proyecto desde VsCode
    - En la barra de menú, selecciona `Terminal` y luego `Nueva Terminal` para abrir una nueva terminal.
    - La terminal debe estar ubicada en la raíz del proyecto.
    - Ejecutar `wsl`
    - Ejecuta el script `./startup.sh`