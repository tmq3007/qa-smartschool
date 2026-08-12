using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class V91SequenceToolUpgradeTests
    {
        private void RunOnStaThread(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        try
                        {
                            new Application();
                        }
                        catch { }
                    }
                    if (Application.Current != null)
                    {
                        lock (Application.Current.Resources)
                        {
                            if (!Application.Current.Resources.Contains("Gray100"))
                            {
                                Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray));
                            }
                        }
                    }
                    
                    InitializeApplicationFull(); action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        private static string GetTextFromResultChild(UIElement child)
        {
            if (child is TextBlock tb) return tb.Text;
            if (child is Border border)
            {
                if (border.Child is TextBlock btb) return btb.Text;
                if (border.Child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBlock gtb && Grid.GetColumn(gtb) == 0) return gtb.Text;
                        if (gridChild is WpfMath.Controls.FormulaControl formulaCtrl) return formulaCtrl.Formula;
                        if (gridChild is ScrollViewer sv && sv.Content is TextBlock svTb) return svTb.Text;
                        if (gridChild is System.Windows.Controls.Panel panel)
                        {
                            var sb = new System.Text.StringBuilder();
                            foreach (var pChild in panel.Children)
                            {
                                if (pChild is TextBlock ptb) sb.Append(ptb.Text);
                                if (pChild is WpfMath.Controls.FormulaControl fCtrl) sb.Append(fCtrl.Formula);
                            }
                            return sb.ToString();
                        }
                    }
                }
            }
            return "";
        }

        [Fact]
        public void Test_AP_NormalCalculation()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var resultPanel = (StackPanel)tool.FindName("apResultPanel");
                
                Assert.NotNull(txtU1);
                Assert.NotNull(txtD);
                Assert.NotNull(txtN);
                Assert.NotNull(resultPanel);

                // Set values
                txtU1.Text = "3";
                txtD.Text = "5";
                txtN.Text = "10";

                // Invoke SolveAP
                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Verify result rows
                Assert.NotEmpty(resultPanel.Children);
                
                bool foundUn = false;
                bool foundSn = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("Uₙ") && text.Contains("48"))
                        foundUn = true;
                    if (text.Contains("Sₙ") && text.Contains("255"))
                        foundSn = true;
                }
                Assert.True(foundUn, "U_n calculation is incorrect or not displayed.");
                Assert.True(foundSn, "S_n calculation is incorrect or not displayed.");
            });
        }

        [Fact]
        public void Test_GP_NormalCalculation_WithSubstitution()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var resultPanel = (StackPanel)tool.FindName("gpResultPanel");
                
                Assert.NotNull(txtU1);
                Assert.NotNull(txtQ);
                Assert.NotNull(txtN);
                Assert.NotNull(resultPanel);

                // Set values
                txtU1.Text = "2";
                txtQ.Text = "3";
                txtN.Text = "8";

                // Invoke SolveGP
                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Verify results & substitution
                Assert.NotEmpty(resultPanel.Children);
                
                bool foundUn = false;
                bool foundSnWithSubstitution = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("Uₙ") && text.Contains("4374"))
                        foundUn = true;
                    // Expected substitution format: Sₙ = U₁(qⁿ−1)/(q−1) = 2×(3⁸−1)/(3−1) = 6560
                    if (text.Contains("Sₙ") && text.Contains("2×") && text.Contains("3⁸") && text.Contains("6560"))
                        foundSnWithSubstitution = true;
                }
                Assert.True(foundUn, "U_n calculation is incorrect or not displayed.");
                Assert.True(foundSnWithSubstitution, "S_n formula substitution is incorrect or not displayed.");
            });
        }

        [Fact]
        public void Test_GP_ZeroFirstTerm_Supported()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var resultPanel = (StackPanel)tool.FindName("gpResultPanel");
                
                txtU1.Text = "0";
                txtQ.Text = "3";
                txtN.Text = "8";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Should compute and display results instead of returning empty
                Assert.NotEmpty(resultPanel.Children);
                
                bool foundUn = false;
                bool foundSn = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("Uₙ") && text.Contains("= 0"))
                        foundUn = true;
                    if (text.Contains("Sₙ") && text.Contains("= 0"))
                        foundSn = true;
                }
                Assert.True(foundUn, "U_n should be calculated as 0 when U_1 = 0.");
                Assert.True(foundSn, "S_n should be calculated as 0 when U_1 = 0.");
            });
        }

        [Fact]
        public void Test_ValidationWarnings_Displayed()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var resultPanel = (StackPanel)tool.FindName("apResultPanel");
                
                // Invalid n
                txtU1.Text = "3";
                txtD.Text = "5";
                txtN.Text = "invalid_n";

                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Should display validation alert
                Assert.NotEmpty(resultPanel.Children);
                bool foundWarning = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.ToLower().Contains("sử dụng số hạng đầu") || text.ToLower().Contains("nhập công sai d") || text.ToLower().Contains("nhập số hạng thứ n") || text.ToLower().Contains("nhập tổng sₙ") || text.ToLower().Contains("số số hạng n phải là số nguyên") || text.ToLower().Contains("số hạng n phải là số nguyên"))
                    {
                        foundWarning = true;
                        break;
                    }
                }
                Assert.True(foundWarning, "Validation warning for n was not displayed.");
            });
        }

        [Fact]
        public void Test_GraphWindow_BuildHtml_OfflineEvaluatorUpgraded()
        {
            var method = typeof(QASmartClass.LearningTools.Views.Math.GraphWindow).GetMethod("BuildHtml",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string html = (string)method.Invoke(null, new object[] { "calc.setExpression({id:'pts', latex:'(n, 3+(5)(n-1))'});" });
            Assert.NotNull(html);
            
            // Verify our offline parser upgrades exist in the template
            Assert.Contains("function evaluateLatex", html);
            Assert.Contains("var sliders = {}", html);
            Assert.Contains("var activeSliderVar", html);
        }

        [Fact]
        public void Test_AP_Inverse_FindU1()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbAP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var txtUn = (TextBox)tool.FindName("txtAP_Un");
                var resultPanel = (StackPanel)tool.FindName("apResultPanel");

                Assert.NotNull(cbMode);
                Assert.NotNull(txtU1);
                Assert.NotNull(txtD);
                Assert.NotNull(txtN);
                Assert.NotNull(txtUn);
                Assert.NotNull(resultPanel);

                // Mode 1: Tìm U1 khi biết d, n, Un
                cbMode.SelectedIndex = 1;
                txtD.Text = "5";
                txtN.Text = "10";
                txtUn.Text = "48";

                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Check that U1 was solved and filled as "3"
                Assert.Equal("3", txtU1.Text);

                // Verify result rows
                bool foundInfo = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("Tìm U₁ khi biết"))
                    {
                        foundInfo = true;
                        break;
                    }
                }
                Assert.True(foundInfo, "Inverse mode info header was not displayed.");
            });
        }

        [Fact]
        public void Test_AP_Inverse_FindD()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbAP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var txtUn = (TextBox)tool.FindName("txtAP_Un");
                var resultPanel = (StackPanel)tool.FindName("apResultPanel");

                cbMode.SelectedIndex = 3; // Mode 3: Tìm d khi biết U1, n, Un
                txtU1.Text = "3";
                txtN.Text = "10";
                txtUn.Text = "48";

                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Check that d was solved and filled as "5"
                Assert.Equal("5", txtD.Text);
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindQ_NewtonRaphson()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtSn = (TextBox)tool.FindName("txtGP_Sn");

                cbMode.SelectedIndex = 4; // Mode 4: Tìm q khi biết U1, n, Sn
                txtU1.Text = "2";
                txtN.Text = "8";
                txtSn.Text = "6560";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Newton Raphson should find q = 3
                Assert.Equal("3", txtQ.Text);
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindN_NegativeQ_Supported()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtUn = (TextBox)tool.FindName("txtGP_Un");

                cbMode.SelectedIndex = 5; // Mode 5: Tìm n khi biết U1, q, Un
                txtU1.Text = "5";
                txtQ.Text = "-2";
                txtUn.Text = "-160";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Should find n = 6
                Assert.Equal("6", txtN.Text);
            });
        }

        [Fact]
        public void Test_AP_Inverse_FindN_FromSn()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbAP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var txtSn = (TextBox)tool.FindName("txtAP_Sn");

                Assert.NotNull(cbMode);
                Assert.NotNull(txtU1);
                Assert.NotNull(txtD);
                Assert.NotNull(txtN);
                Assert.NotNull(txtSn);

                cbMode.SelectedIndex = 6; // Mode 6: Tìm n khi biết U1, d, Sn
                txtU1.Text = "3";
                txtD.Text = "5";
                txtSn.Text = "255";

                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Should find n = 10
                Assert.Equal("10", txtN.Text);
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindN_FromSn()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtSn = (TextBox)tool.FindName("txtGP_Sn");

                Assert.NotNull(cbMode);
                Assert.NotNull(txtU1);
                Assert.NotNull(txtQ);
                Assert.NotNull(txtN);
                Assert.NotNull(txtSn);

                cbMode.SelectedIndex = 6; // Mode 6: Tìm n khi biết U1, q, Sn
                txtU1.Text = "2";
                txtQ.Text = "3";
                txtSn.Text = "6560";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Should find n = 8
                Assert.Equal("8", txtN.Text);
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindQ_DoubleRoots()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtUn = (TextBox)tool.FindName("txtGP_Un");
                var resultPanel = (StackPanel)tool.FindName("gpResultPanel");

                Assert.NotNull(cbMode);
                Assert.NotNull(txtU1);
                Assert.NotNull(txtQ);
                Assert.NotNull(txtN);
                Assert.NotNull(txtUn);
                Assert.NotNull(resultPanel);

                cbMode.SelectedIndex = 3; // Mode 3: Tìm q khi biết U1, n, Un
                txtU1.Text = "5";
                txtN.Text = "3"; // n-1 = 2 (chẵn)
                txtUn.Text = "20"; // ratio = 4, q = 2 or -2

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Positive root should be set in TextBox q
                Assert.Equal("2", txtQ.Text);

                // Check that resultPanel contains both roots notice
                bool foundDoubleRootsMessage = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if ((text.Contains("Công bội q có thể có 2 giá trị đối nhau") || text.Contains("Bài toán có 2 nghiệm công bội hợp lệ")) && text.Contains("q = 2") && text.Contains("q = -2"))
                    {
                        foundDoubleRootsMessage = true;
                        break;
                    }
                }
                Assert.True(foundDoubleRootsMessage, "Double roots warning message was not found in result panel.");
            });
        }

        [Fact]
        public void Test_SequenceTool_HistoryPersistence()
        {
            RunOnStaThread(() =>
            {
                // Delete existing sequence_history.json if exists to start fresh
                string filePath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "QASmartClass",
                    "sequence_history.json"
                );
                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); } catch { }
                }

                var tool1 = new SequenceTool();
                var txtU1 = (TextBox)tool1.FindName("txtAP_U1");
                var txtD = (TextBox)tool1.FindName("txtAP_D");
                var txtN = (TextBox)tool1.FindName("txtAP_N");
                
                txtU1.Text = "3";
                txtD.Text = "5";
                txtN.Text = "10";

                var solveAPMethod = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                solveAPMethod.Invoke(tool1, null);

                // Verify file exists
                Assert.True(System.IO.File.Exists(filePath), "sequence_history.json was not created after calculation.");

                // Instantiate new tool2 and verify history is loaded
                var tool2 = new SequenceTool();
                tool2.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var apHistoryField = typeof(SequenceTool).GetField("_apHistory", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(apHistoryField);
                var apHistory = (System.Collections.Generic.List<SequenceTool.APConfig>)apHistoryField.GetValue(tool2);
                Assert.NotEmpty(apHistory);
                Assert.Equal(3.0, apHistory[0].U1);
                Assert.Equal(5.0, apHistory[0].D);
                Assert.Equal(10, apHistory[0].N);
            });
        }

        [Fact]
        public void Test_AP_Quiz_Generation_CorrectAnswer()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                // Generate quiz
                var generateMethod = typeof(SequenceTool).GetMethod("GenerateAPQuiz", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(generateMethod);
                generateMethod.Invoke(tool, null);

                // Get correct answer and question text via reflection
                var correctAnswerField = typeof(SequenceTool).GetField("_apQuizCorrectAnswer", BindingFlags.NonPublic | BindingFlags.Instance);
                var questionTextField = typeof(SequenceTool).GetField("_apQuizQuestionText", BindingFlags.NonPublic | BindingFlags.Instance);
                
                Assert.NotNull(correctAnswerField);
                Assert.NotNull(questionTextField);

                double correctAnswer = (double)correctAnswerField.GetValue(tool);
                string questionText = (string)questionTextField.GetValue(tool);

                Assert.NotEmpty(questionText);
                Assert.True(correctAnswer != 0);
            });
        }

        [Fact]
        public void Test_GP_Quiz_Generation_CorrectAnswer()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                // Generate quiz
                var generateMethod = typeof(SequenceTool).GetMethod("GenerateGPQuiz", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(generateMethod);
                generateMethod.Invoke(tool, null);

                // Get correct answer and question text via reflection
                var correctAnswerField = typeof(SequenceTool).GetField("_gpQuizCorrectAnswer", BindingFlags.NonPublic | BindingFlags.Instance);
                var questionTextField = typeof(SequenceTool).GetField("_gpQuizQuestionText", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.NotNull(correctAnswerField);
                Assert.NotNull(questionTextField);

                double correctAnswer = (double)correctAnswerField.GetValue(tool);
                string questionText = (string)questionTextField.GetValue(tool);

                Assert.NotEmpty(questionText);
                Assert.True(correctAnswer != 0);
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindQ_NegativeSn_EvenN()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtSn = (TextBox)tool.FindName("txtGP_Sn");

                cbMode.SelectedIndex = 4; // Mode 4: Tìm q khi biết U1, n, Sn
                txtU1.Text = "5";
                txtN.Text = "4";
                txtSn.Text = "-25";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Newton Raphson on h(q) should find q = -2
                Assert.Equal("-2", txtQ.Text);
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindN_BoundaryQ_NegativeOne()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtUn = (TextBox)tool.FindName("txtGP_Un");

                cbMode.SelectedIndex = 5; // Mode 5: Tìm n khi biết U1, q, Un
                txtU1.Text = "2";
                txtQ.Text = "-1";
                txtUn.Text = "-2";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Boundary handler should resolve n = 2
                Assert.Equal("2", txtN.Text);
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindN_BoundaryQ_NegativeOne_FromSn()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtSn = (TextBox)tool.FindName("txtGP_Sn");

                cbMode.SelectedIndex = 6; // Mode 6: Tìm n khi biết U1, q, Sn
                txtU1.Text = "3";
                txtQ.Text = "-1";
                txtSn.Text = "0";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Boundary handler should resolve n = 2
                Assert.Equal("2", txtN.Text);
            });
        }

        [Fact]
        public void Test_AP_GP_Inverse_FindN_LimitWarnings()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var apCbMode = (ComboBox)tool.FindName("cbAP_Mode");
                var txtApU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtApD = (TextBox)tool.FindName("txtAP_D");
                var txtApUn = (TextBox)tool.FindName("txtAP_Un");
                var apResultPanel = (StackPanel)tool.FindName("apResultPanel");

                // Set values to yield a huge n (n > 1000)
                apCbMode.SelectedIndex = 5; // Mode 5: Tìm n khi biết U1, d, Un
                txtApU1.Text = "1";
                txtApD.Text = "0.0001";
                txtApUn.Text = "10000"; // n = 99990001 > 1000

                var methodAp = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodAp);
                methodAp.Invoke(tool, null);

                // Verify that limit warning is displayed in apResultPanel
                bool foundWarning = false;
                foreach (UIElement child in apResultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("Số số hạng n quá lớn"))
                    {
                        foundWarning = true;
                        break;
                    }
                }
                Assert.True(foundWarning, "Limit warning for huge n in AP should be displayed.");
            });
        }

        [Fact]
        public void Test_UI_RealToFraction()
        {
            Assert.Equal("3/4", QASmartClass.LearningTools.UI.RealToFraction(0.75));
            Assert.Equal("-4/3", QASmartClass.LearningTools.UI.RealToFraction(-1.333333333));
            Assert.Equal("", QASmartClass.LearningTools.UI.RealToFraction(3.0));
            Assert.Equal("", QASmartClass.LearningTools.UI.RealToFraction(1e-7));
        }

        [Fact]
        public void Test_AP_FractionDisplay_For_D()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbAP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtUn = (TextBox)tool.FindName("txtAP_Un");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var resultPanel = (StackPanel)tool.FindName("apResultPanel");

                cbMode.SelectedIndex = 3; // Mode 3: Tìm d khi biết U1, n, Un
                txtU1.Text = "1";
                txtUn.Text = "2";
                txtN.Text = "4";

                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(tool, null);

                bool foundFraction = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("0.333333 (1/3)"))
                    {
                        foundFraction = true;
                        break;
                    }
                }
                Assert.True(foundFraction, "Fraction (1/3) should be displayed next to d value.");
            });
        }

        [Fact]
        public void Test_GP_FractionDisplay_For_Q()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtUn = (TextBox)tool.FindName("txtGP_Un");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var resultPanel = (StackPanel)tool.FindName("gpResultPanel");

                cbMode.SelectedIndex = 3; // Mode 3: Tìm q khi biết U1, n, Un
                txtU1.Text = "9";
                txtUn.Text = "4";
                txtN.Text = "3";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(tool, null);

                bool foundFraction = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("0.666667 (2/3)"))
                    {
                        foundFraction = true;
                        break;
                    }
                }
                Assert.True(foundFraction, "Fraction (2/3) should be displayed next to q value.");
            });
        }

        [Fact]
        public void Test_GP_GraphAsymptote_SInfinity()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                
                // Case 1: |q| < 1 (convergent) -> should contain y=S_inf
                string jsConvergent = tool.GetGPGraphScript(4.0, 0.5, 8);
                Assert.Contains("id:'sinf'", jsConvergent);
                Assert.Contains("latex:'y=8'", jsConvergent); // S_inf = 4 / (1 - 0.5) = 8
                
                // Case 2: |q| >= 1 (divergent) -> should NOT contain y=S_inf
                string jsDivergent = tool.GetGPGraphScript(4.0, 2.0, 8);
                Assert.DoesNotContain("id:'sinf'", jsDivergent);
            });
        }

        [Fact]
        public void Test_AP_Inverse_FindN_DualRoots_ReturnsBoth()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbAP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtSn = (TextBox)tool.FindName("txtAP_Sn");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var resultPanel = (StackPanel)tool.FindName("apResultPanel");

                cbMode.SelectedIndex = 6; // Mode 6: Tìm n khi biết U1, d, Sn
                txtU1.Text = "5";
                txtD.Text = "-2";
                txtSn.Text = "5";

                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Should set txtAP_N to the larger root (5)
                Assert.Equal("5", txtN.Text);

                // Result panel should contain references to both n = 1 and n = 5
                bool foundN1 = false;
                bool foundN5 = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("n = 1")) foundN1 = true;
                    if (text.Contains("n = 5")) foundN5 = true;
                }
                Assert.True(foundN1, "Alternate root n=1 should be displayed.");
                Assert.True(foundN5, "Default root n=5 should be displayed.");
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindQ_DualRoots_CalculatesBothSums()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtUn = (TextBox)tool.FindName("txtGP_Un");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var resultPanel = (StackPanel)tool.FindName("gpResultPanel");

                cbMode.SelectedIndex = 3; // Mode 3: Tìm q khi biết U1, n, Un
                txtU1.Text = "2";
                txtN.Text = "3";
                txtUn.Text = "18";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // TextBox should have default (larger/positive) q = 3
                Assert.Equal("3", txtQ.Text);

                // Result panel should contain both q = 3 and q = -3
                bool foundQ3 = false;
                bool foundQNeg3 = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("q = 3")) foundQ3 = true;
                    if (text.Contains("q = -3")) foundQNeg3 = true;
                }
                Assert.True(foundQ3, "Root q=3 should be displayed.");
                Assert.True(foundQNeg3, "Alternate root q=-3 should be displayed.");
            });
        }

        [Fact]
        public void Test_AP_NegativeD_Parenthesized()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbAP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtN = (TextBox)tool.FindName("txtAP_N");
                var resultPanel = (StackPanel)tool.FindName("apResultPanel");

                cbMode.SelectedIndex = 0; // Mode 0: Xuôi
                txtU1.Text = "3";
                txtD.Text = "-5";
                txtN.Text = "10";

                var method = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Formula should contain ×(-5)
                bool foundParenthesizedD = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("×(-5)"))
                    {
                        foundParenthesizedD = true;
                        break;
                    }
                }
                Assert.True(foundParenthesizedD, "Negative d should be parenthesized in formula.");
            });
        }

        [Fact]
        public void Test_GP_Inverse_FindQ_NewtonRaphson_DualQ()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var cbMode = (ComboBox)tool.FindName("cbGP_Mode");
                var txtU1 = (TextBox)tool.FindName("txtGP_U1");
                var txtN = (TextBox)tool.FindName("txtGP_N");
                var txtSn = (TextBox)tool.FindName("txtGP_Sn");
                var txtQ = (TextBox)tool.FindName("txtGP_Q");
                var resultPanel = (StackPanel)tool.FindName("gpResultPanel");

                cbMode.SelectedIndex = 4; // Mode 4: Tìm q khi biết U1, n, Sn
                txtU1.Text = "2";
                txtN.Text = "3";
                txtSn.Text = "26";

                var method = typeof(SequenceTool).GetMethod("SolveGP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // TextBox should contain default root q = 3 (since 3 >= -4)
                Assert.Equal("3", txtQ.Text);

                // Result panel should contain references to both roots: q = 3 and q = -4
                bool foundQ3 = false;
                bool foundQNeg4 = false;
                foreach (UIElement child in resultPanel.Children)
                {
                    string text = GetTextFromResultChild(child);
                    if (text.Contains("q = 3")) foundQ3 = true;
                    if (text.Contains("q = -4")) foundQNeg4 = true;
                }
                Assert.True(foundQ3, "Root q=3 should be found and displayed.");
                Assert.True(foundQNeg4, "Alternate root q=-4 should be found and displayed.");
            });
        }

        [Fact]
        public void Test_AP_QuizStreak_WorksCorrectly()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var txtAnswer = (TextBox)tool.FindName("txtAP_QuizAnswer");
                var lblFeedback = (TextBlock)tool.FindName("lblAP_QuizFeedback");

                // Get private quiz state fields using reflection
                var correctAnsField = typeof(SequenceTool).GetField("_apQuizCorrectAnswer", BindingFlags.NonPublic | BindingFlags.Instance);
                var streakField = typeof(SequenceTool).GetField("_apStreak", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(correctAnsField);
                Assert.NotNull(streakField);

                // Set correct answer
                correctAnsField.SetValue(tool, 15.0);
                streakField.SetValue(tool, 2);

                // Simulate correct check
                txtAnswer.Text = "15";
                var checkMethod = typeof(SequenceTool).GetMethod("APQuizCheck_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(checkMethod);
                checkMethod.Invoke(tool, new object[] { null, null });

                // Streak should increment to 3
                Assert.Equal(3, (int)streakField.GetValue(tool));
                Assert.Contains("Chuỗi đúng liên tiếp: 3 🔥", lblFeedback.Text);

                // Simulate incorrect check
                txtAnswer.Text = "10";
                checkMethod.Invoke(tool, new object[] { null, null });

                // Streak should reset to 0
                Assert.Equal(0, (int)streakField.GetValue(tool));
                Assert.Contains("❌ Chưa chính xác", lblFeedback.Text);
            });
        }

        [Fact]
        public void Test_ExportHtmlReport_ExtractsNestedContent()
        {
            RunOnStaThread(() =>
            {
                var tool = new SequenceTool();
                var txtU1 = (TextBox)tool.FindName("txtAP_U1");
                var txtD = (TextBox)tool.FindName("txtAP_D");
                var txtN = (TextBox)tool.FindName("txtAP_N");

                txtU1.Text = "3";
                txtD.Text = "5";
                txtN.Text = "10";

                var solveMethod = typeof(SequenceTool).GetMethod("SolveAP", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(solveMethod);
                solveMethod.Invoke(tool, null);

                var generateReportMethod = typeof(SequenceTool).GetMethod("GenerateHtmlReport", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(generateReportMethod);

                // Generate HTML report for AP (isAp = true)
                string html = (string)generateReportMethod.Invoke(tool, new object[] { true });

                // The HTML report should extract formulas correctly
                Assert.Contains("🔑 Uₙ = U₁ + (n−1)d =", html);
                Assert.Contains("🔑 Sₙ = n(U₁+Uₙ)/2 =", html);
                Assert.Contains("📋 Dãy:", html);
            });
        }
    }
}
