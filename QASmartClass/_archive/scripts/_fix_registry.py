#!/usr/bin/env python3
"""Fix double-encoded UTF-8 in ToolRegistry.cs using manual decode with mixed encoding handling"""
import os, re

target = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models", "ToolRegistry.cs")

with open(target, "rb") as f:
    raw = f.read()

# Strip BOM
if raw[:3] == b'\xef\xbb\xbf':
    raw = raw[3:]

# Decode the raw file content
text = raw.decode("utf-8")

# Strategy: process the file line by line
# For each line, try to fix mojibake by encoding to cp1252 then decoding as UTF-8
# If a line doesn't need fixing (pure ASCII or already correct), leave it alone

def fix_mojibake(s):
    """Try to fix double-encoded UTF-8 text"""
    # Only fix strings that contain typical mojibake patterns
    mojibake_patterns = ['\u00c3\u00a1', '\u00c3\u00a0', '\u00c3\u00a2', '\u00c3\u00b3', '\u00c3\u00b4', 
                         '\u00c3\u00ba', '\u00c3\u00ad', '\u00c4\u0083', '\u00c4\u0091',
                         '\u00e1\u00bb', '\u00e1\u00ba']
    
    has_mojibake = any(p in s for p in mojibake_patterns)
    if not has_mojibake:
        return s
    
    result = []
    i = 0
    chars = list(s)
    
    while i < len(s):
        # Try to decode a multi-byte sequence
        # CP1252 bytes that form UTF-8 sequences
        chunk = s[i:]
        fixed = False
        
        # Try sequences of 2-4 chars that might be a single UTF-8 char encoded via CP1252
        for length in [3, 2]:
            if i + length <= len(s):
                try:
                    test = s[i:i+length].encode('cp1252')
                    decoded = test.decode('utf-8')
                    # Verify it produces a single character or valid short string
                    if len(decoded) <= 2 and ord(decoded[0]) > 127:
                        result.append(decoded)
                        i += length
                        fixed = True
                        break
                except (UnicodeEncodeError, UnicodeDecodeError):
                    continue
        
        if not fixed:
            result.append(s[i])
            i += 1
    
    return ''.join(result)

# Process the entire text
lines = text.split('\n')
fixed_lines = []
changes = 0

for line_num, line in enumerate(lines, 1):
    fixed_line = fix_mojibake(line)
    if fixed_line != line:
        changes += 1
    fixed_lines.append(fixed_line)

fixed_text = '\n'.join(fixed_lines)

# Write back with BOM
with open(target, "wb") as f:
    f.write(b'\xef\xbb\xbf')
    f.write(fixed_text.encode("utf-8"))

print(f"Fixed {changes} lines")

# Verify
with open(target, "r", encoding="utf-8-sig") as f:
    check = f.read()

ok_words = ["Toán", "Bảng", "Lượng", "Khoa", "Ngữ", "Hình", "Phân"]
found = [w for w in ok_words if w in check]
print(f"Verification: {len(found)}/{len(ok_words)} words OK")

if len(found) >= 4:
    print("SUCCESS!")
else:
    print("May need manual review")
