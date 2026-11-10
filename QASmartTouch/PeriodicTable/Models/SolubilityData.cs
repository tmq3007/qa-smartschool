using System;
using System.Collections.Generic;

namespace QASmartTouch.PeriodicTable.Models
{
    /// <summary>
    /// Model chính chứa toàn bộ dữ liệu biểu đồ hòa tan
    /// </summary>
    public class SolubilityData
    {
        /// <summary>
        /// Thông tin metadata về dữ liệu
        /// </summary>
        public Metadata Metadata { get; set; }

        /// <summary>
        /// Danh sách 14 cation
        /// </summary>
        public List<Ion> Cations { get; set; }

        /// <summary>
        /// Danh sách 17 anion
        /// </summary>
        public List<Ion> Anions { get; set; }

        /// <summary>
        /// Ma trận 238 muối với thông tin tính tan
        /// </summary>
        public List<SaltInfo> SolubilityMatrix { get; set; }

        /// <summary>
        /// Danh sách 15 quy tắc hòa tan
        /// </summary>
        public List<SolubilityRule> Rules { get; set; }
    }

    /// <summary>
    /// Model chứa thông tin metadata
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// Phiên bản dữ liệu
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Ngày cập nhật cuối cùng
        /// </summary>
        public string LastUpdate { get; set; }

        /// <summary>
        /// Mô tả về dữ liệu
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Tổng số cation
        /// </summary>
        public int TotalCations { get; set; }

        /// <summary>
        /// Tổng số anion
        /// </summary>
        public int TotalAnions { get; set; }

        /// <summary>
        /// Tổng số muối
        /// </summary>
        public int TotalSalts { get; set; }
    }
}

