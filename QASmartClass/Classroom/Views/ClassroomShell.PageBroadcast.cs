using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using QASmartClass.Classroom.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class ClassroomShell
    {
        private FrameworkElement? _lastTeachingPageElement;
        private string _lastTeachingPageTitle = "";

        /// <summary>
        /// Chụp ảnh xem trước của một trang trong QA SmartClass (phục vụ hộp chọn nguồn chiếu).
        /// </summary>
        public BitmapSource? TryGetQaPagePreviewBitmap(string pageFormId, int targetWidth = 320)
        {
            try
            {
                return CaptureQaClassPageBitmap(pageFormId, targetWidth, out _, createIfMissing: false);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Chụp trực tiếp giao diện một trang bên trong QA SmartClass (hoặc SMART TOUCH) thành BitmapSource để phát cho học sinh.
        /// Không bị ảnh hưởng bởi thanh menu bên trái của GV hoặc các cửa sổ khác trên màn hình.
        /// </summary>
        public BitmapSource? CaptureQaClassPageBitmap(string pageFormId, int targetWidth, out string resolvedTitle, bool createIfMissing = true)
        {
            resolvedTitle = "Trang SMART CLASS";
            try
            {
                if (pageFormId == "SMART_TOUCH")
                {
                    resolvedTitle = "Bảng trắng tương tác SMART TOUCH";
                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win.GetType().Name.Contains("Form2_MainDashboard", StringComparison.OrdinalIgnoreCase))
                        {
                            return ScreenSourceEnumerator.RenderElementToBitmap(win, targetWidth);
                        }
                    }
                    return null;
                }

                if (pageFormId == "CURRENT" || string.IsNullOrWhiteSpace(pageFormId))
                {
                    if (contentFrame?.Content is FrameworkElement currentEl)
                    {
                        // Nếu GV đang ở trang bài học (không phải trang Điều khiển phát sóng), lưu lại để nếu GV quay về tab Giám sát ngắn hạn vẫn giữ khung hình bài học
                        if (currentEl is not ClassControlHub && currentEl is not BroadcastPage && currentEl is not MonitorPage)
                        {
                            _lastTeachingPageElement = currentEl;
                            _lastTeachingPageTitle = txtPageTitle?.Text?.Trim() ?? "Trang đang mở trên SMART CLASS";
                        }

                        if ((currentEl is ClassControlHub || currentEl is BroadcastPage) && _lastTeachingPageElement != null)
                        {
                            resolvedTitle = _lastTeachingPageTitle;
                            return ScreenSourceEnumerator.RenderElementToBitmap(_lastTeachingPageElement, targetWidth);
                        }

                        resolvedTitle = txtPageTitle?.Text?.Trim() ?? "Trang đang mở trên SMART CLASS";
                        return ScreenSourceEnumerator.RenderElementToBitmap(currentEl, targetWidth);
                    }
                    return ScreenSourceEnumerator.RenderElementToBitmap(contentFrame, targetWidth);
                }

                if (pageFormId == "F_RUNNER" && _activeLessonRunner != null)
                {
                    resolvedTitle = "Tiết học đang trình chiếu";
                    return ScreenSourceEnumerator.RenderElementToBitmap(_activeLessonRunner, targetWidth);
                }

                if (!_pageCache.TryGetValue(pageFormId, out Page? targetPage) || targetPage == null)
                {
                    if (!createIfMissing) return null;
                    targetPage = CreatePageInstance(pageFormId == "F_RUNNER" ? "F2" : pageFormId);
                    if (targetPage != null)
                    {
                        _pageCache[pageFormId] = targetPage;
                    }
                }

                if (targetPage != null)
                {
                    resolvedTitle = !string.IsNullOrWhiteSpace(targetPage.Title) ? targetPage.Title : $"Trang {pageFormId}";
                    if (ReferenceEquals(contentFrame?.Content, targetPage))
                    {
                        return ScreenSourceEnumerator.RenderElementToBitmap(contentFrame, targetWidth);
                    }

                    if (targetPage.ActualWidth < 100 || targetPage.ActualHeight < 100)
                    {
                        double refW = (contentFrame != null && contentFrame.ActualWidth > 200) ? contentFrame.ActualWidth : 1280;
                        double refH = (contentFrame != null && contentFrame.ActualHeight > 200) ? contentFrame.ActualHeight : 720;
                        targetPage.Width = refW;
                        targetPage.Height = refH;
                    }
                    return ScreenSourceEnumerator.RenderElementToBitmap(targetPage, targetWidth);
                }
            }
            catch (Exception ex)
            {
                Log.Debug("[ClassroomShell] CaptureQaClassPageBitmap error ({Page}): {Err}", pageFormId, ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Điều hướng tới trang QA SmartClass được chọn để Giáo viên thao tác trực tiếp trong khi đang phát cho học sinh.
        /// </summary>
        public void NavigateToQaPageForBroadcast(string pageFormId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pageFormId) || pageFormId == "CURRENT") return;
                if (pageFormId == "SMART_TOUCH")
                {
                    _modeService?.GoToScreen();
                    return;
                }
                if (pageFormId == "F_RUNNER")
                {
                    if (_activeLessonRunner != null) NavigateTo("F_RUNNER");
                    else NavigateTo("F2");
                    return;
                }
                NavigateTo(pageFormId);
            }
            catch (Exception ex)
            {
                Log.Warning("[ClassroomShell] NavigateToQaPageForBroadcast error: {Err}", ex.Message);
            }
        }
    }
}

