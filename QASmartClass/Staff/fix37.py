import sys

# 1. Update TouchNumPad.cs
tnp_path = 'D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/LearningTools/Controls/TouchNumPad.cs'
with open(tnp_path, 'r', encoding='utf-8') as f:
    tnp_code = f.read()

# Add _currentPlacement
old_fields = "private static bool _allowNegative = true;"
new_fields = "private static bool _allowNegative = true;\n        private static PlacementMode _currentPlacement = PlacementMode.Bottom;"
tnp_code = tnp_code.replace(old_fields, new_fields)

# Update Attach signature
old_attach = "public static void Attach(TextBox textBox, double step = 1,\n            double min = double.MinValue, double max = double.MaxValue,\n            bool allowDecimal = true, bool allowNegative = true)"
new_attach = "public static void Attach(TextBox textBox, double step = 1,\n            double min = double.MinValue, double max = double.MaxValue,\n            bool allowDecimal = true, bool allowNegative = true,\n            PlacementMode placement = PlacementMode.Bottom)"
tnp_code = tnp_code.replace(old_attach, new_attach)

# Update HandleFocus
old_handle_focus = """_allowDecimal = allowDecimal;
                _allowNegative = allowNegative;
                ShowPopup(textBox);"""
new_handle_focus = """_allowDecimal = allowDecimal;
                _allowNegative = allowNegative;
                _currentPlacement = placement;
                ShowPopup(textBox);"""
tnp_code = tnp_code.replace(old_handle_focus, new_handle_focus)

# Update ShowPopup
old_show_popup = """_popup.PlacementTarget = target;
            _popup.HorizontalOffset = 0;"""
new_show_popup = """_popup.PlacementTarget = target;
            _popup.Placement = _currentPlacement;
            _popup.HorizontalOffset = 0;"""
tnp_code = tnp_code.replace(old_show_popup, new_show_popup)

with open(tnp_path, 'w', encoding='utf-8') as f:
    f.write(tnp_code)


# 2. Update MathPlayerZone.xaml.cs
mpz_path = 'D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/LearningTools/Views/Math/MathPlayerZone.xaml.cs'
with open(mpz_path, 'r', encoding='utf-8') as f:
    mpz_code = f.read()

old_attach_mpz = "QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtAnswer, step: 1);"
new_attach_mpz = "QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtAnswer, step: 1, placement: System.Windows.Controls.Primitives.PlacementMode.Left);"
mpz_code = mpz_code.replace(old_attach_mpz, new_attach_mpz)

with open(mpz_path, 'w', encoding='utf-8') as f:
    f.write(mpz_code)
