using System;
using System.Net.Sockets;

namespace SimpleServer
{
    public abstract class ServerEventArgs
    {
        private readonly Exception ex;
        private readonly Connection connection;

        public ServerEventArgs(Connection connection, Exception ex)
        {
            this.connection = connection;
            this.ex = ex;
        }
        
        public Exception GetException() => ex;

        public Connection GetConnection() => connection;

        public bool OperationSucceeded => ex == null;
    }

    public class ConnectedEventArgs: ServerEventArgs
    {
        public ConnectedEventArgs(Connection connection) : base(connection, null) { }

        public ConnectedEventArgs(Exception ex): base(null, ex) { }
    }

    public class DisconnectedEventArgs: ServerEventArgs
    {
        public DisconnectedEventArgs(Connection connection, Exception ex): base(connection, ex)
        {
            if (connection == null || ex == null)
                throw new ArgumentNullException("Both connection and exception argument must be non-null");
        }
    }

    public abstract class ServerEventArgsWithData : ServerEventArgs
    {
        public ServerEventArgsWithData(Connection connection, byte[] data): base(connection, null)
        {
            Data = data ?? throw new ArgumentNullException("Packet argument cannot be null");
        }

        public ServerEventArgsWithData(Exception ex, Connection connection, byte[] data) : base(connection, ex)
        {
            if (ex == null)
                throw new ArgumentNullException("Exception argument cannot be null");
        }
        
        public byte[] Data { get; }
    }
    
    public class SentEventArgs : ServerEventArgsWithData
    {
        public SentEventArgs(Connection connection, byte[] data) : base(connection, data) { }
        public SentEventArgs(Exception ex, Connection connection, byte[] data): base(ex, connection, data) { }
    }

    public class ReceivedEventArgs: ServerEventArgsWithData
    {
        public ReceivedEventArgs(Connection connection, byte[] data) : base(connection, data) { }
        public ReceivedEventArgs(Exception ex, Connection connection, byte[] data) : base(ex, connection, data) { }
    }
}
