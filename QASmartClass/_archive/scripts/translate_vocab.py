import json
import time
import sys
from deep_translator import GoogleTranslator
from concurrent.futures import ThreadPoolExecutor, as_completed

def translate_mock(text, word):
    if "This is an example sentence using the word" in text:
        return f"Đây là một câu ví dụ sử dụng từ '{word}' trong giao tiếp thực tế."
    elif "It is important to understand how" in text:
        return f"Việc hiểu cách từ '{word}' hoạt động trong ngữ cảnh trang trọng là rất quan trọng."
    return None

def main():
    print("Loading JSON...")
    with open('Assets/Data/Language/VocabularyData.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    # Collect tasks
    translator = GoogleTranslator(source='en', target='vi')
    
    # We will modify the data in place.
    # To do this safely with threads, we'll collect references to the strings that need translation.
    # items_to_translate: list of (dict_obj, key, original_text, word_en)
    items_to_translate = []
    
    for cat in data:
        for w in cat['Words']:
            word = w['En']
            # ExEn
            if w.get('ExEn'):
                ex = w['ExEn']
                if "\n(" not in ex: # Avoid double translation
                    mock = translate_mock(ex, word)
                    if mock:
                        w['ExEn'] = ex + f"\n({mock})"
                    else:
                        items_to_translate.append((w, 'ExEn', ex, word))
            
            # ContextualExamples
            if w.get('ContextualExamples'):
                for k, v in w['ContextualExamples'].items():
                    if "\n(" not in v:
                        mock = translate_mock(v, word)
                        if mock:
                            w['ContextualExamples'][k] = v + f"\n({mock})"
                        else:
                            items_to_translate.append((w['ContextualExamples'], k, v, word))

    print(f"Found {len(items_to_translate)} items to translate via Google Translate.")
    
    if len(items_to_translate) == 0:
        print("Nothing to translate.")
        return

    # Translate function
    def do_translate(item):
        obj, key, original, word = item
        try:
            vi = translator.translate(original)
            return (item, vi, None)
        except Exception as e:
            return (item, None, str(e))

    # Using ThreadPool
    success = 0
    failed = 0
    start_time = time.time()
    
    with ThreadPoolExecutor(max_workers=10) as executor:
        futures = {executor.submit(do_translate, item): item for item in items_to_translate}
        for i, future in enumerate(as_completed(futures)):
            item, vi, err = future.result()
            if vi:
                obj, key, original, word = item
                obj[key] = original + f"\n({vi})"
                success += 1
            else:
                failed += 1
            
            if (i + 1) % 100 == 0:
                print(f"Translated {i + 1}/{len(items_to_translate)}... (Success: {success}, Failed: {failed})")

    print(f"Finished! Success: {success}, Failed: {failed}")
    print(f"Time taken: {time.time() - start_time:.2f}s")
    
    # Save back
    print("Saving to JSON...")
    with open('Assets/Data/Language/VocabularyData.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)
    print("Saved successfully.")

if __name__ == '__main__':
    main()
