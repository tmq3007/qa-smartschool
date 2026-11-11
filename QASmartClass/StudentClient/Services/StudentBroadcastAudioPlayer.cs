using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Serilog;

namespace QASmartClass.StudentClient.Services
{
    /// <summary>
    /// Bộ thu và phát âm thanh trình chiếu trực tiếp (/api/audio_stream — 24kHz 16-bit Signed PCM Mono)
    /// cho máy Học sinh Windows PC, đồng bộ với luồng màn hình chuẩn Google Meet.
    /// </summary>
    public sealed class StudentBroadcastAudioPlayer : IDisposable
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private CancellationTokenSource? _cts;
        private WaveOutEvent? _waveOut;
        private BufferedWaveProvider? _waveBuffer;
        private volatile bool _isMuted;
        private float _volume = 1.0f;

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                if (value)
                {
                    try { _waveBuffer?.ClearBuffer(); } catch { }
                }
            }
        }

        public bool IsSameMachineDefaultMuted { get; private set; }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0f, 1f);
                try
                {
                    if (_waveOut != null) _waveOut.Volume = _volume;
                }
                catch { }
            }
        }

        public void Start(string? teacherIp, int webPort, string? broadcastToken)
        {
            Stop();

            string resolvedIp = string.IsNullOrWhiteSpace(teacherIp) ? "127.0.0.1" : teacherIp.Trim();
            int resolvedPort = webPort > 0 ? webPort : 8080;

            // Nếu mở cả Giáo viên và Học sinh trên cùng 1 máy PC -> mặc định tắt loa HS để tránh dội ngược vào WasapiLoopbackCapture
            IsSameMachineDefaultMuted = IsLocalMachineIp(resolvedIp);
            _isMuted = IsSameMachineDefaultMuted;

            var cts = new CancellationTokenSource();
            _cts = cts;

            _ = Task.Run(() => RunStreamLoopAsync(resolvedIp, resolvedPort, broadcastToken ?? "", cts.Token), cts.Token);
        }

        public void Stop()
        {
            var cts = _cts;
            _cts = null;
            if (cts != null)
            {
                try { cts.Cancel(); } catch { }
                try { cts.Dispose(); } catch { }
            }

            CleanupWaveOut();
        }

        private async Task RunStreamLoopAsync(string teacherIp, int webPort, string token, CancellationToken ct)
        {
            try
            {
                var waveFormat = new WaveFormat(24000, 16, 1);
                var buffer = new BufferedWaveProvider(waveFormat)
                {
                    BufferDuration = TimeSpan.FromMilliseconds(420),
                    DiscardOnBufferOverflow = true
                };
                _waveBuffer = buffer;

                try
                {
                    var waveOut = new WaveOutEvent
                    {
                        DesiredLatency = 90,
                        NumberOfBuffers = 3
                    };
                    waveOut.Init(buffer);
                    waveOut.Volume = _volume;
                    waveOut.Play();
                    _waveOut = waveOut;
                }
                catch (Exception audioInitEx)
                {
                    // Máy học sinh không gắn loa/tai nghe -> vẫn chạy ổn định không báo lỗi
                    Log.Warning("[StudentAudio] Không tìm thấy thiết bị loa đầu ra trên máy HS: {Err}", audioInitEx.Message);
                    return;
                }

                var headerBuf = new byte[2];
                var pcmBuf = new byte[8192];

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        string url = $"http://{teacherIp}:{webPort}/api/audio_stream"
                                   + (!string.IsNullOrEmpty(token) ? $"?token={Uri.EscapeDataString(token)}" : "");

                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        using var resp = await SharedHttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode)
                        {
                            await Task.Delay(800, ct).ConfigureAwait(false);
                            continue;
                        }

                        using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                        while (!ct.IsCancellationRequested)
                        {
                            if (!await ReadExactAsync(stream, headerBuf, 2, ct).ConfigureAwait(false))
                                break;

                            int packetLen = (headerBuf[0] << 8) | headerBuf[1];
                            if (packetLen <= 0 || packetLen > 16384)
                                break;

                            if (pcmBuf.Length < packetLen)
                                pcmBuf = new byte[packetLen];

                            if (!await ReadExactAsync(stream, pcmBuf, packetLen, ct).ConfigureAwait(false))
                                break;

                            if (!_isMuted && _waveBuffer != null)
                            {
                                // Chống trễ tích lũy: nếu đệm > 240ms thì xả bớt để luôn đồng bộ với hình ảnh
                                if (_waveBuffer.BufferedDuration.TotalMilliseconds > 260)
                                {
                                    _waveBuffer.ClearBuffer();
                                }
                                _waveBuffer.AddSamples(pcmBuf, 0, packetLen);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception streamEx)
                    {
                        Log.Debug("[StudentAudio] Audio stream reconnecting: {Err}", streamEx.Message);
                    }

                    if (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(500, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                CleanupWaveOut();
            }
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int count, CancellationToken ct)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct).ConfigureAwait(false);
                if (read <= 0) return false;
                offset += read;
            }
            return true;
        }

        private void CleanupWaveOut()
        {
            var wo = _waveOut;
            _waveOut = null;
            _waveBuffer = null;
            if (wo != null)
            {
                try { wo.Stop(); } catch { }
                try { wo.Dispose(); } catch { }
            }
        }

        private static bool IsLocalMachineIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return true;
            if (ip.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                ip.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                ip.Equals("::1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                if (!IPAddress.TryParse(ip, out var targetAddr)) return false;
                if (IPAddress.IsLoopback(targetAddr)) return true;

                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork && ua.Address.Equals(targetAddr))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

