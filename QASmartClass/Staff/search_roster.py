import os

project_root = ".."
for root, dirs, files in os.walk(project_root):
    if any(p in root for p in ["bin", "obj", ".vs"]):
        continue
    for f in files:
        if f.endswith(".cs"):
            file_path = os.path.join(root, f)
            try:
                with open(file_path, "r", encoding="utf-8") as file:
                    content = file.read()
                    if "class ClassRoster" in content or "ClassRosterService" in content or "GetActiveStudents" in content:
                        print(os.path.relpath(file_path, project_root))
            except Exception:
                pass
