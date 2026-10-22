using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.Classroom.Services;
using QASmartClass.StudentClient.Services;
using Xunit;

namespace QASmartClass.Tests
{
    [Collection("ClassroomNetwork")]
    public class V114TeacherClassroomScaleTests
    {
        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        [Fact]
        public async Task ScaleTest_50Students_Connect_BroadcastAndPrivateMessages_Succeeds()
        {
            // 1. Arrange: Khởi tạo Giáo viên với cổng test ngẫu nhiên
            var discoveryService = new NetworkDiscoveryService();
            string className = "12A3_ScaleTest";
            string teacherName = "Thầy Nguyễn";

            // Bắt đầu lắng nghe
            await discoveryService.StartAsync(className, teacherName);

            // Kiểm tra máy GV đã mở kết nối chưa (NetworkDiscoveryService dùng TCP_PORT = 29877)
            int serverPort = NetworkDiscoveryService.TCP_PORT;

            int totalStudents = 50;
            var students = new List<StudentNetworkClient>();
            var receivedBroadcasts = new ConcurrentDictionary<string, List<string>>();
            var receivedPrivateMsgs = new ConcurrentDictionary<string, List<string>>();

            try
            {
                // 2. Act: Kết nối đồng thời 50 máy học sinh vào máy giáo viên
                var connectTasks = new List<Task>();

                for (int i = 1; i <= totalStudents; i++)
                {
                    string code = $"HS{i:D3}";
                    string name = $"Học Sinh {i}";
                    string pc = $"PC_LAB_{i:D2}";

                    var client = new StudentNetworkClient
                    {
                        StudentCode = code,
                        StudentName = name,
                        PCName = pc
                    };

                    receivedBroadcasts[code] = new List<string>();
                    receivedPrivateMsgs[code] = new List<string>();

                    client.MessageReceived += (sender, msg) =>
                    {
                        if (msg.StartsWith("[Riêng]"))
                        {
                            receivedPrivateMsgs[code].Add(msg);
                        }
                        else
                        {
                            receivedBroadcasts[code].Add(msg);
                        }
                    };

                    students.Add(client);

                    // Kết nối trực tiếp vào máy GV
                    connectTasks.Add(client.ConnectDirectAsync("127.0.0.1", serverPort));
                }

                // Đợi toàn bộ 50 máy học sinh hoàn thành kết nối
                await Task.WhenAll(connectTasks);

                // Cho thời gian xử lý bắt tay và ổn định handshake
                await Task.Delay(1000);

                // 3. Assert kết nối ban đầu
                int connectedStudentsCount = 0;
                foreach (var s in students)
                {
                    if (s.IsConnected) connectedStudentsCount++;
                }

                Assert.Equal(50, connectedStudentsCount);
                Assert.True(discoveryService.ConnectedCount >= 50, 
                    $"Máy giáo viên phải ghi nhận ít nhất 50 kết nối, thực tế: {discoveryService.ConnectedCount}");

                // 4. Test Gửi Tin Nhắn CẢ LỚP (Broadcast Message)
                string broadcastMessage = "Chào cả lớp, các em chuẩn bị kiểm tra 15 phút!";
                await discoveryService.BroadcastMessage(broadcastMessage);

                // Chờ các máy nhận tin nhắn
                await Task.Delay(1500);

                // Kiểm tra 100% (50/50) học sinh đều nhận được tin nhắn cả lớp
                int broadcastReceivedCount = 0;
                foreach (var s in students)
                {
                    if (receivedBroadcasts.TryGetValue(s.StudentCode, out var msgs) && msgs.Contains(broadcastMessage))
                    {
                        broadcastReceivedCount++;
                    }
                }
                Assert.Equal(50, broadcastReceivedCount);

                // 5. Test Gửi Tin Nhắn RIÊNG CHO TỪNG HỌC SINH KHÁC NHAU (Private Message)
                // Giáo viên gửi tin nhắn cá nhân hóa cho từng em: "Em HS001 chú ý...", "Em HS002 chú ý..."
                var privateTasks = new List<Task>();
                for (int i = 1; i <= totalStudents; i++)
                {
                    string targetCode = $"HS{i:D3}";
                    string privateMsg = $"[Riêng] Em {targetCode} cần kiểm tra lại bài số {i}!";
                    privateTasks.Add(discoveryService.SendToStudentAsync(targetCode, $"MSG|{privateMsg}"));
                }

                await Task.WhenAll(privateTasks);
                await Task.Delay(1500);

                // Kiểm tra tính chính xác: Mỗi em chỉ nhận đúng tin nhắn riêng của mình
                for (int i = 1; i <= totalStudents; i++)
                {
                    string code = $"HS{i:D3}";
                    string expectedMsg = $"[Riêng] Em {code} cần kiểm tra lại bài số {i}!";

                    Assert.True(receivedPrivateMsgs.TryGetValue(code, out var msgs), 
                        $"Học sinh {code} phải có danh sách tin nhắn riêng.");
                    Assert.Contains(expectedMsg, msgs);

                    // Đảm bảo không nhận nhầm tin nhắn của em khác
                    string otherCode = (i == 1) ? "HS002" : "HS001";
                    string unexpectedMsg = $"[Riêng] Em {otherCode} cần kiểm tra lại bài số {(i == 1 ? 2 : 1)}!";
                    Assert.DoesNotContain(unexpectedMsg, msgs);
                }

                // 6. Test Giữ kết nối trong thời gian dài (Heartbeat & Long-running stability)
                // Giữ kết nối trong 5 giây, mô phỏng các máy vẫn gửi Heartbeat định kỳ
                await Task.Delay(3000);

                foreach (var s in students)
                {
                    Assert.True(s.IsConnected, $"Học sinh {s.StudentCode} phải vẫn duy trì kết nối ổn định.");
                }
            }
            finally
            {
                // Dọn dẹp tài nguyên
                foreach (var s in students)
                {
                    s.Dispose();
                }
                discoveryService.Stop();
                discoveryService.Dispose();
            }
        }

        [Fact]
        public async Task BidirectionalMessaging_TeacherAndStudent_Succeeds()
        {
            // 1. Arrange: Khởi tạo Giáo viên
            var discoveryService = new NetworkDiscoveryService();
            await discoveryService.StartAsync("10A1_ChatTest", "Thầy Hoàng");
            int serverPort = NetworkDiscoveryService.TCP_PORT;

            var teacherReceived = new List<(string StudentCode, string Message)>();
            var teacherLock = new object();
            discoveryService.MessageReceived += (s, args) =>
            {
                lock (teacherLock)
                {
                    teacherReceived.Add((args.StudentCode, args.Message));
                }
            };

            var alice = new StudentNetworkClient { StudentCode = "HS_ALICE", StudentName = "Alice", PCName = "PC_ALICE" };
            var bob = new StudentNetworkClient { StudentCode = "HS_BOB", StudentName = "Bob", PCName = "PC_BOB" };

            var aliceReceived = new List<string>();
            var bobReceived = new List<string>();

            alice.MessageReceived += (s, msg) => aliceReceived.Add(msg);
            bob.MessageReceived += (s, msg) => bobReceived.Add(msg);

            try
            {
                // 2. Act: Kết nối cả Alice và Bob vào máy giáo viên
                await Task.WhenAll(
                    alice.ConnectDirectAsync("127.0.0.1", serverPort),
                    bob.ConnectDirectAsync("127.0.0.1", serverPort)
                );
                await Task.Delay(1000);

                Assert.True(alice.IsConnected && bob.IsConnected, "Cả Alice và Bob phải kết nối thành công.");

                // 3. Tương tác chiều 1 (HS -> GV): Alice gửi tin nhắn riêng cho Thầy
                string aliceMsg = "Thưa thầy, em nộp bài muộn 5 phút được không ạ?";
                await alice.SendChat(aliceMsg, "TEACHER");
                await Task.Delay(1000);

                // Giáo viên phải nhận được tin nhắn từ Alice
                lock (teacherLock)
                {
                    Assert.Contains(teacherReceived, m => m.StudentCode == "HS_ALICE" && m.Message.Contains(aliceMsg));
                }

                // 4. Tương tác chiều 2 (GV -> HS): Giáo viên trả lời riêng cho Alice
                string teacherReplyAlice = "[Thầy] Được nhé Alice, em nộp trước 11h là được.";
                await discoveryService.SendToStudentAsync("HS_ALICE", $"MSG|{teacherReplyAlice}");
                await Task.Delay(1000);

                // Alice nhận được câu trả lời của thầy
                Assert.Contains(teacherReplyAlice, aliceReceived);

                // Bob TUYỆT ĐỐI KHÔNG nhận được tin nhắn riêng của Alice
                Assert.DoesNotContain(teacherReplyAlice, bobReceived);

                // 5. Tương tác chiều 1 (HS -> GV): Bob gửi câu hỏi lên cho Giáo viên
                string bobQuestion = "Hình câu số 3 bị mờ thầy ơi!";
                await bob.SendQuestion(bobQuestion);
                await Task.Delay(1000);

                lock (teacherLock)
                {
                    Assert.Contains(teacherReceived, m => m.StudentCode == "HS_BOB" && m.Message.Contains(bobQuestion));
                }

                // 6. Tương tác chiều 2 (GV -> CẢ LỚP): Thầy thông báo broadcast cho cả lớp
                string classAnnouncement = "📢 Thầy vừa phóng to hình câu 3 trên bảng chiếu chung rồi nhé.";
                await discoveryService.BroadcastMessage(classAnnouncement);
                await Task.Delay(1000);

                // Cả Alice và Bob đều nhận được thông báo cả lớp
                Assert.Contains(classAnnouncement, aliceReceived);
                Assert.Contains(classAnnouncement, bobReceived);
            }
            finally
            {
                alice.Dispose();
                bob.Dispose();
                discoveryService.Stop();
                discoveryService.Dispose();
            }
        }
    }
}

