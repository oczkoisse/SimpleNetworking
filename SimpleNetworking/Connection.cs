using System;
using System.Net;
using System.Net.Sockets;

namespace SimpleNetworking
{
    public class Connection
    {
        // For intercepting write operations
        public event EventHandler<SentEventArgs> Sent;
        // For intercepting read operations
        public event EventHandler<ReceivedEventArgs> Received;

        private readonly Server parentServer;
        private readonly TcpClient client;

        private NetworkStream networkStream;

        internal Connection(TcpClient tcpClient, Server server)
        {
            client = tcpClient ?? throw new ArgumentNullException("TcpClient argument is null");
            parentServer = server ?? throw new ArgumentNullException("Server argument is null");
            
            networkStream = client.GetStream();

            Read(new ReadIOState(4, false, true));
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

            networkStream = client.GetStream();

            Read(new ReadIOState(4, false, true));
        }

        public void Close()
        {
            parentServer?.Remove(this);
            client.Close();
        }

        public bool IsServerConnection() => parentServer != null;
       
        private void Read(IOState state)
        {
            try
            {
                networkStream.BeginRead(state.Data, 0, state.Size, EndOrContinueRead, state);
            }
            catch (Exception ex)
            {
                Received?.Invoke(this, new ReceivedEventArgs(ex, this, state.Data));
            }
        }

        private void EndOrContinueRead(IAsyncResult ar)
        {
            ReadIOState state = ar.AsyncState as ReadIOState;

            // Get the number of bytes read and advance position by the same amount
            try
            {
                int bytesRead = networkStream.EndRead(ar);
                if (bytesRead > 0)
                    state.AdvancePosition(bytesRead);
            }
            catch (Exception ex)
            {
                Received.Invoke(this, new ReceivedEventArgs(ex, this, state.Data));
            }

            if (state.Remaining > 0)
            {
                Read(state);
            }
            else if (state.LengthRead)
            {
                int length = BitConverter.ToInt32(state.Data, 0);
                Read(new ReadIOState(length, true, false));
            }
            else
            {
                Received?.Invoke(this, new ReceivedEventArgs(this, state.Data));
                Read(new ReadIOState(4, false, true));
            }
        }
        
        public void Write(short data, bool notifyOnSuccess = false)
        {
            byte[] dataArr = BitConverter.GetBytes(data);
            Write(dataArr, notifyOnSuccess);
        }

        public void Write(int data, bool notifyOnSuccess = false)
        {
            byte[] dataArr = BitConverter.GetBytes(data);
            Write(dataArr, notifyOnSuccess);
        }

        public void Write(long data, bool notifyOnSuccess = false)
        {
            byte[] dataArr = BitConverter.GetBytes(data);
            Write(dataArr, notifyOnSuccess);
        }

        public void Write(float data, bool notifyOnSuccess = false)
        {
            byte[] dataArr = BitConverter.GetBytes(data);
            Write(dataArr, notifyOnSuccess);
        }

        public void Write(double data, bool notifyOnSuccess = false)
        {
            byte[] dataArr = BitConverter.GetBytes(data);
            Write(dataArr, notifyOnSuccess);
        }

        public void Write(bool data, bool notifyOnSuccess = false)
        {
            byte[] dataArr = BitConverter.GetBytes(data);
            Write(dataArr, notifyOnSuccess);
        }

        public void Write(byte[] data, bool notifyOnSuccess = true)
        {
            try
            {
                networkStream.BeginWrite(data, 0, data.Length, EndWrite, new WriteIOState(data, notifyOnSuccess));
            }
            catch (Exception ex)
            {
                Sent?.Invoke(this, new SentEventArgs(ex, this, data));
            }
        }
     

        private void EndWrite(IAsyncResult ar)
        {
            IOState state = ar.AsyncState as WriteIOState;

            try
            {
                networkStream.EndWrite(ar);

                state.AdvancePosition(state.Size);

                if (state.NotifySuccess)
                    Sent?.Invoke(this, new SentEventArgs(this, state.Data));
            }
            catch (Exception ex)
            {
                Sent?.Invoke(this, new SentEventArgs(ex, this, state.Data));
            }
        }
    }
}
