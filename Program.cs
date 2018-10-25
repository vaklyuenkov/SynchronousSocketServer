using System;

class ServiceProgram
{
    public static int Main(String[] args)
    {  
        int port = 80;
        SynchronousSocketServer service = new SynchronousSocketServer(port);
        service.StartListening();  
        return 0;
    } 
}