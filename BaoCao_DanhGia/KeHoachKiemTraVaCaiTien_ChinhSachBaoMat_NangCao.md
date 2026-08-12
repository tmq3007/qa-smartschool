# KẾ HOẠCH NÂNG CẤP BẢO MẬT NÂNG CAO & ĐỒNG BỘ CHƯƠNG TRÌNH - GIÁO VIÊN 6.2. CHÍNH SÁCH BẢO MẬT
*Tài liệu đặc tả kiến trúc nâng cấp bảo mật nâng cao, thiết kế Presets và bảng checksheet kiểm thử ngăn ngừa lỗi*

---

## I. MỤC TIÊU & PHẠM VI HỆ THỐNG
Dựa trên báo cáo đánh giá toàn diện từ 15 vai trò chuyên môn, tài liệu này đặc tả kế hoạch thực hiện **Giai đoạn 4: Nâng cấp Bảo mật Nâng cao & Trải nghiệm Presets** cho trang **Chính sách bảo mật** trong hệ thống **QA SmartClass**. 

Mục tiêu là tăng cường lớp phòng thủ mạng/hệ thống phía học sinh (chặn lách qua màn hình ảo) và tối ưu hóa tối đa trải nghiệm sư phạm (mẫu chính sách nhanh, gợi ý whitelist phần mềm, và tự động khóa trang idle).

Phạm vi nâng cấp đồng bộ trực tiếp với các tệp tin trong cấu trúc chương trình:
*   [KeyboardHookHelper.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Services/KeyboardHookHelper.cs) (Chặn màn hình ảo)
*   [PolicyPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/PolicyPage.xaml) (Giao diện Presets, gợi ý Whitelist và Idle lock)
*   [PolicyPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/PolicyPage.xaml.cs) (Logic Presets, gợi ý Whitelist và Idle lock)

---

## II. ĐẶC TẢ CHI TIẾT CÁC PHÂN HỆ CẢI TIẾN (REQUIREMENTS & IMPLEMENTATION DESIGN)

### 1. Chặn màn hình ảo phía Học sinh (Virtual Desktop Shortcuts Block)
*   **Mô tả yêu cầu:** Học sinh giỏi công nghệ hoặc các game thủ học đường có thể lách qua Hook bàn phím hiện tại bằng cách nhấn phím tắt Windows để tạo màn hình ảo (Virtual Desktop), di chuyển game sang màn hình ảo đó để chơi mà không bị giáo viên phát hiện. Cần chặn triệt để các phím tắt hệ thống liên quan đến Màn hình ảo.
*   **Dữ liệu đầu vào (Input):** Các sự kiện gõ phím vật lý gửi qua Low-level Keyboard Hook.
*   **Dữ liệu đầu ra (Output):** Trả về `new IntPtr(1)` (chặn đứng sự kiện truyền đến Windows) nếu tổ hợp phím khớp với phím tắt màn hình ảo.
*   **Phương pháp thực hiện:**
    *   Cập nhật hàm callback sự kiện bàn phím [HookCallback](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Services/KeyboardHookHelper.cs#L85-L125).
    *   Kiểm tra trạng thái phím Windows trái/phải (`VK_LWIN` = 0x5B, `VK_RWIN` = 0x5C) bằng cách kết hợp `GetAsyncKeyState`.
    *   *Mã nguồn chuẩn hóa chặn Virtual Desktops:*
        ```csharp
        // 5. Chặn Win + Tab (Tab = 9, LWin/RWin đang nhấn)
        bool winPressed = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
        if (vkCode == 9 && winPressed)
        {
            Log.Information("[KeyboardHook] Blocked Win + Tab Virtual Desktop Switch.");
            return new IntPtr(1);
        }

        // 6. Chặn Win + Ctrl + D (Tạo màn hình ảo mới, D = 68)
        // 7. Chặn Win + Ctrl + F4 (Đóng màn hình ảo, F4 = 115)
        // 8. Chặn Win + Ctrl + Left/Right (Chuyển màn hình ảo, Left = 37, Right = 39)
        if (winPressed && ctrl)
        {
            if (vkCode == 68 || vkCode == 115 || vkCode == 37 || vkCode == 39)
            {
                Log.Information("[KeyboardHook] Blocked Virtual Desktop Shortcut: VK {Code}", vkCode);
                return new IntPtr(1);
            }
        }
        ```
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Tại sao không vô hiệu hóa luôn tính năng Virtual Desktop qua Group Policy hoặc Registry?
    *   *Tối ưu:* Can thiệp Registry yêu cầu quyền Administrator cao nhất (System) và có thể gây hư hại hoặc thay đổi cấu hình hệ điều hành Windows vĩnh viễn trên máy học sinh. Hook bàn phím ở tầng User qua `SetWindowsHookEx` là giải pháp an toàn, chỉ hoạt động khi ứng dụng client chạy và tự động phục hồi khi thoát app.

---

### 2. Mẫu thiết lập Chính sách bảo mật nhanh (Policy Presets / Templates)
*   **Mô tả yêu cầu:** Giáo viên cần áp dụng các kịch bản thiết lập bảo mật khác nhau một cách nhanh chóng tùy theo loại tiết học mà không cần tích chọn thủ công từng checkbox mỗi lần lên lớp. Ví dụ: Mẫu "Thi cử" (Khóa mạng, khóa USB, chặn app), Mẫu "Thực hành" (Chỉ cho phép whitelist app).
*   **Dữ liệu đầu vào (Input):**
    *   Chọn một mẫu chính sách từ ComboBox `cboPresets`.
    *   Nhập tên mẫu mới và nhấn nút `Lưu mẫu`.
*   **Dữ liệu đầu ra (Output):**
    *   Trạng thái Checked/Unchecked của tất cả Checkbox thay đổi đồng loạt theo mẫu đã lưu.
    *   Mẫu mới được lưu vào SQLite dưới khóa `Policy_Template_{TemplateName}`.
*   **Phương pháp thực hiện:**
    *   Thiết kế giao diện ComboBox và một nút Lưu mẫu ở Header hoặc đầu cột trái trong [PolicyPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/PolicyPage.xaml).
    *   Xây dựng hàm `ApplyPreset(string presetName)` để nạp cấu hình và gán trực tiếp lên các Checkbox.
    *   *Mã nguồn chuẩn hóa:*
        ```csharp
        private void cboPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboPresets.SelectedItem is string presetName)
            {
                if (presetName == "Mặc định (Mở khóa)") { SetAllCheckboxes(false); return; }
                if (presetName == "Thi cử nghiêm ngặt") { SetStrictExamPreset(); return; }
                if (presetName == "Thực hành Tin học") { SetLabPracticePreset(); return; }
                LoadCustomPresetFromDb(presetName);
            }
        }
        ```
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Việc thay đổi checkbox đồng loạt bằng code có kích hoạt các sự kiện Checked/Unchecked của Checkbox gây xung đột loại trừ lẫn nhau không?
    *   *Tối ưu:* Để tránh việc vòng lặp logic kích hoạt lẫn nhau, coder phải sử dụng một biến cờ hiệu `_isBindingPreset = true;` trước khi thay đổi trạng thái CheckBox, và đặt lại `_isBindingPreset = false;` sau khi hoàn tất. Các hàm xử lý sự kiện Checked chỉ chạy logic loại trừ khi `_isBindingPreset == false`.

---

### 3. Gợi ý Whitelist ứng dụng giáo dục bằng 1 click (Suggested Educational Apps)
*   **Mô tả yêu cầu:** Giảm thiểu thao tác gõ chữ cho giáo viên. Tích hợp các nút (badge) gợi ý sẵn các phần mềm học tập phổ biến (Canva, Scratch, Duolingo, MS Office) để giáo viên click thêm nhanh vào whitelist.
*   **Dữ liệu đầu vào (Input):** Click chuột vào các nút ứng dụng gợi ý trong Popup Whitelist.
*   **Dữ liệu đầu ra (Output):** Tên tiến trình tương ứng (ví dụ: `scratch.exe` hoặc `chrome.exe`) được thêm vào danh sách ListBox whitelist.
*   **Phương pháp thực hiện:**
    *   Trong [PolicyPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/PolicyPage.xaml) (Popup Whitelist), bổ sung một `<WrapPanel>` chứa các Button phẳng nhỏ gọn làm badge gợi ý:
    ```xml
    <WrapPanel Grid.Row="2" Margin="0,0,0,10">
        <Button Content="Scratch" Click="SuggestApp_Click" Tag="scratch.exe" Margin="0,0,5,5"/>
        <Button Content="PowerPoint" Click="SuggestApp_Click" Tag="powerpoint.exe" Margin="0,0,5,5"/>
        <Button Content="Word" Click="SuggestApp_Click" Tag="winword.exe" Margin="0,0,5,5"/>
        <Button Content="Trình duyệt Google" Click="SuggestApp_Click" Tag="chrome.exe" Margin="0,0,5,5"/>
    </WrapPanel>
    ```
    *   Trong code-behind: thêm phần tử của thuộc tính `Tag` vào `lstWhitelistApps` sau khi kiểm tra trùng lặp.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Trình duyệt `chrome.exe` nếu được Whitelist sẽ cho phép học sinh mở bất cứ trang web nào bao gồm cả game web.
    *   *Tối ưu:* Khi giáo viên whitelist `chrome.exe`, cần đưa ra một dòng nhắc nhở nhỏ: *"Lưu ý: Whitelist trình duyệt sẽ mở quyền truy cập web. Vui lòng kết hợp với chính sách 'Chỉ cho phép truy cập các trang giáo dục (.edu.vn)' để giới hạn nội dung web."*

---

### 4. Tự động khóa trang Giáo viên khi không hoạt động (Teacher Page Idle Auto-Lock)
*   **Mô tả yêu cầu:** Bảo vệ trang thiết lập chính sách khi giáo viên rời bàn giảng bài. Nếu không phát hiện thao tác chuột hoặc phím trên trang trong vòng 5 phút, hệ thống tự động hiển thị một lớp Grid overlay che phủ toàn bộ nội dung, yêu cầu nhập mã PIN lớp học để tiếp tục sử dụng.
*   **Dữ liệu đầu vào (Input):** Các sự kiện `MouseMove`, `MouseDown`, `KeyDown` trên phạm vi Page.
*   **Dữ liệu đầu ra (Output):** Hiển thị overlay khóa màn hình (`gridIdleLock`).
*   **Phương pháp thực hiện:**
    *   Sử dụng một `DispatcherTimer` với chu kỳ kiểm tra 1 giây.
    *   Lưu trữ biến `_idleTime` (giây). Mỗi giây tăng lên 1. Nếu giáo viên di chuyển chuột hoặc gõ phím, reset `_idleTime = 0`.
    *   Nếu `_idleTime >= 300` (5 phút), đặt `gridIdleLock.Visibility = Visibility.Visible`.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Việc timer chạy liên tục kiểm tra sự kiện Move chuột có gây quá tải CPU của máy giáo viên không?
    *   *Tối ưu:* Sự kiện MouseMove chỉ cập nhật một biến thời gian số nguyên cực kỳ đơn giản (`_lastActivity = DateTime.Now`), do đó chi phí CPU gần như bằng 0. Tránh việc gọi các hàm xử lý giao diện nặng nề trong sự kiện MouseMove.

---

### 5. Tối ưu hóa đa luồng SQLite WAL & BusyTimeout (Thread-safe SQLite Config)
*   **Mô tả yêu cầu:** Tránh hiện tượng lỗi database lock (`Database is locked` hoặc `SqliteException`) khi giáo viên liên tục ghi log hoạt động trong khi hệ thống chạy đa luồng đồng bộ.
*   **Dữ liệu đầu vào (Input):** Connection String của cơ sở dữ liệu SQLite.
*   **Dữ liệu đầu ra (Output):** Kết nối SQLite được tối ưu hóa ở chế độ WAL (Write-Ahead Logging).
*   **Phương pháp thực hiện:**
    *   Trong phần khởi tạo DbContext hoặc cơ sở dữ liệu:
    ```csharp
    using (var connection = db.Database.GetDbConnection())
    {
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
            command.ExecuteNonQuery();
        }
    }
    ```
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Chế độ WAL có tạo ra các file tạm thời làm nặng thư mục ứng dụng không?
    *   *Tối ưu:* Chế độ WAL tạo ra hai file phụ `.db-shm` và `.db-wal`. Khi đóng ứng dụng an toàn, các file này sẽ tự động được gộp lại (checkpoint) và xóa đi. Lợi ích tăng tốc độ ghi đồng thời gấp 5-10 lần vượt trội hơn nhiều so với nhược điểm file tạm ngắn hạn này.

---

## III. BẢNG KIỂM TRA CHẤT LƯỢNG (TESTING CHECKSHEETS)

Dưới đây là bộ checksheet kiểm định chi tiết từng bước được thiết lập dành cho QA và Developer để xác minh tính đúng đắn trước khi phát hành phiên bản nâng cấp.

### 📋 BẢNG KIỂM TRA 1: KIỂM THỬ GIAO DIỆN & TÍNH NĂNG CƠ BẢN (PRESETS & SUGGESTIONS UI)
| STT | Bước thực hiện | Dữ liệu đầu vào (Input) | Dữ liệu đầu ra mong đợi (Expected Output) | Trạng thái | Ký xác nhận |
| :---: | :--- | :--- | :--- | :---: | :---: |
| **1.1** | Mở ComboBox Mẫu nhanh | Click chọn ComboBox Presets ở góc trái. | Hiển thị đầy đủ các lựa chọn mặc định: *"Thi cử nghiêm ngặt"*, *"Thực hành Tin học"*, *"Mặc định"*. | `[x]` | Developer |
| **1.2** | Chọn Mẫu "Thi cử nghiêm ngặt" | Chọn mẫu *"Thi cử nghiêm ngặt"*. | Các checkbox: *Chặn Internet*, *Chặn USB*, *Chặn cài đặt*, *Khóa desktop*, *Yêu cầu Exit PIN* tự động được tích chọn. | `[x]` | Developer |
| **1.3** | Tạo Mẫu tùy chỉnh mới | Tích chọn vài mục, nhấn nút `Lưu mẫu` và nhập tên mẫu `"KiemTra15P"`. | Mẫu `"KiemTra15P"` được lưu vào SQLite. Khi mở lại ComboBox, mẫu mới xuất hiện trong danh sách. | `[x]` | Developer |
| **1.4** | Thêm app gợi ý nhanh | Mở Popup Whitelist, click chọn badge `"Scratch"`. | Tiến trình `"scratch.exe"` tự động được thêm vào danh sách Whitelist hiển thị. | `[x]` | Developer |
| **1.5** | Cảnh báo khi Whitelist Chrome | Click chọn gợi ý `"Trình duyệt Google"`. | Hiển thị thông báo lưu ý giáo viên nên kết hợp với chặn miền `.edu.vn` để giới hạn web. | `[x]` | Developer |

---

### 📋 BẢNG KIỂM TRA 2: BIÊN, NGOẠI LỆ & BẢO MẬT HỆ THỐNG (SECURITY & IDLE AUTO-LOCK)
| STT | Bước thực hiện | Dữ liệu đầu vào (Input) | Dữ liệu đầu ra mong đợi (Expected Output) | Trạng thái | Ký xác nhận |
| :---: | :--- | :--- | :--- | :---: | :---: |
| **2.1** | Chặn phím tắt Win + Tab | Lock Desktop hoạt động, nhấn `Win + Tab` ở máy học sinh. | Màn hình Task view không hiển thị, sự kiện bị chặn hoàn toàn. | `[x]` | QA |
| **2.2** | Chặn tạo màn hình ảo mới | Nhấn `Win + Ctrl + D` ở máy học sinh. | Windows không tạo màn hình ảo mới, học sinh vẫn bị giữ tại màn hình khóa hiện tại. | `[x]` | QA |
| **2.3** | Chặn chuyển đổi màn hình ảo | Nhấn `Win + Ctrl + Left` hoặc `Win + Ctrl + Right`. | Không có sự chuyển dịch màn hình, sự kiện phím bị nuốt. | `[x]` | QA |
| **2.4** | Kiểm tra Idle Auto-Lock | Giáo viên mở trang bảo mật, không đụng vào chuột/phím trong 5 phút. | Lớp phủ khóa `gridIdleLock` xuất hiện, che toàn bộ cài đặt chính sách. | `[x]` | QA |
| **2.5** | Mở khóa Idle Auto-Lock | Nhập mã Exit PIN hợp lệ vào overlay khóa. | Lớp phủ ẩn đi, giáo viên có thể tiếp tục thiết lập chính sách bình thường. | `[x]` | QA |
| **2.6** | Nhập sai PIN mở khóa Idle | Nhập sai mã PIN mở khóa 3 lần. | Khóa màn hình giáo viên trong 1 phút và ghi nhận log cảnh báo vào SQLite. | `[x]` | QA |
| **2.7** | Kiểm thử đa luồng SQLite WAL | Cho 5 luồng ghi log đồng thời vào SQLite. | SQLite thực hiện ghi tuần tự qua file WAL thành công, không xảy ra ngoại lệ `Database is locked`. | `[x]` | QA |

---

## IV. QUY TRÌNH PHÁT TRIỂN & NGHIỆM THU AN TOÀN (FAIL-SAFE DEV PIPELINE)

Để đảm bảo coder không thể phạm sai lầm làm phá vỡ cấu trúc và tính ổn định hiện tại của chương trình, quy trình triển khai được quy chuẩn hóa như sau:

1. **Cô lập logic preset:** Lập trình viên bắt buộc phải kiểm tra biến cờ hiệu `_isBindingPreset` trong tất cả các sự kiện thay đổi checkbox để tránh tạo ra vòng lặp vô hạn kích hoạt sự kiện UI.
2. **Kiểm tra tương thích phím tắt:** Coder phải kiểm tra mã Virtual Key (`vkCode`) chính xác của Win32 API trước khi cấu hình chặn phím tắt màn hình ảo.
3. **Smoke Test & All-tests Compile:** Trước khi bàn giao, coder chạy lệnh:
   `dotnet build QASmartClass.sln -m:1`
   `dotnet test --filter "V88TeacherAccessibilityTests"`
   Đảm bảo tất cả các bài test kiểm tra muối băm động và bảo mật giao diện đều đạt trạng thái **Passed**.
4. **Acceptance Gate:** QA chạy kiểm thử thủ công qua 2 bảng checksheet ở mục III để ký duyệt bàn giao.
