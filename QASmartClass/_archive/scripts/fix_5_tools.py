import os

d = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Workplace'

def replace_in_file(filename, old_str, new_str):
    p = os.path.join(d, filename)
    with open(p, 'r', encoding='utf-8') as f:
        content = f.read()
    if old_str in content:
        content = content.replace(old_str, new_str)
        with open(p, 'w', encoding='utf-8') as f:
            f.write(content)
        print("Fixed", filename)

# 1. KanbanTool
kanban_old = """ClearBoard();
                    foreach (var todo in data.Todo) AddCard(panelTodo, todo.Title, todo.Desc);
                    foreach (var doing in data.Doing) AddCard(panelDoing, doing.Title, doing.Desc);
                    foreach (var done in data.Done) AddCard(panelDone, done.Title, done.Desc);
                    UpdateCounters();"""
kanban_new = """Clear_Click(null, null);
                    foreach (var todo in data.Todo) AddCard(panelTodo, todo.Title, todo.Desc);
                    foreach (var doing in data.Doing) AddCard(panelDoing, doing.Title, doing.Desc);
                    foreach (var done in data.Done) AddCard(panelDone, done.Title, done.Desc);
                    UpdateCounts();"""
replace_in_file("KanbanTool.xaml.cs", kanban_old, kanban_new)

# 2. EisenhowerTool
eis_old = """ClearAll();
                    foreach (var ui in data.UI) CreateCard(ui, panelUI);
                    foreach (var nui in data.NUI) CreateCard(nui, panelNUI);
                    foreach (var uni in data.UNI) CreateCard(uni, panelUNI);
                    foreach (var nuni in data.NUNI) CreateCard(nuni, panelNUNI);
                    UpdateCounters();"""
eis_new = """ApplyTemplate(data.UI, data.NUI, data.UNI, data.NUNI);"""
replace_in_file("EisenhowerTool.xaml.cs", eis_old, eis_new)

# 3. FishboneTool
fish_old = """ApplyTemplate(data.MainProblem, data.Groups);"""
fish_new = """ClearAll();
                    txtProblem.Text = data.MainProblem;
                    var panels = new[] { panelCat1, panelCat2, panelCat3, panelCat4, panelCat5, panelCat6 };
                    var titles = new[] { txtCat1, txtCat2, txtCat3, txtCat4, txtCat5, txtCat6 };
                    var colors = new[] { "#1565C0", "#2E7D32", "#E65100", "#6A1B9A", "#AD1457", "#455A64" };
                    int i = 0;
                    foreach(var kvp in data.Groups) {
                        if(i < 6) {
                            titles[i].Text = kvp.Key;
                            foreach(var cause in kvp.Value) AddCause(panels[i], colors[i], cause);
                            i++;
                        }
                    }"""
replace_in_file("FishboneTool.xaml.cs", fish_old, fish_new)

# 4. FiveSTool
fives_old = """txtSubject.Text = data.Subject;
                    ResetChecklist();"""
fives_new = """Clear_Click(null, null);
                    txtSubject.Text = data.Subject;"""
replace_in_file("FiveSTool.xaml.cs", fives_old, fives_new)

# 5. KpiOkrTool
kpi_old = """_kpis.Clear();
                    txtOkrObjective.Text = data.Obj;
                    foreach (var d in data.Data)
                    {
                        if (double.TryParse(d.Tgt, out double tgt) && double.TryParse(d.Act, out double act))
                            _kpis.Add(new KpiItem { Name = d.Name, TargetValue = tgt, ActualValue = act });
                        else
                            _kpis.Add(new KpiItem { Name = d.Name, TargetValue = 100, ActualValue = 0 });
                    }
                    if (_kpis.Count == 0) AddEmptyKpi();
                    DrawDashboard();"""
kpi_new = """Clear_Click(null, null);
                    txtObjective.Text = data.Obj;
                    dataPanel.Children.Clear();
                    foreach (var d in data.Data) AddDataRow(d.Name, d.Tgt, d.Act);
                    Update_Click(null, null);"""
replace_in_file("KpiOkrTool.xaml.cs", kpi_old, kpi_new)
