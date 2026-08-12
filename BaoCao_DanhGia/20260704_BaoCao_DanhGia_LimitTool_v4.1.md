# BÁO CÁO ĐÁNH GIÁ CHUYÊN GIA: CÔNG CỤ TÍNH GIỚI HẠN (LIMIT)
**HỘI ĐỒNG THẨM ĐỊNH ĐA NGÀNH - DỰ ÁN TRƯỜNG HỌC THÔNG MINH QA SMART SCHOOL**
*Biên bản đánh giá chuyên sâu, kiểm định sư phạm & kỹ thuật - Phiên bản nâng cấp v4.1*

---

## ═══ PHẦN 1: TỔNG QUAN HẠNG MỤC ĐÁNH GIÁ & HIỆN TRẠNG ═══

Để đáp ứng các yêu cầu khắt khe của bộ quy chuẩn sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**, Hội đồng chuyên gia đa ngành gồm 17 thành viên đã tiến hành đánh giá chi tiết công cụ **Giới Hạn** (LimitTool).

### 1. Hiện trạng giao diện & Bố cục (Layout)
*   **Hiện trạng khoảng trống thừa hai bên:** Trước cải tiến, công cụ này không giới hạn độ rộng `MaxWidth` cho Grid chính. Trên màn hình rộng widescreen 1920px+, giao diện bị dãn rộng toàn bộ, tạo khoảng trống lớn vô nghĩa hai bên của panel nhập hệ số đa thức tử/mẫu, bảng kết quả phân tích bước giải giới hạn, đồ thị và lý thuyết.
*   **Cải tiến:** 
    *   Thiết lập `MaxWidth="1200"` cho Grid gốc `rootGrid`, căn lề giữa `Center` để hiển thị cân đối các nội dung tính toán và hướng dẫn.
    *   Sử dụng cơ chế **Dynamic Layout Switching** trong phương thức `SwitchToTab` để tự động nới rộng `MaxWidth` của Grid gốc lên **`1600`** khi chuyển sang Tab 2 "Ứng dụng thực tế" nhằm tối ưu không gian hiển thị của `PracticalAppViewer`.
    *   Bao bọc `contentApp` ở Tab 2 trong Border đặt tên là `borderApp` có `MaxWidth="1600"`, `HorizontalAlignment="Stretch"` và `Margin="20"` thông thoáng.

### 2. Trải nghiệm sư phạm & Logic chức năng (Pedagogy & Functions)
*   **Giải tích giới hạn lớp 11:** Hỗ trợ tính giới hạn của hàm phân thức hữu tỷ bậc hai khi x dần tới x₀ (hoặc vô cực), tính giới hạn của các dãy số đặc biệt phổ biến (1/n, 1/n², (n+1)/n, v.v.), tra cứu công thức giới hạn đặc biệt.
*   **Minh họa sư phạm:** Trình bày chi tiết các bước khử dạng vô định (nhân liên hợp, phân tích đa thức thành nhân tử để triệt tiêu lượng vô định), và biểu diễn trực quan điểm khuyết (open point) hoặc tiệm cận đứng/ngang trên đồ thị Desmos.

---

## ═══ PHẦN 2: BẢNG TỔNG HỢP TIÊU CHÍ TUÂN THỦ (COMPLIANCE MATRIX v4.1) ═══

| ID Quy chuẩn | Tiêu chí đánh giá | Hiện trạng trước cải tiến | Trạng thái sau cải tiến | Kết luận |
| :--- | :--- | :--- | :--- | :---: |
| **QC_01_LAYOUT** | Tối ưu không gian trống | Tab 2 chứa PracticalAppViewer trực tiếp bị kéo dãn tràn mép. | Bao bọc trong Grid `MaxWidth="1600"` căn giữa. | **ĐẠT** |
| **QC_02_ALIGN** | Cân chỉnh căn lề | Grid gốc không có MaxWidth gây dãn rộng vô tận trên màn hình 1920px+. | Nới rộng động `1200`px -> `1600`px tùy Tab. | **ĐẠT** |
| **QC_03_FONT** | Đồng bộ hóa phông chữ | Sử dụng font hệ thống rõ nét. | Đảm bảo phông chữ hiển thị học thuật sắc nét. | **ĐẠT** |
| **QC_04_LANG** | Ngôn ngữ hiển thị | Tiếng Việt sư phạm chuẩn xác 100%. | Đảm bảo tiếng Việt sư phạm chuẩn xác 100%. | **ĐẠT** |

---

## ═══ PHẦN 3: THAO TÁC CẢI TIẾN CHI TIẾT TRÊN MÃ NGUỒN ═══

Hội đồng thẩm định đã phối hợp thực hiện các thay đổi trên mã nguồn giao diện `LimitTool.xaml` và `LimitTool.xaml.cs`:
1.  **Grid gốc:** Thiết lập `x:Name="rootGrid" MaxWidth="1200" HorizontalAlignment="Center"`.
2.  **Tab 2 (Ứng dụng thực tế):** Bao bọc `contentApp` trong Border đặt tên là `borderApp` có `MaxWidth="1600"`, `HorizontalAlignment="Stretch"` và `Margin="20"`.
3.  **Code-behind:** Bổ sung logic xử lý sự kiện `SwitchToTab` thay đổi động `MaxWidth` của Grid gốc.
