using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ByteSync.Tests.TestUtilities.Labs;

public static class WOL
{
    // https://stackoverflow.com/questions/861873/wake-on-lan-using-c-sharp
    public static async Task WakeOnLan(string macAddress)
    {
        byte[] magicPacket = BuildMagicPacket(macAddress);
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces().Where((n) =>
            n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up))
        {
            IPInterfaceProperties iPInterfaceProperties = networkInterface.GetIPProperties();
            foreach (MulticastIPAddressInformation multicastIPAddressInformation in iPInterfaceProperties.MulticastAddresses)
            {
                IPAddress multicastIpAddress = multicastIPAddressInformation.Address;
                if (multicastIpAddress.ToString().StartsWith("ff02::1%", StringComparison.OrdinalIgnoreCase)) // Ipv6: All hosts on LAN (with zone index)
                {
                    UnicastIPAddressInformation? unicastIPAddressInformation = iPInterfaceProperties.UnicastAddresses.Where((u) =>
                        u.Address.AddressFamily == AddressFamily.InterNetworkV6 && !u.Address.IsIPv6LinkLocal).FirstOrDefault();
                    if (unicastIPAddressInformation != null)
                    {
                        await SendWakeOnLan(unicastIPAddressInformation.Address, multicastIpAddress, magicPacket);
                    }
                }
                else if (multicastIpAddress.ToString().Equals("224.0.0.1")) // Ipv4: All hosts on LAN
                {
                    UnicastIPAddressInformation? unicastIPAddressInformation = iPInterfaceProperties.UnicastAddresses.Where((u) =>
                        u.Address.AddressFamily == AddressFamily.InterNetwork && !iPInterfaceProperties.GetIPv4Properties().IsAutomaticPrivateAddressingActive).FirstOrDefault();
                    if (unicastIPAddressInformation != null)
                    {
                        await SendWakeOnLan(unicastIPAddressInformation.Address, multicastIpAddress, magicPacket);
                    }
                }
            }
        }
    }

    static byte[] BuildMagicPacket(string macAddress) // MacAddress in any standard HEX format
    {
        macAddress = Regex.Replace(macAddress, "[: -]", "");
        byte[] macBytes = Convert.FromHexString(macAddress);

        IEnumerable<byte> header = Enumerable.Repeat((byte)0xff, 6); //First 6 times 0xff
        IEnumerable<byte> data = Enumerable.Repeat(macBytes, 16).SelectMany(m => m); // then 16 times MacAddress
        return header.Concat(data).ToArray();
    }

    static async Task SendWakeOnLan(IPAddress localIpAddress, IPAddress multicastIpAddress, byte[] magicPacket)
    {
        using UdpClient client = new(new IPEndPoint(localIpAddress, 0));
        await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(multicastIpAddress, 9));
    }
    
    
    // https://stackoverflow.com/questions/12802888/get-a-machines-mac-address-on-the-local-network-from-its-ip-in-c-sharp
    public static string GetMacAddress(string ipAddress)
    {
        string macAddress = string.Empty;
        System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
        pProcess.StartInfo.FileName = "arp";
        pProcess.StartInfo.Arguments = "-a " + ipAddress;
        pProcess.StartInfo.UseShellExecute = false;
        pProcess.StartInfo.RedirectStandardOutput = true;
        pProcess.StartInfo.CreateNoWindow = true;
        pProcess.Start();
        string strOutput = pProcess.StandardOutput.ReadToEnd();
        string[] substrings = strOutput.Split('-');
        if (substrings.Length >= 8)
        {
            macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2)) 
                         + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6] 
                         + "-" + substrings[7] + "-" 
                         + substrings[8].Substring(0, 2);
            return macAddress;
        }

        else
        {
            return "not found";
        }
    }
}