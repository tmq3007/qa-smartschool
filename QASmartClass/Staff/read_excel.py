import pandas as pd
import json
import os

file_path = r"D:\JOB\QA SmartSchool\Báo cáo kết quả test QA ver 1.1.xlsx"

try:
    xl = pd.ExcelFile(file_path)
    output = []
    for sheet_name in xl.sheet_names:
        df = pd.read_excel(file_path, sheet_name=sheet_name)
        sheet_info = {
            "sheet_name": sheet_name,
            "columns": [str(c) for c in df.columns],
            "rows_count": len(df),
            "head": df.head(10).fillna("").astype(str).to_dict(orient="records")
        }
        output.append(sheet_info)
        
    with open("excel_output_utf8.json", "w", encoding="utf-8") as f:
        json.dump(output, f, ensure_ascii=False, indent=2)
except Exception as e:
    with open("excel_output_utf8.json", "w", encoding="utf-8") as f:
        f.write(str(e))
