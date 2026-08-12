using System;

namespace QASmartTouch.PeriodicTable.Models
{
    /// <summary>
    /// Model đại diện cho một ion (cation hoặc anion)
    /// </summary>
    public class Ion
    {
        /// <summary>
        /// Ký hiệu hóa học của ion (VD: Na⁺, Cl⁻, SO₄²⁻)
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Tên tiếng Việt của ion
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tên tiếng Anh của ion
        /// </summary>
        public string NameEnglish { get; set; }

        /// <summary>
        /// Nhóm hóa học (alkali_metal, halide, oxyanion, ...)
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Điện tích của ion (1, -1, 2, -2, ...)
        /// </summary>
        public int Charge { get; set; }
    }
}

