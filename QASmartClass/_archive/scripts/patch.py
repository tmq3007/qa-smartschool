import os
import re

files_to_patch = {
    "Science/DensityTool.xaml.cs": ["txtMass", "txtVolume", "txtDensity"],
    "Science/PhScaleTool.xaml.cs": ["txtHConc"],
    "Science/LensTool.xaml.cs": ["txtF", "txtD", "txtDp"],
    "Science/WaveSpeedTool.xaml.cs": ["txtFreq", "txtWavelength", "txtVelocity", "txtSourceSpeed", "txtObserverSpeed"],
    "Science/UnitConverterTool.xaml.cs": ["txtFromValue"],
    "Science/BoilingFreezingTool.xaml.cs": ["txtC", "txtF", "txtK"],
    "Science/ElectronConfigTool.xaml.cs": ["txtZ"],
    "Science/CircuitTool.xaml.cs": ["txtU", "txtI", "txtR", "txtUSource", "txtResistors"],
    "Thinking/MentalMathTool.xaml.cs": ["txtAnswer"],
    "Math/MultiplicationTool.xaml.cs": ["txtQuizAnswer"]
}

base_dir = "d:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/LearningTools/Views"

for rel_path, textboxes in files_to_patch.items():
    file_path = os.path.join(base_dir, rel_path)
    if not os.path.exists(file_path):
        continue
    
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    if "TouchNumPad.Attach" in content:
        print(f"Skipping {rel_path}, already has TouchNumPad")
        continue

    # Look for constructor
    class_name = rel_path.split("/")[-1].replace(".xaml.cs", "")
    ctor_pattern = re.compile(r"(public\s+" + class_name + r"\s*\(\)\s*\{[^{}]*InitializeComponent\(\);)", re.DOTALL)
    
    match = ctor_pattern.search(content)
    if not match:
        print(f"Could not find constructor in {rel_path}")
        continue
        
    attach_lines = "\n            // Attach TouchNumPad\n"
    for tb in textboxes:
        if tb == "txtZ" or tb == "txtAnswer" or tb == "txtQuizAnswer":
            attach_lines += f"            QASmartClass.LearningTools.Controls.TouchNumPad.Attach({tb}, step: 1, allowDecimal: false);\n"
        else:
            attach_lines += f"            QASmartClass.LearningTools.Controls.TouchNumPad.Attach({tb}, step: 1);\n"
            
    # Insert after InitializeComponent();
    insert_pos = match.end()
    
    # Check if there is a Loaded event already right after
    # We just put it right after InitializeComponent();
    
    new_content = content[:insert_pos] + attach_lines + content[insert_pos:]
    
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(new_content)
        
    print(f"Patched {rel_path}")
