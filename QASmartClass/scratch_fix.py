import sys

path = r'D:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\Classroom\Views\LessonEditorPage.xaml.cs'
try:
    content = open(path, 'r', encoding='utf-8').read()
    print('Length before:', len(content))
    lines = content.splitlines(keepends=True)
    print('Old line 3549:', repr(lines[3548]).encode('ascii', 'backslashreplace').decode())
    print('Old line 3641:', repr(lines[3640]).encode('ascii', 'backslashreplace').decode())
    
    lines[3548] = '                        " B\u00c0I T\u1eacP\\n\\n1. X\u00e1c \u0111\u1ecbnh lo\u1ea1i LK: MgO, CO\u2082, N\u2082, CaCl\u2082\\n2. V\u1ebd s\u01a1 \u0111\u1ed3 Lewis: H\u2082O, NH\u2083, CH\u2084\\n3. So s\u00e1nh t\u00b0 n\u00f3ng ch\u1ea3y: NaCl vs H\u2082O vs kim c\u01b0\u01a1ng",\n'
    lines[3640] = '                        "- I. KI\u1ebeN TH\u1ee8C\\n\\n V\u1ecb tr\u00ed \u0111\u1ecba l\u00fd: ...\\n \u0111\u1eb7c \u0111i\u1ec3m t\u1ef1 nhi\u00ean: \u0111\u1ecba h\u00ecnh, kh\u00ed h\u1eadu, th\u1ee7y v\u0103n\\n D\u00e2n c\u01b0 - x\u00e3 h\u1ed9i: ...\\n Kinh t\u1ebf: n\u00f4ng nghi\u1ec7p, c\u00f4ng nghi\u1ec7p, d\u1ecbch v\u1ee5\\n\\n Gi\u00e1o vi\u00ean: Cho HS x\u00e1c \u0111\u1ecbnh v\u1ecb tr\u00ed tr\xean b\u1ea3n \u0111\u1ed3\",\n'
    
    new_content = "".join(lines)
    open(path, 'w', encoding='utf-8-sig').write(new_content)
    print('Successfully wrote file')
    print('New length:', len(new_content))
    
    # Read back to verify
    verify = open(path, 'r', encoding='utf-8').read().splitlines(keepends=True)
    print('Verified line 3549:', repr(verify[3548]).encode('ascii', 'backslashreplace').decode())
    print('Verified line 3641:', repr(verify[3640]).encode('ascii', 'backslashreplace').decode())
except Exception as e:
    print('Error:', e)
