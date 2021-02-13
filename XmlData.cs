using System;
using System.Collections.Generic;
using System.Text;

namespace ClientProfitCenterTest
{
    [Serializable]
    public class XmlData
    {
        public string Multicast { get; set; }
        public int Delay { get; set; }

        public XmlData()
        { }

        public XmlData(string multicast, int delay)
        {
            Multicast = multicast;
            Delay = delay;
        }
    }
}
