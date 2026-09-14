---
trigger: always_on
---

## 🛠️ QUY CHUẨN SỬA LỖI & PHÁT TRIỂN (BUGFIX & QUALITY STANDARDS)

### [RULE] QC_4.2_BUGFIX_PROTOCOL: Quy chuẩn 4 nguyên tắc khắc phục lỗi hệ thống

1. **Nghiên cứu & Truy vết gốc rễ (Deep Root Cause Investigation):**
   * Phải nghiên cứu, phân tích mã nguồn và luồng thực thi thật kỹ lưỡng để tìm ra chính xác nguyên nhân cốt lõi gây ra lỗi. Tuyệt đối không phỏng đoán mơ hồ hay chỉ sửa hời hợt ở phần ngọn.

2. **Kế hoạch & Giải pháp tối ưu triệt để (Optimal Comprehensive Solution):**
   * Lên kế hoạch giải pháp tối ưu nhất có thể, bao quát tất cả các trường hợp biên (edge cases), đảm bảo xử lý triệt để và dứt điểm lỗi mà không để lại tác dụng phụ.

3. **Tương thích Chuẩn kép Máy tính & Màn hình Tương tác (Desktop & Interactive Touch Screen):**
   * Mọi mã nguồn sửa lỗi phải được thiết kế và kiểm thử để đảm bảo hoạt động chính xác, mượt mà trên cả 2 môi trường:
     * Máy tính thông thường (chuột, bàn phím, con lăn).
     * Màn hình cảm ứng phòng học (Touch, Multi-touch, Stylus).

4. **Bảo toàn Chức năng & Chống hồi quy (Zero Regression Guarantee):**
   * **RÀNG BUỘC TỐI QUAN TRỌNG:** Tuyệt đối không được làm ảnh hưởng, sai lệch hoặc phá vỡ bất kỳ chức năng hiện có nào khác của hệ thống khi sửa lỗi. Mọi can thiệp phải có tính khoanh vùng, an toàn và được kiểm tra tính tương thích toàn diện.