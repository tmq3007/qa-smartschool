#!/usr/bin/env python3
"""Final pass: fix remaining broken chars in ToolRegistry.cs"""
import os, re

target = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models", "ToolRegistry.cs")

with open(target, "rb") as f:
    raw = f.read()
if raw[:3] == b'\xef\xbb\xbf':
    raw = raw[3:]
text = raw.decode("utf-8")

count = 0

# Fix "VectÆ¡" -> "Vectơ" (Æ¡ = \u01a1 which is wrong, should be ơ = \u01a1... wait that IS ơ)
# Actually \u01a1 IS the correct "ơ" in Vietnamese. Let me check what's actually there.
# The console showed "VectÆ¡" which means the console can't render it but it might be correct.
# Let me check the actual bytes.

# Fix the "Ã Tưởng" issue: should be "Ý Tưởng"
# \u00c3 followed by space = mojibake for \u00dd (Ý) that wasn't decoded
if "\u00c3 T\u01b0\u1edfng" in text:
    text = text.replace("\u00c3 T\u01b0\u1edfng", "\u00dd T\u01b0\u1edfng")
    count += 1
    print("Fixed: Ã Tưởng -> Ý Tưởng")

# Also fix icons - the emoji are still double-encoded (4 bytes each as mojibake)
# These show as sequences like \u00f0\u009f\u0094\u00a2 for 🔢
# The first fix script should have caught these but let's check
emoji_map = {
    "\u00f0\u009f\u0094\u00a2": "\U0001F522",  # 🔢
    "\u00f0\u009f\u0093\u0090": "\U0001F4D0",  # 📐  
    "\u00f0\u009f\u0093\u008a": "\U0001F4CA",  # 📊
    "\u00f0\u009f\u0094\u0084": "\U0001F504",  # 🔄
    "\u00f0\u009f\u0093\u0088": "\U0001F4C8",  # 📈
    "\u00f0\u009f\u0092\u00bb": "\U0001F4BB",  # 💻
    "\u00f0\u009f\u00a7\u00ae": "\U0001F9EE",  # 🧮
    "\u00f0\u009f\u0094\u00b5": "\U0001F535",  # 🔵
    "\u00f0\u009f\u00a7\u008a": "\U0001F9CA",  # 🧊
    "\u00f0\u009f\u0095\u0090": "\U0001F550",  # 🕐
    "\u00f0\u009f\u009a\u0082": "\U0001F682",  # 🚂
    "\u00f0\u009f\u0094\u00ac": "\U0001F52C",  # 🔬
    "\u00f0\u009f\u00a7\u00aa": "\U0001F9EA",  # 🧪
    "\u00f0\u009f\u008c\u00a1": "\U0001F321",  # 🌡
    "\u00f0\u009f\u008c\u008d": "\U0001F30D",  # 🌍
    "\u00f0\u009f\u00aa\u0090": "\U0001FA90",  # 🪐
    "\u00f0\u009f\u0093\u0096": "\U0001F4D6",  # 📖
    "\u00f0\u009f\u0093\u009a": "\U0001F4DA",  # 📚
    "\u00f0\u009f\u0093\u0093": "\U0001F4D3",  # 📓
    "\u00f0\u009f\u0093\u008b": "\U0001F4CB",  # 📋
    "\u00f0\u009f\u0093\u008c": "\U0001F4CC",  # 📌
    "\u00f0\u009f\u009a\u00a6": "\U0001F6A6",  # 🚦
    "\u00f0\u009f\u00a7\u00ac": "\U0001F9EC",  # 🧬
    "\u00f0\u009f\u00a7\u00a0": "\U0001F9E0",  # 🧠
    "\u00f0\u009f\u00a7\u00a9": "\U0001F9E9",  # 🧩
    "\u00f0\u009f\u0083\u008f": "\U0001F0CF",  # 🃏
    "\u00f0\u009f\u0087\u00ac": "\U0001F1EC",  # part of flag
    "\u00f0\u009f\u0087\u00a7": "\U0001F1E7",  # part of flag
    "\u00f0\u009f\u0097\u00a3": "\U0001F5E3",  # 🗣
    "\u00f0\u009f\u008f\u00b9": "\U0001F3F9",  # 🏹
    "\u00f0\u009f\u008f\u009b": "\U0001F3DB",  # 🏛
    "\u00f0\u009f\u008e\u00b2": "\U0001F3B2",  # 🎲
    "\u00f0\u009f\u008e\u00af": "\U0001F3AF",  # 🎯
    "\u00f0\u009f\u0094\u00ad": "\U0001F52D",  # 🔭
    "\u00f0\u009f\u0093\u009d": "\U0001F4DD",  # 📝
}

for old, new in sorted(emoji_map.items(), key=lambda x: -len(x[0])):
    if old in text:
        c = text.count(old)
        text = text.replace(old, new)
        count += c

# Also fix remaining misc
misc_fixes = {
    "\u00e2\u0095\u0090": "\u2550",  # ═
    "\u00e2\u0080\u0094": "\u2014",  # —
    "\u00e2\u0080\u0093": "\u2013",  # –
    "\u00e2\u0086\u0092": "\u2192",  # →
    "\u00c2\u00b2": "\u00b2",  # ²
    "\u00c2\u00b3": "\u00b3",  # ³
    "\u00c2\u00b0": "\u00b0",  # °
    "\u00e2\u0082\u0099": "\u2099",  # ₙ
    "\u00ce\u0094": "\u0394",  # Δ
}
for old, new in misc_fixes.items():
    if old in text:
        c = text.count(old)
        text = text.replace(old, new)
        count += c

with open(target, "wb") as f:
    f.write(b'\xef\xbb\xbf')
    f.write(text.encode("utf-8"))

print(f"Fixed {count} remaining sequences")

# Final verify 
with open(target, "r", encoding="utf-8-sig") as f:
    check = f.read()
tests = ["Toán Tiểu Học", "Bảng Cửu Chương", "Hình Học", "Đổi Đơn Vị", "Ý Tưởng"]
found = [w for w in tests if w in check]
print(f"Final verify: {len(found)}/{len(tests)}: {found}")
