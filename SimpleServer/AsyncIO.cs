using System;
using System.IO;

namespace SimpleServer
{
    public delegate void AsyncReadOperationCallback(AsyncReadOperation readOp);
    
    public class AsyncReadOperation
    {
        public Token Token { get; }
        public byte[] Bytes { get; }

        public int Size { get => Bytes.Length; }

        public bool Threw { get => ThrownException != null; }

        public Exception ThrownException { get; private set; }

        public int Position { get; private set; }

        public int Remaining { get => Size - Position; }

        public bool Successful { get => Remaining == 0 && !Threw; }

        internal AsyncReadOperationCallback Callback { get; }

        internal Stream UnderlyingStream { get; }

        internal AsyncReadOperation(int size, Token token, Stream stream, AsyncReadOperationCallback callback)
        {
            Token = token ?? throw new ArgumentNullException("Token argument is null");
            if (!stream.CanRead)
                throw new ArgumentException("Stream cannot be read from");
            UnderlyingStream = stream ?? throw new ArgumentNullException("Stream argument is null");

            if (size <= 0)
                throw new ArgumentOutOfRangeException("size is not positive");
            Bytes = new byte[size];

            Callback = callback;
        }

        internal void AdvancePosition(int steps)
        {
            if (!Threw)
                throw new InvalidOperationException("AdvancePosition() called when the operation has already failed.");
            if (Position + steps > Size || steps < 0)
                throw new ArgumentOutOfRangeException("Overflow detected in AsyncReadState");
            Position += steps;
        }

       

        internal void Start()
        {
            try
            {
                UnderlyingStream.BeginRead(Bytes, Position, Size, EndOrContinue, null);
            }
            catch (Exception ex)
            {
                ThrownException = ex;
            }
        }

        private void EndOrContinue(IAsyncResult ar)
        {
            // Get the number of bytes read and advance position by the same amount
            try
            {
                int bytesRead = UnderlyingStream.EndRead(ar);
                if (bytesRead > 0)
                    AdvancePosition(bytesRead);
            }
            catch (Exception ex)
            {
                ThrownException = ex;
            }

            if (Remaining > 0 && !Threw)
            {
                UnderlyingStream.BeginRead(Bytes, Position, Remaining, EndOrContinue, null);
            }
            else
            {
                Callback?.Invoke(this);
            }
        }
    }
}
