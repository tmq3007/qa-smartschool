import sys

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/LearningTools/Controls/TouchNumPad.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Add Primitives
code = code.replace("using System.Windows.Shapes;", "using System.Windows.Shapes;\nusing System.Windows.Controls.Primitives;")

# Add static fields
old_fields = """public static class TouchNumPad
    {
        private static Popup? _popup;"""
new_fields = """public static class TouchNumPad
    {
        private static Popup? _popup;
        private static StackPanel? _contentPanel;
        private static Button? _btnMin;"""
code = code.replace(old_fields, new_fields)

# Modify ShowPopup
old_show = """private static void ShowPopup(TextBox target)
        {
            if (_popup == null)
                _popup = CreatePopup();

            _popup.PlacementTarget = target;
            _popup.IsOpen = true;
        }"""
new_show = """private static void ShowPopup(TextBox target)
        {
            if (_popup == null)
                _popup = CreatePopup();

            _popup.PlacementTarget = target;
            _popup.HorizontalOffset = 0;
            _popup.VerticalOffset = 0;
            if (_contentPanel != null) _contentPanel.Visibility = Visibility.Visible;
            if (_btnMin != null) _btnMin.Content = "_";
            _popup.IsOpen = true;
        }"""
code = code.replace(old_show, new_show)

# Modify CreatePopup
old_create = """var mainStack = new StackPanel();

            // Row 1: ▲ increment + display"""
new_create = """var mainStack = new StackPanel();

            // --- Title Bar (Draggable, Minimize, Hide) ---
            var titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(30, 32, 38)), Margin = new Thickness(0, 0, 0, 4) };
            var thumb = new Thumb
            {
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeAll
            };
            var thumbTemplate = new ControlTemplate(typeof(Thumb));
            var borderFactoryThumb = new FrameworkElementFactory(typeof(Border));
            borderFactoryThumb.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            thumbTemplate.VisualTree = borderFactoryThumb;
            thumb.Template = thumbTemplate;

            thumb.DragDelta += (s, e) =>
            {
                if (_popup != null)
                {
                    _popup.HorizontalOffset += e.HorizontalChange;
                    _popup.VerticalOffset += e.VerticalChange;
                }
            };

            var titleText = new TextBlock
            {
                Text = "Bàn phím",
                Foreground = Brushes.LightGray,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                IsHitTestVisible = false
            };

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            _contentPanel = new StackPanel();

            _btnMin = new Button
            {
                Content = "_",
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Width = 24, Height = 24,
                Cursor = Cursors.Hand
            };
            _btnMin.Click += (s, e) =>
            {
                if (_contentPanel.Visibility == Visibility.Visible)
                {
                    _contentPanel.Visibility = Visibility.Collapsed;
                    _btnMin.Content = "□";
                }
                else
                {
                    _contentPanel.Visibility = Visibility.Visible;
                    _btnMin.Content = "_";
                }
            };
            btnStack.Children.Add(_btnMin);

            var btnClose = new Button
            {
                Content = "X",
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Width = 24, Height = 24,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => HidePopup();
            btnStack.Children.Add(btnClose);

            titleBar.Children.Add(thumb);
            titleBar.Children.Add(titleText);
            titleBar.Children.Add(btnStack);

            mainStack.Children.Add(titleBar);
            mainStack.Children.Add(_contentPanel);

            // Row 1: ▲ increment + display"""
code = code.replace(old_create, new_create)

# Replace mainStack -> _contentPanel
code = code.replace("mainStack.Children.Add(topRow);", "_contentPanel.Children.Add(topRow);")
code = code.replace("mainStack.Children.Add(numGrid);", "_contentPanel.Children.Add(numGrid);")
code = code.replace("mainStack.Children.Add(bottomRow);", "_contentPanel.Children.Add(bottomRow);")

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/LearningTools/Controls/TouchNumPad.cs', 'w', encoding='utf-8') as f:
    f.write(code)
