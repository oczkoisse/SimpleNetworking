using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace SimpleServer.Tests
{
    [TestClass()]
    public class ServerTests
    {
        Server server;

        [TestInitialize]
        public void SetupServer()
        {
            server = new Server(8000);
            server.Start();
        }

        [TestCleanup]
        public void StopServer()
        {
            server.Stop();
        }
        

        [TestMethod()]
        public void ConnectTest()
        {
            ManualResetEvent connectCompleted = new ManualResetEvent(false);
            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    connectCompleted.Set();
                }
                else
                    throw args.GetException();
            };

            Connection client = new Connection();
            client.Open(server.Address);
            connectCompleted.WaitOne();
            client.Close();
        }

        [TestMethod()]
        public void ServerSendTest()
        {
            const int totalSends = 10;

            string sent = "Hello";
            
            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    Connection conn = args.GetConnection();
                    Packet packet = new Packet(Encoding.ASCII.GetBytes(sent));
                    for (int i = 0; i < totalSends; i++)
                        conn.Write(packet);
                    conn.Close();
                }
            };
            
            Connection client = new Connection();

            CountdownEvent countdown = new CountdownEvent(totalSends);

            client.Received += (object sender, ReceivedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    Packet packet = args.GetPacket();
                    byte[] data = packet.DataAsCopy;
                    string recvd = Encoding.ASCII.GetString(data);
                    Assert.IsTrue(recvd == sent);
                    countdown.Signal();
                }
            };

            client.Open(server.Address);
            countdown.Wait();
            client.Close();
        }
        
        [TestMethod()]
        public void ServerReceiveTest()
        {
            const int totalReceives = 10;

            CountdownEvent countdown = new CountdownEvent(totalReceives);

            string sent = "Hello";

            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    Connection conn = args.GetConnection();
                    conn.Received += (object rsender, ReceivedEventArgs rargs) =>
                    {
                        if (rargs.OperationSucceeded)
                        {
                            Packet rpacket = rargs.GetPacket();
                            byte[] rdata = rpacket.DataAsCopy;
                            string recvd = Encoding.ASCII.GetString(rdata);
                            Assert.IsTrue(recvd == sent);
                            countdown.Signal();
                        }
                    };
                }
            };

            Connection client = new Connection();
            client.Open(server.Address);

            Packet packet = new Packet(Encoding.ASCII.GetBytes(sent));
            for (int i = 0; i < totalReceives; i++)
                client.Write(packet);
            
            countdown.Wait();
            client.Close();
        }
    }

    [TestClass()]
    public class EchoServerTest
    {
        Server server;

        [TestInitialize()]
        public void Setup()
        {
            server = new Server(9000);
            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    Connection conn = args.GetConnection();
                    conn.Received += (object rsender, ReceivedEventArgs rargs) =>
                    {
                        Packet packet = rargs.GetPacket();
                        Connection rconn = rargs.GetConnection();
                        rconn.Write(packet);
                    };
                }
            };
            server.Start();
        }

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void ClientThread()
        {
            Connection conn = new Connection();
            List<string> sent = new List<string>();
            
            const int totalSends = 10;
            CountdownEvent countdown = new CountdownEvent(totalSends);

            conn.Received += (object sender, ReceivedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    Packet packet = args.GetPacket();
                    byte[] rdata = packet.DataAsCopy;
                    string r = Encoding.ASCII.GetString(rdata);
                    Assert.IsTrue(r == sent[countdown.CurrentCount]);
                    countdown.Signal();
                }
            };

            conn.Open(server.Address);

            for (int j=0; j<10;j++)
            {
                string s = RandomString(10000);
                Packet p = new Packet(Encoding.ASCII.GetBytes(s));
                sent.Add(s);
                conn.Write(p);
            }

            countdown.Wait();
            conn.Close();
        }


        [TestMethod()]
        public void PutLoad()
        {
            for(int i=0; i<1000; i++)
            {
                Thread t = new Thread(new ThreadStart(ClientThread));
                t.Start();
            }
        }

        [TestCleanup()]
        public void Stop()
        {
            server.Stop();
        }
    }
}