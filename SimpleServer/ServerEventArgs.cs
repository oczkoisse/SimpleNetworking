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

    public abstract class PacketizedServerEventArgs : ServerEventArgs
    {
        private readonly Packet packet;

        public PacketizedServerEventArgs(Connection connection, Packet packet): base(connection, null)
        {
            SetPacket(ref this.packet, packet);
        }

        public PacketizedServerEventArgs(Exception ex, Connection connection, Packet packet) : base(connection, ex)
        {
            if (ex == null)
                throw new ArgumentNullException("Exception argument cannot be null");
        }

        private void SetPacket(ref Packet packet, Packet packetValue)
        {
            packet = packetValue ?? throw new ArgumentNullException("Packet argument cannot be null");
        }

        public Packet GetPacket() => packet;
    }
    
    public class SentEventArgs : PacketizedServerEventArgs
    {
        public SentEventArgs(Connection connection, Packet packet) : base(connection, packet) { }
        public SentEventArgs(Exception ex, Connection connection, Packet packet): base(ex, connection, packet) { }
    }

    public class ReceivedEventArgs: PacketizedServerEventArgs
    {
        public ReceivedEventArgs(Connection connection, Packet packet) : base(connection, packet) { }
        public ReceivedEventArgs(Exception ex, Connection connection, Packet packet) : base(ex, connection, packet) { }
    }
}
