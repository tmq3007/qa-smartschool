# Đánh giá chuyển đổi SQLite → PostgreSQL
> Ngày lập: 14/05/2026  
> Dự án: QA SmartSchool v4.0

---

## 1. Tình trạng hiện tại (SQLite)

| Tiêu chí | Giá trị |
|---|---|
| **Engine** | SQLite 3 (file-based) |
| **ORM** | Entity Framework Core 8 |
| **Provider** | `Microsoft.EntityFrameworkCore.Sqlite` |
| **File** | `%LOCALAPPDATA%/QASmartClass/smartclass.db` |
| **Số bảng** | 25+ |
| **Kích thước DB** | ~5-20 MB (tùy dữ liệu) |
| **Concurrent writes** | Giới hạn (file lock) |

### Ưu điểm SQLite
- ✅ Zero-config, không cần cài server
- ✅ Portable — copy 1 file là xong
- ✅ Phù hợp single-user/single-machine
- ✅ Deploy đơn giản cho trường nhỏ

### Hạn chế SQLite
- ❌ Không hỗ trợ > 30 user ghi đồng thời
- ❌ Không có network access (chỉ local)
- ❌ Thiếu advanced features (stored proc, triggers phức tạp)
- ❌ Backup khi đang ghi → có thể corrupt

---

## 2. Khi nào cần chuyển PostgreSQL

| Kịch bản | Cần chuyển? |
|---|---|
| 1 phòng học, 1 GV + 40 HS trên cùng máy | ❌ Không |
| 1 phòng học, GV và HS trên máy riêng qua LAN | ⚠️ Nên xem xét |
| Nhiều phòng học, dữ liệu tập trung 1 server | ✅ Cần |
| Triển khai toàn trường (500+ user) | ✅ Bắt buộc |
| Cần Web Portal cho Phụ huynh (Module 5) | ✅ Cần (API server) |

---

## 3. Các điểm cần thay đổi

### 3.1. Connection String
```csharp
// SQLite (hiện tại)
optionsBuilder.UseSqlite("Data Source=smartclass.db");

// PostgreSQL (mục tiêu)
optionsBuilder.UseNpgsql("Host=localhost;Database=smartclass;Username=qa;Password=xxx");
```

### 3.2. NuGet Package
```diff
- Microsoft.EntityFrameworkCore.Sqlite
+ Npgsql.EntityFrameworkCore.PostgreSQL
```

### 3.3. LINQ queries cần sửa

| Pattern | SQLite | PostgreSQL |
|---|---|---|
| `.Date` property | ⚠️ Client-side eval | ✅ Native support |
| String comparison | Case-sensitive default | Case-sensitive (dùng `ILIKE` cho insensitive) |
| DateTime format | Text storage | Native `timestamp` |
| Auto-increment | `AUTOINCREMENT` | `SERIAL` / `IDENTITY` |

### 3.4. AppDbContext cần hỗ trợ dual-mode
```csharp
// Đề xuất: AppDbContext hỗ trợ cả SQLite và PostgreSQL
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    var provider = AppSettings.DatabaseProvider; // "sqlite" hoặc "postgresql"
    if (provider == "postgresql")
        options.UseNpgsql(AppSettings.PostgresConnectionString);
    else
        options.UseSqlite($"Data Source={AppPaths.DatabaseFile}");
}
```

---

## 4. Kế hoạch chuyển đổi (nếu thực hiện)

### Giai đoạn 1: Chuẩn bị (1 ngày)
1. Cài Docker Desktop + PostgreSQL container
2. Thêm NuGet `Npgsql.EntityFrameworkCore.PostgreSQL`
3. Tạo `appsettings.json` với cấu hình provider

### Giai đoạn 2: Migrate (2 ngày)
1. Tạo migration cho PostgreSQL: `dotnet ef migrations add InitPostgres --provider Npgsql`
2. Sửa các LINQ query dùng `.Date`, string comparison
3. Test toàn bộ 25+ bảng

### Giai đoạn 3: Data migration (1 ngày)
1. Export SQLite → CSV/JSON
2. Import vào PostgreSQL
3. Verify data integrity

### Giai đoạn 4: Deploy (1 ngày)
1. Cài PostgreSQL trên server trường
2. Cấu hình firewall/port
3. Test multi-user concurrent access

---

## 5. Khuyến nghị

> **Hiện tại:** Giữ SQLite cho development và triển khai đơn lẻ (1 phòng).  
> **Khi triển khai toàn trường:** Chuyển PostgreSQL với dual-mode AppDbContext.  
> **Ưu tiên:** Hoàn thành Module 5 (Parent Portal) trước — đây là trigger chính để cần server DB.

---

## 6. Rủi ro & Giải pháp

| Rủi ro | Xác suất | Giải pháp |
|---|---|---|
| LINQ queries không tương thích | Trung bình | Test kỹ từng query, dùng `.ToList()` trước xử lý client |
| Performance giảm do network latency | Thấp | Connection pooling, caching |
| IT trường không biết cài PostgreSQL | Cao | Docker Compose one-click setup |
| Mất dữ liệu khi migrate | Thấp | Backup trước, verify sau |
