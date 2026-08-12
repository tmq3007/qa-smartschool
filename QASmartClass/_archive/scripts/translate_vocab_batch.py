import json
import time
from deep_translator import GoogleTranslator

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

    # Collect items
    # We will store references to the dict and key to update them later
    items_to_translate = []
    texts_to_translate = []
    
    for cat in data:
        for w in cat['Words']:
            word = w['En']
            # ExEn
            if w.get('ExEn'):
                ex = w['ExEn']
                if "\n(" not in ex:
                    mock = translate_mock(ex, word)
                    if mock:
                        w['ExEn'] = ex + f"\n({mock})"
                    else:
                        items_to_translate.append((w, 'ExEn', ex, word))
                        texts_to_translate.append(ex)
            
            # ContextualExamples
            if w.get('ContextualExamples'):
                for k, v in w['ContextualExamples'].items():
                    if "\n(" not in v:
                        mock = translate_mock(v, word)
                        if mock:
                            w['ContextualExamples'][k] = v + f"\n({mock})"
                        else:
                            items_to_translate.append((w['ContextualExamples'], k, v, word))
                            texts_to_translate.append(v)

    print(f"Found {len(texts_to_translate)} items to translate via Google Translate.")
    if len(texts_to_translate) == 0:
        return

    translator = GoogleTranslator(source='en', target='vi')
    batch_size = 50
    success = 0
    
    for i in range(0, len(texts_to_translate), batch_size):
        batch_texts = texts_to_translate[i:i+batch_size]
        batch_items = items_to_translate[i:i+batch_size]
        try:
            translations = translator.translate_batch(batch_texts)
            for j, vi in enumerate(translations):
                if vi:
                    obj, key, original, word = batch_items[j]
                    obj[key] = original + f"\n({vi})"
                    success += 1
            print(f"Translated batch {i//batch_size + 1}/{(len(texts_to_translate)//batch_size)+1} (Total success: {success})")
            time.sleep(1) # Be nice to Google
        except Exception as e:
            print(f"Batch {i//batch_size + 1} failed: {e}")
            time.sleep(5)
            # Retry one by one if batch fails
            for j, text in enumerate(batch_texts):
                try:
                    vi = translator.translate(text)
                    if vi:
                        obj, key, original, word = batch_items[j]
                        obj[key] = original + f"\n({vi})"
                        success += 1
                except:
                    pass

    print("Saving to JSON...")
    with open('Assets/Data/Language/VocabularyData.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)
    print("Saved successfully.")

if __name__ == '__main__':
    main()
