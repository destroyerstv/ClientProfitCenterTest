using System;
using System.Collections.Generic;
using System.Text;

namespace ClientProfitCenterTest
{
    struct Packet
    {
        public UInt64 NumPacket;
        public double Quotation;
        public double StartRange;
        public double EndRange;
        public byte[] GetBytes()
        {
            List<byte> listByte = new List<byte>();
            listByte.AddRange(BitConverter.GetBytes(NumPacket));
            listByte.AddRange(BitConverter.GetBytes(Quotation));
            listByte.AddRange(BitConverter.GetBytes(StartRange));
            listByte.AddRange(BitConverter.GetBytes(EndRange));
            return listByte.ToArray();
        }

        public void GetPacket(byte[] Arr)
        {
            int offset = 0;
            NumPacket = BitConverter.ToUInt64(Arr, offset);
            offset = offset + sizeof(UInt64);
            Quotation = BitConverter.ToDouble(Arr, offset);
            offset = offset + sizeof(double);
            StartRange = BitConverter.ToDouble(Arr, offset);
            offset = offset + sizeof(double);
            EndRange = BitConverter.ToDouble(Arr, offset);
        }

        public void PrintPacket()
        {
            Console.WriteLine("NumPacket = {0}", NumPacket);
            Console.WriteLine("Quotation = {0}", Quotation);
            Console.WriteLine("StartRange = {0}", StartRange);
            Console.WriteLine("EndRange = {0}", EndRange);
            Console.WriteLine("-----------------\n");
        }
    }
}
