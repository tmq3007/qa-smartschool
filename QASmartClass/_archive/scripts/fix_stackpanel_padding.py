import os

d = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Workplace'

for f in os.listdir(d):
    if f.endswith('.xaml'):
        p = os.path.join(d, f)
        with open(p, 'r', encoding='utf-8') as file:
            content = file.read()
        
        new_content = content.replace('<StackPanel Background="White" Padding="12">', '<StackPanel Background="White" Margin="12">')
        
        if content != new_content:
            with open(p, 'w', encoding='utf-8') as file:
                file.write(new_content)
            print(f"Fixed {f}")
