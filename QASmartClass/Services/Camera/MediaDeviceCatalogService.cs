using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace QASmartTouch.Services.Camera
{
    public sealed class AudioEndpointOption
    {
        public string DeviceId { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string RawFriendlyName { get; init; } = "";
        public bool IsDefault { get; init; }
        public bool IsVirtual { get; init; }
        public override string ToString() => DisplayName;
    }

    public sealed class CameraEndpointOption
    {
        public string DeviceId { get; init; } = "0";
        public string DisplayName { get; init; } = "";
        public bool IsVirtual { get; init; }
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Dịch vụ danh mục phần cứng Âm thanh (WASAPI Mic / System Loopback) & Camera dùng chung
    /// cho cả QA SmartTouch (Ghi hình bài giảng) và QA SmartClass (Trình chiếu trực tiếp chuẩn Google Meet).
    /// Tuân thủ tuyệt đối nguyên tắc Zero Fake Hardware: không sinh thiết bị giả khi thiếu phần cứng.
    /// </summary>
    public static class MediaDeviceCatalogService
    {
        public static List<AudioEndpointOption> GetCaptureMicrophones()
        {
            var result = new List<AudioEndpointOption>();
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                string defaultId = "";
                try
                {
                    using var defEp = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                    defaultId = defEp?.ID ?? "";
                }
                catch { }

                var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                var sorted = endpoints
                    .OrderByDescending(d => !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(d => string.Equals(d.ID, defaultId, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var ep in sorted)
                {
                    using (ep)
                    {
                        bool isVirtual = ep.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase);
                        bool isDefault = string.Equals(ep.ID, defaultId, StringComparison.OrdinalIgnoreCase);
                        string suffix = isDefault
                            ? (isVirtual ? " [Thiết bị ảo]" : " [Mặc định]")
                            : (isVirtual ? " [Thiết bị ảo]" : "");

                        result.Add(new AudioEndpointOption
                        {
                            DeviceId = ep.ID,
                            RawFriendlyName = ep.FriendlyName,
                            DisplayName = $"{ep.FriendlyName}{suffix}",
                            IsDefault = isDefault,
                            IsVirtual = isVirtual
                        });
                    }
                }
            }
            catch { }

            return result;
        }

        public static List<AudioEndpointOption> GetSystemRenderLoopbacks()
        {
            var result = new List<AudioEndpointOption>();
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                string defaultId = "";
                try
                {
                    using var defEp = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    defaultId = defEp?.ID ?? "";
                }
                catch { }

                var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
                var sorted = endpoints
                    .OrderByDescending(d => string.Equals(d.ID, defaultId, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var ep in sorted)
                {
                    using (ep)
                    {
                        bool isDefault = string.Equals(ep.ID, defaultId, StringComparison.OrdinalIgnoreCase);
                        string suffix = isDefault ? " [Mặc định]" : "";
                        result.Add(new AudioEndpointOption
                        {
                            DeviceId = ep.ID,
                            RawFriendlyName = ep.FriendlyName,
                            DisplayName = $"{ep.FriendlyName}{suffix}",
                            IsDefault = isDefault,
                            IsVirtual = false
                        });
                    }
                }
            }
            catch { }

            return result;
        }

        public static async Task<List<CameraEndpointOption>> GetCamerasAsync()
        {
            var result = new List<CameraEndpointOption>();
            try
            {
                var discovery = new CameraDiscoveryService();
                var cameras = await discovery.RefreshCamerasAsync().ConfigureAwait(false);
                if (cameras != null && cameras.Count > 0)
                {
                    var sorted = cameras
                        .OrderByDescending(c => !c.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var cam in sorted)
                    {
                        bool isVirtual = cam.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase);
                        string suffix = isVirtual ? " [Thiết bị ảo]" : "";
                        result.Add(new CameraEndpointOption
                        {
                            DeviceId = cam.DeviceId,
                            DisplayName = $"{cam.FriendlyName}{suffix}",
                            IsVirtual = isVirtual
                        });
                    }
                }
            }
            catch { }

            return result;
        }

        public static double ComputeNormalizedMeterPercent(float rawPeak)
        {
            if (rawPeak <= 0.008f) return 0.0;
            return Math.Min(100.0, Math.Pow(rawPeak, 0.55) * 115.0);
        }

        public static double SmoothMeterLevel(double currentSmoothed, double targetPercent)
        {
            if (targetPercent >= currentSmoothed)
            {
                return currentSmoothed * 0.3 + targetPercent * 0.7;
            }
            double decayed = currentSmoothed * 0.65 + targetPercent * 0.35;
            return decayed < 1.0 ? 0.0 : decayed;
        }
    }
}

