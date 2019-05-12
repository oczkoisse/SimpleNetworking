using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SimpleServer
{
    public class Server
    {
        /// <summary>
        /// Subscribe to this event to get newly accepted connections
        /// </summary>
        public event EventHandler<ConnectedEventArgs> Connected;
        
        /// <summary>
        /// Hash set of <see cref="Connection"/>s
        /// </summary>
        private ConcurrentHashSet<Connection> connections;

        /// <summary>
        /// Underlying listener for this server
        /// </summary> 
        private TcpListener listener;
        
        /// <summary>
        /// Create a server instance that will listen at the specified address and port
        /// </summary>
        /// <param name="address">Address at which the server will listening</param>
        /// <param name="port">Port at which the server will start listening</param>
        /// <remarks>
        /// This will just create the server instance but won't start listening immediately.
        /// Use <see cref="Start"/> method to start listening.
        /// </remarks>
        public Server(IPAddress address, int port)
        {
            listener = new TcpListener(address, port);
            connections = new ConcurrentHashSet<Connection>();
        }

        /// <summary>
        /// Returns the endpoint at which the server is set to listen.
        /// </summary>
        public IPEndPoint Address
        {
            get { return (IPEndPoint)listener.LocalEndpoint; }
        }

        /// <summary>
        /// Create a server instance that will listen at the loopback address and the specified port.
        /// </summary>
        /// <param name="port">Port at which the server will start listening</param>
        public Server(int port): this(IPAddress.Loopback, port) { }

        #region Connection accept callbacks

        /// <summary>
        /// Callback for starting the process of accepting a new connection.
        /// Any exception that occurs during this will be encapsulated in 
        /// the <see cref="ConnectedEventArgs"/> event arguments for the 
        /// subscribers to <see cref="Connected"/> event.
        /// </summary>
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

        /// <summary>
        /// Callback for finishing the process of accepting a new connection.
        /// Any exception that occurs during this will be encapsulated in 
        /// the <see cref="ConnectedEventArgs"/> event arguments for the 
        /// subscribers to <see cref="Connected"/> event. Only if the accept
        /// is successfull will it chain another accept call. Also, it adds 
        /// the accepted connection to server's records.
        /// </summary>
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
                if (connections.TryAdd(conn))
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

        /// <summary>
        /// Remove the connection from the server's records.
        /// </summary>
        /// <param name="conn"></param>
        internal void Remove(Connection conn)
        {
            if (conn != null)
            {
                connections.TryRemove(conn);
            }
            else
                throw new ArgumentNullException("Connection argument is null");
        }

        #region Public API
        /// <summary>
        /// Start the server and begin accepting connections.
        /// </summary>
        /// <remarks>Non-blocking call.</remarks>
        public void Start()
        {
            listener.Start();
            // Initiate the connection accepting loop asynchronously
            BeginAccept();
        }

        /// <summary>
        /// Closes all connections one by one and then stops listening for new connections.
        /// </summary>
        public void Stop()
        {
            // listener.Stop() does not close any accepted connections. 
            // so close them manually. This is thread-safe way of iterating.
            foreach (var conn in connections)
            {
                // Close the connection
                conn.Close();
            }

            // Clear internal records of all connections
            connections.Clear();

            // Close the listener finally
            listener.Stop();
        }
        #endregion
    }
}
