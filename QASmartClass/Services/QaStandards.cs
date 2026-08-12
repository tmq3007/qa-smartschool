using System;

namespace QASmartClass.Services
{
    public static class QaStandards
    {
        // Ràng buộc sư phạm (SP-xx)
        public const double MaxLatencyMs = 200.0;       // SP-01: Độ trễ tối đa (LAN)
        public const double PeakLatencyMs = 400.0;      // SP-02: Độ trễ đỉnh chấp nhận
        public const double MaxFrameDropRate = 0.01;     // SP-03: Tỉ lệ frame bị bỏ tối đa (1%)
        public const double MinSsim = 0.90;             // SP-04: SSIM độ nét tối thiểu
        public const double MinTouchTargetPx = 40.0;     // SP-05: Kích thước touch target

        // Ràng buộc kỹ thuật (TC-xx)
        public const int UdpPort = 1982;
        public const int TcpPort = 1980;
        public const int HttpPort = 8080;
    }
}
