import re

file_path = r"Forms\Form2_MainDashboard.xaml.cs"

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Remove lines with CalculateContentBounds
content = re.sub(
    r'\s*var bounds = CalculateContentBounds\(\w+\);[^\n]*\n',
    '',
    content
)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Removed all CalculateContentBounds lines")
