using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace HolePunching
{
    class HolePunchingServer
    {
        private Socket _socket;
        private List<KeyValuePair<IPEndPoint, Socket> > _registeredList;

        public HolePunchingServer (int port, int backlog)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            _socket.Listen(backlog);
            
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BeginServer;
            backgroundWorker.RunWorkerAsync();
        }

        private void ProcessRequest (object sender, DoWorkEventArgs eventArgs)
        {
            Socket tempSocket = (Socket)eventArgs.Argument;
            byte[] bytes = new byte[2048];

            while (true)
            {
                tempSocket.Receive(bytes);
                IPEndPoint remoteEndPoint = (IPEndPoint) tempSocket.RemoteEndPoint;

                MessageType messageType = (MessageType)(bytes[0]);
                switch (messageType)
                {
                    case MessageType.Register:
                        Console.WriteLine("Registrazione per: " + remoteEndPoint);
                        if (!_registeredList.Exists(kvp => kvp.Key == remoteEndPoint))
                            _registeredList.Add(new KeyValuePair<IPEndPoint, Socket>(remoteEndPoint, tempSocket));
                        break;
                    case MessageType.RequestClient:
                        byte[] byteAddress = new byte[4];

                        Buffer.BlockCopy(bytes, 1, byteAddress, 0, 4);

                        /*IpEndPoint requested to hole punching*/
                        IPAddress requestedAddress = new IPAddress(byteAddress);
                        KeyValuePair<IPEndPoint, Socket> keyValuePair = _registeredList.Where(kvp => kvp.Key.Address == requestedAddress).ElementAt(0);
                        IPEndPoint requestedEndPoint = keyValuePair.Key;
                        Socket requestedSocket = keyValuePair.Value;

                        Console.WriteLine(remoteEndPoint + " ha richiesto i parametri di: " + requestedEndPoint);

                        /*Sending informations about endpoints*/
                        byte[] connectRequestedClient = new byte[7];
                        Buffer.BlockCopy(BitConverter.GetBytes((byte)MessageType.ConnectClient), 0, connectRequestedClient, 0, 1);
                        Buffer.BlockCopy(requestedEndPoint.Address.GetAddressBytes(), 0, connectRequestedClient, 1, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(requestedEndPoint.Port), 0, connectRequestedClient, 5, 2);

                        byte[] connectSenderClient = new byte[7];
                        Buffer.BlockCopy(BitConverter.GetBytes((byte)MessageType.ConnectClient), 0, connectSenderClient, 0, 1);
                        Buffer.BlockCopy(remoteEndPoint.Address.GetAddressBytes(), 0, connectSenderClient, 1, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(remoteEndPoint.Port), 0, connectSenderClient, 5, 2);

                        tempSocket.Send(connectRequestedClient);
                        requestedSocket.Send(connectSenderClient);

                        break;
                    case MessageType.Unregister:
                        Console.WriteLine("Deregistrazione per: " + remoteEndPoint);
                        if (_registeredList.Exists(kvp => kvp.Key == remoteEndPoint))
                            _registeredList.RemoveAll(kvp => kvp.Key == remoteEndPoint);
                        break;
                }
            }
        }

        public void BeginServer (object sender, DoWorkEventArgs eventArgs)
        {
            while (true)
            {
                Socket tempSocket = _socket.Accept();

                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += ProcessRequest;
                backgroundWorker.RunWorkerAsync(tempSocket);
            }
        }

    }
}
