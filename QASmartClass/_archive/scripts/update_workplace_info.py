import os
import re

d = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Workplace'

tool_ids = {
    "FiveWhyTool": "five_why",
    "FishboneTool": "fishbone",
    "ParetoTool": "pareto",
    "PdcaTool": "pdca",
    "SwotTool": "swot",
    "EisenhowerTool": "eisenhower",
    "FiveSTool": "five_s",
    "KanbanTool": "kanban",
    "KpiOkrTool": "kpi_okr"
}

xaml_pattern = re.compile(r'<Expander([^>]+)>\s*<TextBlock TextWrapping="Wrap" Padding="12" FontSize="12" FontWeight="Normal" Foreground="#424242" Background="White">(.*?)</TextBlock>\s*</Expander>', re.DOTALL)

for tool_name, tool_id in tool_ids.items():
    xaml_path = os.path.join(d, f"{tool_name}.xaml")
    cs_path = os.path.join(d, f"{tool_name}.xaml.cs")
    
    # 1. Update XAML
    if os.path.exists(xaml_path):
        with open(xaml_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        replacement = r'''<Expander\1>
                    <StackPanel Background="White" Padding="12">
                        <Border Background="#E3F2FD" CornerRadius="6" Padding="10" Margin="0,0,0,10" BorderBrush="#BBDEFB" BorderThickness="1">
                            <StackPanel>
                                <TextBlock Text="💡 NGUỒN GỐC &amp; Ý NGHĨA" FontWeight="Bold" Foreground="#1565C0" Margin="0,0,0,4"/>
                                <TextBlock x:Name="txtToolMeaning" TextWrapping="Wrap" FontSize="12" Foreground="#424242" Margin="0,0,0,8"/>
                                <TextBlock Text="🌍 ÁP DỤNG THỰC TẾ" FontWeight="Bold" Foreground="#E65100" Margin="0,0,0,4"/>
                                <TextBlock x:Name="txtToolApplication" TextWrapping="Wrap" FontSize="12" Foreground="#424242"/>
                            </StackPanel>
                        </Border>
                        <TextBlock TextWrapping="Wrap" FontSize="12" FontWeight="Normal" Foreground="#424242">\2</TextBlock>
                    </StackPanel>
                </Expander>'''
        
        new_content = xaml_pattern.sub(replacement, content)
        if new_content != content:
            with open(xaml_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print(f"Updated XAML: {tool_name}")
            
    # 2. Update C#
    if os.path.exists(cs_path):
        with open(cs_path, 'r', encoding='utf-8') as f:
            cs_content = f.read()
            
        if "txtToolMeaning.Text" not in cs_content:
            inject_code = f'''InitializeComponent();
            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("{tool_id}");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;'''
            
            new_cs = cs_content.replace("InitializeComponent();", inject_code)
            with open(cs_path, 'w', encoding='utf-8') as f:
                f.write(new_cs)
            print(f"Updated CS: {tool_name}")
