#!/usr/bin/env python3
"""Pass 3: Fix remaining mojibake with UPPERCASE Vietnamese chars (0x80-0x9F range)"""
import os, glob

root = os.path.dirname(os.path.abspath(__file__))

# These are the UPPERCASE Vietnamese chars that use control char range 0x80-0x9F
# which both CP1252 and the previous table missed
REPLS = {
    # Uppercase Vietnamese with á»\x8X pattern
    '\u00e1\u00bb\u0080': '\u1ec0',  # Ề
    '\u00e1\u00bb\u0082': '\u1ec2',  # Ể
    '\u00e1\u00bb\u0084': '\u1ec4',  # Ễ
    '\u00e1\u00bb\u0086': '\u1ec6',  # Ệ
    '\u00e1\u00bb\u0088': '\u1ec8',  # Ỉ
    '\u00e1\u00bb\u008a': '\u1eca',  # Ị
    '\u00e1\u00bb\u008c': '\u1ecc',  # Ọ
    '\u00e1\u00bb\u008e': '\u1ece',  # Ỏ
    '\u00e1\u00bb\u0090': '\u1ed0',  # Ố
    '\u00e1\u00bb\u0092': '\u1ed2',  # Ồ
    '\u00e1\u00bb\u0094': '\u1ed4',  # Ổ
    '\u00e1\u00bb\u0096': '\u1ed6',  # Ỗ
    '\u00e1\u00bb\u0098': '\u1ed8',  # Ộ
    '\u00e1\u00bb\u009a': '\u1eda',  # Ớ
    '\u00e1\u00bb\u009c': '\u1edc',  # Ờ
    '\u00e1\u00bb\u009e': '\u1ede',  # Ở
    '\u00e1\u00bb\u00a0': '\u1ee0',  # Ỡ
    '\u00e1\u00bb\u00a2': '\u1ee2',  # Ợ
    '\u00e1\u00bb\u00a4': '\u1ee4',  # Ụ
    '\u00e1\u00bb\u00a6': '\u1ee6',  # Ủ
    '\u00e1\u00bb\u00a8': '\u1ee8',  # Ứ
    '\u00e1\u00bb\u00aa': '\u1eea',  # Ừ
    '\u00e1\u00bb\u00ac': '\u1eec',  # Ữ
    '\u00e1\u00bb\u00ae': '\u1eee',  # Ử
    '\u00e1\u00bb\u00b0': '\u1ef0',  # Ự
    # Uppercase áº\x8X patterns
    '\u00e1\u00ba\u00a0': '\u1ea0',  # Ạ
    '\u00e1\u00ba\u00a2': '\u1ea2',  # Ả
    '\u00e1\u00ba\u00a4': '\u1ea4',  # Ấ
    '\u00e1\u00ba\u00a6': '\u1ea6',  # Ầ
    '\u00e1\u00ba\u00a8': '\u1ea8',  # Ẩ
    '\u00e1\u00ba\u00aa': '\u1eaa',  # Ẫ
    '\u00e1\u00ba\u00ac': '\u1eac',  # Ậ
    '\u00e1\u00ba\u00ae': '\u1eae',  # Ắ
    '\u00e1\u00ba\u00b0': '\u1eb0',  # Ằ
    '\u00e1\u00ba\u00b2': '\u1eb2',  # Ẳ
    '\u00e1\u00ba\u00b4': '\u1eb4',  # Ẵ
    '\u00e1\u00ba\u00b6': '\u1eb6',  # Ặ
    '\u00e1\u00ba\u00b8': '\u1eb8',  # Ẹ
    '\u00e1\u00ba\u00ba': '\u1eba',  # Ẻ
    '\u00e1\u00ba\u00bc': '\u1ebc',  # Ẽ
    '\u00e1\u00ba\u00be': '\u1ebe',  # Ế
    # Uppercase Ã\x8X
    '\u00c3\u0080': '\u00c0',  # À
    '\u00c3\u0083': '\u00c3',  # Ã
    '\u00c3\u0088': '\u00c8',  # È
    '\u00c3\u008c': '\u00cc',  # Ì
    '\u00c3\u0092': '\u00d2',  # Ò
    '\u00c3\u0095': '\u00d5',  # Õ
    '\u00c3\u0099': '\u00d9',  # Ù
    # Ơ/Ư uppercase
    '\u00c6\u00af': '\u01af',  # Ư
    '\u00c6\u00a0': '\u01a0',  # Ơ
}

sorted_repls = sorted(REPLS.items(), key=lambda x: -len(x[0]))

cs_files = glob.glob(os.path.join(root, "**", "*.cs"), recursive=True)
total_files = 0
total_chars = 0

for filepath in sorted(cs_files):
    relpath = os.path.relpath(filepath, root)
    with open(filepath, "rb") as f:
        raw = f.read()
    has_bom = raw[:3] == b'\xef\xbb\xbf'
    content = raw[3:] if has_bom else raw
    try:
        text = content.decode("utf-8")
    except UnicodeDecodeError:
        continue
    
    count = 0
    for old, new in sorted_repls:
        if old in text:
            c = text.count(old)
            text = text.replace(old, new)
            count += c
    
    if count > 0:
        with open(filepath, "wb") as f:
            f.write(b'\xef\xbb\xbf')
            f.write(text.encode("utf-8"))
        total_files += 1
        total_chars += count
        print(f"FIXED: {relpath} ({count} chars)")

print(f"\nTotal: {total_files} files, {total_chars} chars fixed")
