---
name: git-workflow
description: >-
  Quy chuẩn tạo nhánh (Git Branch) và viết Commit Message cho dự án QASmartTouch.
  Kích hoạt skill này khi người dùng yêu cầu tạo branch mới, gợi ý tên branch, hoặc soạn commit message theo chuẩn dự án.
---

# Quy chuẩn Git Branch & Commit Message — `QASmartTouch`

Tài liệu hướng dẫn quy chuẩn khi thực hiện các thao tác Git (tạo branch, soạn commit message) trong dự án `QASmartTouch`.

> **🚨 RÀNG BUỘC QUAN TRỌNG:** KHÔNG ĐƯỢC tự động thực thi các lệnh Git làm thay đổi lịch sử/nhánh (`git checkout`, `git commit`, `git push`, `git reset`...) khi chưa được người dùng yêu cầu rõ hoặc xác nhận. Luôn trình bày tên branch / commit message dự kiến để người dùng duyệt trước.

---

## 1. QUY ƯỚC ĐẶT TÊN BRANCH NGẮN HẠN

Mọi nhánh làm việc bắt buộc tuân theo format:
`<type>/<short-description>` (tiếng Anh, viết thường toàn bộ, phân cách bằng dấu gạch ngang `-`, không gắn tên cá nhân).

| Loại Branch | Định dạng | Ví dụ chuẩn cho `QASmartTouch` | Trường hợp sử dụng |
| :--- | :--- | :--- | :--- |
| **Feature** | `feature/<name>` | `feature/lasso-multi-group`, `feature/3d-cone-tool` | Phát triển tính năng mới |
| **Bugfix** | `bugfix/<name>` | `bugfix/touch-eraser-freeze`, `bugfix/undo-3d-color` | Sửa lỗi trong giai đoạn phát triển/test |
| **Hotfix** | `hotfix/<name>` | `hotfix/export-pdf-blank-ink` | Sửa lỗi khẩn cấp cho bản phát hành |
| **Refactor** | `refactor/<name>` | `refactor/extract-touch-gesture` | Tái cấu trúc, làm sạch code (không đổi hành vi) |
| **Docs** | `docs/<name>` | `docs/update-agent-rules` | Cập nhật tài liệu kỹ thuật / Rules & Skills |

---

## 2. QUY CHUẨN COMMIT MESSAGE (CONVENTIONAL COMMITS)

Format bắt buộc:
`<type>: <description>`

| Type | Ý nghĩa | Ví dụ mẫu cho `QASmartTouch` |
| :--- | :--- | :--- |
| **feat** | Thêm tính năng mới | `feat: add magic wand color tolerance indicator` |
| **fix** | Sửa lỗi code | `fix: rebuild quadtree after undo transform on 3d shapes` |
| **refactor** | Tái cấu trúc code (không đổi logic/UI) | `refactor: extract stroke slicing helper from input partial` |
| **perf** | Tối ưu hiệu năng / cảm ứng | `perf: throttle multi-touch erase hit-test on 86-inch display` |
| **test** | Thêm hoặc sửa Unit Test | `test: add regression tests for selection transform undo` |
| **docs** | Thay đổi tài liệu / `.agents` | `docs: standardize qasmarttouch agent rules and skills` |
| **style** | Chỉnh format XAML / C# thuần túy | `style: clean up indentation in SubMenuPen xaml` |
| **build** | Thay đổi cấu hình MSBuild / `.csproj` | `build: link new helper file in QASmartTouch.csproj` |
| **chore** | Cập nhật config, packages | `chore: update WebView2 package reference` |
