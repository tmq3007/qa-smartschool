import os
import sys

def fix_double_encoding(input_path):
    """Fix files that were double-encoded: UTF-8 -> misread as Latin-1 -> saved as UTF-8"""
    with open(input_path, 'rb') as f:
        raw = f.read()
    
    # Skip UTF-8 BOM if present
    has_bom = raw[:3] == b'\xef\xbb\xbf'
    content = raw[3:] if has_bom else raw
    
    # Decode as UTF-8 to get the "corrupted" text
    text = content.decode('utf-8')
    
    # Convert each char back to its byte value
    # Chars with ordinal < 256 → direct byte value (this reverses the Latin-1 misread)
    # Chars with ordinal >= 256 → these were NOT part of the double-encoding, 
    # keep them by encoding to UTF-8
    result_bytes = bytearray()
    for ch in text:
        cp = ord(ch)
        if cp < 256:
            result_bytes.append(cp)
        else:
            # This char wasn't in Latin-1, so it was already a real Unicode char
            result_bytes.extend(ch.encode('utf-8'))
    
    # Now decode the reassembled bytes as UTF-8
    fixed = result_bytes.decode('utf-8', errors='replace')
    
    # Write back with UTF-8 BOM (to match XAML expectations)
    with open(input_path, 'w', encoding='utf-8-sig') as f:
        f.write(fixed)
    
    return fixed

# List of files to fix
base = r'D:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools'
files = [
    r'Views\Thinking\SudokuTool.xaml',
    r'Views\Thinking\MemoryGameTool.xaml',
    r'Views\Science\LensTool.xaml',
    r'Views\Science\DensityTool.xaml',
    r'Views\Science\ConstantsTool.xaml',
    r'Views\Science\BoilingFreezingTool.xaml',
    r'Views\Multi\TextbookTool.xaml',
    r'Views\Math\TrigonometryTool.xaml',
    r'Views\Math\TrigEquationTool.xaml',
    r'Views\Math\SequenceTool.xaml',
    r'Views\Math\QuadraticTool.xaml',
    r'Views\Math\MultiplicationTool.xaml',
    r'Views\Math\LogarithmTool.xaml',
    r'Views\Math\LinearSystemTool.xaml',
    r'Views\Math\InequalityTool.xaml',
    r'Views\Math\IdentityTool.xaml',
    r'Views\Math\GeometryTool.xaml',
    r'Views\Math\DesmosGraphWindow.xaml',
    r'Views\Math\CubicTool.xaml',
    r'Views\Math\CoordinateTool.xaml',
    r'Views\Language\VocabularyTool.xaml',
    r'Views\Language\IpaTool.xaml',
    r'Views\Language\GrammarTool.xaml',
]

log = open(os.path.join(base, '_fix_log.txt'), 'w', encoding='utf-8')
fixed_count = 0

for rel in files:
    path = os.path.join(base, rel)
    if not os.path.exists(path):
        log.write(f'NOT FOUND: {rel}\n')
        continue
    
    try:
        fixed = fix_double_encoding(path)
        # Verify: check no mojibake patterns remain
        mojibake_markers = ['Ã´', 'á»', 'áº', 'Ã¢', 'Ã¡', 'Ã©', 'Ãª']
        has_mojibake = any(m in fixed for m in mojibake_markers)
        
        if has_mojibake:
            log.write(f'STILL BROKEN: {rel}\n')
        else:
            fixed_count += 1
            log.write(f'FIXED: {rel}\n')
    except Exception as e:
        log.write(f'ERROR: {rel} - {e}\n')

log.write(f'\nTotal fixed: {fixed_count}/{len(files)}\n')
log.close()

# Print the log
with open(os.path.join(base, '_fix_log.txt'), 'r', encoding='utf-8') as f:
    print(f.read())
