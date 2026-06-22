using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noffz.DeviceDiscovery;

namespace DebugDlls
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UsbComFinder.FindComPortForCompositeDevice("2341", "0074", "360C281359323335E1C733334B57305C");
        }
    }
}
