#!/usr/bin/env python3
"""Pass 2: Fix remaining mojibake using explicit replacement tables (Unicode escapes only)"""
import os, glob

root = os.path.dirname(os.path.abspath(__file__))

# Comprehensive replacement map using only Unicode escapes
REPLS = {
    # Vietnamese ề-ỹ series (á» prefix in mojibake)
    '\u00e1\u00bb\u0081': '\u1ec1',  # ề
    '\u00e1\u00bb\u0083': '\u1ec3',  # ể
    '\u00e1\u00bb\u0085': '\u1ec5',  # ễ
    '\u00e1\u00bb\u0087': '\u1ec7',  # ệ
    '\u00e1\u00bb\u0089': '\u1ec9',  # ỉ
    '\u00e1\u00bb\u008b': '\u1ecb',  # ị
    '\u00e1\u00bb\u008d': '\u1ecd',  # ọ
    '\u00e1\u00bb\u008f': '\u1ecf',  # ỏ
    '\u00e1\u00bb\u0091': '\u1ed1',  # ố
    '\u00e1\u00bb\u0093': '\u1ed3',  # ồ
    '\u00e1\u00bb\u0095': '\u1ed5',  # ổ
    '\u00e1\u00bb\u0097': '\u1ed7',  # ỗ
    '\u00e1\u00bb\u0099': '\u1ed9',  # ộ
    '\u00e1\u00bb\u009b': '\u1edb',  # ớ
    '\u00e1\u00bb\u009d': '\u1edd',  # ờ
    '\u00e1\u00bb\u009f': '\u1edf',  # ở
    '\u00e1\u00bb\u00a1': '\u1ee1',  # ỡ
    '\u00e1\u00bb\u00a3': '\u1ee3',  # ợ
    '\u00e1\u00bb\u00a5': '\u1ee5',  # ụ
    '\u00e1\u00bb\u00a7': '\u1ee7',  # ủ
    '\u00e1\u00bb\u00a9': '\u1ee9',  # ứ
    '\u00e1\u00bb\u00ab': '\u1eeb',  # ừ
    '\u00e1\u00bb\u00ad': '\u1eed',  # ữ
    '\u00e1\u00bb\u00af': '\u1eef',  # ử
    '\u00e1\u00bb\u00b1': '\u1ef1',  # ự
    '\u00e1\u00bb\u00b3': '\u1ef3',  # ỳ
    '\u00e1\u00bb\u00b5': '\u1ef5',  # ỵ
    '\u00e1\u00bb\u00b7': '\u1ef7',  # ỷ
    '\u00e1\u00bb\u00b9': '\u1ef9',  # ỹ
    # Vietnamese ạ-ế series (áº prefix in mojibake)
    '\u00e1\u00ba\u00a1': '\u1ea1',  # ạ
    '\u00e1\u00ba\u00a3': '\u1ea3',  # ả
    '\u00e1\u00ba\u00a5': '\u1ea5',  # ấ
    '\u00e1\u00ba\u00a7': '\u1ea7',  # ầ
    '\u00e1\u00ba\u00a9': '\u1ea9',  # ẩ
    '\u00e1\u00ba\u00ab': '\u1eab',  # ẫ
    '\u00e1\u00ba\u00ad': '\u1ead',  # ậ
    '\u00e1\u00ba\u00af': '\u1eaf',  # ắ
    '\u00e1\u00ba\u00b1': '\u1eb1',  # ằ
    '\u00e1\u00ba\u00b3': '\u1eb3',  # ẳ
    '\u00e1\u00ba\u00b5': '\u1eb5',  # ẵ
    '\u00e1\u00ba\u00b7': '\u1eb7',  # ặ
    '\u00e1\u00ba\u00b9': '\u1eb9',  # ẹ
    '\u00e1\u00ba\u00bb': '\u1ebb',  # ẻ
    '\u00e1\u00ba\u00bd': '\u1ebd',  # ẽ
    '\u00e1\u00ba\u00bf': '\u1ebf',  # ế
    # Đ/đ/ă/Ă
    '\u00c4\u0091': '\u0111',  # đ
    '\u00c4\u0090': '\u0110',  # Đ
    '\u00c4\u0083': '\u0103',  # ă
    '\u00c4\u0082': '\u0102',  # Ă
    # Common accented Latin chars  
    '\u00c3\u00a1': '\u00e1',  # á
    '\u00c3\u00a0': '\u00e0',  # à
    '\u00c3\u00a2': '\u00e2',  # â
    '\u00c3\u00a3': '\u00e3',  # ã
    '\u00c3\u00a9': '\u00e9',  # é
    '\u00c3\u00a8': '\u00e8',  # è
    '\u00c3\u00aa': '\u00ea',  # ê
    '\u00c3\u00ad': '\u00ed',  # í
    '\u00c3\u00ac': '\u00ec',  # ì
    '\u00c3\u00b3': '\u00f3',  # ó
    '\u00c3\u00b2': '\u00f2',  # ò
    '\u00c3\u00b4': '\u00f4',  # ô
    '\u00c3\u00b5': '\u00f5',  # õ
    '\u00c3\u00ba': '\u00fa',  # ú
    '\u00c3\u00b9': '\u00f9',  # ù
    '\u00c3\u00bd': '\u00fd',  # ý
    # Uppercase
    '\u00c3\u0081': '\u00c1',  # Á
    '\u00c3\u0082': '\u00c2',  # Â
    '\u00c3\u0089': '\u00c9',  # É
    '\u00c3\u008a': '\u00ca',  # Ê
    '\u00c3\u0093': '\u00d3',  # Ó
    '\u00c3\u0094': '\u00d4',  # Ô
    '\u00c3\u009a': '\u00da',  # Ú
    '\u00c3\u009d': '\u00dd',  # Ý
    # Special
    '\u00e2\u0080\u0094': '\u2014',  # —
    '\u00e2\u0080\u0093': '\u2013',  # –
    '\u00e2\u0086\u0092': '\u2192',  # →
    '\u00e2\u0089\u00a4': '\u2264',  # ≤
    '\u00e2\u0089\u00a5': '\u2265',  # ≥
    '\u00e2\u0088\u009e': '\u221e',  # ∞
    '\u00c2\u00b2': '\u00b2',  # ²
    '\u00c2\u00b3': '\u00b3',  # ³
    '\u00c2\u00b0': '\u00b0',  # °
    '\u00c2\u00b1': '\u00b1',  # ±
    '\u00ce\u0094': '\u0394',  # Δ
    '\u00ce\u00b1': '\u03b1',  # α
    '\u00ce\u00b2': '\u03b2',  # β
    '\u00cf\u0080': '\u03c0',  # π
    '\u00e2\u0082\u0099': '\u2099',  # ₙ
}

# Sort by longest first
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
