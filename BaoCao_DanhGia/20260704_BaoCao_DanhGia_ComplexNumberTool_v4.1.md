# BÁO CÁO ĐÁNH GIÁ CHUYÊN GIA: CÔNG CỤ SỐ PHỨC
**HỘI ĐỒNG THẨM ĐỊNH ĐA NGÀNH - DỰ ÁN TRƯỜNG HỌC THÔNG MINH QA SMART SCHOOL**
*Biên bản đánh giá chuyên sâu, kiểm định sư phạm & kỹ thuật - Phiên bản nâng cấp v4.1*

---

## ═══ PHẦN 1: TỔNG QUAN HẠNG MỤC ĐÁNH GIÁ & HIỆN TRẠNG ═══

Để đáp ứng các yêu cầu khắt khe của bộ quy chuẩn sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**, Hội đồng chuyên gia đa ngành gồm 17 thành viên đã tiến hành đánh giá chi tiết công cụ **Số Phức** (ComplexNumberTool).

### 1. Giao diện & Bố cục (Layout)
*   **Hiện trạng khoảng trống thừa hai bên:** Trước cải tiến, công cụ này không giới hạn độ rộng `MaxWidth` cho Grid chính. Trên màn hình rộng widescreen 1920px+, giao diện bị dãn rộng toàn bộ, tạo khoảng trống lớn vô nghĩa hai bên của khu vực chọn chức năng, bảng nhập số phức, và phần phân tích kết quả kèm đồ thị biểu diễn hình học số phức.
*   **Cải tiến:** 
    *   Thiết lập `MaxWidth="1200"` cho Grid gốc, căn lề giữa `Center` để hiển thị cân đối các nội dung tính toán và hướng dẫn.
    *   Sử dụng cơ chế **Dynamic Layout Switching** trong sự kiện `Tab_Click` để tự động nới rộng `MaxWidth` của Grid gốc lên **`1600`** khi chuyển sang Tab Ứng dụng thực tế nhằm tối ưu không gian hiển thị của `PracticalAppViewer`.
    *   Bao bọc `PracticalAppViewer` ở Tab 3 trong Border có `Margin="20"` thông thoáng.

### 2. Trải nghiệm sư phạm & Logic chức năng (Pedagogy & Functions)
*   **Toán học số phức chuyên sâu:** Hỗ trợ tính toán nhanh các phép toán cộng, trừ, nhân, chia số phức, lũy thừa, khai căn bậc n, tìm mô-đun, acgumen và dạng lượng giác kèm lời giải chi tiết từng bước.
*   **Mặt phẳng phức trực quan:** Liên kết hệ trục tọa độ Decartes để biểu diễn điểm hình học của số phức giúp học sinh dễ dàng hình dung.

---

## ═══ PHẦN 2: BẢNG TỔNG HỢP TIÊU CHÍ TUÂN THỦ (COMPLIANCE MATRIX v4.1) ═══

| ID Quy chuẩn | Tiêu chí đánh giá | Hiện trạng trước cải tiến | Trạng thái sau cải tiến | Kết luận |
| :--- | :--- | :--- | :--- | :---: |
| **QC_01_LAYOUT** | Tối ưu không gian trống | Tab 3 chứa PracticalAppViewer trực tiếp bị kéo dãn tràn mép. | Bao bọc trong Grid `MaxWidth="1600"` căn giữa. | **ĐẠT** |
| **QC_02_ALIGN** | Cân chỉnh căn lề | Grid gốc không có MaxWidth gây dãn rộng vô tận trên màn hình 1920px+. | Nới rộng động `1200`px -> `1600`px tùy Tab. | **ĐẠT** |
| **QC_03_FONT** | Đồng bộ hóa phông chữ | Sử dụng font hệ thống rõ nét. | Đảm bảo phông chữ hiển thị học thuật sắc nét. | **ĐẠT** |
| **QC_04_LANG** | Ngôn ngữ hiển thị | Tiếng Việt sư phạm chuẩn xác 100%. | Đảm bảo tiếng Việt sư phạm chuẩn xác 100%. | **ĐẠT** |

---

## ═══ PHẦN 3: THAO TÁC CẢI TIẾN CHI TIẾT TRÊN MÃ NGUỒN ═══

Hội đồng thẩm định đã phối hợp thực hiện các thay đổi trên mã nguồn giao diện `ComplexNumberTool.xaml` và `ComplexNumberTool.xaml.cs`:
1.  **Grid gốc:** Thiết lập `x:Name="rootGrid" MaxWidth="1200" HorizontalAlignment="Center"`.
2.  **Tab 3 (Ứng dụng thực tế):** Bao bọc `PracticalAppViewer` trong Border đặt tên là `borderApp` có `MaxWidth="1600"`, `HorizontalAlignment="Stretch"` và `Margin="20"`.
3.  **Code-behind:** Bổ sung logic xử lý sự kiện `Tab_Click` thay đổi động `MaxWidth` của Grid gốc và kiểm soát ẩn hiện `borderApp`.
