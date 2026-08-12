using System;

namespace QASmartTouch.PeriodicTable.Models
{
    /// <summary>
    /// Model đại diện cho thông tin về một muối và tính tan của nó
    /// </summary>
    public class SaltInfo
    {
        /// <summary>
        /// Ký hiệu cation (VD: Na⁺, Ca²⁺)
        /// </summary>
        public string Cation { get; set; }

        /// <summary>
        /// Ký hiệu anion (VD: Cl⁻, SO₄²⁻)
        /// </summary>
        public string Anion { get; set; }

        /// <summary>
        /// Trạng thái hòa tan: soluble, slightly_soluble, insoluble, reacts, na
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Mã màu hex để hiển thị (#4CAF50, #FFC107, #F44336, #9C27B0, #9E9E9E)
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Công thức hóa học của muối (VD: NaCl, CaSO₄)
        /// </summary>
        public string SaltFormula { get; set; }

        /// <summary>
        /// Tên tiếng Việt của muối (VD: Natri clorua, Canxi sunfat)
        /// </summary>
        public string SaltName { get; set; }

        /// <summary>
        /// Mô tả độ tan bằng chữ (tan, ít tan, không tan, phản ứng, không tồn tại)
        /// </summary>
        public string SolubilityValue { get; set; }

        /// <summary>
        /// Ghi chú bổ sung (màu sắc kết tủa, ứng dụng, tính chất đặc biệt)
        /// </summary>
        public string Note { get; set; }
    }
}

