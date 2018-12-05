using System;
using System.Net.Sockets;

namespace SimpleServer
{
    public abstract class ServerEventArgs
    {
        private readonly Exception ex;

        public ServerEventArgs(Exception ex)
        {
            this.ex = ex ?? throw new ArgumentNullException("null Exception is not allowed in this constructor");
        }

        public ServerEventArgs() { }

        public Exception GetException() => ex;

        public bool Succeeded => ex == null;
    }

    public abstract class TokenizedServerEventArgs : ServerEventArgs
    {
        private readonly Token token;

        // Successful with a token
        public TokenizedServerEventArgs(Token token) { this.token = token; }
        // Unsuccessful with a token
        public TokenizedServerEventArgs(Token token, Exception ex) : base(ex) { this.token = token; }
        // Unsuccessful without a token
        public TokenizedServerEventArgs(Exception ex): base(ex) { }

        public Token GetToken() => token;

        public bool HasToken() => token != null;
    }

    public class ConnectedEventArgs : TokenizedServerEventArgs
    {
        public ConnectedEventArgs(Exception ex) : base(ex) { }
        public ConnectedEventArgs(Token token): base(token) { }
    }

    public class SentEventArgs : TokenizedServerEventArgs
    {
        public Packet UnderlyingPacket
        {
            get;
        }

        public SentEventArgs(Token token, Packet packet, Exception ex): base(token, ex)
        {
            UnderlyingPacket = packet ?? throw new ArgumentNullException("Packet cannot be null in SentEventArgs");
        }
        public SentEventArgs(Token token, Packet packet): base(token)
        {
            UnderlyingPacket = packet ?? throw new ArgumentNullException("Packet cannot be null in SentEventArgs");
        }
    }

    public class ReceivedEventArgs : TokenizedServerEventArgs
    {
        public Packet UnderlyingPacket
        {
            get;
        }

        public ReceivedEventArgs(Token token, Packet packet, Exception ex) : base(token, ex) { UnderlyingPacket = packet; }
        public ReceivedEventArgs(Token token, Packet packet) : base(token) { UnderlyingPacket = packet; }
    }
}
