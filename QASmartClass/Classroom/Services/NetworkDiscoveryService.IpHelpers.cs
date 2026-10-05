using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    // ═══════════════════════════════════════════════════════════
    //  NETWORK DISCOVERY — IP Address Helpers
    //  Tách từ NetworkDiscoveryService.cs (lines 1019–1106)
    //  ⚠️ CRITICAL: GetActiveLocalIPv4Addresses() là public static
    //     được gọi bởi UdpScreenBroadcastService.cs bên ngoài Classroom.
    //     KHÔNG được đổi tên, namespace, hoặc return type.
    // ═══════════════════════════════════════════════════════════
    public partial class NetworkDiscoveryService
    {
        /// <summary>
        /// Liệt kê tất cả địa chỉ IPv4 LAN thực (không phải VPN/virtual/loopback).
        /// Ưu tiên interfaces có gateway (máy tính nối với router LAN thực).
        /// ⚠️ Public static — được gọi từ UdpScreenBroadcastService.cs.
        /// </summary>
        public static List<string> GetActiveLocalIPv4Addresses()
        {
            var ips = new List<string>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    // Loại bỏ virtual adapters, VPN, WSL, Docker...
                    string name = ni.Name.ToLower();
                    string desc = ni.Description.ToLower();
                    if (name.Contains("virtual")   || desc.Contains("virtual")    ||
                        name.Contains("vmware")    || desc.Contains("vbox")       ||
                        name.Contains("virtualbox")|| desc.Contains("wsl")        ||
                        name.Contains("hyper-v")   || desc.Contains("npcap")      ||
                        name.Contains("docker")    || desc.Contains("loopback")   ||
                        name.Contains("teredo")    || desc.Contains("host-only")  ||
                        name.Contains("fortinet")  || desc.Contains("anyconnect") ||
                        name.Contains("nordvpn")   || desc.Contains("zerotier")   ||
                        name.Contains("tailscale") || desc.Contains("wireguard")  ||
                        name.Contains("vpn"))
                        continue;

                    var ipProps = ni.GetIPProperties();
                    if (ipProps == null) continue;

                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        string ipStr = addr.Address.ToString();
                        if (ipStr.StartsWith("127.")) continue;

                        // Interface có gateway → ưu tiên cao hơn (thêm vào đầu danh sách)
                        bool hasGateway = ipProps.GatewayAddresses?.Count > 0;
                        if (hasGateway)
                            ips.Insert(0, ipStr);
                        else
                            ips.Add(ipStr);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error enumerating network interfaces: {Err}", ex.Message);
            }

            // Fallback: thử kết nối UDP giả để tìm IP thực
            if (ips.Count == 0)
            {
                try
                {
                    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                    socket.Connect("8.8.8.8", 65530);
                    var ip = ((IPEndPoint?)socket.LocalEndPoint)?.Address.ToString();
                    if (!string.IsNullOrEmpty(ip) && ip != "127.0.0.1")
                        ips.Add(ip);
                }
                catch { /* ignore */ }
            }

            if (ips.Count == 0)
                ips.Add("127.0.0.1");

            // Deduplicate
            var uniqueIps = new List<string>();
            foreach (var ip in ips)
                if (!uniqueIps.Contains(ip))
                    uniqueIps.Add(ip);

            return uniqueIps;
        }

        /// <summary>Lấy IP ưu tiên nhất để dùng làm beacon server address.</summary>
        private static string GetLocalIPAddress()
        {
            var ips = GetActiveLocalIPv4Addresses();
            return ips.Count > 0 ? ips[0] : "127.0.0.1";
        }
    }
}
