using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Sensors
{
    class NetworkInterface
    {
        public static string GetValue(string selector, string deselector = "")
        {
            // Native WiFi API is much cheaper than starting netsh and doesn't depend on the Windows display language
            string? nativeValue = NativeWifi.GetValue(selector);
            if (nativeValue != null)
            {
                return nativeValue;
            }

            return getNetshValue(selector, deselector);
        }

        private static string getNetshValue(string selector, string deselector)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = {
                        FileName = "netsh.exe",
                        Arguments = "wlan show interfaces",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string[] output = process.StandardOutput.ReadToEnd().ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                process.WaitForExit();

                foreach (var item in output)
                {
                    string[] line = item.Split(":");
                    if (line.Length < 2)
                    {
                        continue;
                    }

                    string outputResult = Regex.Replace(line[1].Trim(), @"\t|\n|\r", "").Trim();

                    if (!String.IsNullOrEmpty(deselector))
                    {
                        if (item.Contains(selector) && !item.Contains(deselector))
                        {
                            return outputResult;
                        }
                    }

                    if (item.Contains(selector))
                    {
                        return outputResult;
                    }
                }
            }
            catch (Exception) { }

            return "Unknown";
        }
    }

    // https://learn.microsoft.com/en-us/windows/win32/nativewifi/native-wifi-api-reference
    static class NativeWifi
    {
        private const uint clientVersion = 2;
        private const int opcodeCurrentConnection = 7; // wlan_intf_opcode_current_connection
        private const string unknown = "Unknown";

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public int isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DOT11_SSID
        {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ucSSID;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_ASSOCIATION_ATTRIBUTES
        {
            public DOT11_SSID dot11Ssid;
            public int dot11BssType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;
            public int dot11PhyType;
            public uint uDot11PhyIndex;
            public uint wlanSignalQuality;
            public uint ulRxRate;
            public uint ulTxRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_SECURITY_ATTRIBUTES
        {
            public int bSecurityEnabled;
            public int bOneXEnabled;
            public int dot11AuthAlgorithm;
            public int dot11CipherAlgorithm;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_CONNECTION_ATTRIBUTES
        {
            public int isState;
            public int wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
            public WLAN_SECURITY_ATTRIBUTES wlanSecurityAttributes;
        }

        [DllImport("wlanapi.dll")]
        private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid, int opCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, IntPtr pWlanOpcodeValueType);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        // Same values netsh prints in English
        private static readonly string[] interfaceStates = { "not ready", "connected", "ad hoc network formed", "disconnecting", "disconnected", "associating", "discovering", "authenticating" };

        // Returns null for selectors this class doesn't know, so the caller can fall back to netsh
        public static string? GetValue(string selector)
        {
            string key = selector.Trim().ToLowerInvariant();
            if (key != "state" && key != "ssid" && key != "bssid" && key != "signal" && key != "profile" && key != "description" && !key.StartsWith("receive rate") && !key.StartsWith("transmit rate"))
            {
                return null;
            }

            IntPtr client = IntPtr.Zero;
            IntPtr interfaceList = IntPtr.Zero;
            try
            {
                if (WlanOpenHandle(clientVersion, IntPtr.Zero, out _, out client) != 0)
                {
                    // WLAN AutoConfig service not running or no WiFi adapter
                    return unknown;
                }

                if (WlanEnumInterfaces(client, IntPtr.Zero, out interfaceList) != 0)
                {
                    return unknown;
                }

                int count = Marshal.ReadInt32(interfaceList);
                if (count < 1)
                {
                    return unknown;
                }

                // Items start after dwNumberOfItems and dwIndex, prefer a connected interface
                int itemSize = Marshal.SizeOf<WLAN_INTERFACE_INFO>();
                WLAN_INTERFACE_INFO info = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(interfaceList + 8);
                for (int i = 0; i < count; i++)
                {
                    WLAN_INTERFACE_INFO candidate = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(interfaceList + 8 + (i * itemSize));
                    if (candidate.isState == 1)
                    {
                        info = candidate;
                        break;
                    }
                }

                if (key == "state")
                {
                    return (info.isState >= 0 && info.isState < interfaceStates.Length) ? interfaceStates[info.isState] : unknown;
                }

                if (key == "description")
                {
                    return info.strInterfaceDescription;
                }

                Guid interfaceGuid = info.InterfaceGuid;
                if (WlanQueryInterface(client, ref interfaceGuid, opcodeCurrentConnection, IntPtr.Zero, out _, out IntPtr data, IntPtr.Zero) != 0)
                {
                    // Not connected
                    return unknown;
                }

                try
                {
                    WLAN_CONNECTION_ATTRIBUTES connection = Marshal.PtrToStructure<WLAN_CONNECTION_ATTRIBUTES>(data);
                    WLAN_ASSOCIATION_ATTRIBUTES association = connection.wlanAssociationAttributes;

                    switch (key)
                    {
                        case "ssid":
                            int length = (int)Math.Min(association.dot11Ssid.uSSIDLength, 32);
                            return Encoding.UTF8.GetString(association.dot11Ssid.ucSSID, 0, length);
                        case "bssid":
                            return String.Join(":", association.dot11Bssid.Select(x => x.ToString("x2")));
                        case "signal":
                            return association.wlanSignalQuality + "%";
                        case "profile":
                            return connection.strProfileName;
                        default:
                            // Rates are reported in kbps, netsh shows Mbps
                            uint rate = key.StartsWith("receive rate") ? association.ulRxRate : association.ulTxRate;
                            return (rate / 1000.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                finally
                {
                    WlanFreeMemory(data);
                }
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                return null;
            }
            finally
            {
                if (interfaceList != IntPtr.Zero)
                {
                    WlanFreeMemory(interfaceList);
                }

                if (client != IntPtr.Zero)
                {
                    WlanCloseHandle(client, IntPtr.Zero);
                }
            }
        }
    }
}
