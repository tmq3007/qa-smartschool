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
        private FrameworkElement? _pinnedBroadcastPageElement;
        private string _pinnedBroadcastPageTitle = "";

        /// <summary>
        /// Khóa cố định (Pin) trang hiện tại trong SMART CLASS để chiếu cho Học sinh (chuẩn Google Meet "Share this tab").
        /// Khi Giáo viên chuyển sang tab Giám sát / Chat / trang khác, Học sinh vẫn chỉ nhìn thấy đúng trang đã khóa.
        /// </summary>
        public void PinCurrentPageForBroadcast(bool forceActiveFrameContent = false)
        {
            try
            {
                if (contentFrame?.Content is FrameworkElement currentEl)
                {
                    if (!forceActiveFrameContent
                        && (currentEl is ClassControlHub || currentEl is BroadcastPage || currentEl is MonitorPage)
                        && _lastTeachingPageElement != null)
                    {
                        _pinnedBroadcastPageElement = _lastTeachingPageElement;
                        _pinnedBroadcastPageTitle = !string.IsNullOrWhiteSpace(_lastTeachingPageTitle)
                            ? _lastTeachingPageTitle
                            : "Trang bài giảng đã khóa cố định";
                        return;
                    }

                    _pinnedBroadcastPageElement = currentEl;
                    _pinnedBroadcastPageTitle = txtPageTitle?.Text?.Trim() ?? "Trang đang mở trên SMART CLASS";
                    if (currentEl is not ClassControlHub && currentEl is not BroadcastPage && currentEl is not MonitorPage)
                    {
                        _lastTeachingPageElement = currentEl;
                        _lastTeachingPageTitle = _pinnedBroadcastPageTitle;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("[ClassroomShell] PinCurrentPageForBroadcast error: {Err}", ex.Message);
            }
        }

        public void ClearPinnedBroadcastPage()
        {
            _pinnedBroadcastPageElement = null;
            _pinnedBroadcastPageTitle = "";
        }

        public (string ActiveFormId, string ActiveTitle, bool CanSwitchHere) GetActiveQaPageSwitchCandidate(string? lockedPageFormId)
        {
            try
            {
                if (!IsVisible || WindowState == WindowState.Minimized) return ("", "", false);
                if (contentFrame?.Content is not FrameworkElement activeEl) return ("", "", false);
                if (activeEl is BroadcastPage || activeEl is MonitorPage || activeEl is ClassControlHub)
                {
                    return ("", "", false);
                }

                string title = txtPageTitle?.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(title) && activeEl is Page p && !string.IsNullOrWhiteSpace(p.Title))
                {
                    title = p.Title;
                }
                if (string.IsNullOrWhiteSpace(title)) title = "Trang hiện tại trên SMART CLASS";

                string activeId = !string.IsNullOrWhiteSpace(_currentPage) ? _currentPage : "CURRENT";
                if (string.Equals(lockedPageFormId, "CURRENT", StringComparison.OrdinalIgnoreCase))
                {
                    bool differs = _pinnedBroadcastPageElement != null && !ReferenceEquals(activeEl, _pinnedBroadcastPageElement);
                    return ("CURRENT", title, differs);
                }

                if (!string.IsNullOrWhiteSpace(lockedPageFormId))
                {
                    bool differs = !string.Equals(activeId, lockedPageFormId, StringComparison.OrdinalIgnoreCase);
                    return (activeId, title, differs);
                }

                return (activeId, title, true);
            }
            catch
            {
                return ("", "", false);
            }
        }

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

                if (contentFrame?.Content is FrameworkElement observedEl
                    && observedEl is not ClassControlHub
                    && observedEl is not BroadcastPage
                    && observedEl is not MonitorPage)
                {
                    _lastTeachingPageElement = observedEl;
                    _lastTeachingPageTitle = txtPageTitle?.Text?.Trim() ?? "Trang đang mở trên SMART CLASS";
                }

                if (pageFormId == "CURRENT" || string.IsNullOrWhiteSpace(pageFormId))
                {
                    // Khi tạo ảnh xem trước trong hộp chọn nguồn (createIfMissing == false), hiển thị trang bài học gần nhất / đang mở
                    if (!createIfMissing)
                    {
                        var previewTarget = _pinnedBroadcastPageElement
                            ?? ((contentFrame?.Content is FrameworkElement ce && ce is not BroadcastPage && ce is not ClassControlHub) ? ce : _lastTeachingPageElement)
                            ?? (contentFrame?.Content as FrameworkElement);
                        resolvedTitle = _pinnedBroadcastPageTitle;
                        if (string.IsNullOrWhiteSpace(resolvedTitle))
                            resolvedTitle = !string.IsNullOrWhiteSpace(_lastTeachingPageTitle) ? _lastTeachingPageTitle : (txtPageTitle?.Text?.Trim() ?? "Trang SMART CLASS");
                        return ScreenSourceEnumerator.RenderElementToBitmap(previewTarget ?? contentFrame, targetWidth);
                    }

                    // Khi đang trình chiếu thực tế: KHÓA CỐ ĐỊNH vào _pinnedBroadcastPageElement (giống Google Meet "Share this tab")
                    if (_pinnedBroadcastPageElement == null)
                    {
                        PinCurrentPageForBroadcast();
                    }

                    if (_pinnedBroadcastPageElement != null)
                    {
                        resolvedTitle = !string.IsNullOrWhiteSpace(_pinnedBroadcastPageTitle)
                            ? _pinnedBroadcastPageTitle
                            : "Trang SMART CLASS (Đã khóa cố định)";

                        if (_pinnedBroadcastPageElement.ActualWidth < 100 || _pinnedBroadcastPageElement.ActualHeight < 100)
                        {
                            double refW = (contentFrame != null && contentFrame.ActualWidth > 200) ? contentFrame.ActualWidth : 1280;
                            double refH = (contentFrame != null && contentFrame.ActualHeight > 200) ? contentFrame.ActualHeight : 720;
                            _pinnedBroadcastPageElement.Width = refW;
                            _pinnedBroadcastPageElement.Height = refH;
                        }
                        return ScreenSourceEnumerator.RenderElementToBitmap(_pinnedBroadcastPageElement, targetWidth);
                    }

                    // [MYTHWARE_LITE] Cơ chế cũ tự động đi theo contentFrame.Content được thay bằng cơ chế khóa cố định Google Meet ở trên
                    /*
                    if (contentFrame?.Content is FrameworkElement currentEl)
                    {
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
                    */
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

