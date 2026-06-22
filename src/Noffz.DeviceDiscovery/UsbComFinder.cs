using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Noffz.DeviceDiscovery
{
    internal class UsbComFinder
    {
        public static string FindComPort(string vid, string pid, string sn = "")
        {
            string query = "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_" + vid + "%' AND DeviceID LIKE '%PID_" + pid + "%'";
            var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject device in searcher.Get())
            {
                var name = device["Name"]?.ToString();
                string deviceId = device["DeviceID"]?.ToString();
                string serialNumber = deviceId?.Split('\\').LastOrDefault();
                if (name == null)
                    continue;

                // Example Name:
                // "USB Serial Device (COM7)"
                if (sn != "" && (sn != serialNumber && !serialNumber.Contains(sn)))
                {
                    continue;
                }

                var match = Regex.Match(name, @"\(COM\d+\)");
                if (match.Success)
                {
                    return match.Value.Trim('(', ')');
                }
            }

            return "";
        }

        public static string FindComPortByInterface(string vid, string pid, int interfaceIndex, IEnumerable<string> knownPorts)
        {
            string mi = interfaceIndex.ToString("D2");
            string query = "SELECT * FROM Win32_PnPEntity" +
                           " WHERE DeviceID LIKE '%VID_" + vid + "&PID_" + pid + "&MI_" + mi + "%'";

            var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject device in searcher.Get())
            {
                string ftdibusPath = @"SYSTEM\CurrentControlSet\Enum\FTDIBUS";
                using (var ftdibusKey = Registry.LocalMachine.OpenSubKey(ftdibusPath))
                {
                    if (ftdibusKey == null)
                        continue;

                    foreach (string subKeyName in ftdibusKey.GetSubKeyNames())
                    {
                        if (!subKeyName.Contains("VID_" + vid) || !subKeyName.Contains("PID_" + pid))
                            continue;

                        using (var subKey = ftdibusKey.OpenSubKey(subKeyName))
                        {
                            if (subKey == null)
                                continue;

                            using (var instanceKey = subKey.OpenSubKey("0000"))
                            {
                                if (instanceKey == null)
                                    continue;

                                string[] hardwareIds = instanceKey.GetValue("HardwareID") as string[];
                                if (hardwareIds == null)
                                    continue;

                                bool miMatches = Array.Exists(hardwareIds, id => id.EndsWith("MI_" + mi));
                                if (!miMatches)
                                    continue;

                                using (var deviceParamsKey = instanceKey.OpenSubKey("Device Parameters"))
                                {
                                    string portName = deviceParamsKey?.GetValue("PortName")?.ToString();
                                    if (portName != null && !knownPorts.Contains(portName))
                                        return portName;
                                }
                            }
                        }
                    }
                }
            }

            return "";
        }

    }
}
