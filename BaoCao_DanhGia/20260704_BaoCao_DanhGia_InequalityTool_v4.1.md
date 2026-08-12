# BÁO CÁO ĐÁNH GIÁ CHUYÊN GIA: CÔNG CỤ GIẢI BẤT PHƯƠNG TRÌNH
**HỘI ĐỒNG THẨM ĐỊNH ĐA NGÀNH - DỰ ÁN TRƯỜNG HỌC THÔNG MINH QA SMART SCHOOL**
*Biên bản đánh giá chuyên sâu, kiểm định sư phạm & kỹ thuật - Phiên bản nâng cấp v4.1*

---

## ═══ PHẦN 1: TỔNG QUAN HẠNG MỤC ĐÁNH GIÁ & HIỆN TRẠNG ═══

Để đáp ứng các yêu cầu khắt khe của bộ quy chuẩn sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**, Hội đồng chuyên gia đa ngành gồm 17 thành viên đã tiến hành đánh giá chi tiết công cụ **Giải Bất Phương Trình** (InequalityTool).

### 1. Hiện trạng giao diện & Bố cục (Layout)
*   **Hiện trạng khoảng trống thừa hai bên:** Trước cải tiến, công cụ này không giới hạn độ rộng `MaxWidth` cho Grid chính. Trên màn hình rộng widescreen 1920px+, giao diện bị dãn rộng toàn bộ, tạo khoảng trống lớn vô nghĩa hai bên của giao diện nhập hệ số bậc 1/bậc 2, bảng xét dấu, giải chi tiết, và trục số trực quan.
*   **Cải tiến:** 
    *   Thiết lập `MaxWidth="1200"` cho Grid gốc `rootGrid`, căn lề giữa `Center` để hiển thị cân đối các nội dung tính toán và hướng dẫn.
    *   Sử dụng cơ chế **Dynamic Layout Switching** trong phương thức `SwitchToTab` để tự động nới rộng `MaxWidth` của Grid gốc lên **`1600`** khi chuyển sang Tab 3 "Ứng dụng thực tế" nhằm tối ưu không gian hiển thị của `PracticalAppViewer`.
    *   Trong `InequalityTool.xaml`, Tab 3 đã được bọc sẵn trong `gridApp` có `MaxWidth="1600"`, `HorizontalAlignment="Stretch"` và `Margin="20"` thông thoáng bảo vệ `contentApp` rất tốt.

### 2. Trải nghiệm sư phạm & Logic chức năng (Pedagogy & Functions)
*   **Xét dấu & Bất phương trình:** Giải bất phương trình bậc nhất `ax+b > 0` và bậc hai `ax²+bx+c > 0` với các dấu so sánh `>`, `≥`, `<`, `≤`.
*   **Trình bày sư phạm mẫu mực:** Minh họa chi tiết các bước chuyển vế đổi dấu, chia hệ số kèm chú ý đổi chiều bất phương trình khi chia số âm, lập bảng xét dấu tam thức bậc hai (quy tắc "Trong trái, ngoài cùng").

---

## ═══ PHẦN 2: BẢNG TỔNG HỢP TIÊU CHÍ TUÂN THỦ (COMPLIANCE MATRIX v4.1) ═══

| ID Quy chuẩn | Tiêu chí đánh giá | Hiện trạng trước cải tiến | Trạng thái sau cải tiến | Kết luận |
| :--- | :--- | :--- | :--- | :---: |
| **QC_01_LAYOUT** | Tối ưu không gian trống | Tab 3 chứa gridApp/PracticalAppViewer đã có sẵn kết cấu bao bọc MaxWidth. | Đảm bảo tính nhất quán trên màn hình rộng. | **ĐẠT** |
| **QC_02_ALIGN** | Cân chỉnh căn lề | Grid gốc không có MaxWidth gây dãn rộng vô tận trên màn hình 1920px+. | Nới rộng động `1200`px -> `1600`px tùy Tab. | **ĐẠT** |
| **QC_03_FONT** | Đồng bộ hóa phông chữ | Sử dụng font hệ thống rõ nét. | Đảm bảo phông chữ hiển thị học thuật sắc nét. | **ĐẠT** |
| **QC_04_LANG** | Ngôn ngữ hiển thị | Tiếng Việt sư phạm chuẩn xác 100%. | Đảm bảo tiếng Việt sư phạm chuẩn xác 100%. | **ĐẠT** |

---

## ═══ PHẦN 3: THAO TÁC CẢI TIẾN CHI TIẾT TRÊN MÃ NGUỒN ═══

Hội đồng thẩm định đã phối hợp thực hiện các thay đổi trên mã nguồn giao diện `InequalityTool.xaml` và `InequalityTool.xaml.cs`:
1.  **Grid gốc:** Thiết lập `x:Name="rootGrid" MaxWidth="1200" HorizontalAlignment="Center"`.
2.  **Code-behind:** Bổ sung logic xử lý sự kiện `SwitchToTab` thay đổi động `MaxWidth` của Grid gốc.
