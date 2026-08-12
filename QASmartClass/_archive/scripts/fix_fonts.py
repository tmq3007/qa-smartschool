import os
import sys

# Add pip user install path
sys.path.append(r'C:\Users\DELL\AppData\Roaming\Python\Python314\site-packages')
import ftfy

tools_dir = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools'
fixed_count = 0

def process_file(filepath):
    global fixed_count
    if not (filepath.endswith('.xaml') or filepath.endswith('.cs')):
        return

    with open(filepath, 'r', encoding='utf-8-sig') as f:
        try:
            content = f.read()
        except UnicodeDecodeError:
            return

    fixed_content = ftfy.fix_text(content)
    
    if fixed_content != content:
        with open(filepath, 'w', encoding='utf-8-sig') as f:
            f.write(fixed_content)
        print(f'Fixed {os.path.basename(filepath)}')
        fixed_count += 1

for root, dirs, files in os.walk(tools_dir):
    for f in files:
        process_file(os.path.join(root, f))

print(f"Finished! Fixed {fixed_count} files.")
