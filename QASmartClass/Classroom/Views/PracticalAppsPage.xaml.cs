using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class PracticalAppsPage : Page
    {
        private bool _isPhetInitialized = false;
        private bool _isDesmosInitialized = false;

        public PracticalAppsPage()
        {
            InitializeComponent();
            
            // Set up initial tab loading
            Loaded += async (s, e) =>
            {
                try
                {
                    if (QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudent())
                    {
                        btnBackHome.Visibility = Visibility.Collapsed;
                        btnFocusHS.Visibility = Visibility.Collapsed;
                        btnUnfocusHS.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("PracticalApps role check error: {Err}", ex.Message);
                }

                var activeToolId = QASmartClass.Services.LessonStateService.Instance.ActiveToolId;
                if (activeToolId == "desmos_graph")
                {
                    Log.Information("Active tool is Desmos Graphing. Selecting Desmos tab.");
                    tabApps.SelectedItem = tabDesmos;
                }
                else
                {
                    Log.Information("PracticalAppsPage loaded. Initializing PhET simulation WebView.");
                    await InitializePhetAsync();
                }
            };
        }

        #region PhET WebView Loading & Events
        private async Task InitializePhetAsync()
        {
            if (_isPhetInitialized) return;

            try
            {
                await webPhet.EnsureCoreWebView2Async(null);
                
                webPhet.CoreWebView2.Settings.IsScriptEnabled = true;
                webPhet.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webPhet.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webPhet.CoreWebView2.Settings.AreDevToolsEnabled = false;

                webPhet.CoreWebView2.NavigationStarting += Phet_NavigationStarting;
                webPhet.CoreWebView2.NavigationCompleted += Phet_NavigationCompleted;
                webPhet.CoreWebView2.SourceChanged += Phet_SourceChanged;

                webPhet.CoreWebView2.Navigate("https://phet.colorado.edu/vi/simulations/browse?type=html");
                
                _isPhetInitialized = true;
                webPhet.Visibility = Visibility.Visible;
                panelPhetOffline.Visibility = Visibility.Collapsed;
                Log.Information("PhET simulation WebView initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("PhET WebView initialization failed: {Err}", ex.Message);
                panelPhetOffline.Visibility = Visibility.Visible;
                webPhet.Visibility = Visibility.Collapsed;
            }
        }

        private void Phet_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            progressPhet.Visibility = Visibility.Visible;
            txtPhetStatus.Visibility = Visibility.Visible;
            txtPhetStatus.Text = "Đang tải...";
        }

        private void Phet_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            progressPhet.Visibility = Visibility.Collapsed;
            txtPhetStatus.Visibility = Visibility.Collapsed;
            if (!e.IsSuccess)
            {
                Log.Warning("PhET WebView navigation failed: {Status}", e.WebErrorStatus);
            }
        }

        private void Phet_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (webPhet.CoreWebView2 != null)
            {
                txtPhetUrl.Text = webPhet.Source.ToString();
            }
        }

        private void BtnPhetBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isPhetInitialized && webPhet.CanGoBack)
            {
                webPhet.GoBack();
            }
        }

        private void BtnPhetRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isPhetInitialized)
            {
                webPhet.Reload();
            }
        }
        #endregion

        #region Desmos WebView Loading & Events
        private async Task InitializeDesmosAsync()
        {
            if (_isDesmosInitialized) return;

            try
            {
                await webDesmos.EnsureCoreWebView2Async(null);

                webDesmos.CoreWebView2.Settings.IsScriptEnabled = true;
                webDesmos.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webDesmos.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webDesmos.CoreWebView2.Settings.AreDevToolsEnabled = false;

                webDesmos.CoreWebView2.NavigationStarting += Desmos_NavigationStarting;
                webDesmos.CoreWebView2.NavigationCompleted += Desmos_NavigationCompleted;
                webDesmos.CoreWebView2.SourceChanged += Desmos_SourceChanged;

                webDesmos.CoreWebView2.Navigate("https://www.desmos.com/calculator");

                _isDesmosInitialized = true;
                webDesmos.Visibility = Visibility.Visible;
                panelDesmosOffline.Visibility = Visibility.Collapsed;
                Log.Information("Desmos WebView initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("Desmos WebView initialization failed: {Err}", ex.Message);
                panelDesmosOffline.Visibility = Visibility.Visible;
                webDesmos.Visibility = Visibility.Collapsed;
            }
        }

        private void Desmos_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            progressDesmos.Visibility = Visibility.Visible;
            txtDesmosStatus.Visibility = Visibility.Visible;
            txtDesmosStatus.Text = "Đang tải...";
        }

        private void Desmos_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            progressDesmos.Visibility = Visibility.Collapsed;
            txtDesmosStatus.Visibility = Visibility.Collapsed;
            if (!e.IsSuccess)
            {
                Log.Warning("Desmos WebView navigation failed: {Status}", e.WebErrorStatus);
            }
        }

        private void Desmos_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (webDesmos.CoreWebView2 != null)
            {
                txtDesmosUrl.Text = webDesmos.Source.ToString();
            }
        }

        private void BtnDesmosBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isDesmosInitialized && webDesmos.CanGoBack)
            {
                webDesmos.GoBack();
            }
        }

        private void BtnDesmosRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isDesmosInitialized)
            {
                webDesmos.Reload();
            }
        }
        #endregion

        private async void TabApps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabApps.SelectedItem == tabDesmos && !_isDesmosInitialized)
            {
                Log.Information("User switched to Desmos tab. Triggering lazy initialization.");
                await InitializeDesmosAsync();
            }
            else if (tabApps.SelectedItem == tabPhet && !_isPhetInitialized)
            {
                Log.Information("User switched to PhET tab. Triggering lazy initialization.");
                await InitializePhetAsync();
            }
        }

        // =======================================================
        //  NAVIGATION & TEACHING ACTIONS
        // =======================================================
        private void GoBackHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusSectionVisual();
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusStudents();
            }
            catch { }

            var shell = Window.GetWindow(this) as ClassroomShell;
            if (shell != null)
            {
                Log.Information("PracticalApps sub-flow: Navigating back to Learning Tools Hub (F32)");
                shell.NavigateTo("F32");
            }
        }

        private void FocusHS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string toolId = "phet_sim";
                if (tabApps != null)
                {
                    if (tabApps.SelectedItem == tabDesmos)
                        toolId = "desmos_graph";
                }
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.FocusStudents(toolId);
                btnFocusHS.Content = "✅ Đang Focus";
                btnFocusHS.IsEnabled = false;
                btnFocusHS.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 230, 201));
            }
            catch (Exception ex)
            {
                Log.Warning("Focus practical apps click error: {Err}", ex.Message);
            }
        }

        private void UnfocusHS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusSectionVisual();
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusStudents();
                btnFocusHS.Content = "🎯 Focus HS";
                btnFocusHS.IsEnabled = true;
                btnFocusHS.Background = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFrom("#E8F5E9")!;
            }
            catch (Exception ex)
            {
                Log.Warning("Unfocus practical apps click error: {Err}", ex.Message);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clear events and clean up resource to avoid memory leaks
            try
            {
                if (_isPhetInitialized && webPhet != null)
                {
                    webPhet.Dispose();
                }

                if (_isDesmosInitialized && webDesmos != null)
                {
                    webDesmos.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error unloading PracticalAppsPage: {Err}", ex.Message);
            }
        }
    }
}
