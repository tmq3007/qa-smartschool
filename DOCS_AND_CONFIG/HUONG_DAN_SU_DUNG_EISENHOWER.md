# 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: MA TRẬN EISENHOWER (EISENHOWER MATRIX TOOL)

Tài liệu này cung cấp hướng dẫn trực quan, sơ đồ quy trình vận hành, dữ liệu mẫu thực tế và ý nghĩa sư phạm dành cho **Giáo viên** và **Học sinh** khi sử dụng công cụ **Ma Trận Eisenhower** thuộc hệ thống quản lý học tập thông minh **QA SmartClass**.

---

## 🗺️ 1. Sơ Đồ Quy Trình Vận Hành Thời Gian Thực (Sequence Diagram)

Sơ đồ mô tả quy trình học sinh phân loại công việc, hệ thống tự động lưu trữ tiến trình ngoại tuyến vào CSDL SQLite và hiển thị gợi ý tư vấn của Giáo viên AI:

```mermaid
sequenceDiagram
    autonumber
    actor HS as Học sinh (Giao diện)
    actor GV as Giáo viên (Quản lý)
    participant APP as ⏰ Công cụ Ma trận Eisenhower
    participant AI as 💡 Trợ lý Giáo viên AI
    database DB as Cơ sở dữ liệu SQLite (smartclass.db)

    Note over HS, APP: Bước 1: Khởi động & Khôi phục trạng thái
    HS->>APP: Mở Tab "Ma Trận Eisenhower"
    APP->>DB: Gọi LoadWorkplaceState("eisenhower")
    DB-->>APP: Trả về chuỗi dữ liệu JSON tiến trình cũ
    APP->>HS: Khôi phục danh sách công việc & hiển thị lên 4 ô

    Note over HS, APP: Bước 2: Phân loại & Cập nhật
    HS->>APP: Nhập công việc mới -> Bấm "➕ Thêm"
    APP->>HS: Hiện Menu chọn ô phân loại (1, 2, 3, 4)
    HS->>APP: Chọn ô (Ví dụ: Ô 1: Làm ngay)
    APP->>APP: Thêm công việc vào Grid, tự động tính toán số lượng công việc mỗi ô
    
    rect rgb(240, 248, 255)
        Note over APP, AI: Bước 3: Giáo viên AI phân tích & Nhận xét
        APP->>AI: Gửi số lượng công việc ở 4 ô
        AI->>AI: Đánh giá phân bổ (Quá tải Ô 1, Ôn thi khoa học...)
        AI-->>APP: Trả về danh sách lời khuyên (AdviceList)
        APP->>HS: Hiện "💡 Nhận xét của Giáo viên AI"
    end

    Note over HS, DB: Bước 4: Lưu trữ & Đồng bộ
    APP->>DB: Gọi SaveWorkplaceState("eisenhower", StateJson)
    Note over DB: Ghi đồng bộ an toàn bằng SQLite Replace INTO
    
    Note over HS, GV: Bước 5: Xem thống kê & Xuất bản
    HS->>APP: Chuyển sang Tab "Thống Kê Hiệu Suất"
    APP->>HS: Vẽ biểu đồ tròn (Pie Chart) phân bổ nhiệm vụ qua OxyPlot
    HS->>APP: Nhấn nút "📷 Xuất ảnh PNG"
    APP->>HS: Render ảnh độ phân giải cao lưu về máy để nộp cho Giáo viên
```

---

## 👩‍🏫 2. DÀNH CHO GIÁO VIÊN: HƯỚNG DẪN HỌC SINH QUẢN LÝ THỜI GIAN

Giáo viên có thể ứng dụng công cụ Ma Trận Eisenhower trong các giờ sinh hoạt lớp, tiết học kỹ năng sống hoặc hướng nghiệp để giúp học sinh rèn luyện thói quen lập kế hoạch.

### 💡 Các hoạt động gợi ý cho Giáo viên trên lớp:
1.  **Hoạt động ôn thi học kỳ**: Yêu cầu học sinh nhập toàn bộ các công việc ôn tập của các môn học và phân chia vào 4 ô. Nhắc nhở các em tập trung giải quyết dứt điểm các việc ở **Ô 1 (Làm ngay)** nhưng phải dành nhiều thời gian xây dựng lộ trình ở **Ô 2 (Lên lịch)** để tránh bị "nước đến chân mới nhảy".
2.  **Đánh giá mức độ xao lãng**: Nhìn vào biểu đồ tròn thống kê ở Tab 2. Nếu tỷ lệ công việc ở **Ô 4 (Loại bỏ)** quá cao, giáo viên cần định hướng học sinh cắt giảm các hoạt động vô bổ như lướt mạng xã hội hay chơi game quá giờ.
3.  **Hoạt động thảo luận nhóm**: Hướng dẫn học sinh cách ủy quyền ở **Ô 3 (Ủy thác)**. Ví dụ: Trong bài tập nhóm môn Tiếng Anh, phân công bạn giỏi thiết kế làm slide, bạn giỏi phát âm làm thuyết trình, giúp tối ưu thế mạnh từng thành viên.

---

## 🧑‍🎓 3. DÀNH CHO HỌC SINH: BÍ QUYẾT PHÂN BỔ 4 Ô CHI TIẾT

Ma trận chia công việc của em thành 4 nhóm dựa trên hai yếu tố: **Quan trọng** (ảnh hưởng lớn đến kết quả học tập) và **Khẩn cấp** (có deadline sát nút).

```
                      🔴 KHẨN CẤP                   🟢 KHÔNG KHẨN CẤP
             ┌─────────────────────────────┬─────────────────────────────┐
             │      🔥 Ô 1: LÀM NGAY       │      📅 Ô 2: LÊN LỊCH       │
  ⭐ QUAN    │  • Bài tập ngày mai nộp     │  • Ôn thi học kỳ (còn 1 tháng)│
   TRỌNG     │  • Ôn tập kiểm tra chiều nay │  • Luyện nghe tiếng Anh     │
             │  • Sự cố máy tính học tập   │  • Tập thể dục, đọc sách    │
             ├─────────────────────────────┼─────────────────────────────┤
             │      🤝 Ô 3: ỦY THÁC        │      🗑️ Ô 4: LOẠI BỎ        │
  ⚪ KHÔNG   │  • Nhờ em quét nhà hộ       │  • Lướt mạng xã hội vô bổ   │
   TRỌNG     │  • Nhờ bạn mua giùm đồ dùng  │  • Chơi game quá 2 tiếng    │
             │  • Đăng ký hoạt động phụ    │  • Xem phim thâu đêm        │
             └─────────────────────────────┴─────────────────────────────┘
```

### 🎯 Quy tắc ứng phó với từng ô:
*   **Ô 1 (Làm ngay)**: Thực hiện ngay lập tức. Đây là những việc không thể trì hoãn.
*   **Ô 2 (Lên lịch)**: Quyết định sự thành bại và phát triển lâu dài của em. Hãy chọn ngày hoàn thành (`DatePicker`) và phân phối thời gian hợp lý mỗi ngày.
*   **Ô 3 (Ủy thác / Nhờ vả)**: Đây là những việc khẩn cấp nhưng không giúp ích nhiều cho mục tiêu lớn của em. Hãy học cách nhờ sự trợ giúp từ người khác (bạn bè, người thân) để rảnh tay làm việc ở ô 1 và ô 2.
*   **Ô 4 (Loại bỏ)**: Cắt giảm tối đa. Đây là những việc gây lãng phí thời gian và làm giảm hiệu suất học tập.

---

## 📊 4. DỮ LIỆU MẪU & VÍ DỤ THỰC TẾ (PRACTICAL EXAMPLES)

Hệ thống cung cấp sẵn các mẫu kế hoạch điển hình trong nút **🎯 Mẫu kế hoạch**. Dưới đây là nội dung chi tiết của các mẫu này giúp học sinh tham khảo:

### 🏫 Mẫu 1: Kế hoạch ôn thi học kỳ (Dành cho Học sinh)
*   **Ô 1 (Làm ngay)**:
    1. Làm đề cương ôn tập môn Toán hình (nộp sáng mai).
    2. Học thuộc lòng 10 bài thơ Ngữ Văn lớp 9.
    3. Hoàn thành bài trắc nghiệm Vật Lý trực tuyến trước 22h tối nay.
*   **Ô 2 (Lên lịch)**:
    1. Luyện giải đề thi thử môn Tiếng Anh (Hạn chót: 2 tuần nữa).
    2. Đọc sách tham khảo Hóa học hữu cơ (Mỗi ngày 20 phút).
    3. Ôn tập từ vựng Unit 10 Tiếng Anh.
*   **Ô 3 (Ủy thác)**:
    1. Đi mua bút bi và giấy nháp chuẩn bị thi (Có thể nhờ mẹ hoặc em trai mua giúp khi đi chợ).
    2. Quét dọn góc học tập gọn gàng.
*   **Ô 4 (Loại bỏ)**:
    1. Xem video review phim trên Facebook (Giảm từ 2 tiếng xuống 15 phút).
    2. Chơi game online thâu đêm trước ngày thi.

### 💼 Mẫu 2: Lập kế hoạch dự án Khoa học kỹ thuật (Dành cho Nhóm học sinh)
*   **Ô 1 (Làm ngay)**:
    1. Viết báo cáo tóm tắt ý tưởng dự án nộp cho Ban tổ chức trường.
    2. Sửa lỗi mô hình mạch điện tử bị chập.
*   **Ô 2 (Lên lịch)**:
    1. Tìm kiếm và đọc thêm các bài báo khoa học quốc tế liên quan.
    2. Chuẩn bị slide thuyết trình cho vòng chung khảo (Còn 3 tuần).
*   **Ô 3 (Ủy thác)**:
    1. Thiết kế ảnh bìa slide và in ấn tài liệu phát tay (Ủy quyền cho bạn khéo tay nhất trong nhóm).
    2. Chuẩn bị nước uống và phòng họp nhóm.
*   **Ô 4 (Loại bỏ)**:
    1. Tranh cãi vô bổ về các chi tiết trang trí không quan trọng của mô hình.

---

## 📈 5. Ý NGHĨA KHOA HỌC GIÁO DỤC CỦA CÔNG CỤ

*   **Rèn luyện kỹ năng tự chủ (Self-regulation)**: Giúp học sinh chuyển từ trạng thái thụ động (chờ nhắc nhở) sang chủ động lập kế hoạch, sắp xếp thứ tự ưu tiên học tập một cách khoa học.
*   **Giảm thiểu Stress học đường**: Phân loại công việc rõ ràng giúp học sinh giải tỏa cảm giác quá tải trước khối lượng bài tập lớn, tăng sự tự tin khi đối mặt với kỳ thi.
*   **Tư duy phản biện của Giáo viên AI**: Khung nhận xét AI cung cấp các cảnh báo kịp thời (ví dụ: cảnh báo khi học sinh nhét quá nhiều việc vào ô "Làm ngay" mà không lên lịch ở ô "Lên lịch"), giúp định hướng tư duy lập kế hoạch đúng đắn từ sớm.
