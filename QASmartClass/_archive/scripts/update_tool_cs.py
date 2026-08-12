import os

tools_dir = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Workplace'

def replace_template_click(filepath, new_code):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    start_idx = content.find("private void Template_Click")
    if start_idx == -1:
        return
        
    brace_count = 0
    in_method = False
    end_idx = -1
    
    for i in range(start_idx, len(content)):
        if content[i] == '{':
            if not in_method:
                in_method = True
            brace_count += 1
        elif content[i] == '}':
            brace_count -= 1
            if in_method and brace_count == 0:
                end_idx = i + 1
                break
                
    if end_idx != -1:
        updated_content = content[:start_idx] + new_code + content[end_idx:]
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(updated_content)

# FiveWhy
code_five_why = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("five_why");

            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.FiveWhyData)t.Data;
                item.Click += (_, _) => ApplyTemplate(data.Problem, new[] { data.Why1, data.Why2, data.Why3, data.Why4, data.Why5 }, data.How);
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# Fishbone
code_fishbone = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("fishbone");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.FishboneData)t.Data;
                item.Click += (_, _) => ApplyTemplate(data.MainProblem, data.Groups);
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# Pareto
code_pareto = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("pareto");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.ParetoData)t.Data;
                item.Click += (_, _) =>
                {
                    _items.Clear();
                    foreach (var i in data.Items)
                        _items.Add(new ParetoItem { Category = i.Name, Value = i.Value });
                    SortAndDraw();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# Pdca
code_pdca = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("pdca");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.PdcaData)t.Data;
                item.Click += (_, _) =>
                {
                    Clear_Click(null, null);
                    txtPlan.Text = data.Plan;
                    txtDo.Text = data.Do;
                    txtCheck.Text = data.Check;
                    txtAct.Text = data.Act;
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# Swot
code_swot = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("swot");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.SwotData)t.Data;
                item.Click += (_, _) =>
                {
                    ClearAll();
                    foreach (var s in data.S) AddItem(panelS, s);
                    foreach (var w in data.W) AddItem(panelW, w);
                    foreach (var o in data.O) AddItem(panelO, o);
                    foreach (var t1 in data.T) AddItem(panelT, t1);
                    GenerateStrategy();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# Eisenhower
code_eisenhower = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("eisenhower");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.EisenhowerData)t.Data;
                item.Click += (_, _) =>
                {
                    ClearAll();
                    foreach (var ui in data.UI) CreateCard(ui, panelUI);
                    foreach (var nui in data.NUI) CreateCard(nui, panelNUI);
                    foreach (var uni in data.UNI) CreateCard(uni, panelUNI);
                    foreach (var nuni in data.NUNI) CreateCard(nuni, panelNUNI);
                    UpdateCounters();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# FiveS
code_fives = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("five_s");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.FiveSData)t.Data;
                item.Click += (_, _) =>
                {
                    txtSubject.Text = data.Subject;
                    ResetChecklist();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# Kanban
code_kanban = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("kanban");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.KanbanData)t.Data;
                item.Click += (_, _) =>
                {
                    ClearBoard();
                    foreach (var todo in data.Todo) AddCard(panelTodo, todo.Title, todo.Desc);
                    foreach (var doing in data.Doing) AddCard(panelDoing, doing.Title, doing.Desc);
                    foreach (var done in data.Done) AddCard(panelDone, done.Title, done.Desc);
                    UpdateCounters();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

# KpiOkr
code_kpiokr = """private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("kpi_okr");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.KpiOkrData)t.Data;
                item.Click += (_, _) =>
                {
                    _kpis.Clear();
                    txtOkrObjective.Text = data.Obj;
                    foreach (var d in data.Data)
                    {
                        if (double.TryParse(d.Tgt, out double tgt) && double.TryParse(d.Act, out double act))
                            _kpis.Add(new KpiItem { Name = d.Name, TargetValue = tgt, ActualValue = act });
                        else
                            _kpis.Add(new KpiItem { Name = d.Name, TargetValue = 100, ActualValue = 0 });
                    }
                    if (_kpis.Count == 0) AddEmptyKpi();
                    DrawDashboard();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }"""

replace_template_click(os.path.join(tools_dir, "FiveWhyTool.xaml.cs"), code_five_why)
replace_template_click(os.path.join(tools_dir, "FishboneTool.xaml.cs"), code_fishbone)
replace_template_click(os.path.join(tools_dir, "ParetoTool.xaml.cs"), code_pareto)
replace_template_click(os.path.join(tools_dir, "PdcaTool.xaml.cs"), code_pdca)
replace_template_click(os.path.join(tools_dir, "SwotTool.xaml.cs"), code_swot)
replace_template_click(os.path.join(tools_dir, "EisenhowerTool.xaml.cs"), code_eisenhower)
replace_template_click(os.path.join(tools_dir, "FiveSTool.xaml.cs"), code_fives)
replace_template_click(os.path.join(tools_dir, "KanbanTool.xaml.cs"), code_kanban)
replace_template_click(os.path.join(tools_dir, "KpiOkrTool.xaml.cs"), code_kpiokr)

print("Updated 9 tools with new grouped ContextMenu templates!")
