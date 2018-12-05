using System;

namespace SimpleServer
{
    public class Packet
    {
        public Packet(byte[] data)
        {
            Data = data ?? throw new ArgumentNullException("null bytes array passed to Packet");
        }

        public Packet(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException("Length must be a positive number");
            Data = new byte[length];
        }
        
        public byte[] Data
        {
            get;
        }

        internal byte[] FullData
        {
            get
            {
                byte[] fullData = new byte[4 + Data.Length];
                BitConverter.GetBytes(Data.Length).CopyTo(fullData, 0);
                Data.CopyTo(fullData, 4);
                return fullData;
            }
        }

        public int Length
        {
            get { return Data.Length; }
        }

        internal int FullLength
        {
            get { return Data.Length + 4; }
        }
    }
}
