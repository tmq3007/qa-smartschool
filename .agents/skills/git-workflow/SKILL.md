---
name: git-workflow
description: >-
  Quy chuẩn tạo nhánh (Git Branch) và viết Commit Message cho dự án QA SmartSchool.
  Kích hoạt skill này khi người dùng yêu cầu tạo branch mới, gợi ý tên branch, hoặc soạn commit message theo chuẩn dự án.
---

# Quy chuẩn Git Branch & Commit Message - QA SmartSchool

Tài liệu hướng dẫn và ràng buộc quy chuẩn khi thực hiện các thao tác Git (tạo branch, viết commit message) trong dự án.

---

## 1. QUY ƯỚC ĐẶT TÊN BRANCH NGẮN HẠN

Mọi nhánh làm việc cá nhân bắt buộc tuân theo format:
`<type>/<short-description>`

### 1.1. Phân loại Branch Type

| Loại Branch | Định dạng | Ví dụ chuẩn | Trường hợp sử dụng |
| :--- | :--- | :--- | :--- |
| **Feature** | `feature/<name>` | `feature/login`, `feature/agent-integration` | Phát triển tính năng mới |
| **Bugfix** | `bugfix/<name>` | `bugfix/login-error` | Sửa lỗi trong giai đoạn phát triển/test |
| **Hotfix** | `hotfix/<name>` | `hotfix/payment-crash` | Sửa lỗi khẩn cấp trên Production |
| **Refactor** | `refactor/<name>` | `refactor/user-service` | Tối ưu hóa, cải tiến cấu trúc code |
| **Docs** | `docs/<name>` | `docs/api-documentation` | Cập nhật tài liệu kỹ thuật |

### 1.2. Tiêu chuẩn so sánh đặt tên Branch

| Tiêu chuẩn | KHÔNG NÊN (Không đạt) | NÊN (Đạt chuẩn) | Lý do |
| :--- | :--- | :--- | :--- |
| **Ký tự & Định dạng** | `Feature/Login`, `fix_bug` | `feature/login`, `bugfix/payment` | Dùng tiếng Anh, viết thường, phân cách bằng `-` |
| **Nội dung** | `test`, `abc`, `branch1`, `mybranch` | `feature/order-management` | Rõ nghĩa, mô tả đúng nghiệp vụ |
| **Tên cá nhân** | `nam/login`, `huy/fix-db` | `feature/customer-reservation` | Không dùng tên riêng, quản lý theo module |

---

## 2. QUY CHUẨN COMMIT MESSAGE

Format bắt buộc của Commit Message:
`<type>: <description>`

### 2.1. Phân loại Commit Type

| Type | Ý nghĩa | Ví dụ mẫu |
| :--- | :--- | :--- |
| **feat** | Thêm tính năng mới | `feat: add user registration`, `feat: add agent skill` |
| **fix** | Sửa lỗi code | `fix: resolve duplicate order creation` |
| **refactor** | Tái cấu trúc code (không đổi logic/UI) | `refactor: simplify authentication service` |
| **perf** | Tối ưu hiệu năng | `perf: optimize order query` |
| **test** | Thêm hoặc sửa Unit/Integration Test | `test: add unit tests for order service` |
| **docs** | Thay đổi tài liệu | `docs: update API documentation` |
| **style** | Format code (khoảng trắng, dấu chấm phẩy) | `style: format java user controller` |
| **chore** | Cập nhật config, dependencies | `chore: update dependencies` |
| **build** | Thay đổi hệ thống build (Gradle, Maven, MSBuild) | `build: update gradle configuration` |
| **ci** | Thay đổi tệp tin cấu hình CI/CD | `ci: update deployment workflow` |

---

## 3. CÁCH ỨNG DỤNG CHO CÁC TÁC VỤ THƯỜNG GẶP

### 3.1. Khi tạo Branch cho tính năng mới (ví dụ: Thêm Agent)
```bash
# Tạo và chuyển sang branch mới
git checkout -b feature/agent-integration

# Hoặc dùng lệnh git switch
git switch -c feature/agent-integration
```

### 3.2. Khi Commit thay đổi
```bash
git add .
git commit -m "feat: add git workflow agent skill"
```
