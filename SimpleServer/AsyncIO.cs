using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleServer
{
    internal delegate void AsyncReadOperationCallback(AsyncReadOperation readOp);
    internal delegate void AsyncWriteOperationCallback(AsyncWriteOperation writeOp);

    internal abstract class AsyncOperation
    {
        public byte[] Bytes { get; }

        public int Size { get => Bytes.Length; }

        public bool Threw { get => ThrownException != null; }

        public Exception ThrownException
        {
            get;
            protected set;
        }

        public int Position { get; private set; }

        public int Remaining { get => Size - Position; }

        public bool Successful { get => Remaining == 0 && !Threw; }

        public Stream UnderlyingStream { get; }

        protected void AdvancePosition(int steps)
        {
            if (Threw)
                throw new InvalidOperationException("AdvancePosition() called when the operation has already failed.");
            if (Position + steps > Size || steps < 0)
                throw new ArgumentOutOfRangeException("Overflow detected");
            Position += steps;
        }

        public AsyncOperation(int size, Stream stream)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException("Size argument is not positive");
            Bytes = new byte[size];
            UnderlyingStream = stream ?? throw new ArgumentNullException("Stream argument is null");
        }

        public AsyncOperation(byte[] bytes, Stream stream)
        {
            Bytes = bytes ?? throw new ArgumentNullException("Bytes argument is null");
            UnderlyingStream = stream ?? throw new ArgumentNullException("Stream argument is null");
        }

        public abstract void Start();
        
    }
    internal class AsyncReadOperation : AsyncOperation
    {
        public AsyncReadOperationCallback Callback { get; }

        public AsyncReadOperation(int size, Stream stream, AsyncReadOperationCallback callback) : base(size, stream)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream cannot be read from");

            Callback = callback;
        }

        public override void Start()
        {
            BeginRead();
        }

        private void BeginRead()
        {
            try
            {
                UnderlyingStream.BeginRead(Bytes, Position, Remaining, EndOrContinueRead, null);
            }
            catch (Exception ex)
            {
                ThrownException = ex;
                Callback?.Invoke(this);
            }
        }

        private void EndOrContinueRead(IAsyncResult ar)
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
                BeginRead();
            }
            else
            {
                Callback?.Invoke(this);
            }
        }
    }

    internal class AsyncWriteOperation: AsyncOperation
    {
        public AsyncWriteOperationCallback Callback { get; }

        public AsyncWriteOperation(byte[] bytes, Stream stream, AsyncWriteOperationCallback callback): base(bytes, stream)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream cannot be written to");

            Callback = callback;
        }

        public override void Start()
        {
            try
            {
                UnderlyingStream.BeginWrite(Bytes, 0, Bytes.Length, EndWrite, null);
            }
            catch(Exception ex)
            {
                ThrownException = ex;
                Callback?.Invoke(this);
            }
        }

        private void EndWrite(IAsyncResult ar)
        {
            try
            {
                UnderlyingStream.EndWrite(ar);
                AdvancePosition(Bytes.Length);
            }
            catch(Exception ex)
            {
                ThrownException = ex;
            }

            Callback?.Invoke(this);
        }
    }
}
