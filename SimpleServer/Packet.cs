using System;

namespace SimpleServer
{
    public class Packet
    {
        public Packet(byte[] data)
        {
            Data = data ?? throw new ArgumentNullException("null bytes array passed to Packet");
        }
        
        public byte[] Data
        {
            get;
        }

        public int Length
        {
            get { return Data.Length; }
        }

        internal byte[] LengthData
        {
            get => BitConverter.GetBytes(Length);
        }
    }
}
