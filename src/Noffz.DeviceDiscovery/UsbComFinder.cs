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
    public class UsbComFinder
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

        public static string FindComPortForCompositeDevice(string vid, string pid, string sn = "")
        {
            string vidPidPattern = "VID_" + vid + "&PID_" + pid;
            string query = "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%" + vidPidPattern + "%'";
            var allDevices = new ManagementObjectSearcher(query).Get()
                .Cast<ManagementObject>()
                .ToList();

            foreach (ManagementObject device in allDevices)
            {
                string deviceId = device["DeviceID"]?.ToString() ?? "";
                string name = device["Name"]?.ToString() ?? "";

                // Skip entries that don't have a COM port
                var match = Regex.Match(name, @"\(COM\d+\)");
                if (!match.Success)
                    continue;

                if (!string.IsNullOrEmpty(sn))
                {
                    // Case 1: Simple device — SN is on the same entry as the COM port
                    // DeviceID: USB\VID_2341&PID_0074\<SN>
                    string[] parts = deviceId.Split('\\');
                    string lastSegment = parts.LastOrDefault() ?? "";
                    if (lastSegment.Equals(sn, StringComparison.OrdinalIgnoreCase))
                        return match.Value.Trim('(', ')');

                    // Case 2: Composite device — SN is on the parent, COM port is on the MI_ child.
                    // Find the parent composite entry for this interface and check its SN.
                    // The interface DeviceID looks like: USB\VID_xxxx&PID_xxxx&MI_00\6&xxx&0&0000
                    // The parent DeviceID looks like:    USB\VID_xxxx&PID_xxxx\<SN>
                    if (deviceId.Contains("&MI_"))
                    {
                        string parentId = "USB\\VID_" + vid + "&PID_" + pid + "\\" + sn;
                        bool parentExists = allDevices.Any(d =>
                            string.Equals(d["DeviceID"]?.ToString(), parentId, StringComparison.OrdinalIgnoreCase));
                        if (parentExists)
                            return match.Value.Trim('(', ')');
                    }

                    continue;
                }

                return match.Value.Trim('(', ')');
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
