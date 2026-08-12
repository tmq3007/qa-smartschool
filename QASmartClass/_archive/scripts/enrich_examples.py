import json
import time
import requests
import nltk
from nltk.corpus import wordnet as wn
from deep_translator import GoogleTranslator
from concurrent.futures import ThreadPoolExecutor, as_completed

def get_examples_api(word):
    try:
        r = requests.get(f'https://api.dictionaryapi.dev/api/v2/entries/en/{word}', timeout=5)
        if r.status_code == 200:
            examples = []
            data = r.json()
            for entry in data:
                for meaning in entry.get('meanings', []):
                    for definition in meaning.get('definitions', []):
                        if 'example' in definition:
                            # Clean up example (e.g. capitalize first letter)
                            ex = definition['example']
                            if ex:
                                ex = ex[0].upper() + ex[1:]
                                if ex not in examples:
                                    examples.append(ex)
            return examples
    except Exception as e:
        print(f"API Error for {word}: {e}")
    return []

def get_examples_wordnet(word):
    examples = []
    try:
        for syn in wn.synsets(word):
            for ex in syn.examples():
                if ex:
                    ex = ex[0].upper() + ex[1:]
                    if ex not in examples:
                        examples.append(ex)
    except:
        pass
    return examples

def get_real_examples(word):
    # Try Dictionary API (Solution B)
    exs = get_examples_api(word)
    if len(exs) < 2:
        # Fallback to WordNet
        exs += get_examples_wordnet(word)
        # Remove duplicates
        exs = list(dict.fromkeys(exs))
        
    if len(exs) == 0:
        return [f"The word {word} is used in English.", f"Here is another sentence for {word}."]
    elif len(exs) == 1:
        return [exs[0], exs[0]]
    else:
        return exs[:2]

def is_mock(text):
    return "This is an example" in text or "It is important" in text or "Đây là một câu ví dụ" in text or "Việc hiểu cách từ" in text

def main():
    print("Loading JSON...")
    with open('Assets/Data/Language/VocabularyData.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    # Collect items that need enriching
    words_to_process = []
    
    for cat in data:
        for w in cat['Words']:
            needs_update = False
            if w.get('ExEn') and is_mock(w['ExEn']):
                needs_update = True
            if w.get('ContextualExamples'):
                for v in w['ContextualExamples'].values():
                    if is_mock(v):
                        needs_update = True
                        break
            if needs_update:
                words_to_process.append(w)

    print(f"Found {len(words_to_process)} words with mock examples.")
    if len(words_to_process) == 0:
        return

    def process_word(w):
        word_en = w['En']
        real_exs = get_real_examples(word_en)
        
        translator = GoogleTranslator(source='en', target='vi')
        
        # Translate
        try:
            vi_exs = [translator.translate(ex) for ex in real_exs]
        except:
            vi_exs = ["", ""]
            
        final_exs = []
        for en, vi in zip(real_exs, vi_exs):
            if vi:
                final_exs.append(f"{en}\n({vi})")
            else:
                final_exs.append(en)
                
        # Update word object
        if w.get('ExEn') and is_mock(w['ExEn']):
            w['ExEn'] = final_exs[0]
            
        if w.get('ContextualExamples'):
            keys = list(w['ContextualExamples'].keys())
            if len(keys) > 0 and is_mock(w['ContextualExamples'][keys[0]]):
                w['ContextualExamples'][keys[0]] = final_exs[0]
            if len(keys) > 1 and is_mock(w['ContextualExamples'][keys[1]]):
                w['ContextualExamples'][keys[1]] = final_exs[1]
                
        return word_en

    success = 0
    with ThreadPoolExecutor(max_workers=10) as executor:
        futures = {executor.submit(process_word, w): w for w in words_to_process}
        for i, future in enumerate(as_completed(futures)):
            try:
                word_en = future.result()
                success += 1
                if (i+1) % 20 == 0:
                    print(f"Processed {i+1}/{len(words_to_process)} words.")
            except Exception as e:
                print("Error processing word:", e)

    print(f"Finished processing {success} words.")
    
    print("Saving to JSON...")
    with open('Assets/Data/Language/VocabularyData.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)
    print("Saved successfully.")

if __name__ == '__main__':
    main()
