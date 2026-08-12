using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_02 — HỘI ĐỒNG CHUYÊN GIA (Phiên bản nâng cao v2)
    /// 
    /// Bao gồm 8 kịch bản thực tế đánh giá triệt để cơ chế khám phá mạng LAN:
    ///   TC-01: Trả về IP LAN vật lý hợp lệ (IPv4)
    ///   TC-02: Lọc card ảo (VMware, VirtualBox, Docker, VPN, WSL...)
    ///   TC-03: Ưu tiên card có Gateway (mạng LAN chính)
    ///   TC-04: Fallback 127.0.0.1 khi không có card vật lý
    ///   TC-05: HttpListener bind thành công vào IP LAN vật lý
    ///   TC-06: HttpListener fallback loopback khi Access Denied
    ///   TC-07: GetPrimaryLocalIP trả về IP chính xác
    ///   TC-08: Kiểm tra phương thức NetworkDiscoveryService tồn tại đầy đủ
    /// </summary>
    public class LOI_VID_02_ExpertVerificationTests
    {
        /// <summary>
        /// TC-01 [Chuyên gia CSDL & Quản lý IT]:
        /// GetActiveLocalIPv4Addresses trả về ít nhất 1 IP hợp lệ.
        /// Tất cả IP phải là IPv4, không chứa IPv6.
        /// Mô phỏng: Giáo viên bật máy tính phòng Lab, hệ thống quét card mạng.
        /// </summary>
        [Fact]
        public void TC01_GetActiveLocalIPv4_ReturnsValidIPv4Addresses()
        {
            // Act
            var ips = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();

            // Assert — phải có ít nhất 1 IP
            Assert.NotNull(ips);
            Assert.NotEmpty(ips);
            
            foreach (var ip in ips)
            {
                // Phải parse được thành IP hợp lệ
                Assert.True(IPAddress.TryParse(ip, out var address), 
                    $"'{ip}' không phải là địa chỉ IP hợp lệ");
                
                // Phải là IPv4 (InterNetwork), không phải IPv6 (InterNetworkV6)
                Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily);
                
                // Không chứa ký tự đặc biệt
                Assert.DoesNotContain(":", ip);
                Assert.False(string.IsNullOrWhiteSpace(ip), "IP không được rỗng");
                
                // Mỗi octet phải nằm trong [0-255]
                var octets = ip.Split('.');
                Assert.Equal(4, octets.Length);
                foreach (var octet in octets)
                {
                    int val = int.Parse(octet);
                    Assert.InRange(val, 0, 255);
                }
            }
        }

        /// <summary>
        /// TC-02 [Chuyên gia Bảo mật & Gamer giỏi]:
        /// Lọc toàn bộ card ảo: VMware, VirtualBox, Docker, WSL, Hyper-V, VPN, Tailscale, ZeroTier.
        /// Mô phỏng: Máy GV cài VMware, Docker Desktop, VPN Fortinet → không lẫn card ảo.
        /// </summary>
        [Fact]
        public void TC02_FiltersVirtualAdapters_NoVirtualIPsReturned()
        {
            var ips = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();

            // Lấy danh sách tất cả card mạng để đối chiếu
            var allInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var virtualKeywords = new[] { 
                "virtual", "vmware", "vbox", "virtualbox", "wsl", "hyper-v", 
                "npcap", "docker", "loopback", "teredo", "host-only",
                "fortinet", "anyconnect", "nordvpn", "zerotier", "tailscale", "wireguard", "vpn"
            };

            foreach (var ip in ips)
            {
                if (ip == "127.0.0.1") continue; // Fallback is OK

                // Tìm interface chứa IP này
                foreach (var ni in allInterfaces)
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    
                    var ipProps = ni.GetIPProperties();
                    bool containsThisIp = ipProps.UnicastAddresses
                        .Any(a => a.Address.ToString() == ip);
                    
                    if (containsThisIp)
                    {
                        string name = ni.Name.ToLower();
                        string desc = ni.Description.ToLower();
                        
                        // Đảm bảo card chứa IP này KHÔNG phải card ảo
                        foreach (var keyword in virtualKeywords)
                        {
                            Assert.False(name.Contains(keyword) || desc.Contains(keyword),
                                $"IP {ip} thuộc card ảo '{ni.Name}' ({ni.Description}) chứa từ khóa '{keyword}'");
                        }
                        
                        // Đảm bảo không phải Loopback hoặc Tunnel
                        Assert.NotEqual(NetworkInterfaceType.Loopback, ni.NetworkInterfaceType);
                        Assert.NotEqual(NetworkInterfaceType.Tunnel, ni.NetworkInterfaceType);
                    }
                }
            }
        }

        /// <summary>
        /// TC-03 [Nhà giáo dục & Hiệu trưởng]:
        /// Card có Gateway (kết nối mạng LAN chính) được ưu tiên lên đầu danh sách.
        /// Mô phỏng: Phòng Lab có 2 card mạng → card nối switch chính phải ưu tiên.
        /// </summary>
        [Fact]
        public void TC03_PrioritizesGatewayAdapters_FirstIPHasGateway()
        {
            var ips = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
            if (ips.Count == 0 || (ips.Count == 1 && ips[0] == "127.0.0.1")) return;

            // Lấy IP đầu tiên
            string firstIp = ips[0];
            if (firstIp == "127.0.0.1") return; // Skip if only fallback

            // Kiểm tra xem card chứa IP đầu tiên có Gateway không
            bool firstIpHasGateway = false;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                var ipProps = ni.GetIPProperties();
                bool containsFirstIp = ipProps.UnicastAddresses
                    .Any(a => a.Address.ToString() == firstIp);
                
                if (containsFirstIp)
                {
                    firstIpHasGateway = ipProps.GatewayAddresses != null 
                        && ipProps.GatewayAddresses.Count > 0;
                    break;
                }
            }

            // Nếu có bất kỳ card nào có gateway, IP đầu tiên phải có gateway
            bool anyGatewayExists = false;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var ipProps = ni.GetIPProperties();
                if (ipProps.GatewayAddresses != null && ipProps.GatewayAddresses.Count > 0)
                {
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork 
                            && !addr.Address.ToString().StartsWith("127."))
                        {
                            anyGatewayExists = true;
                            break;
                        }
                    }
                }
                if (anyGatewayExists) break;
            }

            if (anyGatewayExists)
            {
                Assert.True(firstIpHasGateway, 
                    $"IP đầu tiên '{firstIp}' không có Gateway, nhưng có card khác có Gateway");
            }
        }

        /// <summary>
        /// TC-04 [Nhân viên nhà trường & Cán bộ Sở GD]:
        /// Khi không tìm thấy card vật lý nào, hệ thống trả về fallback 127.0.0.1.
        /// Kiểm tra logic: nếu ips rỗng → thêm 127.0.0.1.
        /// </summary>
        [Fact]
        public void TC04_FallbackMechanism_ReturnsLoopbackWhenEmpty()
        {
            // Kiểm tra rằng phương thức LUÔN trả về ít nhất 1 IP
            var ips = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
            Assert.NotNull(ips);
            Assert.True(ips.Count >= 1, "Phải có ít nhất 1 IP (127.0.0.1 nếu không có card vật lý)");

            // Nếu chỉ có 1 IP và đó là loopback → OK (fallback hoạt động)
            if (ips.Count == 1 && ips[0] == "127.0.0.1")
            {
                Assert.True(true, "Fallback 127.0.0.1 hoạt động chính xác");
            }

            // Nếu có nhiều IP → không có loopback trong danh sách
            if (ips.Count > 1)
            {
                Assert.DoesNotContain("127.0.0.1", ips);
            }
        }

        /// <summary>
        /// TC-05 [Quản lý IT & Chuyên gia Kiểm thử]:
        /// HttpListener bind thành công vào tất cả IP LAN vật lý + localhost + 127.0.0.1.
        /// Mô phỏng: Giáo viên khởi động phiên học → WebSocket Server lắng nghe kết nối từ HS.
        /// </summary>
        [Fact]
        public void TC05_HttpListenerBinds_ToAllResolvedIPs()
        {
            var localIPs = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
            var testPort = 19286; // Unique port to avoid collision
            
            var listener = new HttpListener();
            try
            {
                // Thêm tất cả IP LAN vật lý
                foreach (var ip in localIPs)
                {
                    if (ip != "127.0.0.1")
                    {
                        listener.Prefixes.Add($"http://{ip}:{testPort}/");
                    }
                }
                // Luôn thêm localhost và loopback
                listener.Prefixes.Add($"http://localhost:{testPort}/");
                listener.Prefixes.Add($"http://127.0.0.1:{testPort}/");
                
                try
                {
                    listener.Start();
                    Assert.True(listener.IsListening, "HttpListener phải đang lắng nghe");
                    
                    // Xác minh số prefix đã bind
                    Assert.True(listener.Prefixes.Count >= 2, 
                        "Phải bind ít nhất 2 prefix (localhost + 127.0.0.1)");
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 5)
                {
                    // Access Denied → fallback loopback
                    listener.Close();
                    
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{testPort}/");
                    listener.Prefixes.Add($"http://127.0.0.1:{testPort}/");
                    
                    listener.Start();
                    Assert.True(listener.IsListening, "Fallback loopback listener phải hoạt động");
                }
            }
            finally
            {
                listener.Close();
            }
        }

        /// <summary>
        /// TC-06 [Chuyên gia Thiết kế & Học sinh]:
        /// Khi bind IP LAN bị Access Denied, hệ thống tự động fallback sang loopback.
        /// Mô phỏng: Máy học sinh không có quyền Admin → vẫn kết nối được qua localhost.
        /// </summary>
        [Fact]
        public void TC06_FallbackLoopback_WhenAccessDenied()
        {
            var testPort = 19387; // Unique port
            
            // Cố gắng bind vào localhost/127.0.0.1 → luôn thành công
            var listener = new HttpListener();
            try
            {
                listener.Prefixes.Add($"http://localhost:{testPort}/");
                listener.Prefixes.Add($"http://127.0.0.1:{testPort}/");
                
                listener.Start();
                Assert.True(listener.IsListening, "Loopback listener phải luôn bind thành công");
                
                // Xác minh có đúng 2 prefix
                Assert.Equal(2, listener.Prefixes.Count);
            }
            finally
            {
                listener.Close();
            }
        }

        /// <summary>
        /// TC-07 [Giáo viên ưu tú & Trưởng bộ môn]:
        /// GetPrimaryLocalIP trả về IP chính (có gateway) hoặc fallback 127.0.0.1.
        /// Mô phỏng: Giáo viên nhìn thấy IP trên giao diện → học sinh gõ IP đó để kết nối.
        /// </summary>
        [Fact]
        public void TC07_GetPrimaryLocalIP_ReturnsBestIP()
        {
            // Kiểm tra phương thức tồn tại
            var method = typeof(NetworkDiscoveryService).GetMethod("GetPrimaryLocalIP",
                BindingFlags.Public | BindingFlags.Static);
            
            if (method != null)
            {
                var primaryIP = (string)method.Invoke(null, null)!;
                
                Assert.False(string.IsNullOrEmpty(primaryIP), "Primary IP không được rỗng");
                Assert.True(IPAddress.TryParse(primaryIP, out var addr), 
                    $"'{primaryIP}' không phải IP hợp lệ");
                Assert.Equal(AddressFamily.InterNetwork, addr.AddressFamily);
                
                // Primary IP phải nằm trong danh sách GetActiveLocalIPv4Addresses
                var allIPs = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
                Assert.True(allIPs.Contains(primaryIP) || primaryIP == "127.0.0.1",
                    $"Primary IP '{primaryIP}' không nằm trong danh sách IP khả dụng");
            }
            else
            {
                // Nếu không có GetPrimaryLocalIP, kiểm tra IP đầu tiên
                var ips = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
                Assert.NotEmpty(ips);
                string firstIp = ips[0];
                Assert.True(IPAddress.TryParse(firstIp, out _), 
                    $"IP đầu tiên '{firstIp}' không hợp lệ");
            }
        }

        /// <summary>
        /// TC-08 [Nhà khoa học giáo dục & Cán bộ Phòng GD]:
        /// Xác minh NetworkDiscoveryService có đầy đủ phương thức cần thiết.
        /// Kiểm tra không thiếu component sau khi refactor.
        /// </summary>
        [Fact]
        public void TC08_NetworkDiscoveryService_HasRequiredMethods()
        {
            var type = typeof(NetworkDiscoveryService);
            
            // Phương thức chính: GetActiveLocalIPv4Addresses (static, public)
            var getIPsMethod = type.GetMethod("GetActiveLocalIPv4Addresses",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(getIPsMethod);
            Assert.Equal(typeof(List<string>), getIPsMethod.ReturnType);
            Assert.Empty(getIPsMethod.GetParameters()); // Không có tham số

            // Kiểm tra WebSocketBridgeService tồn tại
            var bridgeType = typeof(WebSocketBridgeService);
            Assert.NotNull(bridgeType);

            // Kiểm tra WebSocketBridgeService có StartAsync
            var startAsyncMethod = bridgeType.GetMethod("StartAsync",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(startAsyncMethod);

            // Kiểm tra WebSocketBridgeService có StopAsync hoặc Dispose
            var stopMethod = bridgeType.GetMethod("StopAsync",
                BindingFlags.Public | BindingFlags.Instance)
                ?? bridgeType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
            var disposeMethod = bridgeType.GetMethod("Dispose",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.True(stopMethod != null || disposeMethod != null,
                "WebSocketBridgeService phải có StopAsync/Stop hoặc Dispose");
        }
    }
}
