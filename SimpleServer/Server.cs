using System;
using System.IO;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleServer
{
    public class Server
    {
        private ConcurrentDictionary<Token, TcpClient> clients;
        private TcpListener listener;

        #region Events
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<SentEventArgs> Sent;
        public event EventHandler<ReceivedEventArgs> Received;
        #endregion

        public Server(IPAddress address, int port)
        {
            listener = new TcpListener(address, port);
            clients = new ConcurrentDictionary<Token, TcpClient>();
        }

        public IPEndPoint Address
        {
            get { return (IPEndPoint)listener.LocalEndpoint; }
        }

        public void Start()
        {
            listener.Start();
            BeginAccept();
        }

        public Server(int port): this(IPAddress.Loopback, port) { }

        #region Implementation
        private void BeginAccept()
        {
            try
            {
                listener.BeginAcceptTcpClient(DoAccept, null);
            }
            catch (Exception ex)
            {
                Connected(this, new ConnectedEventArgs(ex));
            }
        }
        private void DoAccept(IAsyncResult res)
        {
            TcpClient client = listener.EndAcceptTcpClient(res);
            Token token = new Token();
            if (clients.TryAdd(token, client))
            {
                Connected(this, new ConnectedEventArgs(token));
                BeginReadClient(token);
            }
            else
            {
                Connected(this, new ConnectedEventArgs(new Exception("Failed to add connection")));
            }
            BeginAccept();
        }
        
        private void BeginReadClient(Token token)
        {
            if (clients.TryGetValue(token, out TcpClient client))
            {
                if (TryGetStream(client, out NetworkStream networkStream))
                {
                    AsyncReadOperation readOp = new AsyncReadOperation(4, token, networkStream, OnReadLength);
                    readOp.Start();
                }
            }
        }

        private void OnReadLength(AsyncReadOperation readLengthOp)
        {
            if (readLengthOp.Successful)
            {
                int length = BitConverter.ToInt32(readLengthOp.Bytes, 0);
                AsyncReadOperation readDataOp = new AsyncReadOperation(length, readLengthOp.Token, readLengthOp.UnderlyingStream, OnReadData);
                readDataOp.Start();
            }
        }

        private void OnReadData(AsyncReadOperation readDataOp)
        {
            Packet packet = new Packet(readDataOp.Bytes);

            if (readDataOp.Successful)
            {
                Received(this, new ReceivedEventArgs(readDataOp.Token, packet));
                BeginReadClient(readDataOp.Token);
            }
            else
            {
                Received(this, new ReceivedEventArgs(readDataOp.Token, packet, readDataOp.ThrownException));
            }
        }

        private bool TryGetStream(TcpClient client, out NetworkStream networkStream)
        {
            try
            {
                networkStream = client.GetStream();
            }
            catch (ObjectDisposedException)
            {
                // The TcpClient has been closed.
                networkStream = null;
            }
            return networkStream != null;
        }
        
        #endregion

        #region Public API
        public void Stop()
        {
            // listener.Stop() does not close any accepted connections. 
            // so close them manually
            foreach (TcpClient client in clients.Values)
            {
                client.Close();
            }
            clients.Clear();
            // Close the listener finally
            listener.Stop();
        }

        public void Disconnect(Token token)
        {
            if (token != null)
            {
                if (clients.TryRemove(token, out TcpClient client))
                {
                    client.Close();
                }
            }
        }

        public void Send(Token token, Packet packet)
        {
            if (clients.TryGetValue(token, out TcpClient client))
            {
                if (TryGetStream(client, out NetworkStream networkStream))
                {
                    if (networkStream.CanWrite)
                    {
                        try
                        {
                            // TODO: Protect NetworkStream against concurrent writes
                            // For now, this is done by doing the writes sequentially (blocking)
                            networkStream.Write(BitConverter.GetBytes(packet.Length), 0, 4);
                            networkStream.Write(packet.Data, 0, packet.Length);
                            Sent(this, new SentEventArgs(token, packet));
                        }
                        catch (Exception ex)
                        {
                            Sent(this, new SentEventArgs(token, packet, ex));
                        }
                    }
                }
            }
        }
        #endregion
    }
}
