using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Service quản lý ghi nhớ thiết bị trong lớp học.
    /// - Tự động lưu thông tin thiết bị khi HS kết nối lần đầu
    /// - Nhận diện thiết bị cũ khi kết nối lại (theo PCName/DeviceId)
    /// - Cho phép GV quản lý danh sách thiết bị đã ghi nhớ
    /// </summary>
    public class DeviceMemoryService
    {
        private readonly AppDbContext _db;

        public DeviceMemoryService(AppDbContext db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════════════
        //  GHI NHỚ THIẾT BỊ KHI KẾT NỐI
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Ghi nhớ (hoặc cập nhật) thiết bị khi HS kết nối.
        /// Gọi sau khi nhận được JOIN từ client.
        /// </summary>
        public RememberedDevice SaveOrUpdateDevice(
            string pcName, string studentCode, string studentName,
            string ipAddress, string clientVersion = "", string macAddress = "",
            int classroomId = 0)
        {
            try
            {
                // Tìm thiết bị đã ghi nhớ trước đó (ưu tiên theo PCName)
                var existing = FindDevice(pcName, macAddress);

                if (existing != null)
                {
                    // Cập nhật thông tin
                    existing.LastKnownIP = ipAddress;
                    existing.StudentCode = studentCode;
                    existing.StudentName = studentName;
                    existing.ClientVersion = clientVersion;
                    existing.LastConnected = DateTime.Now;
                    existing.ConnectionCount++;
                    existing.IsActive = true;

                    if (!string.IsNullOrEmpty(macAddress))
                        existing.MACAddress = macAddress;

                    if (classroomId > 0)
                        existing.ClassroomId = classroomId;

                    _db.SaveChanges();

                    Log.Information("DeviceMemory: Updated device {PCName} → {Student} ({Code}), #{Count} connections",
                        pcName, studentName, studentCode, existing.ConnectionCount);

                    return existing;
                }
                else
                {
                    // Thiết bị mới → lưu lần đầu
                    var device = new RememberedDevice
                    {
                        PCName = pcName,
                        DeviceId = GenerateDeviceId(pcName, macAddress),
                        LastKnownIP = ipAddress,
                        MACAddress = macAddress,
                        StudentCode = studentCode,
                        StudentName = studentName,
                        ClientVersion = clientVersion,
                        ClassroomId = classroomId,
                        FirstSeen = DateTime.Now,
                        LastConnected = DateTime.Now,
                        ConnectionCount = 1,
                        IsActive = true
                    };

                    _db.RememberedDevices.Add(device);
                    _db.SaveChanges();

                    Log.Information("DeviceMemory: NEW device saved {PCName} → {Student} ({Code})",
                        pcName, studentName, studentCode);

                    return device;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("DeviceMemory: Save error: {Err}", ex.Message);
                return null!;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TÌM THIẾT BỊ ĐÃ GHI NHỚ
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm thiết bị đã ghi nhớ theo PCName hoặc MAC.
        /// </summary>
        public RememberedDevice? FindDevice(string pcName, string macAddress = "")
        {
            try
            {
                // Ưu tiên tìm theo MAC (chính xác nhất, không đổi)
                if (!string.IsNullOrEmpty(macAddress))
                {
                    var byMac = _db.RememberedDevices
                        .FirstOrDefault(d => d.MACAddress == macAddress && d.IsActive);
                    if (byMac != null) return byMac;
                }

                // Tìm theo PCName
                if (!string.IsNullOrEmpty(pcName))
                {
                    return _db.RememberedDevices
                        .FirstOrDefault(d => d.PCName == pcName && d.IsActive);
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Warning("DeviceMemory: Find error: {Err}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Thử nhận diện HS từ PCName khi thiết bị kết nối lại.
        /// Trả về (studentCode, studentName) nếu tìm thấy, null nếu thiết bị mới.
        /// </summary>
        public (string code, string name)? TryRecognize(string pcName, string macAddress = "")
        {
            var device = FindDevice(pcName, macAddress);
            if (device != null && !string.IsNullOrEmpty(device.StudentCode))
            {
                Log.Information("DeviceMemory: Recognized {PCName} → {Student} ({Code})",
                    pcName, device.StudentName, device.StudentCode);
                return (device.StudentCode, device.StudentName);
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  DANH SÁCH THIẾT BỊ
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Lấy tất cả thiết bị đã ghi nhớ (đang active).
        /// </summary>
        public List<RememberedDevice> GetAllDevices(int classroomId = 0)
        {
            try
            {
                var query = _db.RememberedDevices.Where(d => d.IsActive);

                if (classroomId > 0)
                    query = query.Where(d => d.ClassroomId == classroomId || d.ClassroomId == 0);

                return query.OrderByDescending(d => d.LastConnected).ToList();
            }
            catch (Exception ex)
            {
                Log.Warning("DeviceMemory: GetAll error: {Err}", ex.Message);
                return new List<RememberedDevice>();
            }
        }

        /// <summary>
        /// Lấy thiết bị chưa kết nối trong phiên hiện tại.
        /// So sánh danh sách ghi nhớ vs danh sách đang kết nối.
        /// </summary>
        public List<RememberedDevice> GetOfflineDevices(IEnumerable<string> connectedPCNames)
        {
            try
            {
                var connected = connectedPCNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return _db.RememberedDevices
                    .Where(d => d.IsActive)
                    .ToList()
                    .Where(d => !connected.Contains(d.PCName))
                    .OrderByDescending(d => d.LastConnected)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Warning("DeviceMemory: GetOffline error: {Err}", ex.Message);
                return new List<RememberedDevice>();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  QUẢN LÝ THIẾT BỊ (GV)
        // ═══════════════════════════════════════════════════════════

        /// <summary>Cập nhật ghi chú cho thiết bị (VD: "Máy bàn 5")</summary>
        public bool UpdateNotes(int deviceId, string notes)
        {
            try
            {
                var device = _db.RememberedDevices.Find(deviceId);
                if (device != null)
                {
                    device.Notes = notes;
                    _db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>Gán thiết bị cho HS cụ thể (GV tự chỉ định)</summary>
        public bool AssignToStudent(int deviceId, string studentCode, string studentName)
        {
            try
            {
                var device = _db.RememberedDevices.Find(deviceId);
                if (device != null)
                {
                    device.StudentCode = studentCode;
                    device.StudentName = studentName;
                    _db.SaveChanges();
                    Log.Information("DeviceMemory: Manually assigned {PCName} → {Student}", device.PCName, studentName);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>Ẩn thiết bị (không xóa, chỉ deactivate)</summary>
        public bool DeactivateDevice(int deviceId)
        {
            try
            {
                var device = _db.RememberedDevices.Find(deviceId);
                if (device != null)
                {
                    device.IsActive = false;
                    _db.SaveChanges();
                    Log.Information("DeviceMemory: Deactivated {PCName}", device.PCName);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>Xóa thiết bị khỏi danh sách ghi nhớ</summary>
        public bool RemoveDevice(int deviceId)
        {
            try
            {
                var device = _db.RememberedDevices.Find(deviceId);
                if (device != null)
                {
                    _db.RememberedDevices.Remove(device);
                    _db.SaveChanges();
                    Log.Information("DeviceMemory: Removed {PCName}", device.PCName);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>Xóa tất cả thiết bị (reset)</summary>
        public int ClearAll()
        {
            try
            {
                var all = _db.RememberedDevices.ToList();
                _db.RememberedDevices.RemoveRange(all);
                _db.SaveChanges();
                Log.Information("DeviceMemory: Cleared all {Count} devices", all.Count);
                return all.Count;
            }
            catch { return 0; }
        }

        // ═══════════════════════════════════════════════════════════
        //  SYNC VỚI BẢNG STUDENT
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Đồng bộ thông tin thiết bị vào bảng Student.
        /// Cập nhật PCName, IP, LastSeen cho HS tương ứng.
        /// </summary>
        public void SyncToStudentTable(string studentCode, string pcName, string ipAddress)
        {
            try
            {
                var student = _db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                if (student != null)
                {
                    student.PCName = pcName;
                    student.IPAddress = ipAddress;
                    student.LastSeen = DateTime.Now;
                    student.IsOnline = true;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("DeviceMemory: SyncToStudent error: {Err}", ex.Message);
            }
        }

        /// <summary>
        /// Đánh dấu student offline khi thiết bị ngắt kết nối.
        /// </summary>
        public void MarkStudentOffline(string studentCode)
        {
            try
            {
                var student = _db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                if (student != null)
                {
                    student.IsOnline = false;
                    student.LastSeen = DateTime.Now;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("DeviceMemory: MarkOffline error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private static string GenerateDeviceId(string pcName, string macAddress)
        {
            if (!string.IsNullOrEmpty(macAddress))
                return $"{pcName}|{macAddress}";
            return pcName;
        }
    }
}
