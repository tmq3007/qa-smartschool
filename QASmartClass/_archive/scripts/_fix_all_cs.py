#!/usr/bin/env python3
"""Fix double-encoded UTF-8 in ALL .cs files under LearningTools"""
import os, glob

root = os.path.dirname(os.path.abspath(__file__))

def fix_mojibake(text):
    """Fix remaining mojibake using character-by-character approach"""
    # Check if text contains mojibake indicators
    if '\u00c3' not in text and '\u00e1' not in text and '\u00c4' not in text:
        return text, 0
    
    result = []
    i = 0
    count = 0
    
    while i < len(text):
        fixed = False
        
        # Try sequences of 2-4 chars that might be a single UTF-8 char encoded via CP1252
        for length in [4, 3, 2]:
            if i + length <= len(text):
                chunk = text[i:i+length]
                try:
                    encoded = chunk.encode('cp1252')
                    decoded = encoded.decode('utf-8')
                    # Verify it produces valid non-ASCII character(s)
                    if len(decoded) <= 2 and any(ord(c) > 127 for c in decoded):
                        result.append(decoded)
                        i += length
                        fixed = True
                        count += 1
                        break
                except (UnicodeEncodeError, UnicodeDecodeError):
                    continue
        
        if not fixed:
            result.append(text[i])
            i += 1
    
    return ''.join(result), count

# Find all .cs files
cs_files = glob.glob(os.path.join(root, "**", "*.cs"), recursive=True)
# Exclude fix scripts
cs_files = [f for f in cs_files if not f.endswith('.py')]

total_files_fixed = 0
total_chars_fixed = 0

for filepath in sorted(cs_files):
    relpath = os.path.relpath(filepath, root)
    
    with open(filepath, "rb") as f:
        raw = f.read()
    
    # Strip BOM
    has_bom = raw[:3] == b'\xef\xbb\xbf'
    content = raw[3:] if has_bom else raw
    
    try:
        text = content.decode("utf-8")
    except UnicodeDecodeError:
        print(f"SKIP (not UTF-8): {relpath}")
        continue
    
    # Check for mojibake indicators  
    if '\u00c3' not in text and '\u00e1\u00bb' not in text and '\u00e1\u00ba' not in text and '\u00c4' not in text:
        continue
    
    fixed_text, char_count = fix_mojibake(text)
    
    if char_count > 0:
        # Write back with BOM
        with open(filepath, "wb") as f:
            f.write(b'\xef\xbb\xbf')
            f.write(fixed_text.encode("utf-8"))
        
        total_files_fixed += 1
        total_chars_fixed += char_count
        print(f"FIXED: {relpath} ({char_count} chars)")

print(f"\n{'='*50}")
print(f"Total: {total_files_fixed} files fixed, {total_chars_fixed} character sequences corrected")
