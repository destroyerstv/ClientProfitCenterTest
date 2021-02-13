using System;
using System.Collections.Generic;
using System.Text;

namespace ClientProfitCenterTest
{
    class DataSaver
    {
        private object locker;
        private UInt64 packetLoss;
        private UInt64 currNum;
        private double med;
        private double mod;
        private double avr;
        private double dev;

        public DataSaver()
        {
            locker = new object();
            packetLoss = 0;
            currNum = 0;
            med = 0;
            mod = 0;
            avr = 0;
            dev = 0;
        }

        public void SetPacketLoss(UInt64 PacketNum)
        {
            lock (locker)
            {
                UInt64 tmpPacketLoss = PacketNum - (currNum + 1);
                if (tmpPacketLoss > 0)
                    packetLoss += tmpPacketLoss;

                currNum = PacketNum;
            }
        }

        public void SetAvr(double Avr)
        {
            lock (locker)
            {
                avr = Avr;
            }
        }

        public void SetDev(double Dev)
        {
            lock (locker)
            {
                dev = Dev;
            }
        }
        public void SetMed(double Med)
        {
            lock (locker)
            {
                med = Med;
            }
        }
        public void SetMod(double Mod)
        {
            lock (locker)
            {
                mod = Mod;
            }
        }

        public void PrintData()
        {
            lock (locker)
            {
                Console.WriteLine("paketLoss = {0}", packetLoss);
                Console.WriteLine("avr = {0: 0.00}", avr);
                Console.WriteLine("med = {0: 0.00}", med);
                Console.WriteLine("mod = {0: 0.00}", mod);
                Console.WriteLine("dev = {0: 0.00}", dev);
            }
        }
    }
}
