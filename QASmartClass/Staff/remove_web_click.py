import re

path = 'D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/Views/BroadcastPage.xaml.cs'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

# The section to remove starts with QuickLink_Click and ends with SendWeb_Click
patt = re.compile(r'\s*// ==========================================================\s*// 🔹 4\. GỬI WEBSITE\s*// ==========================================================.*?catch \(Exception ex\) \{ ShowToast\(\$"\u274c Lỗi: \{ex\.Message\}", "#C62828"\); \}\s*\}\s*', re.DOTALL)

c = re.sub(patt, '\n\n', c)

# Fallback pattern if the above fails
patt2 = re.compile(r'\s*private void QuickLink_Click.*?private async void SendWeb_Click.*?\n        }\s*', re.DOTALL)
c = re.sub(patt2, '\n\n', c)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('Success')
