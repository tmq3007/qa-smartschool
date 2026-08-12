#!/usr/bin/env python3
"""Fix remaining mojibake in ToolRegistry.cs using byte-level approach"""
import os

target = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models", "ToolRegistry.cs")

with open(target, "rb") as f:
    raw = f.read()

# Strip BOM
if raw[:3] == b'\xef\xbb\xbf':
    raw = raw[3:]

# Work at byte level - find sequences where UTF-8 was double-encoded
# Pattern: C3 xx or C4 xx or E1 BA/BB xx sequences that are still mojibake
# These are the UTF-8 bytes of Latin-1 characters that represent UTF-8 bytes

def fix_remaining(data):
    """
    Scan bytes for remaining mojibake patterns.
    The text currently has some chars correctly decoded but others still broken.
    We look for sequences of bytes that when decoded as UTF-8, produce characters
    in the C0-FF range (Latin supplement), which could be re-interpreted.
    """
    text = data.decode('utf-8')
    
    # Build replacement table for specific known broken patterns
    # These use Unicode escapes to avoid source file encoding issues
    repls = {}
    
    # Vietnamese lowercase with hooks/horns - á» series (U+1EBx range encoded as C3+A1 C2+BB+xx)
    repls['\u00e1\u00bb\u0081'] = '\u1ec1'  # ề
    repls['\u00e1\u00bb\u0083'] = '\u1ec3'  # ể  
    repls['\u00e1\u00bb\u0085'] = '\u1ec5'  # ễ
    repls['\u00e1\u00bb\u0087'] = '\u1ec7'  # ệ
    repls['\u00e1\u00bb\u0089'] = '\u1ec9'  # ỉ
    repls['\u00e1\u00bb\u008b'] = '\u1ecb'  # ị
    repls['\u00e1\u00bb\u008d'] = '\u1ecd'  # ọ
    repls['\u00e1\u00bb\u008f'] = '\u1ecf'  # ỏ
    repls['\u00e1\u00bb\u0091'] = '\u1ed1'  # ố
    repls['\u00e1\u00bb\u0093'] = '\u1ed3'  # ồ
    repls['\u00e1\u00bb\u0095'] = '\u1ed5'  # ổ
    repls['\u00e1\u00bb\u0097'] = '\u1ed7'  # ỗ
    repls['\u00e1\u00bb\u0099'] = '\u1ed9'  # ộ
    repls['\u00e1\u00bb\u009b'] = '\u1edb'  # ớ
    repls['\u00e1\u00bb\u009d'] = '\u1edd'  # ờ
    repls['\u00e1\u00bb\u009f'] = '\u1edf'  # ở
    repls['\u00e1\u00bb\u00a1'] = '\u1ee1'  # ỡ
    repls['\u00e1\u00bb\u00a3'] = '\u1ee3'  # ợ
    repls['\u00e1\u00bb\u00a5'] = '\u1ee5'  # ụ
    repls['\u00e1\u00bb\u00a7'] = '\u1ee7'  # ủ
    repls['\u00e1\u00bb\u00a9'] = '\u1ee9'  # ứ
    repls['\u00e1\u00bb\u00ab'] = '\u1eeb'  # ừ
    repls['\u00e1\u00bb\u00ad'] = '\u1eed'  # ữ
    repls['\u00e1\u00bb\u00af'] = '\u1eef'  # ử
    repls['\u00e1\u00bb\u00b1'] = '\u1ef1'  # ự
    repls['\u00e1\u00bb\u00b3'] = '\u1ef3'  # ỳ
    repls['\u00e1\u00bb\u00b5'] = '\u1ef5'  # ỵ
    repls['\u00e1\u00bb\u00b7'] = '\u1ef7'  # ỷ
    repls['\u00e1\u00bb\u00b9'] = '\u1ef9'  # ỹ
    
    # áº series
    repls['\u00e1\u00ba\u00a1'] = '\u1ea1'  # ạ
    repls['\u00e1\u00ba\u00a3'] = '\u1ea3'  # ả
    repls['\u00e1\u00ba\u00a5'] = '\u1ea5'  # ấ
    repls['\u00e1\u00ba\u00a7'] = '\u1ea7'  # ầ
    repls['\u00e1\u00ba\u00a9'] = '\u1ea9'  # ẩ
    repls['\u00e1\u00ba\u00ab'] = '\u1eab'  # ẫ
    repls['\u00e1\u00ba\u00ad'] = '\u1ead'  # ậ
    repls['\u00e1\u00ba\u00af'] = '\u1eaf'  # ắ
    repls['\u00e1\u00ba\u00b1'] = '\u1eb1'  # ằ
    repls['\u00e1\u00ba\u00b3'] = '\u1eb3'  # ẳ
    repls['\u00e1\u00ba\u00b5'] = '\u1eb5'  # ẵ
    repls['\u00e1\u00ba\u00b7'] = '\u1eb7'  # ặ
    repls['\u00e1\u00ba\u00b9'] = '\u1eb9'  # ẹ
    repls['\u00e1\u00ba\u00bb'] = '\u1ebb'  # ẻ
    repls['\u00e1\u00ba\u00bd'] = '\u1ebd'  # ẽ
    repls['\u00e1\u00ba\u00bf'] = '\u1ebf'  # ế

    # Đ/đ/ă/Ă
    repls['\u00c4\u0091'] = '\u0111'  # đ
    repls['\u00c4\u0090'] = '\u0110'  # Đ
    repls['\u00c4\u0083'] = '\u0103'  # ă
    repls['\u00c4\u0082'] = '\u0102'  # Ă
    
    # Common accented Latin chars
    repls['\u00c3\u00a1'] = '\u00e1'  # á
    repls['\u00c3\u00a0'] = '\u00e0'  # à
    repls['\u00c3\u00a2'] = '\u00e2'  # â
    repls['\u00c3\u00a3'] = '\u00e3'  # ã
    repls['\u00c3\u00a9'] = '\u00e9'  # é
    repls['\u00c3\u00a8'] = '\u00e8'  # è
    repls['\u00c3\u00aa'] = '\u00ea'  # ê
    repls['\u00c3\u00ad'] = '\u00ed'  # í
    repls['\u00c3\u00ac'] = '\u00ec'  # ì
    repls['\u00c3\u00b3'] = '\u00f3'  # ó
    repls['\u00c3\u00b2'] = '\u00f2'  # ò
    repls['\u00c3\u00b4'] = '\u00f4'  # ô
    repls['\u00c3\u00b5'] = '\u00f5'  # õ
    repls['\u00c3\u00ba'] = '\u00fa'  # ú
    repls['\u00c3\u00b9'] = '\u00f9'  # ù
    repls['\u00c3\u00bd'] = '\u00fd'  # ý
    
    # Uppercase accented
    repls['\u00c3\u0081'] = '\u00c1'  # Á
    repls['\u00c3\u0080'] = '\u00c0'  # À
    repls['\u00c3\u0082'] = '\u00c2'  # Â  
    repls['\u00c3\u0089'] = '\u00c9'  # É
    repls['\u00c3\u008a'] = '\u00ca'  # Ê
    repls['\u00c3\u0093'] = '\u00d3'  # Ó
    repls['\u00c3\u0094'] = '\u00d4'  # Ô
    repls['\u00c3\u009a'] = '\u00da'  # Ú
    
    # Punctuation / special
    repls['\u00e2\u0080\u0094'] = '\u2014'  # —
    repls['\u00e2\u0080\u0093'] = '\u2013'  # –
    repls['\u00e2\u0080\u009c'] = '\u201c'  # "
    repls['\u00e2\u0080\u009d'] = '\u201d'  # "
    repls['\u00e2\u0080\u0098'] = '\u2018'  # '
    repls['\u00e2\u0080\u0099'] = '\u2019'  # '
    repls['\u00e2\u0086\u0092'] = '\u2192'  # →
    repls['\u00e2\u0088\u00ab'] = '\u222b'  # ∫
    repls['\u00e2\u0088\u009e'] = '\u221e'  # ∞
    repls['\u00e2\u0088\u0091'] = '\u2211'  # ∑
    repls['\u00e2\u0084\u0082'] = '\u2102'  # ℂ
    repls['\u00ce\u0094'] = '\u0394'  # Δ
    repls['\u00c2\u00b2'] = '\u00b2'  # ²
    repls['\u00c2\u00b3'] = '\u00b3'  # ³
    repls['\u00c2\u00b0'] = '\u00b0'  # °
    repls['\u00e2\u0082\u0099'] = '\u2099'  # ₙ
    
    # 4-byte emoji (encoded as 4 mojibake chars each)
    repls['\u00f0\u009f\u0094\u00a2'] = '\U0001F522'  # 🔢
    repls['\u00f0\u009f\u0093\u0090'] = '\U0001F4D0'  # 📐
    repls['\u00f0\u009f\u0093\u008a'] = '\U0001F4CA'  # 📊
    repls['\u00f0\u009f\u0094\u0084'] = '\U0001F504'  # 🔄
    repls['\u00f0\u009f\u0093\u0088'] = '\U0001F4C8'  # 📈
    repls['\u00f0\u009f\u0092\u00bb'] = '\U0001F4BB'  # 💻
    repls['\u00f0\u009f\u00a7\u00ae'] = '\U0001F9EE'  # 🧮
    repls['\u00f0\u009f\u0094\u00b5'] = '\U0001F535'  # 🔵
    repls['\u00f0\u009f\u00a7\u008a'] = '\U0001F9CA'  # 🧊
    repls['\u00f0\u009f\u0095\u0090'] = '\U0001F550'  # 🕐
    repls['\u00f0\u009f\u009a\u0082'] = '\U0001F682'  # 🚂
    repls['\u00f0\u009f\u0094\u00ac'] = '\U0001F52C'  # 🔬
    repls['\u00f0\u009f\u00a7\u00aa'] = '\U0001F9EA'  # 🧪
    repls['\u00f0\u009f\u008c\u00a1'] = '\U0001F321'  # 🌡
    repls['\u00f0\u009f\u008c\u008d'] = '\U0001F30D'  # 🌍
    repls['\u00f0\u009f\u00aa\u0090'] = '\U0001FA90'  # 🪐
    repls['\u00f0\u009f\u0093\u0096'] = '\U0001F4D6'  # 📖
    repls['\u00f0\u009f\u0093\u009a'] = '\U0001F4DA'  # 📚
    repls['\u00f0\u009f\u0093\u0093'] = '\U0001F4D3'  # 📓
    repls['\u00f0\u009f\u0093\u008b'] = '\U0001F4CB'  # 📋
    repls['\u00f0\u009f\u0093\u008c'] = '\U0001F4CC'  # 📌
    repls['\u00f0\u009f\u009a\u00a6'] = '\U0001F6A6'  # 🚦
    repls['\u00f0\u009f\u00a7\u00ac'] = '\U0001F9EC'  # 🧬
    repls['\u00f0\u009f\u00a7\u00a0'] = '\U0001F9E0'  # 🧠
    repls['\u00f0\u009f\u00a7\u00a9'] = '\U0001F9E9'  # 🧩
    repls['\u00f0\u009f\u0083\u008f'] = '\U0001F0CF'  # 🃏
    repls['\u00f0\u009f\u0087\u00ac\u00f0\u009f\u0087\u00a7'] = '\U0001F1EC\U0001F1E7'  # 🇬🇧
    repls['\u00f0\u009f\u0097\u00a3'] = '\U0001F5E3'  # 🗣
    repls['\u00f0\u009f\u008f\u00b9'] = '\U0001F3F9'  # 🏹
    repls['\u00f0\u009f\u008f\u009b'] = '\U0001F3DB'  # 🏛
    repls['\u00f0\u009f\u008e\u00b2'] = '\U0001F3B2'  # 🎲
    repls['\u00f0\u009f\u008e\u00af'] = '\U0001F3AF'  # 🎯
    repls['\u00f0\u009f\u0094\u00ad'] = '\U0001F52D'  # 🔭
    repls['\u00f0\u009f\u0093\u009d'] = '\U0001F4DD'  # 📝
    
    # Box drawing
    repls['\u00e2\u0095\u0090'] = '\u2550'  # ═
    
    # Sort by length (longest first)
    sorted_r = sorted(repls.items(), key=lambda x: -len(x[0]))
    
    count = 0
    for old, new in sorted_r:
        if old in text:
            c = text.count(old)
            text = text.replace(old, new)
            count += c
    
    return text, count

fixed_text, total = fix_remaining(raw)

# Write
with open(target, "wb") as f:
    f.write(b'\xef\xbb\xbf')
    f.write(fixed_text.encode("utf-8"))

print(f"Fixed {total} character sequences")

# Quick verify
with open(target, "r", encoding="utf-8-sig") as f:
    check = f.read()

tests = ["Toán Tiểu Học", "Bảng Cửu Chương", "Lượng Giác", "Hình Học", "Đổi", "Đáng Nhớ", "Khoa Học"]
found = [w for w in tests if w in check]
print(f"Verified: {len(found)}/{len(tests)}: {found}")
