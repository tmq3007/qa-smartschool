import os

filepath = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Workplace\FiveSTool.xaml'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# The original block at the bottom
orig_resource = """    <UserControl.Resources>
        <Style x:Key="TabButtonStyle" TargetType="Button">
            <Setter Property="Background" Value="#F5F5F5"/>
            <Setter Property="Foreground" Value="#757575"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="BorderBrush" Value="#E0E0E0"/>
            <Setter Property="Padding" Value="12,8"/>
            <Setter Property="Margin" Value="0,0,4,0"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}" 
                                BorderBrush="{TemplateBinding BorderBrush}" 
                                BorderThickness="{TemplateBinding BorderThickness}" 
                                CornerRadius="4,4,0,0" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#E0E0E0"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </UserControl.Resources>"""

# The one I added at the top
my_resource = """    <UserControl.Resources>
        <Style x:Key="TabButtonStyle" TargetType="Button">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="BorderThickness" Value="0,0,0,3"/>
            <Setter Property="BorderBrush" Value="Transparent"/>
            <Setter Property="Foreground" Value="#757575"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Padding" Value="12,8"/>
            <Setter Property="Margin" Value="0,0,8,0"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Foreground" Value="#00796B"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </UserControl.Resources>"""

# Replace the bottom one with empty string
if orig_resource in content:
    content = content.replace(orig_resource, "")

# Replace the top one with the original one
if my_resource in content:
    content = content.replace(my_resource, orig_resource)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print("Fixed FiveSTool.xaml")
