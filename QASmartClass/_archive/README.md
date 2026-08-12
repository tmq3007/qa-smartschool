# 📁 _archive — Scripts & Tools Archive

> **Ngày di chuyển:** 2026-05-14
> **Lý do:** Dọn dẹp root directory, tách scripts bảo trì ra khỏi production code

## Nội dung

Thư mục `scripts/` chứa các Python scripts dùng 1 lần để sửa lỗi hàng loạt trong quá trình phát triển. Các scripts này **không phải production code** và không ảnh hưởng đến build.

### Danh sách scripts:
- `fix_fonts.py` — Sửa font Consolas → Segoe UI
- `fix_bounds.py` — Sửa bounds cho controls
- `fix_corner_radius.py` — Chuẩn hóa corner radius
- `fix_xaml.py` — Sửa lỗi XAML hàng loạt
- `fix_pareto.py` — Sửa Pareto chart tool
- `patch.py` — Patch general fixes
- `clean_data.py` — Dọn dữ liệu test
- `generate_templates.py` — Tạo templates cho LearningTools
- Và các file khác...

> ⚠️ **Không xóa thư mục này** — giữ lại để tham khảo nếu cần.
