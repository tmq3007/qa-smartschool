# QA Smart Touch (`QASmartTouch`) — Hiến pháp Dự án & Giao thức Vibe Coding

## 🎯 1. MỤC TIÊU SẢN PHẨM & CẤU TRÚC MÃ NGUỒN LIÊN KẾT (STANDALONE TARGET & LINKED SOURCE)

### [RULE] QST_TARGET_SCOPE: Tập trung 100% cho `QASmartTouch` Độc lập
1. **Đích biên dịch & phát hành duy nhất:**
   * Mọi phân tích, thiết kế, sửa lỗi và phát triển tính năng đều phục vụ sản phẩm độc lập **`QASmartTouch/QASmartTouch.csproj`** (`.NET 9.0-windows`, `x64`, biên dịch với hằng số `SMARTTOUCH_STANDALONE;NO_SMARTSCHOOL`).
   * **Không** cần biên dịch hay tối ưu cho các phân hệ quản lý lớp học mạng LAN của `QASmartClass`. Tuyệt đối không đưa các thư viện chỉ thuộc `QASmartClass` (EF Core, SQLite, mạng học sinh, `SmartLibrary.Desktop`) vào mã nguồn của `QASmartTouch`.
2. **Đặc thù Lưu trữ Vật lý Liên kết (`Linked Source Files`):**
   * Phần lớn tệp `.cs` và `.xaml` của Bảng viết hiện đặt vật lý tại thư mục `QASmartClass/` và được nhúng sang `QASmartTouch/QASmartTouch.csproj` qua thẻ `<Compile Include="..\QASmartClass\..." Link="..." />` và `<Page Include="..\QASmartClass\..." Link="..." />`.
   * **Ràng buộc sống còn khi thêm tệp mới:**
     * Các thư mục đã có wildcard tự động trong `QASmartTouch.csproj`: `WhiteboardCore\**\*.cs`, `Controls\**\*`, `Handlers\**\*.cs`, `Managers\**\*.cs`, `Models\**\*.cs`, `Services\Camera\**\*.cs`, `Services\Charts\**\*.cs`, `Services\Canvas\**\*.cs`, `Services\Tools\**\*.cs`, và `Forms\Form2_*`.
     * Nếu tạo tệp `.cs` / `.xaml` mới trực tiếp trong `QASmartClass/Services/` (cấp gốc), `QASmartClass/Shared/`, hoặc hộp thoại trong `QASmartClass/Forms/` (không mang tiền tố `Form2_`), **BẮT BUỘC** phải mở `QASmartTouch/QASmartTouch.csproj` khai báo thẻ `<Compile>` / `<Page>` tương ứng.
   * Nếu chạm vào đoạn code dùng chung có chứa lời gọi sang hệ thống lớp học cũ, bắt buộc giữ hoặc bọc trong `#if !SMARTTOUCH_STANDALONE`.

---

## ⚡ 2. GIAO THỨC VIBE CODING LINH HOẠT 2 CẤP ĐỘ (2-TIER EXECUTION PROTOCOL)

### [RULE] QST_EXECUTION_TIERS: Phân cấp Tốc độ vs. An toàn Kiến trúc
Trước mỗi yêu cầu của người dùng, AI phải tự phân loại chính xác vào 1 trong 2 cấp độ sau:

1. **🟢 CẤP ĐỘ 1 — `FAST-TRACK` (Thực thi ngay, không rườm rà):**
   * **Điều kiện áp dụng:** Tinh chỉnh giao diện XAML cục bộ (màu sắc, kích thước, căn lề, chữ hiển thị, icon), sửa lỗi nhỏ rõ ràng gói gọn trong **1 tệp**, **KHÔNG** thay đổi luồng sự kiện Chuột/Cảm ứng/Stylus, chuyển đổi công cụ, Chọn vùng, Biến đổi hình học hay Undo/Redo.
   * **Quy trình:** Kiểm tra đúng tệp XAML/C# đang active $\rightarrow$ Sửa gọn trực tiếp $\rightarrow$ Chạy `dotnet build QASmartTouch/QASmartTouch.csproj` $\rightarrow$ Báo cáo ngắn gọn kết quả.

2. **🔴 CẤP ĐỘ 2 — `ARCHITECT GATE` (Phân tích Toàn diện & Chờ Duyệt trước khi Sửa):**
   * **Điều kiện áp dụng:** Mọi yêu cầu sửa lỗi hoặc thêm tính năng đụng tới **Logic Bảng vẽ, Cảm ứng/Chuột/Bút/Tẩy, Chuyển đổi Công cụ, Chọn vùng/Xoay/Lật/Resize/Đổi màu, Undo/Redo, Lưu/Mở `.qasc`, Xuất PDF/Ảnh, Quay video/Camera, hoặc can thiệp từ 2 tệp trở lên** (trừ khi người dùng ra lệnh rõ *"sửa luôn không cần hỏi"*).
   * **Quy trình bắt buộc:** **TUYỆT ĐỐI KHÔNG SỬA FILE NGAY.** Kích hoạt các Skill chuyên ngành liên quan và trình bày bản thiết kế súc tích gồm:
     1. **Nguyên nhân gốc & Luồng sự kiện/trạng thái** (kèm link tới đúng file và tên hàm thực tế).
     2. **Đánh giá Tác động 4 Cổng Kỹ thuật** (`architecture_gates.md`) — đặc biệt chỉ rõ cách phòng tránh lỗi dây chuyền sang Công cụ khác và Undo/Redo.
     3. **Kế hoạch can thiệp phẫu thuật (Surgical Plan)** — liệt kê chính xác tệp và hàm dự kiến sửa.
     $\rightarrow$ **Dừng lại chờ người dùng xác nhận ("OK" / "Tiến hành")** rồi mới áp dụng thay đổi vào mã nguồn.

---

## ✅ 3. CHỐT CHẶN HOÀN THÀNH BẮT BUỘC (DEFINITION OF DONE)

### [RULE] QST_BUILD_AND_SMOKE_CHECKLIST: Biên dịch 0 Lỗi & Kịch bản Test Tay 30 Giây
Sau **MỌI** lần chỉnh sửa mã nguồn `.cs` / `.xaml` / `.csproj`:
1. **Kiểm chứng Biên dịch Cứng:** Bắt buộc chạy lệnh `dotnet build QASmartTouch/QASmartTouch.csproj` và đảm bảo kết quả **`0 Error(s)`** trước khi kết thúc lượt trả lời. Nếu có lỗi biên dịch, phải xử lý triệt để ngay.
2. **Checklist Test Tay Hồi Quy 30 Giây:** Vì dự án kiểm thử thực tế qua thao tác tay của người phát triển, cuối mỗi báo cáo hoàn thành sửa code, AI **BẮT BUỘC** cung cấp danh sách **3–5 bước Test Tay Nhanh**:
   * **1–2 bước kiểm tra trực tiếp** lỗi vừa sửa / tính năng vừa thêm.
   * **2–3 bước kiểm tra hồi quy liên đới** nhắm đúng vào các công cụ dễ bị ảnh hưởng chéo nhất (ví dụ: *"Thử chuyển qua Tẩy rồi quay lại Chọn vùng"*, *"Thử bấm Undo $\rightarrow$ Redo rồi kéo di chuyển lại đối tượng"*).

---

## 🔤 4. QUY CHUẨN NGÔN NGỮ & THƯƠNG HIỆU (BRANDING STANDARDS)

### [RULE] QST_LANGUAGE_BRANDING: Bảo toàn Từ khóa Thương hiệu
* Giữ nguyên Tiếng Anh cho các nhãn chế độ hoạt động cốt lõi mang tính thương hiệu hệ thống: **`SMART TOUCH`**, **`DESKTOP`** (và **`SMART CLASS`** nếu xuất hiện trong hằng số/enum chuyển chế độ dùng chung). Không tự ý Việt hóa các từ khóa này.
