using System;

namespace SimpleNetworking
{
    public abstract class IOState
    {
        public int Position { get; private set; }
        
        public bool NotifySuccess { get; }

        public byte[] Data { get; }

        public IOState(byte[] data, bool notifySuccess)
        {
            Data = data ?? throw new ArgumentNullException("Data argument is null"); ;
            Position = 0;
            NotifySuccess = notifySuccess;
        }

        public IOState(int size, bool notifySuccess) : this(new byte[size], notifySuccess) { }

        internal void AdvancePosition(int steps)
        {
            if (Position + steps > Size || steps < 0)
                throw new ArgumentOutOfRangeException("Overflow detected");
            Position += steps;
        }

        public int Size { get => Data.Length; }

        public int Remaining { get => Size - Position; }
    }

    public class ReadIOState : IOState
    {
        public ReadIOState(int size, bool notifySuccess, bool isLengthRead): base(size, notifySuccess)
        {
            LengthRead = isLengthRead;
        }

        public bool LengthRead { get; }
    }


    public class WriteIOState: IOState
    {
        public WriteIOState(byte[] data, bool notifySuccess) : base(data, notifySuccess) { }
    }

}
