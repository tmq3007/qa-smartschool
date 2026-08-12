import json

def main():
    print("Loading JSON...")
    with open('Assets/Data/Language/VocabularyData.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    dup_contexts_removed = 0
    mock_collocations_removed = 0

    for cat in data:
        for w in cat['Words']:
            word = w['En'].lower()
            
            # 1. Remove duplicate contextual examples
            if w.get('ContextualExamples'):
                keys = list(w['ContextualExamples'].keys())
                seen_values = set()
                keys_to_delete = []
                for k in keys:
                    v = w['ContextualExamples'][k]
                    if v in seen_values:
                        keys_to_delete.append(k)
                    else:
                        seen_values.add(v)
                
                for k in keys_to_delete:
                    del w['ContextualExamples'][k]
                    dup_contexts_removed += 1
                    
                # If ContextualExamples is empty, delete it
                if len(w['ContextualExamples']) == 0:
                    del w['ContextualExamples']

            # 2. Remove mock collocations
            if w.get('Collocations'):
                valid_collocs = []
                for c in w['Collocations']:
                    c_lower = c.lower()
                    if c_lower == f"a beautiful {word}" or c_lower == f"the new {word}" or c_lower == f"my favorite {word}":
                        mock_collocations_removed += 1
                    elif c_lower == f"say {word}" and word in ["hello", "good morning", "goodbye"]:
                        # Keep valid ones
                        valid_collocs.append(c)
                    else:
                        # Other generic mocks to catch?
                        valid_collocs.append(c)
                w['Collocations'] = valid_collocs

    print(f"Removed {dup_contexts_removed} duplicate contextual examples.")
    print(f"Removed {mock_collocations_removed} mock collocations.")

    print("Saving to JSON...")
    with open('Assets/Data/Language/VocabularyData.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)
    print("Saved successfully.")

if __name__ == '__main__':
    main()
