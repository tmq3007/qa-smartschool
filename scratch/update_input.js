const fs = require('fs');
const file = 'QASmartClass/Forms/Form2_MainDashboard.Input.cs';
let content = fs.readFileSync(file, 'utf8');

const target = '        private Shape? CreateShapePreview(string shapeName)\r\n        {\r\n            if (SubjectShapeGeometryService.IsSubjectShape(shapeName))';
const replacement = `        private Shape? CreateShapePreview(string shapeName)
        {
            if (shapeName == "mLine")
            {
                var line = new Line
                {
                    Stroke = new SolidColorBrush(_currentPenColor),
                    StrokeThickness = _currentPenSize,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Tag = "mLine"
                };
                Panel.SetZIndex(line, _currentInkingZIndex);
                return line;
            }
            if (shapeName == "mDashedLine")
            {
                var dashedLine = new Line
                {
                    Stroke = new SolidColorBrush(_currentPenColor),
                    StrokeThickness = _currentPenSize,
                    StrokeDashArray = new DoubleCollection { 8, 4 },
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Tag = "mDashedLine"
                };
                Panel.SetZIndex(dashedLine, _currentInkingZIndex);
                return dashedLine;
            }

            if (SubjectShapeGeometryService.IsSubjectShape(shapeName))`.replace(/\n/g, '\r\n');

if (!content.includes(target)) {
    console.error('Target not found!');
    process.exit(1);
}
content = content.replace(target, replacement);
fs.writeFileSync(file, content, 'utf8');
console.log('Successfully updated Form2_MainDashboard.Input.cs');

