 = Get-Content "d:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Multi\MindmapTool.xaml.cs" -Raw -Encoding UTF8

 = "            border.MouseLeftButtonDown += (s, e) => { SelectNode(node); e.Handled = true; };
            border.MouseRightButtonDown += (s, e) => { ShowNodeMenu(node, e); e.Handled = true; };
            border.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && _dragNode == node)
                {
                    var pos = e.GetPosition(mindmapCanvas);
                    node.X = pos.X - _dragOffset.X;
                    node.Y = pos.Y - _dragOffset.Y;
                    Canvas.SetLeft(border, node.X);
                    Canvas.SetTop(border, node.Y);
                    UpdateConnectors(node);
                }
            };
            border.MouseLeftButtonDown += (s, e) =>
            {
                _dragNode = node;
                _dragOffset = e.GetPosition(border);
                border.CaptureMouse();
            };
            border.MouseLeftButtonUp += (s, e) =>
            {
                _dragNode = null;
                border.ReleaseMouseCapture();
            };"

 = "            border.MouseRightButtonDown += (s, e) => { ShowNodeMenu(node, e); e.Handled = true; };
            border.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && _dragNode == node)
                {
                    var pos = e.GetPosition(mindmapCanvas);
                    node.X = pos.X - _dragOffset.X;
                    node.Y = pos.Y - _dragOffset.Y;
                    Canvas.SetLeft(border, node.X);
                    Canvas.SetTop(border, node.Y);
                    UpdateConnectors(node);
                }
            };
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    EditNodeText(node);
                    e.Handled = true;
                    return;
                }
                SelectNode(node);
                _dragNode = node;
                _dragOffset = e.GetPosition(border);
                border.CaptureMouse();
                e.Handled = true;
            };
            border.MouseLeftButtonUp += (s, e) =>
            {
                _dragNode = null;
                border.ReleaseMouseCapture();
            };"

 = .Replace(, )

 = "        private void EditNodeText(MindNode node)
        {
            if (node.Visual == null) return;
            var border = (Border)node.Visual;
            var tb = border.Child as TextBlock;
            if (tb == null) return;

            var editBox = new TextBox
            {
                Text = node.Text,
                FontSize = tb.FontSize,
                FontWeight = tb.FontWeight,
                FontFamily = tb.FontFamily,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Attach Touch Keyboard
            QASmartClass.LearningTools.Controls.TouchTextPad.Attach(editBox, mode: \"text\");

            Action commit = () =>
            {
                if (border.Child != tb)
                {
                    node.Text = editBox.Text;
                    tb.Text = editBox.Text;
                    border.Child = tb;
                }
            };

            editBox.LostFocus += (s, e) => commit();
            editBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) commit();
            };

            border.Child = editBox;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
            {
                editBox.Focus();
                editBox.SelectAll();
            }));
        }

        private void ChangeNodeColor"

 =  -replace "(?s)        private void EditNodeText\(MindNode node\).*?        private void ChangeNodeColor", 

Set-Content "d:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Multi\MindmapTool.xaml.cs" -Value  -Encoding UTF8
