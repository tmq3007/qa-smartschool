using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace QASmartClass.Converters
{
    public class ActionTranslationConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Login", "Đăng nhập hệ thống" },
            { "Logout", "Đăng xuất hệ thống" },
            { "Save_SchoolMenu", "Lưu thực đơn trường" },
            { "Approve_LeaveRequest", "Phê duyệt đơn xin nghỉ" },
            { "Reject_LeaveRequest", "Từ chối đơn xin nghỉ" },
            { "Export_MOET_Report", "Xuất báo cáo Bộ GD&ĐT" },
            { "Payroll_Calculated", "Tính lương nhân sự" },
            { "Payroll_BatchCalculated", "Tính lương hàng loạt" },
            { "Payroll_Finalized", "Chốt bảng lương" },
            { "Send_PushNotification", "Gửi thông báo đẩy" },
            { "Add_Asset", "Thêm tài sản/thiết bị" },
            { "Report_Asset", "Báo cáo hỏng hóc tài sản" },
            { "Fix_Asset", "Sửa chữa tài sản" },
            { "Decommission_Asset", "Thanh lý tài sản" },
            { "Book_Asset", "Đặt lịch mượn thiết bị" },
            { "Add_SecurityLog", "Ghi nhật ký bảo vệ" },
            { "Task_Created", "Giao việc mới" },
            { "Delete_Task", "Xóa công việc" },
            { "AI_Copilot", "Sử dụng Trợ lý AI" },
            { "Question_Approved", "Duyệt câu hỏi" },
            { "Question_Rejected", "Từ chối câu hỏi" },
            { "Request_Parent_Signature", "Yêu cầu chữ ký phụ huynh" },
            { "Parent_Signed", "Phụ huynh ký duyệt" },
            { "Share_Question", "Chia sẻ câu hỏi ngân hàng" },
            { "Delete_Question", "Xóa câu hỏi" },
            { "Bulk_Approve_Questions", "Duyệt câu hỏi hàng loạt" },
            { "Bulk_Reject_Questions", "Từ chối câu hỏi hàng loạt" },
            { "Apply_Difficulty_Recommendation", "Cập nhật độ khó câu hỏi" },
            { "Assess_SoftSkill", "Đánh giá kỹ năng mềm" },
            { "Medical_Emergency_Resolved", "Xử lý sự cố y tế khẩn cấp" },
            { "Update_Config", "Cập nhật cấu hình hệ thống" },
            { "Backup_Database", "Sao lưu cơ sở dữ liệu" },
            { "Restore_Database", "Khôi phục cơ sở dữ liệu" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string actionKey)
            {
                if (_translations.TryGetValue(actionKey, out string translated))
                {
                    return translated;
                }
                // Fallback: Trả về chính chuỗi gốc nếu không tìm thấy từ dịch
                return actionKey;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
