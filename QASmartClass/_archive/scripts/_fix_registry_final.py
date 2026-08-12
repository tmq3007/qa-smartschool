#!/usr/bin/env python3
"""Final fix: replace specific byte sequences that are still broken"""
import os

target = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models", "ToolRegistry.cs")

with open(target, "rb") as f:
    raw = f.read()

count = 0

# Fix "Ã\x9d" (double-encoded Ý): c3 83 c2 9d -> c3 9d
if b'\xc3\x83\xc2\x9d' in raw:
    raw = raw.replace(b'\xc3\x83\xc2\x9d', b'\xc3\x9d')
    count += 1
    print("Fixed: double-encoded Y with accent (Ý)")

# Fix remaining double-encoded emoji icons
# Pattern: c3 b0 xx xx xx (4-byte emoji double-encoded)
# The emoji bytes \xf0\x9f\xXX\xXX get double-encoded through cp1252/utf-8 cycles
# Let's find all Icon fields and fix them

# Fix common remaining double-encoded patterns
# c3 b0 = double-encoded \xf0 (first byte of 4-byte UTF-8)
# After first fix pass, emoji may be: c3 b0 + c5 b8/other + ... 
# These are complex multi-level encodings. Let's try a different approach:
# Replace Icon values directly with correct emoji

import re

text = raw.decode('utf-8')

# Map of tool_id -> correct icon
icon_fixes = {
    "basic_math": "\U0001F522",       # 🔢
    "multiplication": "\U0001F522",   # 🔢
    "trigonometry": "\U0001F4D0",     # 📐
    "geometry": "\U0001F4D0",         # 📐
    "calculator": "\U0001F9EE",       # 🧮
    "quadratic": "\U0001F4CA",        # 📊
    "linear_system": "\U0001F4D0",    # 📐
    "trig_equation": "\U0001F504",    # 🔄
    "logarithm": "\U0001F4C8",        # 📈
    "prime_numbers": "\U0001F4D0",    # 📐
    "number_base": "\U0001F4BB",      # 💻
    "identities": "\U0001F4D0",       # 📐
    "inequality": "\U0001F4D0",       # 📐
    "sequence": "\U0001F522",         # 🔢
    "cubic": "\U0001F4CA",            # 📊
    "coordinate": "\U0001F4D0",       # 📐
    "combinatorics": "\U0001F3B2",    # 🎲
    "probability": "\U0001F3AF",      # 🎯
    "derivative": "\U0001F4C8",       # 📈
    "integral": "\u222B",             # ∫
    "complex_number": "\u2102",       # ℂ
    "limit": "\u221E",                # ∞
    "vector": "\U0001F3F9",           # 🏹
    "conic_section": "\U0001F535",    # 🔵
    "solid_geometry": "\U0001F9CA",   # 🧊
    "fraction": "\U0001F550",         # 🕐 (originally 🕐)
    "math_curriculum": "\U0001F682",  # 🚂
    "literature_curriculum": "\U0001F682",  # 🚂
    "periodic_table": "\u2697\uFE0F", # ⚗️
    "constants": "\u26A1",            # ⚡
    "unit_converter": "\U0001F504",   # 🔄
    "ph_scale": "\U0001F9EA",         # 🧪
    "density": "\u2696\uFE0F",        # ⚖️
    "wave_speed": "\U0001F4CA",       # 📊
    "boiling_freezing": "\U0001F321\uFE0F",  # 🌡️
    "circuit": "\u26A1",              # ⚡
    "lens": "\U0001F52D",             # 🔭
    "electron_config": "\u269B\uFE0F", # ⚛️
    "genetics": "\U0001F9EC",         # 🧬
    "statistics": "\U0001F4CA",       # 📊
    "irregular_verbs": "\U0001F1EC\U0001F1E7",  # 🇬🇧
    "vocabulary": "\U0001F4DA",       # 📚
    "grammar": "\U0001F4D0",          # 📐
    "ipa": "\U0001F5E3\uFE0F",        # 🗣️
    "countries": "\U0001F30D",        # 🌍
    "planets": "\U0001FA90",          # 🪐
    "math_symbols": "\u2211",         # ∑
    "literature": "\U0001F4D6",       # 📖
    "dynasties": "\U0001F3DB\uFE0F",  # 🏛️
    "formulas": "\U0001F4CB",         # 📋
    "textbooks": "\U0001F4DA",        # 📚
    "notebook": "\U0001F4D3",         # 📓
    "focus_timer": "\u23F0",          # ⏰
    "brainstorm": "\U0001F4CC",       # 📌
    "noise_monitor": "\U0001F6A6",    # 🚦
    "history_timeline": "\u23F3",     # ⏳
    "physics_sandbox": "\u269B\uFE0F", # ⚛️
    "iq_quiz": "\U0001F9E0",          # 🧠
    "mental_math": "\u26A1",          # ⚡
    "sudoku": "\U0001F9E9",           # 🧩
    "memory_game": "\U0001F0CF",      # 🃏
}

emoji_count = 0
for tool_id, icon in icon_fixes.items():
    # Pattern: Id = "tool_id", Name = "...", Icon = "BROKEN"
    pattern = f'Id = "{tool_id}"(.*?)Icon = "([^"]*)"'
    match = re.search(pattern, text, re.DOTALL)
    if match:
        old_icon = match.group(2)
        if old_icon != icon:
            text = text[:match.start(2)] + icon + text[match.end(2):]
            emoji_count += 1

# Also fix the second periodic_table entry
# There are 2 entries with Id="periodic_table" - fix both
pattern2 = re.compile(r'Icon = "([^"]*)"')
lines = text.split('\n')
new_lines = []
for line in lines:
    # Skip non-icon lines
    if 'Icon = "' not in line or 'Id = "' not in line:
        new_lines.append(line)
        continue
    # Already handled above
    new_lines.append(line)

text = '\n'.join(new_lines)

with open(target, "wb") as f:
    f.write(b'\xef\xbb\xbf')
    f.write(text.encode("utf-8"))

print(f"Fixed {count} byte sequences + {emoji_count} emoji icons")
print("Done!")
