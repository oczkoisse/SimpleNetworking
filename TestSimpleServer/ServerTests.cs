using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

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
                    byte[] data = Encoding.ASCII.GetBytes(sent);
                    for (int i = 0; i < totalSends; i++)
                    {
                        conn.Write(data.Length);
                        conn.Write(data);
                    }
                    conn.Close();
                }
            };
            
            Connection client = new Connection();

            CountdownEvent countdown = new CountdownEvent(totalSends);

            client.Received += (object sender, ReceivedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    byte[] data = args.Data;
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
                            byte[] rdata = rargs.Data;
                            string recvd = Encoding.ASCII.GetString(rdata);
                            Assert.IsTrue(recvd == sent);
                            countdown.Signal();
                        }
                    };
                }
            };

            Connection client = new Connection();
            client.Open(server.Address);

            byte[] data = Encoding.ASCII.GetBytes(sent);
            for (int i = 0; i < totalReceives; i++)
            {
                client.Write(data.Length);
                client.Write(data);
            }
            
            countdown.Wait();
            client.Close();
        }
    }

    [TestClass()]
    public class EchoServerTest
    {
        Server server;
        const int totalClients = 1000;
        const int messageByteCount = 10000;
        const int messagesPerClient = 10;
        CountdownEvent allClientsDone;
        Random seeder = new Random();

        [TestInitialize()]
        public void Setup()
        {
            allClientsDone = new CountdownEvent(totalClients);

            server = new Server(9000);
            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    Connection conn = args.GetConnection();
                    conn.Received += (object rsender, ReceivedEventArgs rargs) =>
                    {
                        byte[] data = rargs.Data;
                        Connection rconn = rargs.GetConnection();
                        rconn.Write(data.Length);
                        rconn.Write(data);
                    };
                }
            };
            server.Start();
        }

        public static string RandomString(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void ClientThread()
        {
            Random random;
            lock(seeder)
            {
                random = new Random(seeder.Next());
            }
            
            Connection conn = new Connection();
            BlockingCollection<string> q = new BlockingCollection<string>();
            
            CountdownEvent allMessagesReceived = new CountdownEvent(messagesPerClient);

            conn.Received += (object sender, ReceivedEventArgs args) =>
            {
                if (args.OperationSucceeded)
                {
                    byte[] rdata = args.Data;
                    string r = Encoding.ASCII.GetString(rdata);
                    string s = q.Take();

                    if (r == s)
                        allMessagesReceived.Signal();
                    else
                        Assert.Fail();
                }
            };

            conn.Open(server.Address);

            for (int j=0; j<messagesPerClient; j++)
            {
                string s = RandomString(messageByteCount, random);
                byte[] data = Encoding.ASCII.GetBytes(s);
                q.Add(s);
                conn.Write(data.Length);
                conn.Write(data);
            }
            q.CompleteAdding();

            allMessagesReceived.Wait();
            conn.Close();
            allClientsDone.Signal();
        }


        [TestMethod()]
        public void PutLoad()
        {
            for(int i=0; i<totalClients; i++)
            {
                Thread t = new Thread(ClientThread);
                t.Start();
            }
            allClientsDone.Wait();
        }

        [TestCleanup()]
        public void Stop()
        {
            server.Stop();
        }
    }
}