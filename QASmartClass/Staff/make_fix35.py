import sys

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Add _lastGraphExprBox
class_start = "public partial class StemToolsPage : Page\n    {"
class_new = "public partial class StemToolsPage : Page\n    {\n        private TextBox? _lastGraphExprBox;"
code = code.replace(class_start, class_new)

# Add GotFocus hooks
loaded_start = "Loaded += (_, _) =>\n        {"
loaded_new = """Loaded += (_, _) =>
        {
            txtExpr1.GotFocus += (s, e) => _lastGraphExprBox = txtExpr1;
            txtExpr2.GotFocus += (s, e) => _lastGraphExprBox = txtExpr2;
            _lastGraphExprBox = txtExpr1;"""
code = code.replace(loaded_start, loaded_new)

# Modify AddGraphPresetBtn
old_click = "btn.Click += (_, _) => { if (txtExpr1 != null) txtExpr1.Text = expr; };"
new_click = "btn.Click += (_, _) => { if (_lastGraphExprBox != null) _lastGraphExprBox.Text = expr; else if (txtExpr1 != null) txtExpr1.Text = expr; };"
code = code.replace(old_click, new_click)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Staff/fix35.py', 'w', encoding='utf-8') as f:
    f.write(code)
