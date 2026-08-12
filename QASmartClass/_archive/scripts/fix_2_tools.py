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
                    foreach (var todo in data.Todo) AddCard(colTodo, todo.Title, todo.Desc);
                    foreach (var doing in data.Doing) AddCard(colDoing, doing.Title, doing.Desc);
                    foreach (var done in data.Done) AddCard(colDone, done.Title, done.Desc);
                    UpdateCounts();"""
kanban_new = """Clear_Click(null, null);
                    foreach (var todo in data.Todo) AddCard(colTodo, todo.Title, todo.Desc, "#FFFFFF", "#E0E0E0");
                    foreach (var doing in data.Doing) AddCard(colDoing, doing.Title, doing.Desc, "#FFF9C4", "#FBC02D");
                    foreach (var done in data.Done) AddCard(colDone, done.Title, done.Desc, "#E8F5E9", "#4CAF50");
                    UpdateCounts();"""
replace_in_file("KanbanTool.xaml.cs", kanban_old, kanban_new)

# 2. SwotTool
swot_old = """ApplyTemplate(data.S, data.W, data.O, data.T);"""
swot_new = """ApplyTemplate(t.Name, data.S, data.W, data.O, data.T);"""
replace_in_file("SwotTool.xaml.cs", swot_old, swot_new)
