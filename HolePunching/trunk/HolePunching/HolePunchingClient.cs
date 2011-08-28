using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HolePunching
{
    public class HolePunchingClient
    {
        private Socket _clientSocket;

        private Socket _serverSocket;
        private Socket _connectSocket;
        //private Socket _serverSocket;

        private Socket _holePunchedSocket;

        public HolePunchingClient (IPEndPoint localEndPoint)
        {
            /*For the moment, only TCP Hole Punching is supported*/
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _clientSocket.Bind(localEndPoint);

            //_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //_serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //_serverSocket.Bind(localEndPoint);
        }

        private void ProcessClientRequest (object sender, DoWorkEventArgs eventArgs)
        {
            try
            {
                IPEndPoint iep = (IPEndPoint)eventArgs.Argument;
                _connectSocket.Connect(iep);
                Console.WriteLine("Connessione effettuata correttamente verso: " + iep);
                _serverSocket.Close();
                _holePunchedSocket = _connectSocket;
            }
            catch (Exception e)
            {
                Console.WriteLine("In ProcessClientRequest: " + e.Message);
                return;
            }
        }

        private void ProcessServerRequest (object sender, DoWorkEventArgs eventArgs)
        {
            try
            {
                Socket s = _serverSocket.Accept();
                Console.WriteLine("Ricevuta connessione da: " + s.RemoteEndPoint);
                _holePunchedSocket = s;
            }
            catch (Exception e)
            {
                Console.WriteLine("In ProcessServerRequest: " + e.Message);
                return;
            }
        }

        private void ProcessRequest (object sender, DoWorkEventArgs eventArgs)
        {
            while (true)
            {
                byte[] bytes = new byte[2048];
                _clientSocket.Receive(bytes);

                MessageType messageType = (MessageType) bytes[0];
                Console.WriteLine("Ricevuto MessageType: " + messageType);

                switch (messageType)
                {
                    case MessageType.ConnectClient:
                        byte[] byteAddress = new byte[4];
                        byte[] bytePort = new byte[2];
                        Buffer.BlockCopy(bytes, 1, byteAddress, 0, 4);
                        Buffer.BlockCopy(bytes, 5, bytePort, 0, 2);
                        IPEndPoint remoteEndPoint = new IPEndPoint(new IPAddress(byteAddress), BitConverter.ToUInt16(bytePort, 0));
                        Console.WriteLine("Indirizzo verso il quale effettuare HP: " + remoteEndPoint);

                        _connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _connectSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        _connectSocket.Bind(_clientSocket.LocalEndPoint);

                        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        _serverSocket.Bind(_clientSocket.LocalEndPoint);

                        BackgroundWorker bwClient = new BackgroundWorker();
                        bwClient.DoWork += ProcessClientRequest;

                        BackgroundWorker bwServer = new BackgroundWorker();
                        bwServer.DoWork += ProcessServerRequest;

                        bwClient.RunWorkerAsync(remoteEndPoint);
                        bwServer.RunWorkerAsync();

                        break;
                }
            }
        }

        public void Connect (IPEndPoint serverEndPoint)
        {
            _clientSocket.Connect(serverEndPoint);
            _clientSocket.Send(BitConverter.GetBytes((byte)MessageType.Register));

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += ProcessRequest;
            bw.RunWorkerAsync();
        }
    }
}
