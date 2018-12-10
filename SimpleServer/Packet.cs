using System;

namespace SimpleServer
{
    public class Packet
    {
        public Packet(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data array cannot be null");
            }

            FullData = new byte[4 + data.Length];

            BitConverter.GetBytes(data.Length).CopyTo(FullData, 0);
            data.CopyTo(FullData, 4);

            Data = new ArraySegment<byte>(FullData, 0, data.Length);
        }
        
        
        public ArraySegment<byte> Data
        {
            get;
        }

        public byte[] DataAsCopy
        {
            get
            {
                byte[] dataCopy = new byte[Data.Count];
                Array.Copy(Data.Array, 4, dataCopy, 0, dataCopy.Length);
                return dataCopy;
            }
        }

        internal byte[] FullData
        {
            get;
        }

        public int Length
        {
            get => Data.Count;
        }

        internal int FullLength
        {
            get => FullData.Length;
        }
        
    }
}
