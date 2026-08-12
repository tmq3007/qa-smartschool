using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service for screen recording as animated GIF or WebP
    /// Supports both GIF (optimized) and WebP (zero dithering)
    /// </summary>
    public class ScreenRecorderService
    {
        private bool _isRecording = false;
        private DateTime _recordingStartTime;
        private string _defaultSavePath;
        private string? _currentRecordingPath;
        private CancellationTokenSource? _recordingCancellation;
        private List<System.Drawing.Bitmap> _capturedFrames = new List<System.Drawing.Bitmap>();
        private Task? _recordingTask;
        private Task? _saveTask;
        private RecordingFormat _recordingFormat = RecordingFormat.GIF;

        public bool IsRecording => _isRecording;
        public bool IsSaving => _saveTask != null && !_saveTask.IsCompleted;
        public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
        public RecordingFormat CurrentFormat => _recordingFormat;

        /// <summary>Raised on UI thread when the save operation completes</summary>
        public event Action<string?>? SaveCompleted;

        public ScreenRecorderService()
        {
            // Default save path: Documents/QASmartTouch/Recordings
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _defaultSavePath = Path.Combine(documentsPath, "QASmartTouch", "Recordings");
            
            // Create directory if not exists
            Directory.CreateDirectory(_defaultSavePath);
            
            System.Diagnostics.Debug.WriteLine($"✅ ScreenRecorderService initialized. Save path: {_defaultSavePath}");
        }

        /// <summary>
        /// Start recording
        /// </summary>
        public void StartRecording(string? customPath = null)
        {
            if (_isRecording)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Already recording");
                return;
            }

            try
            {
                // Choose file extension based on format
                var extension = _recordingFormat == RecordingFormat.WebP ? ".webp" : ".gif";
                var fileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var savePath = customPath ?? _defaultSavePath;
                _currentRecordingPath = Path.Combine(savePath, fileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(savePath);
                
                _recordingStartTime = DateTime.Now;
                _isRecording = true;
                _capturedFrames.Clear();
                _recordingCancellation = new CancellationTokenSource();
                
                // Start capture task
                _recordingTask = Task.Run(() => CaptureFrames(_recordingCancellation.Token));
                
                System.Diagnostics.Debug.WriteLine($"⏺️ GIF Recording started: {_currentRecordingPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error starting recording: {ex.Message}");
                _isRecording = false;
                throw;
            }
        }

        /// <summary>
        /// Capture frames continuously
        /// </summary>
        private void CaptureFrames(CancellationToken cancellationToken)
        {
            const int fps = 10; // 10 frames per second (doubled for better quality)
            const int frameDelay = 1000 / fps; // 100ms per frame

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Capture screen
                    var frame = CaptureScreen();
                    if (frame != null)
                    {
                        lock (_capturedFrames)
                        {
                            _capturedFrames.Add(frame);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"📸 Frame captured: {_capturedFrames.Count}");
                    }

                    // Wait for next frame
                    Thread.Sleep(frameDelay);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Frame capture error: {ex.Message}");
            }
        }

        /// <summary>
        /// Capture current screen with DPI-aware physical pixel dimensions
        /// </summary>
        private System.Drawing.Bitmap? CaptureScreen()
        {
            try
            {
                // L\u1ea5y DPI th\u1ef1c c\u1ee7a system (kh\u00f4ng ph\u1ea3i WPF DIPs)
                double dpiX = 1.0, dpiY = 1.0;
                using (var gDpi = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpiX = gDpi.DpiX / 96.0;
                    dpiY = gDpi.DpiY / 96.0;
                }

                // T\u00ednh k\u00edch th\u01b0\u1edbc physical pixel c\u1ee7a m\u00e0n h\u00ecnh
                int physW = (int)(SystemParameters.PrimaryScreenWidth  * dpiX);
                int physH = (int)(SystemParameters.PrimaryScreenHeight * dpiY);

                var bitmap = new System.Drawing.Bitmap(physW, physH,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.CopyFromScreen(0, 0, 0, 0, bitmap.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\u274c Screen capture error: {ex.Message}");
                return null;
            }
        }

        /// <summary>Stop recording — returns immediately; saving runs in background.</summary>
        public void StopRecording()
        {
            if (!_isRecording)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Not recording");
                return;
            }

            _isRecording = false;
            var duration = DateTime.Now - _recordingStartTime;

            // Dừng capture task ngay lập tức
            _recordingCancellation?.Cancel();

            // Chụp lại các frames đã ghi để lưu
            List<System.Drawing.Bitmap> framesToSave;
            lock (_capturedFrames)
            {
                framesToSave = new List<System.Drawing.Bitmap>(_capturedFrames);
                _capturedFrames.Clear();
            }

            var pathToSave  = _currentRecordingPath;
            var saveFormat  = _recordingFormat;
            _currentRecordingPath = null;

            System.Diagnostics.Debug.WriteLine(
                $"⏹️ Recording stopped. Frames: {framesToSave.Count}, Duration: {duration:mm\\:ss}");

            if (framesToSave.Count == 0 || pathToSave == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No frames to save");
                SaveCompleted?.Invoke(null);
                return;
            }

            // Lưu file trong background (không block UI)
            _saveTask = Task.Run(() =>
            {
                string? savedPath = null;
                try
                {
                    System.Diagnostics.Debug.WriteLine($"💾 Saving {saveFormat}: {pathToSave}");

                    if (saveFormat == RecordingFormat.WebP)
                        SaveAsWebP(pathToSave, framesToSave);
                    else
                        SaveAsGif(pathToSave, framesToSave);

                    savedPath = System.IO.File.Exists(pathToSave) ? pathToSave : null;
                    System.Diagnostics.Debug.WriteLine($"✅ Saved: {savedPath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Save error: {ex.Message}");
                }
                finally
                {
                    foreach (var f in framesToSave) f?.Dispose();
                    // Raise event trên UI thread
                    System.Windows.Application.Current?.Dispatcher.Invoke(
                        () => SaveCompleted?.Invoke(savedPath));
                }
            });
        }

        /// <summary>
        /// Save frames as animated GIF — fast version using Graphics.DrawImage
        /// </summary>
        private void SaveAsGif(string path, List<System.Drawing.Bitmap> frames)
        {
            if (frames.Count == 0) return;

            // Scale down để giảm kích thước file và tăng tốc độ encode
            const int maxW = 1280;
            const int maxH = 720;

            var scaledFrames = new List<System.Drawing.Bitmap>();
            try
            {
                foreach (var src in frames)
                {
                    double scaleW = (double)maxW / src.Width;
                    double scaleH = (double)maxH / src.Height;
                    double scale  = Math.Min(1.0, Math.Min(scaleW, scaleH));
                    int w = (int)(src.Width  * scale);
                    int h = (int)(src.Height * scale);

                    // Chuyển sang Format32bppArgb rồi về Format8bppIndexed qua DrawImage
                    var scaled = new System.Drawing.Bitmap(w, h,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var g = System.Drawing.Graphics.FromImage(scaled))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, 0, 0, w, h);
                    }
                    scaledFrames.Add(scaled);
                }

                // Lấy GIF encoder đúng cách
                var encoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Gif);
                if (encoder == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ GIF encoder not found, saving first frame only");
                    scaledFrames[0].Save(path, System.Drawing.Imaging.ImageFormat.Gif);
                    return;
                }

                var ep = new System.Drawing.Imaging.EncoderParameters(1);
                ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.SaveFlag,
                    (long)System.Drawing.Imaging.EncoderValue.MultiFrame);

                scaledFrames[0].Save(path, encoder, ep);

                ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.SaveFlag,
                    (long)System.Drawing.Imaging.EncoderValue.FrameDimensionTime);

                for (int i = 1; i < scaledFrames.Count; i++)
                    scaledFrames[0].SaveAdd(scaledFrames[i], ep);

                ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.SaveFlag,
                    (long)System.Drawing.Imaging.EncoderValue.Flush);
                scaledFrames[0].SaveAdd(ep);

                System.Diagnostics.Debug.WriteLine(
                    $"✅ GIF saved: {scaledFrames.Count} frames @ {scaledFrames[0].Width}x{scaledFrames[0].Height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GIF save error: {ex.Message}");
                if (frames.Count > 0)
                    frames[0].Save(path, System.Drawing.Imaging.ImageFormat.Gif);
            }
            finally
            {
                foreach (var f in scaledFrames) f?.Dispose();
            }
        }

        /// <summary>
        /// Optimize frames for GIF encoding to reduce dithering
        /// </summary>
        private List<System.Drawing.Bitmap> OptimizeFramesForGif(List<System.Drawing.Bitmap> frames)
        {
            var optimizedFrames = new List<System.Drawing.Bitmap>();
            
            try
            {
                foreach (var frame in frames)
                {
                    // Create optimized bitmap with better color quantization
                    var optimized = new System.Drawing.Bitmap(frame.Width, frame.Height, 
                        System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    
                    // Copy palette from original (this helps reduce dithering)
                    var palette = optimized.Palette;
                    
                    // Build optimized 256-color palette from frame
                    BuildOptimizedPalette(frame, palette);
                    optimized.Palette = palette;
                    
                    // Copy pixels with optimized color matching
                    CopyWithOptimizedColors(frame, optimized);
                    
                    optimizedFrames.Add(optimized);
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Optimized {frames.Count} frames for GIF");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Frame optimization failed: {ex.Message}, using original frames");
                // Return original frames if optimization fails
                return frames;
            }
            
            return optimizedFrames;
        }

        /// <summary>
        /// Build optimized 256-color palette from source image
        /// </summary>
        private void BuildOptimizedPalette(System.Drawing.Bitmap source, System.Drawing.Imaging.ColorPalette palette)
        {
            // Use median cut algorithm for better color selection
            var colors = new Dictionary<System.Drawing.Color, int>();
            
            // Sample colors from image (every 4th pixel for performance)
            for (int y = 0; y < source.Height; y += 4)
            {
                for (int x = 0; x < source.Width; x += 4)
                {
                    var color = source.GetPixel(x, y);
                    if (colors.ContainsKey(color))
                        colors[color]++;
                    else
                        colors[color] = 1;
                }
            }
            
            // Get top 256 most used colors
            var topColors = colors.OrderByDescending(c => c.Value)
                                 .Take(256)
                                 .Select(c => c.Key)
                                 .ToArray();
            
            // Fill palette
            for (int i = 0; i < palette.Entries.Length && i < topColors.Length; i++)
            {
                palette.Entries[i] = topColors[i];
            }
            
            // Fill remaining with grayscale
            for (int i = topColors.Length; i < palette.Entries.Length; i++)
            {
                int gray = (i * 255) / palette.Entries.Length;
                palette.Entries[i] = System.Drawing.Color.FromArgb(gray, gray, gray);
            }
        }

        /// <summary>
        /// Copy pixels with optimized color matching to reduce dithering
        /// </summary>
        private void CopyWithOptimizedColors(System.Drawing.Bitmap source, System.Drawing.Bitmap dest)
        {
            var palette = dest.Palette.Entries;
            var destData = dest.LockBits(
                new System.Drawing.Rectangle(0, 0, dest.Width, dest.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                dest.PixelFormat);
            
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)destData.Scan0;
                    
                    for (int y = 0; y < source.Height; y++)
                    {
                        for (int x = 0; x < source.Width; x++)
                        {
                            var color = source.GetPixel(x, y);
                            var paletteIndex = FindNearestColorIndex(color, palette);
                            ptr[y * destData.Stride + x] = (byte)paletteIndex;
                        }
                    }
                }
            }
            finally
            {
                dest.UnlockBits(destData);
            }
        }

        /// <summary>
        /// Find nearest color in palette using Euclidean distance
        /// </summary>
        private int FindNearestColorIndex(System.Drawing.Color color, System.Drawing.Color[] palette)
        {
            int nearestIndex = 0;
            int minDistance = int.MaxValue;
            
            for (int i = 0; i < palette.Length; i++)
            {
                int distance = ColorDistance(color, palette[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }
            
            return nearestIndex;
        }

        /// <summary>
        /// Calculate color distance using weighted RGB
        /// </summary>
        private int ColorDistance(System.Drawing.Color c1, System.Drawing.Color c2)
        {
            // Weighted RGB distance (human eye is more sensitive to green)
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            
            return (dr * dr * 2) + (dg * dg * 4) + (db * db * 3);
        }

        /// <summary>
        /// Get image encoder
        /// </summary>
        private System.Drawing.Imaging.ImageCodecInfo? GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            // Phải dùng GetImageEncoders() (không phải GetImageDecoders)
            var codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
                if (codec.FormatID == format.Guid) return codec;
            return null;
        }

        /// <summary>
        /// Get formatted recording time
        /// </summary>
        public string GetFormattedRecordingTime()
        {
            if (!_isRecording) return "00:00";
            
            var duration = RecordingDuration;
            return $"{duration:mm\\:ss}";
        }

        /// <summary>
        /// Set default save path
        /// </summary>
        public void SetDefaultSavePath(string path)
        {
            _defaultSavePath = path;
            Directory.CreateDirectory(path);
            System.Diagnostics.Debug.WriteLine($"📁 Recording save path updated: {path}");
        }

        /// <summary>
        /// Get default save path
        /// </summary>
        public string GetDefaultSavePath()
        {
            return _defaultSavePath;
        }

        /// <summary>
        /// Open recordings folder
        /// </summary>
        public void OpenRecordingsFolder()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _defaultSavePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error opening folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Set recording format (GIF or WebP)
        /// </summary>
        public void SetRecordingFormat(RecordingFormat format)
        {
            _recordingFormat = format;
            System.Diagnostics.Debug.WriteLine($"📹 Recording format set to: {format}");
        }

        /// <summary>
        /// Get current recording format
        /// </summary>
        public RecordingFormat GetRecordingFormat()
        {
            return _recordingFormat;
        }

        /// <summary>
        /// Save frames as WebP with zero dithering (first frame only for now)
        /// </summary>
        private void SaveAsWebP(string path, List<System.Drawing.Bitmap> frames)
        {
            if (frames.Count == 0) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"🎬 Encoding WebP (first frame)...");

                // Use Imazen.WebP library for encoding
                // Note: Animated WebP support is complex and requires additional implementation
                // For now, we save the first frame as high-quality static WebP
                
                using (var ms = new System.IO.MemoryStream())
                {
                    var encoder = new Imazen.WebP.SimpleEncoder();
                    encoder.Encode(frames[0], ms, 90f); // Quality 90
                    
                    File.WriteAllBytes(path, ms.ToArray());
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ WebP created (first frame, quality 90, zero dithering)");
                if (frames.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Note: Saved first frame only. Use GIF for animation.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WebP save error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Falling back to GIF...");
                
                // Fallback to GIF if WebP fails
                SaveAsGif(path.Replace(".webp", ".gif"), frames);
            }
        }
    }
}
