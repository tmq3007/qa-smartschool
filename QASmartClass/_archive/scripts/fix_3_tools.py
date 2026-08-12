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
kanban_old = """Clear_Click(null, null);
                    foreach (var todo in data.Todo) AddCard(panelTodo, todo.Title, todo.Desc);
                    foreach (var doing in data.Doing) AddCard(panelDoing, doing.Title, doing.Desc);
                    foreach (var done in data.Done) AddCard(panelDone, done.Title, done.Desc);
                    UpdateCounts();"""
kanban_new = """Clear_Click(null, null);
                    foreach (var todo in data.Todo) AddCard(colTodo, todo.Title, todo.Desc);
                    foreach (var doing in data.Doing) AddCard(colDoing, doing.Title, doing.Desc);
                    foreach (var done in data.Done) AddCard(colDone, done.Title, done.Desc);
                    UpdateCounts();"""
replace_in_file("KanbanTool.xaml.cs", kanban_old, kanban_new)

# 2. SwotTool
swot_old = """ClearAll();
                    foreach (var s in data.S) AddItem(panelS, s);
                    foreach (var w in data.W) AddItem(panelW, w);
                    foreach (var o in data.O) AddItem(panelO, o);
                    foreach (var t1 in data.T) AddItem(panelT, t1);
                    GenerateStrategy();"""
swot_new = """ApplyTemplate(data.S, data.W, data.O, data.T);"""
replace_in_file("SwotTool.xaml.cs", swot_old, swot_new)

# 3. ParetoTool
pareto_old = """_items.Clear();
                    foreach (var i in data.Items)
                        _items.Add(new ParetoItem { Category = i.Name, Value = i.Value });
                    SortAndDraw();"""
pareto_new = """Clear_Click(null, null);
                    foreach (var i in data.Items) AddDataRow(i.Name, i.Value.ToString());
                    DrawChart();"""
replace_in_file("ParetoTool.xaml.cs", pareto_old, pareto_new)
