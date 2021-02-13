using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ClientProfitCenterTest
{

    class Program
    {
        static DataSaver dataSaver = new DataSaver();
        static ConcurrentQueue<Packet> queue = new ConcurrentQueue<Packet>();
        static bool BreakThread = false;

        static void GetXmlParam(string filename, out string multicast, out int delay)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(XmlData));

            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
            {
                XmlData newXmlData = (XmlData)formatter.Deserialize(fs);
                multicast = newXmlData.Multicast;
                delay = newXmlData.Delay;
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter xml config argument.");
                Console.WriteLine("Usage: ClientProfitCenter <filename>");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File {0} not exist", args[0]);
                return;
            }

            string multicast;
            int delay;
            GetXmlParam(args[0], out multicast, out delay);

            ReceiveMsgParam receiveMsgParam;
            receiveMsgParam.Multicast = multicast;
            receiveMsgParam.Delay = delay;

            //Thread receiveMsgThread = new Thread(new ParameterizedThreadStart(ReceiveMessage));
            //receiveMsgThread.Start(receiveMsgParam);

            //Thread counterThread = new Thread(new ThreadStart(Counter));
            //counterThread.Start();

            Thread watchDogThread = new Thread(new ParameterizedThreadStart(WatchDog));
            watchDogThread.Start(receiveMsgParam);

            while (Console.ReadKey().Key == ConsoleKey.Enter)
                dataSaver.PrintData();

            BreakThread = true;
        }

        private static void WatchDog(Object obj)
        {
            Thread receiveMsgThread = new Thread(new ParameterizedThreadStart(ReceiveMessage));
            receiveMsgThread.Start(obj);

            Thread counterThread = new Thread(new ThreadStart(Counter));
            counterThread.Start();

            while (!BreakThread)
            {
                if (!receiveMsgThread.IsAlive)
                {
                    receiveMsgThread = null;
                    receiveMsgThread = new Thread(new ParameterizedThreadStart(ReceiveMessage));
                    receiveMsgThread.Start(obj);
                }

                Thread.Sleep(100);
            }
        }

        private static void ReceiveMessage(Object obj)
        {
            bool isClosed = true;

            Packet packet;
            packet.NumPacket = 0;
            packet.Quotation = 0;
            packet.StartRange = 0;
            packet.EndRange = 0;

            ReceiveMsgParam receiveMsgParam = (ReceiveMsgParam)obj;

            IPAddress Multicast = IPAddress.Parse(receiveMsgParam.Multicast);
            IPEndPoint remoteIp = null;
            UdpClient receiver = null;
            try
            {
                while (!BreakThread)
                {
                    if (isClosed)
                    {
                        receiver = new UdpClient(8001);
                        isClosed = false;
                        receiver.JoinMulticastGroup(Multicast);
                    }
                    if (receiver.Available > 0)
                    {
                        byte[] data = receiver.Receive(ref remoteIp); // получаем данные
                        packet.GetPacket(data);
                        queue.Enqueue(packet);
                        packet.PrintPacket();
                        receiver.Close();
                        isClosed = true;
                    }
                    Thread.Sleep(receiveMsgParam.Delay);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (!isClosed)
                    receiver.Close();

                Multicast = null;
                remoteIp = null;
                receiver = null;
            }
        }

        public static void Counter()
        {
            Dictionary<double, int> DicMode = new Dictionary<double, int>();
            Packet packet;
            UInt64 QuotationsCount = 0;
            double Quotations = 0;
            double QuotationsQ = 0;
            double Delta = 0;
            double StartRange = 0;
            double EndRange = 0;

            while (!BreakThread)
            {
                if (queue.IsEmpty)
                {
                    Thread.Sleep(100);
                    continue;
                }

                for (int i = 0; i < queue.Count; i++)
                {
                    if (queue.TryDequeue(out packet))
                    {
                        QuotationsCount++;
                        Quotations += packet.Quotation;
                        QuotationsQ += packet.Quotation * packet.Quotation;

                        if ((packet.StartRange != StartRange) || (packet.EndRange != EndRange))
                        {
                            DicMode.Clear();
                            EndRange = packet.EndRange;
                            StartRange = packet.StartRange;
                            Delta = (EndRange - StartRange) / 3.0;
                            DicMode.Add(StartRange + Delta, 0);
                            DicMode.Add(StartRange + (Delta * 2), 0);
                            DicMode.Add(StartRange + (Delta * 3), 0);
                        }

                        if (packet.Quotation <= (StartRange + Delta))
                            DicMode[StartRange + Delta]++;
                        else if (packet.Quotation <= (StartRange + (Delta * 2)))
                            DicMode[StartRange + (Delta * 2)]++;
                        else
                            DicMode[StartRange + (Delta * 3)]++;

                        dataSaver.SetPacketLoss(packet.NumPacket);
                        dataSaver.SetAvr(Quotations / QuotationsCount);
                        dataSaver.SetDev(System.Math.Sqrt((QuotationsQ - (Quotations * Quotations) / QuotationsCount) * (1.0d / (QuotationsCount - 1))));
                        dataSaver.SetMed(Median(DicMode, StartRange, Delta));
                        dataSaver.SetMod(Mode(DicMode, StartRange, Delta));
                    }
                }
            }
        }

        private static double Mode(Dictionary<double, int> DicMode, double StartRange, double Delta)
        {
            double mode = 0;
            if ((DicMode[StartRange + Delta] >= DicMode[StartRange + (Delta * 2)]) && (DicMode[StartRange + Delta] > DicMode[StartRange + (Delta * 3)]))
                mode = StartRange + (Delta * (DicMode[StartRange + Delta] / ((DicMode[StartRange + Delta]) + (DicMode[StartRange + Delta] - DicMode[StartRange + (Delta * 2)]))));
            else if ((DicMode[StartRange + Delta] < DicMode[StartRange + (Delta * 2)]) && (DicMode[StartRange + (Delta * 2)] >= DicMode[StartRange + (Delta * 3)]))
                mode = StartRange + Delta + (Delta * ((DicMode[StartRange + (Delta * 2)] - DicMode[StartRange + Delta]) / ((DicMode[StartRange + (Delta * 2)] - DicMode[StartRange + Delta]) + (DicMode[StartRange + (Delta * 2)] - DicMode[StartRange + (Delta * 3)]))));
            else if ((DicMode[StartRange + Delta] <= DicMode[StartRange + (Delta * 3)]) && (DicMode[StartRange + (Delta * 2)] < DicMode[StartRange + (Delta * 3)]))
                mode = StartRange + Delta + Delta + (Delta * ((DicMode[StartRange + (Delta * 3)] - DicMode[StartRange + (Delta * 2)]) / ((DicMode[StartRange + (Delta * 3)] - DicMode[StartRange + (Delta * 2)]) + (DicMode[StartRange + (Delta * 3)]))));
            return mode;
        }

        private static double Median(Dictionary<double, int> DicMode, double StartRange, double Delta)
        {
            double ef = (DicMode[StartRange + Delta] + DicMode[StartRange + (Delta * 2)] + DicMode[StartRange + (Delta * 3)])/2;
            double med = StartRange + Delta + (Delta * ((ef - DicMode[StartRange + Delta]) / (DicMode[StartRange + (Delta * 2)])));
            return med;
        }
    }
}
