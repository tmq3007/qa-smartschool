import sys

# Modify StemToolsPage.xaml.cs
cs_path = 'D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs'
with open(cs_path, 'r', encoding='utf-8') as f:
    cs_code = f.read()

old_btn = "Content = label, FontSize = 20, Height = 64,"
new_btn = "Content = label, FontSize = 18, Height = 48,"
cs_code = cs_code.replace(old_btn, new_btn)

with open(cs_path, 'w', encoding='utf-8') as f:
    f.write(cs_code)

# Modify StemToolsPage.xaml
xaml_path = 'D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/Views/StemToolsPage.xaml'
with open(xaml_path, 'r', encoding='utf-8') as f:
    xaml_code = f.read()

old_border = '<Border Background="White" CornerRadius="0,12,12,12" BorderBrush="#D0D5DD" BorderThickness="1" Padding="40">'
new_border = '<Border Background="White" CornerRadius="0,12,12,12" BorderBrush="#D0D5DD" BorderThickness="1" Padding="16">'
xaml_code = xaml_code.replace(old_border, new_border)

# Add ScrollViewer around Grid inside Tab 1
old_grid = '''<Border Background="White" CornerRadius="0,12,12,12" BorderBrush="#D0D5DD" BorderThickness="1" Padding="16">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="650"/>'''
new_grid = '''<Border Background="White" CornerRadius="0,12,12,12" BorderBrush="#D0D5DD" BorderThickness="1" Padding="16">
                            <ScrollViewer VerticalScrollBarVisibility="Auto">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="650"/>'''
xaml_code = xaml_code.replace(old_grid, new_grid)

# We also need to close the ScrollViewer.
# TabItem closes after </Border>
# So we find the first TabItem closing tag after this border.
# Or just replace the exact structure where Grid ends.
# Let's see how the Grid ends. It ends right before </Border>\n</TabItem>
# Wait, let's just do a string replace for the specific Grid close.

old_end = '''                                </Grid>
                        </Border>
                    </TabItem>

                    <!-- TAB 2: ĐỔI ĐƠN VỊ -->'''

new_end = '''                                </Grid>
                            </ScrollViewer>
                        </Border>
                    </TabItem>

                    <!-- TAB 2: ĐỔI ĐƠN VỊ -->'''

xaml_code = xaml_code.replace(old_end, new_end)

with open(xaml_path, 'w', encoding='utf-8') as f:
    f.write(xaml_code)
