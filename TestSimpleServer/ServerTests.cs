using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleServer;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleServer.Tests
{
    [TestClass()]
    public class ServerTests
    {
        Server server;
        TcpClient c;
        

        [TestInitialize]
        public void SetupServer()
        {
            server = new Server(8000);
            server.Start();
            c = new TcpClient();
        }

        [TestCleanup]
        public void StopServer()
        {
            server.Stop();
        }
        

        [TestMethod()]
        public void ConnectTest()
        {
            var waitTillConnected = new ManualResetEvent(false);
            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.Succeeded)
                {
                    waitTillConnected.Set();
                }
            };

            c.Connect(server.Address);

            waitTillConnected.WaitOne(100);
        }

        [TestMethod()]
        public void SendTest()
        {
            Token token = null;
            var waitTillConnected = new ManualResetEvent(false);
            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.Succeeded)
                {
                    token = args.GetToken();
                    waitTillConnected.Set();
                }
            };
            
            c.Connect(server.Address);

            waitTillConnected.WaitOne(100);

            string sent = "Hello";

            var waitTillSent = new ManualResetEvent(false);
            server.Sent += (object sender, SentEventArgs args) =>
            {
                if (args.Succeeded)
                    waitTillSent.Set();
            };

            byte[] sentData = Encoding.ASCII.GetBytes(sent);
            server.Send(token, new Packet(sentData));
            waitTillSent.WaitOne(100);

            var stream = c.GetStream();
            byte[] lengthData = new byte[4];
            stream.Read(lengthData, 0, lengthData.Length);
            byte[] actualData = new byte[BitConverter.ToInt32(lengthData, 0)];
            stream.Read(actualData, 0, actualData.Length);
            Assert.IsTrue(Encoding.ASCII.GetString(actualData) == sent);
        }

        [TestMethod()]
        public void ReceiveTest()
        {
            Token token = null;
            var waitTillConnected = new ManualResetEvent(false);
            server.Connected += (object sender, ConnectedEventArgs args) =>
            {
                if (args.Succeeded)
                {
                    token = args.GetToken();
                    waitTillConnected.Set();
                }
            };
            
            c.Connect(server.Address);

            waitTillConnected.WaitOne(100);

            
            string sent = "Hello";
            var waitTillReceived = new ManualResetEvent(false);
            server.Received += (object sender, ReceivedEventArgs args) =>
            {
                if (args.Succeeded)
                {
                    string received = Encoding.ASCII.GetString(args.UnderlyingPacket.Data);
                    Assert.IsTrue(received == sent);
                    waitTillReceived.Set();
                }
            };

            var stream = c.GetStream();
            byte[] sentData = Encoding.ASCII.GetBytes(sent);
            stream.Write(sentData, 0, sentData.Length);
            
            waitTillReceived.WaitOne(100);
        }
    }
}