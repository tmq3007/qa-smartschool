import os
import re

d = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Workplace'
for f in os.listdir(d):
    if f.endswith('.xaml'):
        p = os.path.join(d, f)
        with open(p, 'r', encoding='utf-8') as file:
            content = file.read()
        
        # Only remove CornerRadius from Expander tags
        def replace_expander(match):
            return re.sub(r'\s+CornerRadius="[^"]+"', '', match.group(0))
            
        new_content = re.sub(r'<Expander[^>]+>', replace_expander, content)
        
        if content != new_content:
            with open(p, 'w', encoding='utf-8') as file:
                file.write(new_content)
            print(f"Fixed {f}")
