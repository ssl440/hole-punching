using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HolePunching;

namespace Test
{
    class ClientMain
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: Program ip_address port local_port");
                return;
            }

            Console.WriteLine(args[0]);
            IPAddress serverIpAddress = IPAddress.Parse(args[0]);
            int serverPort = int.Parse(args[1]);
            int localPort = int.Parse(args[2]);

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
            HolePunchingClient hpc = new HolePunchingClient(localEndPoint);
            
            IPEndPoint remoteEndPoint = new IPEndPoint(serverIpAddress, serverPort);
            hpc.Connect(remoteEndPoint);

            Console.WriteLine("Insert ip_address of natted host: ");
            IPAddress remoteAddress = IPAddress.Parse(Console.ReadLine());

            Socket s = hpc.HolePunch(remoteAddress);

            Console.WriteLine("In main, my socket is: local ---> " + s.LocalEndPoint + ", remote ---> " + s.RemoteEndPoint);
            
            return;
        }
    }
}
