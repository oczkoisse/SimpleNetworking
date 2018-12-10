using System;
using System.Net;
using System.Net.Sockets;

namespace SimpleServer
{
    public class Connection
    {
        // For intercepting write operations
        public event EventHandler<SentEventArgs> Sent;
        // For intercepting read operations
        public event EventHandler<ReceivedEventArgs> Received;

        private readonly Server parentServer;
        private readonly TcpClient client;

        internal Connection(TcpClient tcpClient, Server server)
        {
            client = tcpClient ?? throw new ArgumentNullException("TcpClient argument is null");
            parentServer = server ?? throw new ArgumentNullException("Server argument is null");
            BeginRead();
        }

        public Connection()
        {
            client = new TcpClient();
            parentServer = null;
        }

        public void Open(IPEndPoint endpoint)
        {
            if (IsServerConnection())
                throw new InvalidOperationException("Open() cannot be called on a Connection returned by Server");
            client.Connect(endpoint);
            BeginRead();
        }

        public void Close()
        {
            parentServer?.Remove(this);
            client.Close();
        }

        public bool IsServerConnection() => parentServer != null;

        private bool TryGetStream(out NetworkStream networkStream)
        {
            try
            {
                networkStream = client.GetStream();
            }
            catch (Exception)
            {
                networkStream = null;
            }
            return networkStream != null;
        }

        private void BeginRead()
        {
            if (TryGetStream(out NetworkStream networkStream))
            {
                AsyncReadOperation readOp = new AsyncReadOperation(4, networkStream, OnReadLength);
                readOp.Start();
            }
        }
        

        private void OnReadLength(AsyncReadOperation readLengthOp)
        {
            Packet packet = new Packet(readLengthOp.Bytes);

            if (readLengthOp.Successful)
            {
                int length = BitConverter.ToInt32(readLengthOp.Bytes, 0);
                AsyncReadOperation readDataOp = new AsyncReadOperation(length, readLengthOp.UnderlyingStream, OnReadData);
                readDataOp.Start();
            }
            else
            {
                Received?.Invoke(this, new ReceivedEventArgs(readLengthOp.ThrownException, this, packet));
            }
        }

        private void OnReadData(AsyncReadOperation readDataOp)
        {
            Packet packet = new Packet(readDataOp.Bytes);

            if (readDataOp.Successful)
            {
                Received?.Invoke(this, new ReceivedEventArgs(this, packet));
                // Necessary that BeginRead is after raising the event
                BeginRead();
            }
            else
            {
                Received?.Invoke(this, new ReceivedEventArgs(readDataOp.ThrownException, this, packet));
            }
        }

        // Write a packet to connection. Not thread-safe.
        public void Write(Packet packet)
        {
            if (TryGetStream(out NetworkStream networkStream))
            {
                AsyncWriteOperation writeOp = new AsyncWriteOperation(packet.FullData, networkStream, OnWriteData);
                writeOp.Start();
            }
        }

        private void OnWriteData(AsyncWriteOperation writeOp)
        {
            Packet packet = new Packet(writeOp.Bytes);

            if (writeOp.Successful)
            {
                Sent?.Invoke(this, new SentEventArgs(this, packet));
            }
            else
            {
                Sent?.Invoke(this, new SentEventArgs(writeOp.ThrownException, this, packet));
            }
        }
    }
}
