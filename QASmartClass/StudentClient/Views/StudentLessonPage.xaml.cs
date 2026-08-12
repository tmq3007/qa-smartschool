using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using QASmartClass.Data;
using QASmartClass.StudentClient.Views;
using QASmartTouch;
using QASmartTouch.Services;
using Serilog;

namespace QASmartClass.StudentClient.Views
{

    public partial class StudentLessonPage : Page
    {
    	private Border? _currentFocusedBlock;
    	private int _consecutiveLostPings = 0;
    	private bool _isResettingNetwork = false;

    	public StudentLessonPage()
    	{
    		InitializeComponent();
    		base.Loaded += delegate
    		{
    			LoadTodaysLesson();
    			StartPingTimer();
    		};
    		base.Unloaded += delegate
    		{
    			StopPingTimer();
    		};
    	}

    	public void LoadTodaysLesson()
    	{
    		try
    		{
    			App app = (App)Application.Current;
    			if (app?.Database == null)
    			{
    				return;
    			}
    			var database = app.Database;
    			string className = app.StudentNetwork?.ClassName ?? "";
    			if (string.IsNullOrEmpty(className) && app.ClassRoster?.ActiveRoster != null)
    			{
    				className = app.ClassRoster.ActiveRoster.ClassName;
    			}
    			Lesson? lesson = null;
    			if (App.LessonState.IsLessonActive && App.LessonState.ActiveLessonId > 0)
    			{
    				lesson = database.Lessons.FirstOrDefault(l => l.Id == App.LessonState.ActiveLessonId);
    			}
    			if (lesson == null)
    			{
    				lesson = (from l in database.Lessons
    					where (l.Status == "Approved" || l.Status == "Taught")
                              && (string.IsNullOrEmpty(className) || l.ClassName == className || l.Grade == className)
    					orderby l.UpdatedAt descending
    					select l).FirstOrDefault();
    			}
    			if (lesson != null)
    			{
    				txtLessonTitle.Text = "📖 " + lesson.Title;
    				txtLessonInfo.Text = $"Môn: {lesson.Subject}  •  Lớp: {lesson.ClassName ?? lesson.Grade}  •  GV: {lesson.TeacherName}  •  {lesson.DurationMinutes} phút";
    				TextBlock textBlock = txtStatus;
    				string status = lesson.Status;
    				if (1 == 0)
    				{
    				}
    				string text = status switch
    				{
    					"Approved" => "✅ Đã duyệt", 
    					"Taught" => "📚 Đã dạy", 
    					"Draft" => "📝 Bản nháp", 
    					_ => lesson.Status, 
    				};
    				if (1 == 0)
    				{
    				}
    				textBlock.Text = text;
    				List<LessonContent> list = (from c in database.LessonContents
    					where c.LessonId == lesson.Id
    					orderby c.SortOrder
    					select c).ToList();
    				txtBlockCount.Text = $"{list.Count} nội dung";
    				if (list.Any())
    				{
    					lessonContentPanel.Children.Clear();
    					foreach (LessonContent item in list)
    					{
    						RenderContentBlock(item.ContentType, item.Data, item.SortOrder);
    					}
    					emptyState.Visibility = Visibility.Collapsed;
    					lessonContentPanel.Visibility = Visibility.Visible;
    				}
    				else
    				{
    					lessonContentPanel.Children.Clear();
    					lessonContentPanel.Visibility = Visibility.Collapsed;
    					emptyState.Visibility = Visibility.Visible;
    					txtEmptyMessage.Text = "Bài giảng chưa có nội dung chi tiết.\nĐang chờ GV trình chiếu...";
    				}
    				
    				string title = lesson.Title;
    				System.Threading.Tasks.Task.Run(() =>
    				{
    					try
    					{
    						using (var logDb = new AppDbContext())
    						{
    							logDb.EventLogs.Add(new QASmartClass.Data.EventLog
    							{
    								EventType = "LESSON_VIEW",
    								Actor = "Student",
    								Details = "Xem bài giảng: " + title,
    								Timestamp = DateTime.Now
    							});
    							logDb.SaveChanges();
    						}
    					}
    					catch (Exception exLog)
    					{
    						Log.Warning("Asynchronous logging error: {Err}", exLog.Message);
    					}
    				});
    				Log.Information("Student viewed lesson: {Title}, {ContentCount} contents", lesson.Title, list.Count);
    			}
    			else
    			{
    				txtLessonTitle.Text = "📖 Bài giảng hôm nay";
    				txtLessonInfo.Text = "Chưa có bài giảng nào được duyệt.";
    				txtBlockCount.Text = "—";
    				txtStatus.Text = "⏳ Chờ GV";
    				statusBadge.Background = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 243, 224));
    				txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
    				
    				lessonContentPanel.Children.Clear();
    				lessonContentPanel.Visibility = Visibility.Collapsed;
    				emptyState.Visibility = Visibility.Visible;
    			}
    			if (App.LessonState.IsLessonActive && App.LessonState.ActiveLessonStage > 0)
    			{
    				SetStageView(App.LessonState.ActiveLessonStage);
    				Log.Information("StudentLessonPage auto-set stage view: {Stage}", App.LessonState.ActiveLessonStage);
    			}
    			if (App.FocusState.ActiveFocusSort >= 0 && !string.IsNullOrEmpty(App.FocusState.ActiveFocusType))
    			{
    				base.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)delegate
    				{
    					try
    					{
    						FocusBlock(App.FocusState.ActiveFocusSort, App.FocusState.ActiveFocusType);
    						Log.Information("StudentLessonPage auto-restored focus: #{Sort} ({Type})", App.FocusState.ActiveFocusSort, App.FocusState.ActiveFocusType);
    					}
    					catch (Exception ex2)
    					{
    						Log.Warning("Auto-focus restore error: {Err}", ex2.Message);
    					}
    				});
    			}
    			ApplyTheme();
    			if (!App.BroadcastState.IsScreenBroadcastActive || string.IsNullOrEmpty(App.BroadcastState.ScreenCapturePath))
    			{
    				return;
    			}
    			base.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)delegate
    			{
    				try
    				{
    					if (Window.GetWindow(this) is StudentShell)
    					{
    						app.RaiseLocalCommand("CMD|SCREEN_BROADCAST_START|" + App.BroadcastState.ScreenCapturePath);
    						Log.Information("StudentLessonPage auto-restored broadcast overlay");
    					}
    				}
    				catch (Exception ex2)
    				{
    					Log.Warning("Auto-broadcast restore error: {Err}", ex2.Message);
    				}
    			});
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("LoadTodaysLesson error: {Err}", ex.Message);
    		}
    	}

    	private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    	{
    		LoadTodaysLesson();
    	}

    	private void RenderContentBlock(string contentType, string data, int sortOrder)
    	{
    		if (1 == 0)
    		{
    		}
    		(string, string, string) tuple = contentType switch
    		{
    			"Text" => ("📄 Văn bản", "#E3F2FD", "#1565C0"), 
    			"Image" => ("🖼️ Hình ảnh", "#E8F5E9", "#2E7D32"), 
    			"Video" => ("🎬 Video", "#FFF3E0", "#E65100"), 
    			"Simulation" => ("🔬 Mô phỏng PhET", "#F3E5F5", "#7B1FA2"), 
    			"PDF" => ("📄 Tài liệu PDF", "#FFEBEE", "#C62828"), 
    			"Quiz" => ("❓ Câu hỏi Quiz", "#E0F2F1", "#00695C"), 
    			_ => ("📝 Nội dung", "#F5F5F5", "#424242"), 
    		};
    		if (1 == 0)
    		{
    		}
    		(string, string, string) tuple2 = tuple;
    		string item = tuple2.Item1;
    		string item2 = tuple2.Item2;
    		string item3 = tuple2.Item3;
    		SolidColorBrush background = (SolidColorBrush)new BrushConverter().ConvertFrom(item2);
    		SolidColorBrush foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(item3);
    		Border border = new Border
    		{
    			Background = Brushes.White,
    			CornerRadius = new CornerRadius(10.0),
    			BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
    			BorderThickness = new Thickness(1.0),
    			Margin = new Thickness(0.0, 0.0, 0.0, 12.0),
    			Padding = new Thickness(18.0, 16.0, 18.0, 16.0),
    			Tag = sortOrder
    		};
    		StackPanel stackPanel = new StackPanel();
    		DockPanel dockPanel = new DockPanel
    		{
    			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
    		};
    		Border border2 = new Border
    		{
    			Background = background,
    			CornerRadius = new CornerRadius(6.0),
    			Padding = new Thickness(8.0, 4.0, 8.0, 4.0)
    		};
    		border2.Child = new TextBlock
    		{
    			Text = item,
    			FontSize = 12.0,
    			FontWeight = FontWeights.SemiBold,
    			Foreground = foreground
    		};
    		DockPanel.SetDock(border2, Dock.Left);
    		dockPanel.Children.Add(border2);
    		Border border3 = new Border
    		{
    			Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
    			CornerRadius = new CornerRadius(4.0),
    			Padding = new Thickness(6.0, 2.0, 6.0, 2.0),
    			HorizontalAlignment = HorizontalAlignment.Right,
    			VerticalAlignment = VerticalAlignment.Center
    		};
    		border3.Child = new TextBlock
    		{
    			Text = $"Thứ tự: {sortOrder}",
    			FontSize = 10.5,
    			Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158))
    		};
    		DockPanel.SetDock(border3, Dock.Right);
    		dockPanel.Children.Add(border3);
    		stackPanel.Children.Add(dockPanel);
    		
    		string textContent = data.Replace("\\n", "\n").Replace("\n", "\n"); // handle replacement cleanly
    		
    		if (contentType == "Image")
    		{
    			RenderImageContent(stackPanel, textContent);
    		}
    		else if (contentType == "Video")
    		{
    			RenderMediaLauncher(stackPanel, "🎬", "Phát Video bài giảng", textContent, item3, "#FFECE0", () => {
    				ExecuteSafeFile(textContent, new[] { ".mp4", ".avi", ".mov", ".mkv" });
    			});
    		}
    		else if (contentType == "PDF")
    		{
    			RenderMediaLauncher(stackPanel, "📄", "Mở tài liệu PDF", textContent, item3, "#FFEBEB", () => {
    				ExecuteSafeFile(textContent, new[] { ".pdf" });
    			});
    		}
    		else if (contentType == "Simulation")
    		{
    			RenderMediaLauncher(stackPanel, "🔬", "Mở mô phỏng PhET", textContent, item3, "#F9F0FF", () => {
    				OpenSimulation(textContent);
    			});
    		}
    		else if (contentType == "Quiz")
    		{
    			RenderMediaLauncher(stackPanel, "❓", "Truy cập phòng làm Quiz", textContent, item3, "#E0F7FA", () => {
    				if (Window.GetWindow(this) is StudentShell shell)
    				{
    					shell.NavigateTo("S3");
    				}
    			});
    		}
    		else
    		{
    			RichTextBox element = BuildRichTextContent(textContent);
    			stackPanel.Children.Add(element);
    		}
    		
    		border.Child = stackPanel;
    		lessonContentPanel.Children.Add(border);
    	}
    	private async void RenderImageContent(StackPanel container, string imagePath)
    	{
    		try
    		{
    			string actualPath = imagePath;
    			string? expectedHash = null;
    			if (imagePath.Contains("|"))
    			{
    				var parts = imagePath.Split('|');
    				actualPath = parts[0];
    				expectedHash = parts[1];
    			}

    			byte[]? imgData = null;

    			if (System.IO.File.Exists(actualPath))
    			{
    				var fileInfo = new System.IO.FileInfo(actualPath);
    				if (fileInfo.Length > 20 * 1024 * 1024)
    				{
    					RenderLoadError(container, $"🖼️ [Tệp ảnh quá lớn ({fileInfo.Length / (1024.0 * 1024.0):F1}MB > 20MB) không được phép nạp để bảo vệ hệ thống]");
    					Log.Warning("Blocked loading large local image: {Path} ({Size} bytes)", actualPath, fileInfo.Length);
    					return;
    				}

    				imgData = System.IO.File.ReadAllBytes(actualPath);
    			}
    			else if (Uri.TryCreate(actualPath, UriKind.Absolute, out Uri? uriResult) && 
    			         (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
    			{
    				long? contentLength = null;
    				try
    				{
    					using (var client = new System.Net.Http.HttpClient())
    					{
    						client.Timeout = TimeSpan.FromSeconds(3.0);
    						var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, uriResult);
    						var response = await client.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
    						if (response.IsSuccessStatusCode)
    						{
    							contentLength = response.Content.Headers.ContentLength;
    						}
    					}
    				}
    				catch (Exception exUri)
    				{
    					Log.Warning("Error fetching headers for URL image: {Err}", exUri.Message);
    				}

    				if (contentLength.HasValue && contentLength.Value > 20 * 1024 * 1024)
    				{
    					RenderLoadError(container, $"🖼️ [Tệp ảnh quá lớn ({contentLength.Value / (1024.0 * 1024.0):F1}MB > 20MB) không được phép nạp để bảo vệ hệ thống]");
    					Log.Warning("Blocked loading large URL image: {Url} ({Size} bytes)", actualPath, contentLength.Value);
    					return;
    				}

    				try
    				{
    					using (var client = new System.Net.Http.HttpClient())
    					{
    						var response = await client.GetAsync(uriResult, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
    						response.EnsureSuccessStatusCode();

    						long? length = response.Content.Headers.ContentLength;
    						if (length.HasValue && length.Value > 20 * 1024 * 1024)
    						{
    							RenderLoadError(container, $"🖼️ [Tệp ảnh quá lớn ({length.Value / (1024.0 * 1024.0):F1}MB > 20MB) không được phép nạp để bảo vệ hệ thống]");
    							return;
    						}

    						using (var stream = await response.Content.ReadAsStreamAsync())
    						{
    							var ms = new System.IO.MemoryStream();
    							byte[] buffer = new byte[8192];
    							int bytesRead;
    							long totalBytes = 0;
    							while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    							{
    								totalBytes += bytesRead;
    								if (totalBytes > 20 * 1024 * 1024)
    								{
    									RenderLoadError(container, "🖼️ [Tệp ảnh quá lớn (>20MB) không được phép nạp để bảo vệ hệ thống]");
    									Log.Warning("Blocked loading large URL image during stream: {Url}", actualPath);
    									return;
    								}
    								ms.Write(buffer, 0, bytesRead);
    							}
    							imgData = ms.ToArray();
    						}
    					}
    				}
    				catch (Exception exDownload)
    				{
    					Log.Warning("Failed to download image: {Err}", exDownload.Message);
    					RenderLoadError(container, $"⚠️ Lỗi nạp hình ảnh: {actualPath}");
    					return;
    				}
    			}

    			if (imgData != null)
    			{
    				if (expectedHash != null)
    				{
    					string actualHash = await System.Threading.Tasks.Task.Run(() =>
    					{
    						try
    						{
    							using (var sha256 = System.Security.Cryptography.SHA256.Create())
    							{
    								byte[] hashBytes = sha256.ComputeHash(imgData);
    								return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    							}
    						}
    						catch (Exception exHash)
    						{
    							Log.Warning("Error computing SHA256 for image data: {Err}", exHash.Message);
    							return "";
    						}
    					});

    					if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
    					{
    						RenderLoadError(container, "⚠️ Lỗi bảo mật: Tệp ảnh đã bị chỉnh sửa bất hợp pháp hoặc bị lỗi truyền tải (Sai mã băm SHA-256)");
    						Log.Warning("SHA-256 validation failed for image data. Expected: {Exp}, Actual: {Act}", expectedHash, actualHash);
    						return;
    					}
    				}

    				base.Dispatcher.Invoke(() =>
    				{
    					var bitmap = new System.Windows.Media.Imaging.BitmapImage();
    					bitmap.BeginInit();
    					bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
    					bitmap.StreamSource = new System.IO.MemoryStream(imgData);
    					bitmap.EndInit();
    					bitmap.Freeze();

    					Image wpfImage = new Image
    					{
    						Source = bitmap,
    						HorizontalAlignment = HorizontalAlignment.Center,
    						Stretch = Stretch.Uniform,
    						MaxWidth = 750,
    						MaxHeight = 450,
    						Margin = new Thickness(0, 8, 0, 8)
    					};
    					container.Children.Add(wpfImage);
    				});
    			}
    			else
    			{
    				RenderLoadError(container, $"🖼️ [Hình ảnh không khả dụng hoặc đường dẫn sai: {actualPath}]");
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("RenderImageContent error: {Err}", ex.Message);
    			RenderLoadError(container, $"⚠️ Lỗi nạp hình ảnh: {imagePath}");
    		}
    	}
    	private void RenderMediaLauncher(StackPanel container, string icon, string buttonText, string filePath, string fgHex, string bgHex, Action onLaunch)
    	{
    		Border cardBorder = new Border
    		{
    			Background = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex),
    			BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex),
    			BorderThickness = new Thickness(1),
    			CornerRadius = new CornerRadius(8),
    			Padding = new Thickness(14, 10, 14, 10),
    			Margin = new Thickness(0, 6, 0, 6)
    		};
    		
    		DockPanel dock = new DockPanel();
    		
    		TextBlock iconText = new TextBlock
    		{
    			Text = icon,
    			FontSize = 26,
    			VerticalAlignment = VerticalAlignment.Center,
    			Margin = new Thickness(0, 0, 12, 0)
    		};
    		DockPanel.SetDock(iconText, Dock.Left);
    		dock.Children.Add(iconText);
    		
    		Button actionBtn = new Button
    		{
    			Content = buttonText,
    			Background = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex),
    			Foreground = Brushes.White,
    			FontWeight = FontWeights.Bold,
    			FontSize = 13,
    			Padding = new Thickness(14, 8, 14, 8),
    			Cursor = Cursors.Hand,
    			VerticalAlignment = VerticalAlignment.Center
    		};
    		actionBtn.Click += (s, e) => onLaunch();
    		DockPanel.SetDock(actionBtn, Dock.Right);
    		dock.Children.Add(actionBtn);
    		
    		string displayPath = filePath;
    		try
    		{
    			if (filePath.Contains(":\\") || filePath.StartsWith("\\\\"))
    			{
    				displayPath = System.IO.Path.GetFileName(filePath);
    			}
    		}
    		catch {}

    		TextBlock detailsText = new TextBlock
    		{
    			Text = $"Tài nguyên học tập:\n{displayPath}",
    			FontSize = 14.5,
    			Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex),
    			FontWeight = FontWeights.Medium,
    			TextWrapping = TextWrapping.Wrap,
    			VerticalAlignment = VerticalAlignment.Center
    		};
    		dock.Children.Add(detailsText);
    		
    		cardBorder.Child = dock;
    		container.Children.Add(cardBorder);
    	}

    	private void RenderLoadError(StackPanel container, string errorMessage)
    	{
    		base.Dispatcher.Invoke(() =>
    		{
    			Border errBorder = new Border
    			{
    				Background = new SolidColorBrush(Color.FromRgb(255, 235, 235)),
    				BorderBrush = new SolidColorBrush(Color.FromRgb(239, 154, 154)),
    				BorderThickness = new Thickness(1),
    				CornerRadius = new CornerRadius(6),
    				Padding = new Thickness(12, 10, 12, 10),
    				Margin = new Thickness(0, 5, 0, 5)
    			};
    			errBorder.Child = new TextBlock
    			{
    				Text = errorMessage,
    				FontSize = 13.0,
    				Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
    				TextWrapping = TextWrapping.Wrap,
    				FontWeight = FontWeights.SemiBold
    			};
    			container.Children.Add(errBorder);
    		});
    	}

    	private void ExecuteSafeFile(string path, string[] allowedExtensions)
    	{
    		try
    		{
    			if (string.IsNullOrWhiteSpace(path)) return;
    			
    			string extension = System.IO.Path.GetExtension(path).ToLower();
    			if (!allowedExtensions.Contains(extension))
    			{
    				MessageBox.Show("Định dạng tệp tin không an sau hoặc không được phép thực thi!", "Cảnh báo Bảo mật", MessageBoxButton.OK, MessageBoxImage.Warning);
    				return;
    			}

    			if (!System.IO.File.Exists(path))
    			{
    				MessageBox.Show($"Tệp tin không tồn tại trên hệ thống: {path}", "Lỗi đường dẫn", MessageBoxButton.OK, MessageBoxImage.Error);
    				return;
    			}

    			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    			{
    				FileName = path,
    				UseShellExecute = true
    			});
    		}
    		catch (Exception ex)
    		{
    			MessageBox.Show($"Không thể mở tài nguyên: {ex.Message}", "Lỗi thực thi", MessageBoxButton.OK, MessageBoxImage.Error);
    		}
    	}

    	private void OpenSimulation(string url)
    	{
    		try
    		{
    			if (string.IsNullOrWhiteSpace(url)) return;
    			
    			SecureWebWindow webWindow = new SecureWebWindow(url);
    			webWindow.Owner = Window.GetWindow(this);
    			webWindow.Show();
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("Failed to open simulation in SecureWebWindow, falling back to system browser: {Err}", ex.Message);
    			try
    			{
    				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    				{
    					FileName = url,
    					UseShellExecute = true
    				});
    			}
    			catch (Exception ex2)
    			{
    				MessageBox.Show($"Không thể chạy trình duyệt mô phỏng: {ex2.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
    			}
    		}
    	}

    	private static RichTextBox BuildRichTextContent(string text)
    	{
    		RichTextBox richTextBox = new RichTextBox
    		{
    			BorderThickness = new Thickness(0.0),
    			IsReadOnly = true,
    			Padding = new Thickness(0.0),
    			Background = Brushes.Transparent,
    			FontFamily = new FontFamily("Segoe UI"),
    			FontSize = 14.5,
    			Cursor = Cursors.Arrow
    		};
    		FlowDocument flowDocument = new FlowDocument
    		{
    			PagePadding = new Thickness(0.0),
    			LineStackingStrategy = LineStackingStrategy.MaxHeight
    		};
    		string[] array = text.Split('\n');
    		Paragraph paragraph = null;
    		string[] array2 = array;
    		foreach (string text2 in array2)
    		{
    			string text3 = text2.TrimEnd('\r');
    			if (string.IsNullOrWhiteSpace(text3))
    			{
    				if (paragraph != null)
    				{
    					flowDocument.Blocks.Add(paragraph);
    					paragraph = null;
    				}
    			}
    			else if (IsHeading(text3))
    			{
    				if (paragraph != null)
    				{
    					flowDocument.Blocks.Add(paragraph);
    					paragraph = null;
    				}
    				Paragraph paragraph2 = new Paragraph
    				{
    					Margin = new Thickness(0.0, 8.0, 0.0, 4.0)
    				};
    				if (text3.StartsWith("📌") || text3.StartsWith("✍️"))
    				{
    					paragraph2.Inlines.Add(new Run(text3)
    					{
    						FontSize = 16.0,
    						FontWeight = FontWeights.Bold,
    						Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
    					});
    				}
    				else if (text3.StartsWith("📖"))
    				{
    					paragraph2.Inlines.Add(new Run(text3)
    					{
    						FontSize = 15.0,
    						FontWeight = FontWeights.Bold,
    						Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
    					});
    				}
    				else if (text3.StartsWith("📝"))
    				{
    					paragraph2.Inlines.Add(new Run(text3)
    					{
    						FontSize = 14.0,
    						FontWeight = FontWeights.SemiBold,
    						Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
    					});
    				}
    				else if (text3.StartsWith("⚡"))
    				{
    					paragraph2.Inlines.Add(new Run(text3)
    					{
    						FontSize = 14.0,
    						FontWeight = FontWeights.SemiBold,
    						Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
    					});
    				}
    				else
    				{
    					paragraph2.Inlines.Add(new Run(text3)
    					{
    						FontSize = 15.0,
    						FontWeight = FontWeights.Bold,
    						Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
    					});
    				}
    				flowDocument.Blocks.Add(paragraph2);
    			}
    			else if (text3.TrimStart().StartsWith("•") || text3.TrimStart().StartsWith("🔸") || text3.TrimStart().StartsWith("🔹") || text3.TrimStart().StartsWith("🟢") || text3.TrimStart().StartsWith("🟡") || text3.TrimStart().StartsWith("🔴") || text3.TrimStart().StartsWith("✅") || text3.TrimStart().StartsWith("❌") || text3.TrimStart().StartsWith("🎯"))
    			{
    				if (paragraph != null)
    				{
    					flowDocument.Blocks.Add(paragraph);
    					paragraph = null;
    				}
    				Paragraph paragraph3 = new Paragraph
    				{
    					Margin = new Thickness(12.0, 1.0, 0.0, 1.0)
    				};
    				paragraph3.Inlines.Add(new Run(text3)
    				{
    					FontSize = 14.5,
    					Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
    				});
    				flowDocument.Blocks.Add(paragraph3);
    			}
    			else if (text3.TrimStart().StartsWith("→") || text3.TrimStart().StartsWith("   "))
    			{
    				if (paragraph != null)
    				{
    					flowDocument.Blocks.Add(paragraph);
    					paragraph = null;
    				}
    				Paragraph paragraph4 = new Paragraph
    				{
    					Margin = new Thickness(24.0, 0.0, 0.0, 1.0)
    				};
    				paragraph4.Inlines.Add(new Run(text3.TrimStart())
    				{
    					FontSize = 13.0,
    					Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
    					FontStyle = FontStyles.Italic
    				});
    				flowDocument.Blocks.Add(paragraph4);
    			}
    			else if (IsFormula(text3))
    			{
    				if (paragraph != null)
    				{
    					flowDocument.Blocks.Add(paragraph);
    					paragraph = null;
    				}
    				Paragraph paragraph5 = new Paragraph
    				{
    					Margin = new Thickness(16.0, 4.0, 16.0, 4.0),
    					Padding = new Thickness(12.0, 6.0, 12.0, 6.0),
    					Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
    				};
    				paragraph5.Inlines.Add(new Run(text3.Trim())
    				{
    					FontSize = 14.0,
    					FontWeight = FontWeights.SemiBold,
    					FontFamily = new FontFamily("Segoe UI"),
    					Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92))
    				});
    				flowDocument.Blocks.Add(paragraph5);
    			}
    			else if (IsNumberedItem(text3))
    			{
    				if (paragraph != null)
    				{
    					flowDocument.Blocks.Add(paragraph);
    					paragraph = null;
    				}
    				Paragraph paragraph6 = new Paragraph
    				{
    					Margin = new Thickness(8.0, 2.0, 0.0, 2.0)
    				};
    				paragraph6.Inlines.Add(new Run(text3)
    				{
    					FontSize = 14.5,
    					Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
    				});
    				flowDocument.Blocks.Add(paragraph6);
    			}
    			else
    			{
    				if (paragraph == null)
    				{
    					paragraph = new Paragraph
    					{
    						Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
    					};
    				}
    				else
    				{
    					paragraph.Inlines.Add(new LineBreak());
    				}
    				paragraph.Inlines.Add(new Run(text3)
    				{
    					FontSize = 14.5,
    					Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
    				});
    			}
    		}
    		if (paragraph != null)
    		{
    			flowDocument.Blocks.Add(paragraph);
    		}
    		richTextBox.Document = flowDocument;
    		return richTextBox;
    	}

    	private static bool IsHeading(string line)
    	{
    		return line.StartsWith("📌") || line.StartsWith("📖") || line.StartsWith("✍️") || line.StartsWith("📝") || line.StartsWith("⚡") || line.StartsWith("🏁");
    	}

    	private static bool IsFormula(string line)
    	{
    		string text = line.TrimStart();
    		return (text.StartsWith("y =") || text.StartsWith("x =") || text.StartsWith("a²") || text.StartsWith("S =") || text.StartsWith("E =") || text.StartsWith("u =") || text.StartsWith("∫") || text.StartsWith("Δ") || text.StartsWith("lim") || text.StartsWith("cos") || text.StartsWith("sin") || text.StartsWith("v =") || text.StartsWith("ω =") || text.StartsWith("f =") || text.StartsWith("λ =") || text.StartsWith("b² =") || text.StartsWith("c² =") || text.StartsWith("E⃗")) && text.Length < 80;
    	}

    	private static bool IsNumberedItem(string line)
    	{
    		string text = line.TrimStart();
    		return text.Length > 2 && char.IsDigit(text[0]) && (text[1] == '.' || (char.IsDigit(text[1]) && text[2] == '.'));
    	}

    	public void FocusBlock(int sortOrder, string contentType)
    	{
    		try
    		{
    			focusIndicator.Visibility = Visibility.Visible;
    			if (btnCloseFocus != null) btnCloseFocus.Visibility = Visibility.Collapsed;
    			txtFocusTitle.Text = "🎯 GV yêu cầu tập trung — " + contentType;
    			txtFocusDetail.Text = $"Hãy đọc kỹ nội dung #{sortOrder} được đánh dấu bên dưới";
    			_currentFocusedBlock = null;
    			foreach (object child in lessonContentPanel.Children)
    			{
    				if (child is Border border && border.Tag is int num)
    				{
    					if (num == sortOrder)
    					{
    						double focusZoomScale = AppSettings.FocusZoomScale;
    						double focusFontScale = AppSettings.FocusFontScale;
    						border.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
    						border.BorderThickness = new Thickness(5.0);
    						border.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
    						border.Opacity = 1.0;
    						border.Padding = new Thickness(36.0, 28.0, 36.0, 28.0);
    						border.Margin = new Thickness(0.0, 24.0, 0.0, 24.0);
    						border.LayoutTransform = new ScaleTransform(focusZoomScale, focusZoomScale);
    						border.RenderTransformOrigin = new Point(0.5, 0.5);
    						ScaleBlockFontSize(border, focusFontScale);
    						ScaleBadgeInBlock(border, focusFontScale);
    						border.Effect = new DropShadowEffect
    						{
    							BlurRadius = 40.0,
    							ShadowDepth = 8.0,
    							Color = Color.FromRgb(25, 118, 210),
    							Opacity = 0.5
    						};
    						_currentFocusedBlock = border;
    						border.BringIntoView();
    					}
    					else
    					{
    						border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    						border.BorderThickness = new Thickness(1.0);
    						border.Background = Brushes.White;
    						border.Opacity = AppSettings.FocusDimOpacity;
    						border.Effect = null;
    						border.LayoutTransform = null;
    						border.Padding = new Thickness(16.0, 12.0, 16.0, 12.0);
    						border.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
    						ScaleBlockFontSize(border, 1.0);
    						ScaleBadgeInBlock(border, 1.0);
    					}
    				}
    			}
    			if (_currentFocusedBlock != null)
    			{
    				Log.Information("Student focused on block {Sort} ({Type})", sortOrder, contentType);
    			}
    			else
    			{
    				Log.Warning("Focus block {Sort} not found", sortOrder);
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("FocusBlock error: {Err}", ex.Message);
    		}
    	}

    	private void ScaleBlockFontSize(Border border, double scale)
    	{
    		if (border.Child is Panel panel)
    		{
    			ScalePanelChildren(panel, scale);
    		}
    	}

    	private void ScalePanelChildren(Panel panel, double scale)
    	{
    		foreach (object child2 in panel.Children)
    		{
    			if (child2 is TextBlock textBlock && textBlock.Tag == null)
    			{
    				double num = 13.0;
    				if (textBlock.FontWeight == FontWeights.Bold || textBlock.FontWeight == FontWeights.SemiBold)
    				{
    					num = 15.0;
    				}
    				textBlock.FontSize = num * scale;
    			}
    			else if (child2 is RichTextBox richTextBox)
    			{
    				double num2 = 14.5;
    				richTextBox.FontSize = num2 * scale;
    				if (richTextBox.Document == null)
    				{
    					continue;
    				}
    				foreach (Block block in richTextBox.Document.Blocks)
    				{
    					if (!(block is Paragraph paragraph))
    					{
    						continue;
    					}
    					foreach (Inline inline in paragraph.Inlines)
    					{
    						if (!(inline is Run run))
    						{
    							continue;
    						}
    						double num3 = run.FontSize / ((scale == 1.0) ? 1.0 : ((run.FontSize > 14.0) ? scale : 1.0));
    						if (scale == 1.0)
    						{
    							if (run.FontWeight == FontWeights.Bold)
    							{
    								run.FontSize = 16.0;
    							}
    							else if (run.FontWeight == FontWeights.SemiBold)
    							{
    								run.FontSize = 14.0;
    							}
    							else
    							{
    								run.FontSize = 14.5;
    							}
    						}
    						else
    						{
    							run.FontSize *= scale;
    						}
    					}
    				}
    			}
    			else if (child2 is Border border && border.Child is Panel child)
    			{
    				ScalePanelChildren(child, scale);
    			}
    			else if (child2 is DockPanel panel2)
    			{
    				ScalePanelChildren(panel2, scale);
    			}
    		}
    	}

    	private void ScaleBadgeInBlock(Border border, double scale)
    	{
    		try
    		{
    			if (!(border.Child is StackPanel stackPanel))
    			{
    				return;
    			}
    			foreach (object child2 in stackPanel.Children)
    			{
    				if (!(child2 is DockPanel dockPanel))
    				{
    					continue;
    				}
    				{
    					foreach (object child3 in dockPanel.Children)
    					{
    						if (!(child3 is Border border2 && border2.Child is TextBlock child))
    						{
    							continue;
    						}
    						if (scale == 1.0)
    						{
    							if (child.FontWeight == FontWeights.SemiBold)
    							{
    								child.FontSize = 11.0;
    							}
    							else
    							{
    								child.FontSize = 9.0;
    							}
    						}
    						else
    						{
    							double num = ((child.FontWeight == FontWeights.SemiBold) ? 11 : 9);
    							child.FontSize = num * scale;
    						}
    					}
    					break;
    				}
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("ScaleBadgeInBlock error: {Err}", ex.Message);
    		}
    	}

    	private void CloseFocus_Click(object sender, RoutedEventArgs e)
    	{
    		UnfocusAll();
    	}

    	public void UnfocusAll()
    	{
    		focusIndicator.Visibility = Visibility.Collapsed;
    		if (btnCloseFocus != null) btnCloseFocus.Visibility = Visibility.Visible;
    		foreach (object child in lessonContentPanel.Children)
    		{
    			if (child is Border border && border.Tag is int)
    			{
    				border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    				border.BorderThickness = new Thickness(1.0);
    				border.Background = Brushes.White;
    				border.Opacity = 1.0;
    				border.Effect = null;
    				border.LayoutTransform = null;
    				border.Padding = new Thickness(16.0, 12.0, 16.0, 12.0);
    				border.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
    				ScaleBlockFontSize(border, 1.0);
    				ScaleBadgeInBlock(border, 1.0);
    			}
    		}
    		_currentFocusedBlock = null;
    		Log.Information("Student: all blocks unfocused");
    	}

    	public void SetStageView(int stage)
    	{
    		try
    		{
    			stageReviewPanel.Visibility = Visibility.Collapsed;
    			stageActivityPanel.Visibility = Visibility.Collapsed;
    			emptyState.Visibility = Visibility.Collapsed;
    			lessonContentPanel.Visibility = Visibility.Collapsed;
    			focusIndicator.Visibility = Visibility.Collapsed;
    			switch (stage)
    			{
    			case 1:
    				emptyState.Visibility = Visibility.Visible;
    				txtEmptyMessage.Text = "⏳ GV đang chuẩn bị tiết học...\nBài giảng sẽ bắt đầu trong giây lát.";
    				break;
    			case 2:
    				stageReviewPanel.Visibility = Visibility.Visible;
    				break;
    			case 3:
    				if (lessonContentPanel.Children.Count > 0)
    				{
    					lessonContentPanel.Visibility = Visibility.Visible;
    					break;
    				}
    				emptyState.Visibility = Visibility.Visible;
    				txtEmptyMessage.Text = "📖 GV đang giảng bài mới.\nNội dung sẽ hiển thị khi GV phát cho lớp.";
    				break;
    			case 4:
    				stageActivityPanel.Visibility = Visibility.Visible;
    				txtStageIcon.Text = "🎮";
    				txtStageName.Text = "Thực Hành & Mini Game";
    				txtStageHint.Text = "Hãy tham gia hoạt động theo hướng dẫn của GV!\nQuiz Battle, Task Nhóm, hoặc Mô phỏng sẽ hiển thị tự động.";
    				if (lessonContentPanel.Children.Count > 0)
    				{
    					lessonContentPanel.Visibility = Visibility.Visible;
    				}
    				break;
    			case 5:
    				stageActivityPanel.Visibility = Visibility.Visible;
    				txtStageIcon.Text = "🏆";
    				txtStageName.Text = "Đánh Giá Kết Quả";
    				txtStageHint.Text = "GV đang tổng hợp điểm số.\nChờ kết quả Leaderboard trên SmartScreen!";
    				break;
    			case 6:
    				stageActivityPanel.Visibility = Visibility.Visible;
    				txtStageIcon.Text = "📝";
    				txtStageName.Text = "Tổng Kết Tiết Học";
    				txtStageHint.Text = "GV đang giao bài tập về nhà.\nKiểm tra thông báo bài tập!";
    				if (lessonContentPanel.Children.Count > 0)
    				{
    					lessonContentPanel.Visibility = Visibility.Visible;
    				}
    				break;
    			default:
    				emptyState.Visibility = Visibility.Visible;
    				break;
    			}
    			if (stage == 3 || stage == 4)
    			{
    				if (App.FocusState.ActiveFocusSort >= 0 && !string.IsNullOrEmpty(App.FocusState.ActiveFocusType))
    				{
    					FocusBlock(App.FocusState.ActiveFocusSort, App.FocusState.ActiveFocusType);
    				}
    			}
    			Log.Information("Student stage view set to: {Stage}", stage);
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("SetStageView error: {Err}", ex.Message);
    		}
    	}
    	private DispatcherTimer? _pingTimer;

    	private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    	{
    		if (e.Key == Key.F12)
    		{
    			bool isTest = AppDomain.CurrentDomain.FriendlyName.Contains("testhost");
    			if (dbDiagnosticOverlay.Visibility == Visibility.Visible)
    			{
    				if (isTest)
    				{
    					dbDiagnosticOverlay.Visibility = Visibility.Collapsed;
    				}
    				else
    				{
    					var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation
    					{
    						From = dbDiagnosticOverlay.Opacity,
    						To = 0.0,
    						Duration = new Duration(TimeSpan.FromMilliseconds(200.0))
    					};
    					fadeAnimation.Completed += (s, ev) =>
    					{
    						dbDiagnosticOverlay.Visibility = Visibility.Collapsed;
    					};
    					dbDiagnosticOverlay.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
    				}
    			}
    			else
    			{
    				if (isTest)
    				{
    					dbDiagnosticOverlay.Visibility = Visibility.Visible;
    				}
    				else
    				{
    					dbDiagnosticOverlay.Opacity = 0.0;
    					dbDiagnosticOverlay.Visibility = Visibility.Visible;
    					var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation
    					{
    						From = 0.0,
    						To = 1.0,
    						Duration = new Duration(TimeSpan.FromMilliseconds(200.0))
    					};
    					dbDiagnosticOverlay.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
    				}
    				PingTimer_Tick(null, EventArgs.Empty);
    			}
    			e.Handled = true;
    		}
    	}

    	private void StartPingTimer()
    	{
    		if (_pingTimer == null)
    		{
    			_pingTimer = new DispatcherTimer();
    			_pingTimer.Interval = TimeSpan.FromSeconds(5.0);
    			_pingTimer.Tick += PingTimer_Tick;
    		}
    		_pingTimer.Start();
    		PingTimer_Tick(null, EventArgs.Empty);
    	}

    	private void StopPingTimer()
    	{
    		_pingTimer?.Stop();
    	}

    	private async void PingTimer_Tick(object? sender, EventArgs e)
    	{
    		try
    		{
    			App? app = Application.Current as App;
    			string teacherIP = app?.StudentNetwork?.ServerIP ?? "";

    			if (!string.IsNullOrEmpty(teacherIP))
    			{
    				var reply = await System.Threading.Tasks.Task.Run(async () =>
    				{
    					try
    					{
    						using (var ping = new System.Net.NetworkInformation.Ping())
    						{
    							return await ping.SendPingAsync(teacherIP, 1500);
    						}
    					}
    					catch
    					{
    						return null;
    					}
    				});

    				if (reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success)
    				{
    					txtNetworkStatus.Text = "📶 Đang kết nối";
    					txtNetworkStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
    					txtDiagLatency.Text = $"Độ trễ: {reply.RoundtripTime} ms";
    					
    					if (Resources["sbBlinkAlert"] is System.Windows.Media.Animation.Storyboard sb)
    					{
    						sb.Stop(this);
    					}
    					txtNetworkStatus.Opacity = 1.0;

    					if (_consecutiveLostPings >= 6)
    					{
    						borderOfflineBanner.Visibility = Visibility.Collapsed;
    						LoadTodaysLesson();
    					}
    					_consecutiveLostPings = 0;
    				}
    				else
    				{
    					txtNetworkStatus.Text = "⚠️ Mất kết nối";
    					txtNetworkStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
    					txtDiagLatency.Text = "Độ trễ: Ngoại tuyến (Mất kết nối)";
    					
    					if (Resources["sbBlinkAlert"] is System.Windows.Media.Animation.Storyboard sb)
    					{
    						sb.Begin(this, true);
    					}

    					_consecutiveLostPings++;
    					if (_consecutiveLostPings >= 6)
    					{
    						if (borderOfflineBanner.Visibility != Visibility.Visible)
    						{
    							borderOfflineBanner.Visibility = Visibility.Visible;
    							if (Window.GetWindow(this) is StudentShell shell)
    							{
    								shell.ShowNotification("Tự học ngoại tuyến", "Mạng LAN chập chờn. Đã tự động mở khóa máy.", "#E65100");
                                    shell.CloseScreenBroadcast();
    							}
    						}
    					}
    				}
    				txtDiagTeacherIp.Text = $"IP Giáo viên: {teacherIP}";
    			}
    			else
    			{
    				txtNetworkStatus.Text = "⚠️ Chưa kết nối";
    				txtNetworkStatus.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
    				txtDiagLatency.Text = "Độ trễ: Ngoại tuyến";
    				txtDiagTeacherIp.Text = "IP Giáo viên: Chưa kết nối";
    				
    				if (Resources["sbBlinkAlert"] is System.Windows.Media.Animation.Storyboard sb)
    				{
    					sb.Begin(this, true);
    				}
 
    				_consecutiveLostPings++;
    				if (_consecutiveLostPings >= 6)
    				{
    					if (borderOfflineBanner.Visibility != Visibility.Visible)
    					{
    						borderOfflineBanner.Visibility = Visibility.Visible;
    						if (Window.GetWindow(this) is StudentShell shell)
    						{
    							shell.ShowNotification("Tự học ngoại tuyến", "Mạng LAN chập chờn. Đã tự động mở khóa máy.", "#E65100");
                                shell.CloseScreenBroadcast();
    						}
    					}
    				}
    			}

    			string localIp = "Chưa xác định";
    			try
    			{
    				var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
    				var ip = host.AddressList.FirstOrDefault(i => i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
    				if (ip != null)
    				{
    					localIp = ip.ToString();
    				}
    			}
    			catch {}
    			txtDiagLocalIp.Text = $"IP Học sinh: {localIp}";
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("Ping check error: {Err}", ex.Message);
    		}
    	}

    	private async void BtnResetNetwork_Click(object sender, RoutedEventArgs e)
    	{
    		if (_isResettingNetwork) return;
    		_isResettingNetwork = true;
    		var resetButton = sender as Button;
    		if (resetButton != null) resetButton.IsEnabled = false;

    		try
    		{
    			App app = (App)Application.Current;
    			if (app == null) return;

    			if (Window.GetWindow(this) is StudentShell shell)
    			{
    				shell.ShowNotification("⚡ Kết nối mạng", "Đang khởi chạy lại kết nối mạng LAN...", "#E65100");
    			}

    			if (app.StudentNetwork != null)
    			{
    				app.StudentNetwork.Stop();
    				await app.StudentNetwork.StartAsync();
    			}

    			if (Window.GetWindow(this) is StudentShell shell2)
    			{
    				shell2.ShowNotification("📶 Kết nối mạng", "Đã thiết lập lại kết nối UDP Discovery thành công!", "#2E7D32");
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("BtnResetNetwork_Click error: {Err}", ex.Message);
    		}
    		finally
    		{
    			if (resetButton != null) resetButton.IsEnabled = true;
    			_isResettingNetwork = false;
    		}
    	}

    	private Brush GetThemeForeground(Brush originalBrush, bool isDark)
    	{
    		if (originalBrush is SolidColorBrush scb)
    		{
    			Color c = scb.Color;
    			if (isDark)
    			{
    				if (c.R == 33 && c.G == 33 && c.B == 33) return new SolidColorBrush(Color.FromRgb(248, 250, 252));
    				if (c.R == 55 && c.G == 55 && c.B == 55) return new SolidColorBrush(Color.FromRgb(226, 232, 240));
    				if (c.R == 97 && c.G == 97 && c.B == 97) return new SolidColorBrush(Color.FromRgb(203, 213, 225));
    				if (c.R == 25 && c.G == 118 && c.B == 210) return new SolidColorBrush(Color.FromRgb(96, 165, 250));
    				if (c.R == 46 && c.G == 125 && c.B == 50) return new SolidColorBrush(Color.FromRgb(74, 222, 128));
    				if (c.R == 230 && c.G == 81 && c.B == 0) return new SolidColorBrush(Color.FromRgb(251, 146, 60));
    				if (c.R == 0 && c.G == 105 && c.B == 92) return new SolidColorBrush(Color.FromRgb(45, 212, 191));
    			}
    			else
    			{
    				if (c.R == 248 && c.G == 250 && c.B == 252) return new SolidColorBrush(Color.FromRgb(33, 33, 33));
    				if (c.R == 226 && c.G == 232 && c.B == 240) return new SolidColorBrush(Color.FromRgb(55, 55, 55));
    				if (c.R == 203 && c.G == 213 && c.B == 225) return new SolidColorBrush(Color.FromRgb(97, 97, 97));
    				if (c.R == 96 && c.G == 165 && c.B == 250) return new SolidColorBrush(Color.FromRgb(25, 118, 210));
    				if (c.R == 74 && c.G == 222 && c.B == 128) return new SolidColorBrush(Color.FromRgb(46, 125, 50));
    				if (c.R == 251 && c.G == 146 && c.B == 60) return new SolidColorBrush(Color.FromRgb(230, 81, 0));
    				if (c.R == 45 && c.G == 212 && c.B == 191) return new SolidColorBrush(Color.FromRgb(0, 105, 92));
    			}
    		}
    		return originalBrush;
    	}

    	public void ApplyTheme()
    	{
    		try
    		{
    			bool isDark = StudentShell.IsDarkMode;
    			
    			if (isDark)
    			{
    				this.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
    				headerBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    				headerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
    				txtLessonTitle.Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
    				txtLessonInfo.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
    				contentScrollViewer.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
    				
    				btnRefresh.Background = new SolidColorBrush(Color.FromRgb(51, 65, 85));
    				btnRefresh.Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249));
    				btnRefresh.BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105));
    				
    				emptyState.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    				emptyState.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
    				txtEmptyMessage.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
    				
    				stageReviewPanel.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    				stageActivityPanel.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    			}
    			else
    			{
    				this.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));
    				headerBorder.Background = Brushes.White;
    				headerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    				txtLessonTitle.Foreground = new SolidColorBrush(Color.FromRgb(26, 32, 44));
    				txtLessonInfo.Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104));
    				contentScrollViewer.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));
    				
    				btnRefresh.Background = new SolidColorBrush(Color.FromRgb(237, 242, 247));
    				btnRefresh.Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104));
    				btnRefresh.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 224));
    				
    				emptyState.Background = Brushes.White;
    				emptyState.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    				txtEmptyMessage.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
    				
    				stageReviewPanel.Background = Brushes.White;
    				stageActivityPanel.Background = Brushes.White;
    			}
    			
    			foreach (object child in lessonContentPanel.Children)
    			{
    				if (child is Border blockBorder)
    				{
    					bool isFocused = (blockBorder == _currentFocusedBlock);
    					
    					if (isDark)
    					{
    						if (isFocused)
    						{
    							blockBorder.Background = new SolidColorBrush(Color.FromRgb(23, 37, 84));
    							blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
    						}
    						else
    						{
    							blockBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    							blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
    						}
    					}
    					else
    					{
    						if (isFocused)
    						{
    							blockBorder.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
    							blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
    						}
    						else
    						{
    							blockBorder.Background = Brushes.White;
    							blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    						}
    					}
    					
    					if (blockBorder.Child is StackPanel panel)
    					{
    						foreach (object panelChild in panel.Children)
    						{
    							if (panelChild is DockPanel headerDock)
    							{
    								foreach (object dockChild in headerDock.Children)
    								{
    									if (dockChild is Border badgeBorder && badgeBorder.Child is TextBlock badgeText)
    									{
    										if (badgeText.Text.StartsWith("Thứ tự:"))
    										{
    											if (isDark)
    											{
    												badgeBorder.Background = new SolidColorBrush(Color.FromRgb(51, 65, 85));
    												badgeText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
    											}
    											else
    											{
    												badgeBorder.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    												badgeText.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
    											}
    										}
    									}
    								}
    							}
    							else if (panelChild is RichTextBox rtb)
    							{
    								if (rtb.Document != null)
    								{
    									foreach (Block block in rtb.Document.Blocks)
    									{
    										if (block is Paragraph paragraph)
    										{
    											if (paragraph.Background is SolidColorBrush bgScb)
    											{
    												Color bgCol = bgScb.Color;
    												if (isDark && bgCol.R == 245 && bgCol.G == 245 && bgCol.B == 245)
    												{
    													paragraph.Background = new SolidColorBrush(Color.FromRgb(51, 65, 85));
    												}
    												else if (!isDark && bgCol.R == 51 && bgCol.G == 65 && bgCol.B == 85)
    												{
    													paragraph.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    												}
    											}
    											
    											foreach (Inline inline in paragraph.Inlines)
    											{
    												if (inline is Run run)
    												{
    													run.Foreground = GetThemeForeground(run.Foreground, isDark);
    												}
    											}
    										}
    									}
    								}
    							}
    							else if (panelChild is Border mediaCard)
    							{
    								if (isDark)
    								{
    									mediaCard.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    									mediaCard.BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105));
    								}
    								
    								if (mediaCard.Child is DockPanel mediaDock)
    								{
    									foreach (object dockChild in mediaDock.Children)
    									{
    										if (dockChild is TextBlock tb)
    										{
    											if (tb.Text.StartsWith("Tài nguyên"))
    											{
    												if (isDark)
    												{
    													tb.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
    												}
    											}
    										}
    										else if (dockChild is Button btn)
    										{
    											if (isDark)
    											{
    												btn.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
    												btn.Foreground = Brushes.White;
    											}
    										}
    									}
    								}
    							}
    						}
    					}
    				}
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("StudentLessonPage.ApplyTheme error: {Err}", ex.Message);
    		}
    	}
    }
}
