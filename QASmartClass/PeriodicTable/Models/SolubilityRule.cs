using System;
using System.Collections.Generic;

namespace QASmartTouch.PeriodicTable.Models
{
    /// <summary>
    /// Model đại diện cho một quy tắc hòa tan trong hóa học
    /// </summary>
    public class SolubilityRule
    {
        /// <summary>
        /// ID của quy tắc
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Danh mục quy tắc (Muối tan, Halogenua, Sunfat, Hiđroxit, ...)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Nội dung quy tắc (VD: "Tất cả muối Na⁺, K⁺, NH₄⁺ đều TAN")
        /// </summary>
        public string Rule { get; set; }

        /// <summary>
        /// Danh sách ví dụ minh họa
        /// </summary>
        public List<string> Examples { get; set; }

        /// <summary>
        /// Danh sách ngoại lệ của quy tắc
        /// </summary>
        public List<string> Exceptions { get; set; }
    }
}

