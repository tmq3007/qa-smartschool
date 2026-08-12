# -*- coding: utf-8 -*-
import os
import json
import random
import glob

DATA_DIR = r"d:\JOB\QA SmartClass -062026\QASmartClass\Resources\MathData\Phase4_Data"

def load_json(filepath):
    # Try reading with utf-8-sig (UTF-8 with BOM) or utf-8
    for encoding in ['utf-8-sig', 'utf-8']:
        try:
            with open(filepath, 'r', encoding=encoding) as f:
                return json.load(f)
        except UnicodeDecodeError:
            continue
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
            return None
    return None

def save_json(filepath, data):
    try:
        # Write with utf-8-sig to preserve BOM (important for Windows WPF/Visual Studio)
        with open(filepath, 'w', encoding='utf-8-sig') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        return True
    except Exception as e:
        print(f"Error saving {filepath}: {e}")
        return False

def get_option_text(options, letter):
    idx = ord(letter) - ord('A')
    if 0 <= idx < len(options):
        opt = options[idx]
        if len(opt) > 3 and opt[1] == ')' and opt[2] == ' ':
            return opt[3:]
        return opt
    return ""

def main():
    print("=== STARTING EXAM QUESTION GENERATION ===")
    
    question_pool = {}
    chapter_files = glob.glob(os.path.join(DATA_DIR, "G[0-9][0-9]_CH*.json"))
    
    print(f"Found {len(chapter_files)} chapter files to extract questions from.")
    
    for filepath in chapter_files:
        data = load_json(filepath)
        if not data or 'metadata' not in data or 'sections' not in data:
            continue
        
        grade = data['metadata'].get('grade')
        if grade is None:
            continue
        
        grade = int(grade)
        if grade not in question_pool:
            question_pool[grade] = []
            
        for sec in data['sections']:
            for prob in sec.get('problems', []):
                if 'question' in prob and 'options' in prob and 'correctAnswer' in prob:
                    q_copy = {
                        'question': prob['question'],
                        'options': list(prob['options']),
                        'correctAnswer': prob['correctAnswer'],
                        'difficulty': prob.get('difficulty', 'Medium'),
                        'solution': prob.get('solution', ''),
                        'topic': prob.get('type', 'chuyen_de')
                    }
                    question_pool[grade].append(q_copy)

    # Print stats
    total_extracted = 0
    for grade in sorted(question_pool.keys()):
        count = len(question_pool[grade])
        print(f"Grade {grade}: {count} questions in pool.")
        total_extracted += count
    print(f"Total questions indexed: {total_extracted}")

    # 2. Process mock exams and empty exams
    exam_files = glob.glob(os.path.join(DATA_DIR, "*.json"))
    exams_processed = 0
    
    for filepath in exam_files:
        filename = os.path.basename(filepath)
        if "_CH" in filename or "tool_topic_mapping" in filename or "topic_schema" in filename:
            continue
            
        data = load_json(filepath)
        if not data or 'metadata' not in data:
            continue
            
        questions = data.get('questions', [])
        metadata = data['metadata']
        exam_id = metadata.get('examId', '')
        exam_type = metadata.get('examType', '')
        grade = metadata.get('grade', 12)
        total_q = metadata.get('totalQuestions', 20)
        
        is_empty = len(questions) == 0
        selected_questions = []
        
        if is_empty:
            print(f"Populating empty exam: {filename} (Type: {exam_type}, Grade: {grade}, Target: {total_q} questions)")
            
            pool = []
            if exam_type == "DGNL" or exam_type == "MockExam" or "THPT_Mock" in filename or "DGNL_Exam" in filename:
                g12_pool = question_pool.get(12, [])
                g11_pool = question_pool.get(11, [])
                g10_pool = question_pool.get(10, [])
                thpt_pool = g12_pool + g11_pool + g10_pool
                
                g12_target = int(total_q * 0.7)
                g11_target = int(total_q * 0.2)
                g10_target = total_q - g12_target - g11_target
                
                if len(g12_pool) >= g12_target:
                    selected_questions.extend(random.sample(g12_pool, g12_target))
                else:
                    selected_questions.extend(g12_pool)
                    
                if len(g11_pool) >= g11_target:
                    selected_questions.extend(random.sample(g11_pool, g11_target))
                else:
                    selected_questions.extend(g11_pool)
                    
                if len(g10_pool) >= g10_target:
                    selected_questions.extend(random.sample(g10_pool, g10_target))
                else:
                    selected_questions.extend(g10_pool)
                
                needed = total_q - len(selected_questions)
                if needed > 0 and len(thpt_pool) > 0:
                    remaining = [q for q in thpt_pool if q not in selected_questions]
                    if len(remaining) >= needed:
                        selected_questions.extend(random.sample(remaining, needed))
                    else:
                        selected_questions.extend(random.choices(thpt_pool, k=needed))
            elif exam_type == "TuyenSinh10":
                pool = question_pool.get(9, [])
                if len(pool) >= total_q:
                    selected_questions = random.sample(pool, total_q)
                else:
                    selected_questions = list(pool)
                    needed = total_q - len(selected_questions)
                    if len(pool) > 0:
                        selected_questions.extend(random.choices(pool, k=needed))
            else:
                pool = question_pool.get(int(grade), [])
                if len(pool) >= total_q:
                    selected_questions = random.sample(pool, total_q)
                else:
                    selected_questions = list(pool)
                    needed = total_q - len(selected_questions)
                    if len(pool) > 0:
                        selected_questions.extend(random.choices(pool, k=needed))
            
            formatted_questions = []
            for idx, src_q in enumerate(selected_questions):
                formatted_options = []
                for o_idx, opt in enumerate(src_q['options']):
                    prefix = f"{chr(65 + o_idx)}) "
                    if not (opt.startswith("A)") or opt.startswith("B)") or opt.startswith("C)") or opt.startswith("D)")):
                        formatted_options.append(prefix + opt)
                    else:
                        formatted_options.append(opt)
                
                sol = src_q['solution']
                if not sol:
                    ans = src_q['correctAnswer']
                    val = get_option_text(formatted_options, ans)
                    sol = f"Áp dụng lý thuyết bài học và biến đổi, ta có đáp án đúng là {ans}. Giá trị tương ứng: {val}. Phương pháp giải chi tiết: 1. Đọc kỹ đề bài và lập điều kiện. 2. Thực hiện phép toán để tìm kết quả. 3. Đối chiếu và chọn đáp án."
                
                formatted_questions.append({
                    "id": idx + 1,
                    "topic": src_q.get('topic', 'chuyen_de'),
                    "difficulty": src_q.get('difficulty', 'Medium'),
                    "question": src_q['question'],
                    "options": formatted_options,
                    "answer": src_q['correctAnswer'],
                    "solution": sol
                })
            
            data['questions'] = formatted_questions
            save_json(filepath, data)
            exams_processed += 1
        else:
            modified = False
            for idx, q in enumerate(questions):
                sol = q.get('solution', '')
                if not sol:
                    ans = q.get('answer', 'A')
                    options = q.get('options', [])
                    val = get_option_text(options, ans)
                    q['solution'] = f"Theo dữ kiện bài toán, ta tính toán và đối chiếu các phương án. Đáp án đúng là {ans}. Giải thích: {val if val else 'Phép biến đổi toán học thỏa mãn điều kiện đề bài.'}."
                    modified = True
            
            if modified:
                print(f"Enriched solutions for exam: {filename}")
                save_json(filepath, data)
                exams_processed += 1

    print(f"=== COMPLETED EXAM ENRICHMENT. Processed/Created: {exams_processed} files. ===")

if __name__ == "__main__":
    main()
