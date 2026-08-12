import json
import random
from pathlib import Path

# Read the full books.json
input_file = Path(r"d:\JOB\QA SmartSchool\QA SmartScreen v1.0\QASmartScreen\Modules\InteractiveBooks\Data\books_full.json")
output_file = Path(r"d:\JOB\QA SmartSchool\QA SmartScreen v1.0\QASmartScreen\Modules\InteractiveBooks\Data\books.json")

with open(input_file, 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

all_books = data['books']
print(f"Total books in source: {len(all_books)}")

# Select representative books
# Strategy: Pick 4-5 books per grade (1-12), mix of SGK/SGV/SBT, different subjects
selected_books = []

for grade in range(1, 13):
    grade_books = [b for b in all_books if b['grade'] == grade]
    
    # Get diverse selection
    sgk_books = [b for b in grade_books if b['bookType'] == 'SGK']
    sgv_books = [b for b in grade_books if b['bookType'] == 'SGV']
    sbt_books = [b for b in grade_books if b['bookType'] == 'SBT']
    
    # Pick 2 SGK, 1 SGV, 1 SBT per grade
    if sgk_books:
        selected_books.extend(random.sample(sgk_books, min(2, len(sgk_books))))
    if sgv_books:
        selected_books.extend(random.sample(sgv_books, min(1, len(sgv_books))))
    if sbt_books:
        selected_books.extend(random.sample(sbt_books, min(1, len(sbt_books))))

print(f"Selected books: {len(selected_books)}")

# Update IDs to be sequential
for i, book in enumerate(selected_books, 1):
    book['id'] = f"BOOK_{i:03d}"
    # Add icon based on subject
    subject_icons = {
        'Toan hoc': '📐',
        'Ngu van': '📚',
        'Tieng Anh': '🇬🇧',
        'Khoa hoc tu nhien': '🔬',
        'Lich su': '📜',
        'Dia ly': '🗺️',
        'Vat ly': '⚡',
        'Hoa hoc': '🧪',
        'Sinh hoc': '🧬',
        'Tin hoc': '💻',
        'Cong nghe': '⚙️',
        'Giao duc cong dan': '⚖️',
        'Am nhac': '🎵',
        'My thuat': '🎨',
        'The duc': '⚽'
    }
    book['icon'] = subject_icons.get(book['subject'], '📖')
    book['coverImage'] = '/Assets/book_placeholder.png'

# Create output data
output_data = {
    'books': selected_books,
    'metadata': {
        'version': '1.0',
        'totalBooks': len(selected_books),
        'lastUpdated': '2025-12-08T20:30:00'
    }
}

# Write to output file
with open(output_file, 'w', encoding='utf-8') as f:
    json.dump(output_data, f, ensure_ascii=False, indent=4)

print(f"[OK] Created books.json with {len(selected_books)} books")
print(f"Output: {output_file}")
