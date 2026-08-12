using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QASmartClass.KeyGen
{
    class Program
    {
        private const string PRIVATE_KEY_PATH = "private_key.pem";
        private const string PUBLIC_KEY_PATH = "public_key.pem";
        private const string HISTORY_FILE = "keygen_history.csv";

        // Cấu hình gói license
        static readonly Dictionary<string, (List<string> features, int maxStudents, int initialDays, int initialYears, int graceDays)> Packages = new()
        {
            ["trial_7"]    = (new() { "smartscreen" }, 10, 7, 0, 0),
            ["trial_30"]   = (new() { "smartscreen" }, 10, 30, 0, 0),
            ["basic"]      = (new() { "smartscreen", "learning_tools" }, 25, 365, 1, 7),
            ["standard"]   = (new() { "smartscreen", "smartclass", "learning_tools", "quiz" }, 35, 1095, 3, 15),
            ["premium"]    = (new() { "smartscreen", "smartclass", "learning_tools", "quiz", "screen_broadcast", "file_transfer" }, 45, 1095, 3, 30),
            ["enterprise"] = (new() { "smartscreen", "smartclass", "learning_tools", "quiz", "screen_broadcast", "file_transfer", "multi_room", "api" }, 100, 1825, 5, 60),
        };

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 0)
            {
                ShowHelp();
                RunInteractiveMenu();
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "genkeys":
                    GenerateKeys();
                    break;
                case "activate":
                    if (args.Length < 4) { ShowHelp(); return; }
                    GenerateActivation(
                        requestCode: args[1],
                        schoolName: args[2],
                        licenseType: args[3].ToLowerInvariant()
                    );
                    break;
                case "renew":
                    if (args.Length < 3) { ShowHelp(); return; }
                    GenerateRenewal(
                        requestCode: args[1],
                        existingLicPath: args[2]
                    );
                    break;
                case "info":
                    if (args.Length < 2) { ShowHelp(); return; }
                    ShowLicenseInfo(args[1]);
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        static void RunInteractiveMenu()
        {
            Console.WriteLine("\n=================================================");
            Console.WriteLine(" CHẾ ĐỘ TƯƠNG TÁC (INTERACTIVE MODE)");
            Console.WriteLine("=================================================");
            while (true)
            {
                Console.WriteLine("\nChọn chức năng:");
                Console.WriteLine("1. genkeys  - Tạo cặp khóa bảo mật mới");
                Console.WriteLine("2. activate - Cấp file bản quyền cho Trường mới");
                Console.WriteLine("3. renew    - Gia hạn file bản quyền cũ");
                Console.WriteLine("4. info     - Xem thông tin file bản quyền (.lic)");
                Console.WriteLine("0. Thoát");
                Console.Write("\nNhập lựa chọn (0-4): ");
                
                var choice = Console.ReadLine();
                Console.WriteLine();
                
                switch (choice)
                {
                    case "1":
                        GenerateKeys();
                        break;
                    case "2":
                        Console.Write("Nhập Mã máy (Request Code) [VD: QASC-1234-5678]: ");
                        var req = Console.ReadLine()?.Trim();
                        Console.Write("Nhập Tên Trường [VD: THPT Bui Thi Xuan]: ");
                        var school = Console.ReadLine()?.Trim();
                        Console.Write($"Nhập Loại Gói [{string.Join(", ", Packages.Keys)}]: ");
                        var type = Console.ReadLine()?.Trim();
                        
                        if (!string.IsNullOrEmpty(req) && !string.IsNullOrEmpty(school) && !string.IsNullOrEmpty(type))
                        {
                            GenerateActivation(req, school, type);
                        }
                        else
                        {
                            Console.WriteLine("❌ Thiếu thông tin. Đã hủy bỏ.");
                        }
                        break;
                    case "3":
                        Console.Write("Nhập Mã máy (Request Code): ");
                        var rReq = Console.ReadLine()?.Trim();
                        Console.Write("Nhập tên đường dẫn file license cũ [VD: license_cu.lic]: ");
                        var rFile = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(rReq) && !string.IsNullOrEmpty(rFile))
                        {
                            GenerateRenewal(rReq, rFile);
                        }
                        break;
                    case "4":
                        Console.Write("Nhập tên đường dẫn file license [VD: license.lic]: ");
                        var infoFile = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(infoFile))
                        {
                            ShowLicenseInfo(infoFile);
                        }
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("❌ Lựa chọn không hợp lệ!");
                        break;
                }
            }
        }

        static void GenerateKeys()
        {
            if (File.Exists(PRIVATE_KEY_PATH) || File.Exists(PUBLIC_KEY_PATH))
            {
                Console.WriteLine("⚠️ Phát hiện cặp key đã tồn tại! Ghi đè sẽ làm HỎNG toàn bộ license đã cấp.");
                Console.Write("Bạn có THỰC SỰ muốn tạo mới? (Type 'YES'): ");
                if (Console.ReadLine() != "YES")
                {
                    Console.WriteLine("Đã hủy bỏ.");
                    return;
                }
            }

            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKey = Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey());
            var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

            File.WriteAllText(PRIVATE_KEY_PATH, privateKey);
            File.WriteAllText(PUBLIC_KEY_PATH, publicKey);

            Console.WriteLine("✅ Đã tạo cặp key ECDSA P-256 thành công.");
            Console.WriteLine($"   Private: {PRIVATE_KEY_PATH} (GIỮ BÍ MẬT!)");
            Console.WriteLine($"   Public:  {PUBLIC_KEY_PATH} (nhúng vào app)");
            Console.WriteLine($"\n📋 COPY Public Key này vào hằng số PUBLIC_KEY_BASE64 trong LicenseService.cs:\n");
            Console.WriteLine(publicKey);
        }

        static void GenerateActivation(string requestCode, string schoolName, string licenseType)
        {
            if (!File.Exists(PRIVATE_KEY_PATH))
            {
                Console.WriteLine($"❌ Không tìm thấy {PRIVATE_KEY_PATH}. Vui lòng chạy lệnh 'genkeys' trước.");
                return;
            }

            if (!Packages.TryGetValue(licenseType, out var pkg))
            {
                Console.WriteLine($"❌ Gói '{licenseType}' không hợp lệ.");
                Console.WriteLine($"   Chọn: {string.Join(", ", Packages.Keys)}");
                return;
            }

            var now = DateTime.UtcNow;
            
            // Build license JSON
            var license = new
            {
                version = 2,
                hardware_hash = requestCode,
                school_name = schoolName,
                school_id = $"SCH-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                customer_id = $"CUST-{now:yyyy}-{Random.Shared.Next(1, 9999):D4}",
                license_type = licenseType,
                features = pkg.features,
                max_students = pkg.maxStudents,
                contract_start = now,
                initial_years = pkg.initialYears,
                issued_at = now,
                expires_at = now.AddDays(pkg.initialDays),
                renewal_years = 1,
                grace_period_days = pkg.graceDays,
                reactivation_count = 0,
                max_reactivations = 3,
                renewal_history = new List<object>()
            };

            var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = false });
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            // Sign
            var privateKeyB64 = File.ReadAllText(PRIVATE_KEY_PATH).Trim();
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyB64), out _);
            var signature = ecdsa.SignData(jsonBytes, HashAlgorithmName.SHA256);

            // Output format: Base64(JSON).Base64(Signature)
            var activationCode = Convert.ToBase64String(jsonBytes) + "." + Convert.ToBase64String(signature);

            // Print
            Console.WriteLine("\n✅ MÃ KÍCH HOẠT (ACTIVATION CODE) ĐÃ TẠO:\n");
            Console.WriteLine($"   Mã máy: {requestCode}");
            Console.WriteLine($"   Trường: {schoolName}");
            Console.WriteLine($"   Gói:    {licenseType.ToUpper()} ({pkg.initialDays} ngày)");
            Console.WriteLine($"   Hạn:    {license.expires_at:dd/MM/yyyy}");
            Console.WriteLine("\n📋 CODE (Copy gửi cho khách / dán vào file .lic):\n");
            Console.WriteLine(activationCode);

            // Save .lic file
            var cleanName = string.Join("_", schoolName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            var fileName = $"license_{cleanName}_{now:yyyyMMdd}.lic";
            File.WriteAllText(fileName, activationCode);
            Console.WriteLine($"\n💾 Đã lưu backup: {fileName}");

            LogHistory("ACTIVATE", requestCode, schoolName, licenseType, license.expires_at.ToString("yyyy-MM-dd"));
        }

        static void GenerateRenewal(string requestCode, string existingLicPath)
        {
            if (!File.Exists(PRIVATE_KEY_PATH))
            {
                Console.WriteLine($"❌ Không tìm thấy {PRIVATE_KEY_PATH}. Vui lòng chạy lệnh 'genkeys' trước.");
                return;
            }

            if (!File.Exists(existingLicPath))
            {
                Console.WriteLine($"❌ Không tìm thấy file license cũ: {existingLicPath}");
                return;
            }

            var oldCode = File.ReadAllText(existingLicPath).Trim();
            var parts = oldCode.Split('.');
            if (parts.Length != 2)
            {
                Console.WriteLine("❌ File license cũ không hợp lệ (không đúng format 2 phần).");
                return;
            }

            // Decode old json
            string oldJson;
            try
            {
                oldJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            }
            catch
            {
                Console.WriteLine("❌ Không thể decode Base64 phần payload JSON.");
                return;
            }

            var oldLicense = JsonSerializer.Deserialize<JsonElement>(oldJson);

            // Kiểm tra requestCode khớp file cũ không
            var oldHash = oldLicense.GetProperty("hardware_hash").GetString();
            if (oldHash != requestCode)
            {
                Console.WriteLine($"⚠️ CẢNH BÁO: Request Code nhập vào ({requestCode}) KHÁC với Request Code trong file cũ ({oldHash}).");
                Console.WriteLine($"   (Việc gia hạn sẽ dùng Request Code mới này, tương đương Re-activate).");
            }

            var schoolName = oldLicense.GetProperty("school_name").GetString();
            var oldExpiry = oldLicense.GetProperty("expires_at").GetDateTime();
            
            // Tính hạn mới: cộng thêm 1 năm (từ hạn cũ, hoặc từ hôm nay nếu đã hết hạn lâu)
            var baseDate = (oldExpiry < DateTime.UtcNow ? DateTime.UtcNow : oldExpiry);
            var newExpiry = baseDate.AddYears(1);

            // Lịch sử gia hạn
            var renewalHistory = new List<object>();
            if (oldLicense.TryGetProperty("renewal_history", out var oldHistory))
            {
                foreach (var item in oldHistory.EnumerateArray())
                {
                    renewalHistory.Add(item);
                }
            }

            renewalHistory.Add(new
            {
                renewed_at = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                extended_to = newExpiry.ToString("yyyy-MM-dd"),
                by = Environment.UserName
            });

            // Rebuild JSON (version 2)
            var license = new
            {
                version = 2,
                hardware_hash = requestCode,
                school_name = schoolName,
                school_id = oldLicense.GetProperty("school_id").GetString(),
                customer_id = oldLicense.GetProperty("customer_id").GetString(),
                license_type = oldLicense.GetProperty("license_type").GetString(),
                features = oldLicense.GetProperty("features").EnumerateArray().Select(x => x.GetString()).ToList(),
                max_students = oldLicense.GetProperty("max_students").GetInt32(),
                contract_start = oldLicense.GetProperty("contract_start").GetDateTime(),
                initial_years = oldLicense.GetProperty("initial_years").GetInt32(),
                issued_at = DateTime.UtcNow,
                expires_at = newExpiry,
                renewal_years = 1,
                grace_period_days = oldLicense.GetProperty("grace_period_days").GetInt32(),
                reactivation_count = (oldHash != requestCode) ? oldLicense.GetProperty("reactivation_count").GetInt32() + 1 : oldLicense.GetProperty("reactivation_count").GetInt32(),
                max_reactivations = oldLicense.GetProperty("max_reactivations").GetInt32(),
                renewal_history = renewalHistory
            };

            var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = false });
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            // Sign
            var privateKeyB64 = File.ReadAllText(PRIVATE_KEY_PATH).Trim();
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyB64), out _);
            var signature = ecdsa.SignData(jsonBytes, HashAlgorithmName.SHA256);

            // Output
            var renewalCode = Convert.ToBase64String(jsonBytes) + "." + Convert.ToBase64String(signature);

            Console.WriteLine("\n✅ MÃ GIA HẠN (RENEWAL CODE) ĐÃ TẠO:\n");
            Console.WriteLine($"   Trường:    {schoolName}");
            Console.WriteLine($"   Gói:       {license.license_type.ToUpper()}");
            Console.WriteLine($"   Hạn cũ:    {oldExpiry:dd/MM/yyyy}");
            Console.WriteLine($"   Hạn mới:   {newExpiry:dd/MM/yyyy} (+1 năm)");
            Console.WriteLine($"   Lần renew: thứ {renewalHistory.Count}");
            if (oldHash != requestCode)
            {
                Console.WriteLine($"   Re-activate: Lần {license.reactivation_count} / {license.max_reactivations}");
            }
            Console.WriteLine("\n📋 RENEWAL CODE:\n");
            Console.WriteLine(renewalCode);

            // Save .lic file
            var cleanName = string.Join("_", schoolName!.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            var fileName = $"renewal_{cleanName}_{DateTime.Now:yyyyMMdd}.lic";
            File.WriteAllText(fileName, renewalCode);
            Console.WriteLine($"\n💾 Đã lưu backup: {fileName}");

            LogHistory("RENEW", requestCode, schoolName, license.license_type, newExpiry.ToString("yyyy-MM-dd"));
        }

        static void ShowLicenseInfo(string licPath)
        {
            if (!File.Exists(licPath))
            {
                Console.WriteLine($"❌ Không tìm thấy file: {licPath}");
                return;
            }

            var code = File.ReadAllText(licPath).Trim();
            var parts = code.Split('.');
            if (parts.Length != 2)
            {
                Console.WriteLine("❌ File license không hợp lệ (không đúng format 2 phần).");
                return;
            }

            try
            {
                var jsonBytes = Convert.FromBase64String(parts[0]);
                var json = Encoding.UTF8.GetString(jsonBytes);
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                var formattedJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(json), options);

                Console.WriteLine("\n📄 THÔNG TIN FILE LICENSE:\n");
                Console.WriteLine(formattedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi decode file: {ex.Message}");
            }
        }

        static void LogHistory(string action, string requestCode, string school, string type, string expiry)
        {
            try
            {
                bool isNew = !File.Exists(HISTORY_FILE);
                using var sw = File.AppendText(HISTORY_FILE);
                if (isNew)
                {
                    sw.WriteLine("Timestamp,Action,RequestCode,School,Type,ExpiryDate,User");
                }
                sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{action},{requestCode},\"{school}\",{type},{expiry},{Environment.UserName}");
            }
            catch { /* Ignore log error */ }
        }

        static void ShowHelp()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("  QA SmartClass — License Key Generator v2.1");
            Console.WriteLine("=================================================");
            Console.WriteLine("\nCÁC LỆNH HỖ TRỢ:\n");
            Console.WriteLine("  1. genkeys");
            Console.WriteLine("     Tạo cặp private/public key ECDSA P-256 mới.");
            Console.WriteLine("\n  2. activate <request_code> \"<school_name>\" <type>");
            Console.WriteLine("     Tạo mã kích hoạt cho trường mới.");
            Console.WriteLine("     Các gói (type): trial_7, trial_30, basic, standard, premium, enterprise");
            Console.WriteLine("\n  3. renew <request_code> <old_license.lic>");
            Console.WriteLine("     Gia hạn thêm 1 năm dựa trên file license cũ của khách hàng.");
            Console.WriteLine("\n  4. info <license.lic>");
            Console.WriteLine("     Đọc và hiển thị nội dung bên trong file license (JSON).");
            
            Console.WriteLine("\nVÍ DỤ:\n");
            Console.WriteLine("  keygen genkeys");
            Console.WriteLine("  keygen activate QASC-A1B2-C3D4-E5F6 \"THCS Nguyen Du\" trial_30");
            Console.WriteLine("  keygen activate QASC-A1B2-C3D4-E5F6 \"THCS Nguyen Du\" premium");
            Console.WriteLine("  keygen renew QASC-A1B2-C3D4-E5F6 license_THCS_Nguyen_Du_20260512.lic");
            Console.WriteLine("  keygen info license_THCS_Nguyen_Du_20260512.lic");
            Console.WriteLine();
        }
    }
}
