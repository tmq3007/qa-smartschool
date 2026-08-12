# QA SmartClass v4.2 Project Rules

## 📐 BỐ CỤC GIAO DIỆN (UI LAYOUT STANDARDS)

### [RULE] QC_4.2_LAYOUT_GRID: Quy chuẩn bố cục màn hình có Menu điều hướng (Master-Detail)

1. **Ràng buộc Grid gốc (Root Grid):**
   * Đối với tất cả UserControl hoặc Page có chứa thanh thực đơn điều hướng bên trái (Sidebar ListBox/Menu), Grid ngoài cùng (`rootGrid`) bắt buộc phải được thiết lập chế độ kéo dãn: `HorizontalAlignment="Stretch"` và không được cấu hình `MaxWidth`.
   * **Mục tiêu:** Tránh lỗi co cụm giao diện vào giữa và tránh hiện tượng dịch chuyển ngang thanh menu (jumping sidebar) khi thay đổi tab hoặc thay đổi kích thước màn hình.

2. **Giới hạn không gian hiển thị nội dung chi tiết (Content-Level Constraint):**
   * Chỉ được giới hạn độ rộng tối đa (`MaxWidth`) cho riêng các panel nội dung nằm ở cột chi tiết bên phải (Right Column) của mỗi tab:
     * Cấp độ ôn tập/luyện tập toán học: `MaxWidth="1200"` hoặc `MaxWidth="1400"`.
     * Cấp độ hiển thị ứng dụng thực tế chứa ảnh lớn: `MaxWidth="1600"`.
   * Luôn đảm bảo căn lề của các Panel nội dung chi tiết cân đối giữa hoặc trái tùy theo đặc thù thiết kế sư phạm của tab đó.

## 🔤 QUY CHUẨN NGÔN NGỮ (LANGUAGE & BRANDING STANDARDS)

### [RULE] QC_4.2_LANGUAGE_BRANDING: Ràng buộc giữ nguyên từ khóa thương hiệu Tiếng Anh

1. **Danh sách từ khóa Tiếng Anh bắt buộc giữ nguyên:**
   * Đối với các thành phần chức năng cốt lõi và chế độ hoạt động (App Mode) liên quan trực tiếp đến thương hiệu hoặc hành vi hệ thống, tuyệt đối KHÔNG được Việt hóa các nhãn từ khóa sau:
     * **SMART CLASS:** Chế độ quản lý lớp học của giáo viên (tương ứng với nhãn nút bấm hoặc Mode trong hệ thống).
     * **SMART TOUCH:** Chế độ bảng vẽ/bảng trắng tương tác cảm ứng.
     * **DESKTOP:** Chế độ chuyển đổi hiển thị màn hình Windows Desktop.
   * **Mục tiêu:** Giữ tính nhất quán về thuật ngữ thương hiệu và tài liệu kỹ thuật hướng dẫn sử dụng trong toàn bộ hệ sinh thái QA SmartClass v4.2.
