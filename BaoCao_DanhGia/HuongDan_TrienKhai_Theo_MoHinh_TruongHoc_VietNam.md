# HƯỚNG DẪN TRIỂN KHAI THÍCH ỨNG THEO CẤP HỌC VÀ ĐIỀU KIỆN HẠ TẠNG TẠI VIỆT NAM

*Biên soạn bởi Trưởng bộ phận thiết kế hệ thống QA Smart School*
*Tài liệu hướng dẫn cấu hình và phân kỳ triển khai cá nhân hóa cho từng mô hình trường học*

---

## 🏛️ ĐẶT VẤN ĐỀ
Giáo dục Việt Nam có sự phân hóa rõ rệt về cấp học (Tiểu học, THCS, THPT) và có sự chênh lệch lớn về điều kiện cơ sở vật chất giữa các vùng miền (Trường chuẩn quốc gia, trường chất lượng cao thành thị vs. trường vùng sâu vùng xa, phòng máy tính cũ). 

Để hệ thống **QA Smart School** vận hành hiệu quả, không thể áp dụng một công thức triển khai duy nhất. Tài liệu này cung cấp phương án thích ứng linh hoạt về cấu hình phần mềm và lộ trình triển khai theo từng kịch bản thực tế.

---

## ══ PHẦN 1: THÍCH ỨNG THEO CẤP HỌC (EDUCATIONAL LEVEL ADAPTATION) ══

Mỗi cấp học có sự khác biệt lớn về tâm sinh lý học sinh và mục tiêu chương trình giáo dục phổ thông (GDPT 2018). Do đó, cấu hình giao diện và phân hệ chức năng sẽ được tùy biến như sau:

### 1. Cấp Tiểu học (Primary School)
*   **Đặc điểm học sinh**: Học sinh nhỏ tuổi, khả năng tập trung ngắn, thao tác bàn phím chưa thành thạo, cần nhiều hình ảnh trực quan và sự giám sát chặt chẽ của phụ huynh.
*   **Tùy biến chức năng phần mềm**:
    *   *Giao diện*: Kích hoạt chế độ giao diện tối giản (Simple UI), các nút bấm to, thân thiện với màn hình cảm ứng (nếu có). Thay các nhãn chữ dài bằng các icon SVG sinh động.
    *   *Học tập*: Vô hiệu hóa chức năng đếm từ và soạn bài viết tự luận dài ở `StudentSubmitPage`. Tập trung hiển thị phân hệ `GameHubPage` (các trò chơi toán học, đố vui trí tuệ) và làm trắc nghiệm nhanh `StudentQuizPage` có giao diện chấm điểm hoạt họa vui tươi.
    *   *Quản lý của Phụ huynh (Parent Portal)*: Bật tối đa các tính năng y tế học đường (`HealthRecordView`), theo dõi xe đưa đón, điểm danh vào lớp (`ParentAttendancePage`), và duyệt đơn nghỉ phép của con.
*   **Chiến lược triển khai**: Ưu tiên triển khai **Cổng thông tin Phụ huynh** và **Hậu cần bán trú** (Căng tin, Y tế học đường) trước để nhà trường quản lý an toàn cho học sinh, sau đó mới đưa các công cụ học tập cơ bản vào phòng máy.

### 2. Cấp THCS (Junior High School)
*   **Đặc điểm học sinh**: Học sinh đang phát triển tư duy logic, bắt đầu làm quen với các môn khoa học tự nhiên/xã hội phân hóa, tích cực tham gia các phong trào thi đua tập thể.
*   **Tùy biến chức năng phần mềm**:
    *   *Học tập*: Bật chức năng viết tự do ở mức độ cơ bản (bài tập làm văn ngắn 100-200 từ). Kích hoạt các công cụ hỗ trợ học tập lớp 6-9 như: Công cụ Số nguyên tố, Giải hệ phương trình bậc nhất 2 ẩn (phương pháp Cramer/Thế trực quan).
    *   *Thi đua & Phong trào*: Bật phân hệ Đoàn/Đội (`YouthUnion`) phục vụ đội cờ đỏ chấm điểm thi đua nề nếp trực tuần giữa các lớp, tự động tính điểm thi đua và hiển thị lên bảng xếp hạng toàn trường `EmulationBoardPage`.
*   **Chiến lược triển khai**: Triển khai song song phân hệ **Tương tác lớp học** (Toán, Văn, Anh) và phân hệ **Thi đua nề nếp Đoàn/Đội** để tạo thói quen kỷ luật cho học sinh.

### 3. Cấp THPT (Senior High School)
*   **Đặc điểm học sinh**: Tư duy độc lập cao, chuẩn bị cho các kỳ thi quốc gia và định hướng nghề nghiệp, có khả năng tự học tốt.
*   **Tùy biến chức năng phần mềm**:
    *   *Học tập*: Kích hoạt toàn bộ các tính năng nâng cao của `StudentSubmitPage` (soạn bài luận dài, kiểm soát số từ nghiêm ngặt theo Hướng dẫn sư phạm, lưu bản nháp mã hóa bảo mật chéo). Bật các chế độ nâng cao của công cụ toán học (Giải phương trình bậc 2 có số phức $i$, vẽ đồ thị Parabol phân tích giao điểm).
    *   *Hướng nghiệp & Tâm lý*: Bật tính năng trắc nghiệm tính cách chọn nghề `CareerTestPage` và thiết lập mục tiêu học tập `GoalSettingPage`. Mở cổng tư vấn tâm lý học đường ẩn danh `AnonymousChatPage` để học sinh chia sẻ áp lực thi cử.
    *   *Tự chủ học tập*: Học sinh tự quản lý hồ sơ năng lực cá nhân `PortfolioPage` để tích lũy sản phẩm học tập phục vụ xét tuyển đại học.
*   **Chiến lược triển khai**: Triển khai nhanh chóng toàn bộ phân hệ chuyên môn (Giáo án, Ngân hàng đề thi, Chấm bài AI) và phân hệ Tự học/Tư vấn hướng nghiệp của học sinh.

### 4. Mô hình Trường Liên cấp (Multi-level / K-12 School)
*   **Đặc điểm đặc thù**: Trường học giảng dạy tích hợp nhiều cấp học (Tiểu học - THCS hoặc THCS - THPT hoặc cả 3 cấp từ Lớp 1 đến Lớp 12) trên cùng một hạ tầng phòng máy Lab dùng chung hoặc cùng một hệ thống mạng trường.
*   **Giải pháp thích ứng & Cấu hình động**:
    *   *Chuyển đổi giao diện động (Dynamic UI Switch)*: Hệ thống **không cấu hình cứng** tham số `GradeLevel` trong tệp `settings.json` cục bộ của máy tính. Thay vào đó, ngay khi học sinh đăng nhập thành công (`StudentLoginWindow.xaml.cs`), hệ thống truy vấn thông tin lớp học của học sinh đó từ cơ sở dữ liệu.
        *   Nếu học sinh thuộc khối Lớp 1 - 5: Giao diện `StudentShell` tự động ẩn tab Soạn bài viết tự luận dài và đếm từ, chỉ mở làm trắc nghiệm vui và Game học thuật.
        *   Nếu học sinh thuộc khối Lớp 6 - 9: Mở phân hệ viết văn ngắn, các công cụ Toán/Số học lớp 6-9, và bảng chấm điểm thi đua cờ đỏ.
        *   Nếu học sinh thuộc khối Lớp 10 - 12: Kích hoạt toàn bộ tính năng nâng cao (số phức ở phương trình bậc 2, vẽ vectơ học thuật, trắc nghiệm hướng nghiệp, tư vấn ẩn danh).
    *   *Phân cấp quyền hạn liên cấp*: Giáo viên dạy liên cấp được cấp quyền chuyển đổi linh hoạt chế độ xem (View Mode) trên `TeacherHubWindow` để truy cập giáo án, ngân hàng đề thi và sổ chấm điểm tương ứng với từng cấp học đang giảng dạy.
    *   *Cơ chế dọn dẹp đĩa cứng thông minh*: Lệnh dọn dẹp hàng đợi `PendingSync` và tệp tạm nháp `.tmp` được kích hoạt lập tức khi xảy ra sự kiện Đăng xuất (Logout) hoặc Đăng nhập mới (Login), đảm bảo máy tính sạch sẽ trước khi học sinh cấp học khác vào ngồi máy.

---

## ══ PHẦN 2: THÍCH ỨNG THEO ĐIỀU KIỆN HẠ TẦNG (INFRASTRUCTURE ADAPTATION) ══

Tùy thuộc vào ngân sách và cơ sở vật chất phòng máy tính của từng trường, hệ thống sẽ được điều chỉnh cấu hình kỹ thuật để vận hành tối ưu nhất:

### Mô hình A: Phòng máy khó khăn (Hạ tầng yếu)
*Cấu hình máy trạm: RAM <4GB, CPU đời cũ, ổ cứng cơ học HDD đã phân mảnh, mạng LAN nội bộ chập chờn, thường xuyên rớt gói.*
*   **Giải pháp cấu hình & Kỹ thuật tối ưu**:
    *   *Cơ sở dữ liệu*: Bắt buộc kích hoạt SQLite ở chế độ `Write-Ahead Logging (WAL)` kết hợp `PRAGMA synchronous = NORMAL` để tăng tốc độ ghi đĩa HDD và tránh treo ứng dụng do nghẽn ổ cứng.
    *   *Đồ họa & Âm thanh*: Tắt các hiệu ứng đồ họa SVG động, tắt âm thanh hiệu ứng khi nộp bài thành công để giải phóng RAM tối đa.
    *   *Cơ chế đồng bộ mạng*: 
        *   Tăng chu kỳ đồng bộ nhật ký sự kiện (`EventLogs`) từ 5 phút lên 15 phút/lần hoặc chỉ đồng bộ thủ công một lần vào cuối buổi học để giảm tải cho băng thông mạng LAN phòng máy.
        *   Tận dụng triệt để cơ chế **Tự học ngoại tuyến (Offline Mode)**: Bài làm nháp của học sinh và file nộp bài tập được lưu tạm vào thư mục đĩa cứng cục bộ `PendingSync` kèm timestamp và mã học sinh. Việc đồng bộ lên máy giáo viên sẽ được xếp hàng gửi ngầm dần dần thay vì gửi đồng loạt gây nghẽn mạng.
        *   Tránh các câu truy vấn phức tạp hoặc biểu đồ thời gian thực trên giao diện học sinh.

### Mô hình B: Phòng máy đạt chuẩn (Hạ tầng trung bình - khá)
*Cấu hình máy trạm: RAM 8GB, ổ cứng SSD tốc độ cao, mạng LAN ổn định qua cáp mạng Cat6, Server giáo viên chạy mượt.*
*   **Giải pháp cấu hình & Kỹ thuật tối ưu**:
    *   *Đồ họa*: Kích hoạt đầy đủ các biểu tượng SVG động, thanh tiến độ đổi màu sắc trực quan khi học sinh gõ bài.
    *   *Mạng & Đồng bộ*: Kích hoạt chu kỳ đồng bộ nhật ký tự động 5 phút/lần. Bật tính năng tự động phân cấp thư mục nhận file `ReceivedFiles/MonHoc_GiaoVien/`.
    *   *Tương tác*: Cho phép học sinh tùy chọn thu gọn/mở rộng cẩm nang hướng dẫn bên phải màn hình để tối ưu hóa không gian làm việc.

### Mô hình C: Trường chất lượng cao / Trường Quốc tế (Hạ tầng hiện đại)
*Thiết bị cá nhân 1-1 (BYOD - Bring Your Own Device: Laptop/Tablet), Wi-Fi phủ sóng tốc độ cao toàn trường, bảng tương tác thông minh (Smart Board) tại mỗi phòng học.*
*   **Giải pháp cấu hình & Kỹ thuật tối ưu**:
    *   *Tích hợp AI*: Bật toàn bộ phân hệ trợ lý AI hỗ trợ giáo viên soạn bài giảng (`AiAssistantPage`) và chấm điểm tự luận (`AiCopilotWindow`).
    *   *Tương tác bảng vẽ*: Sử dụng bảng vẽ phác thảo `StudentLocalWhiteboardPage` đồng bộ thời gian thực lên bảng tương tác chung của giáo viên để trình bày bài giải trước lớp.
    *   *Dữ liệu lớn (Big Data)*: Bật phân tích xu hướng học tập thời gian thực, tự động trích xuất các KPI chất lượng giáo dục gửi trực tiếp về bảng tin của Hiệu trưởng.
    *   *Thanh toán điện tử*: Kích hoạt cổng thanh toán học phí trực tuyến liên kết ngân hàng/ví điện tử trên cổng phụ huynh.

---

## ══ PHẦN 3: BẢNG TỔNG HỢP CẤU HÌNH THÍCH ỨNG HỆ THỐNG ══

Để kỹ thuật viên triển khai không bị nhầm lẫn, dưới đây là bảng cấu hình tham số trong tệp `settings.json` tại máy trạm của học sinh theo từng mô hình hạ tầng:

| Tham số cấu hình | Mô hình A (Hạ tầng yếu) | Mô hình B (Hạ tầng trung bình) | Mô hình C (Hạ tầng hiện đại) |
| :--- | :--- | :--- | :--- |
| `EnableWALMode` | `true` (Bắt buộc) | `true` | `true` |
| `SyncIntervalMinutes` | `15` (Hoặc gửi thủ công) | `5` | `1` (Thời gian thực) |
| `EnableAudioEffects` | `false` | `true` | `true` |
| `EnableDynamicSVG` | `false` (Dùng emoji tĩnh) | `true` | `true` |
| `AutoCleanPendingSyncDays`| `7` | `14` | `30` |
| `EnableOfflineFallback` | `true` (Độ nhạy cao) | `true` | `true` (Dự phòng) |
| `AIAssistanceEnabled` | `false` | `false` (Hoặc chỉ bật ở server) | `true` (Toàn diện) |
| `LocalDatabasePath` | Đường dẫn phân vùng đóng băng ngoại lệ | Đường dẫn mặc định | Bộ nhớ đám mây / Local cache |

---

## ══ PHẦN 4: LỘ TRÌNH TRIỂN KHAI PHÂN KỲ LINH HOẠT (ROLLOUT STRATEGY) ══

Tùy vào điều kiện thực tế, nhà trường có thể lựa chọn 1 trong 3 lộ trình triển khai sau:

```
MÔ HÌNH TRƯỜNG THPT / TRƯỜNG CHUYÊN:
Giai đoạn 1 (Dạy & Học) ──> Giai đoạn 2 (Hành chính chuyên môn) ──> Giai đoạn 3 (Hướng nghiệp & Phụ huynh)

MÔ HÌNH TRƯỜNG TIỂU HỌC / BÁN TRÚ:
Giai đoạn 4 (Y tế, Căng tin, Đưa đón) ──> Giai đoạn 3 (Phụ huynh) ──> Giai đoạn 1 (Làm bài trắc nghiệm vui)

MÔ HÌNH VÙNG KHÓ KHĂN:
Giai đoạn 1 (Offline Mode) ──> Ổn định hạ tầng mạng ──> Triển khai các giai đoạn quản lý hành chính sau
```

Sự linh hoạt này giúp **QA Smart School** không chỉ là một phần mềm công nghệ cao, mà là một giải pháp thực tế, bền bỉ và có khả năng bám rễ sâu vào mọi ngôi trường trên khắp đất nước Việt Nam.
