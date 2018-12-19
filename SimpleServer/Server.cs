using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SimpleServer
{
    public class Server
    {
        // For subscribing to new connections
        public event EventHandler<ConnectedEventArgs> Connected;

        // Faking a HashSet
        private ConcurrentDictionary<Connection, byte> connections;
        
        // Underlying listener for this server
        private TcpListener listener;
        
        
        public Server(IPAddress address, int port)
        {
            listener = new TcpListener(address, port);
            connections = new ConcurrentDictionary<Connection, byte>();
        }

        public IPEndPoint Address
        {
            get { return (IPEndPoint)listener.LocalEndpoint; }
        }


        public Server(int port): this(IPAddress.Loopback, port) { }

        #region Connection accept callbacks
        private void BeginAccept()
        {
            try
            {
                listener.BeginAcceptTcpClient(DoAccept, null);
            }
            catch(ObjectDisposedException)
            {
                // Server is stopped
                // Can't do anything other than not listen anymore
            }
            catch (Exception ex)
            {
                Connected?.Invoke(this, new ConnectedEventArgs(ex));
            }
        }
        private void DoAccept(IAsyncResult res)
        {
            TcpClient client = null;
            try
            {
                client = listener.EndAcceptTcpClient(res);
            }
            catch(ObjectDisposedException)
            {
                // Server is stopped
                // Can't do anything other than not listen anymore
            }
            catch (Exception ex)
            {
                Connected?.Invoke(this, new ConnectedEventArgs(ex));
            }

            if (client != null)
            {
                BeginAccept();

                Connection conn = new Connection(client, this);
                if (connections.TryAdd(conn, 0))
                {
                    Connected?.Invoke(this, new ConnectedEventArgs(conn));
                }
                else
                {
                    Connected?.Invoke(this, new ConnectedEventArgs(new Exception("Unable to add connection")));
                }
            }
        }
        #endregion

        #region Public API
        public void Start()
        {
            listener.Start();
            // Initiate the connection accepting loop asynchronously
            BeginAccept();
        }

        public void Stop()
        {
            // listener.Stop() does not close any accepted connections. 
            // so close them manually. This is thread-safe way of iterating.
            foreach (var keyValue in connections)
            {
                // Close the connection
                keyValue.Key.Close();
            }
            // Close the listener finally
            listener.Stop();
        }

        internal void Remove(Connection conn)
        {
            if (conn != null)
            {
                connections.TryRemove(conn, out byte _);
            }
            else
                throw new ArgumentNullException("Connection argument is null");
        }
        #endregion
    }
}
