using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HolePunching;

namespace TestServer
{
    class ServerMain
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Program port backlog");
                return;
            }

            int port = int.Parse(args[0]);
            int backlog = int.Parse(args[1]);


            HolePunchingServer hps = new HolePunchingServer(port, backlog);
            hps.BeginServer();

            Console.ReadLine();
        }
    }
}
