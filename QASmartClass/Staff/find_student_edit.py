import os

file_path = r"..\Classroom\Views\StudentPage.xaml.cs"
try:
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        lines = content.splitlines()
        for idx, line in enumerate(lines):
            if "edit" in line.lower() or "save" in line.lower() or "student" in line.lower() and ("void" in line or "task" in line.lower()):
                print(f"Line {idx+1}: {line.strip()}")
except Exception as e:
    print(f"Error: {e}")
