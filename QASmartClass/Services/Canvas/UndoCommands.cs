using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Collections.Generic;

namespace QASmartClass.Services.Canvas
{
    public interface IUndoableCommand
    {
        void Execute();
        void Unexecute();
    }

    public class AddStrokeCommand : IUndoableCommand
    {
        private readonly InkCanvas _inkCanvas;
        private readonly Stroke _stroke;

        public AddStrokeCommand(InkCanvas inkCanvas, Stroke stroke)
        {
            _inkCanvas = inkCanvas;
            _stroke = stroke;
        }

        public void Execute()
        {
            if (!_inkCanvas.Strokes.Contains(_stroke))
            {
                _inkCanvas.Strokes.Add(_stroke);
            }
        }

        public void Unexecute()
        {
            if (_inkCanvas.Strokes.Contains(_stroke))
            {
                _inkCanvas.Strokes.Remove(_stroke);
            }
        }
    }

    public class EraseStrokesCommand : IUndoableCommand
    {
        private readonly InkCanvas _inkCanvas;
        private readonly StrokeCollection _erasedStrokes;

        public EraseStrokesCommand(InkCanvas inkCanvas, StrokeCollection erasedStrokes)
        {
            _inkCanvas = inkCanvas;
            _erasedStrokes = erasedStrokes;
        }

        public void Execute()
        {
            foreach (var stroke in _erasedStrokes)
            {
                if (_inkCanvas.Strokes.Contains(stroke))
                {
                    _inkCanvas.Strokes.Remove(stroke);
                }
            }
        }

        public void Unexecute()
        {
            foreach (var stroke in _erasedStrokes)
            {
                if (!_inkCanvas.Strokes.Contains(stroke))
                {
                    _inkCanvas.Strokes.Add(stroke);
                }
            }
        }
    }

    public class AddShapeCommand : IUndoableCommand
    {
        private readonly System.Windows.Controls.Canvas _shapeCanvas;
        private readonly UIElement _shape;
        private readonly List<UIElement> _shapeElementsList;

        public AddShapeCommand(System.Windows.Controls.Canvas shapeCanvas, UIElement shape, List<UIElement> shapeElementsList)
        {
            _shapeCanvas = shapeCanvas;
            _shape = shape;
            _shapeElementsList = shapeElementsList;
        }

        public void Execute()
        {
            if (!_shapeCanvas.Children.Contains(_shape))
            {
                _shapeCanvas.Children.Add(_shape);
            }
            if (!_shapeElementsList.Contains(_shape))
            {
                _shapeElementsList.Add(_shape);
            }
        }

        public void Unexecute()
        {
            if (_shapeCanvas.Children.Contains(_shape))
            {
                _shapeCanvas.Children.Remove(_shape);
            }
            if (_shapeElementsList.Contains(_shape))
            {
                _shapeElementsList.Remove(_shape);
            }
        }
    }

    public class AddTextCommand : IUndoableCommand
    {
        private readonly System.Windows.Controls.Canvas _shapeCanvas;
        private readonly UIElement _textElement;
        private readonly List<UIElement> _shapeElementsList;

        public AddTextCommand(System.Windows.Controls.Canvas shapeCanvas, UIElement textElement, List<UIElement> shapeElementsList)
        {
            _shapeCanvas = shapeCanvas;
            _textElement = textElement;
            _shapeElementsList = shapeElementsList;
        }

        public void Execute()
        {
            if (!_shapeCanvas.Children.Contains(_textElement))
            {
                _shapeCanvas.Children.Add(_textElement);
            }
            if (!_shapeElementsList.Contains(_textElement))
            {
                _shapeElementsList.Add(_textElement);
            }
        }

        public void Unexecute()
        {
            if (_shapeCanvas.Children.Contains(_textElement))
            {
                _shapeCanvas.Children.Remove(_textElement);
            }
            if (_shapeElementsList.Contains(_textElement))
            {
                _shapeElementsList.Remove(_textElement);
            }
        }
    }

    public class ClearCanvasCommand : IUndoableCommand
    {
        private readonly InkCanvas _inkCanvas;
        private readonly System.Windows.Controls.Canvas _shapeCanvas;
        private readonly List<UIElement> _shapeElementsList;
        
        private StrokeCollection _strokesSnapshot;
        private List<UIElement> _shapesSnapshot;

        public ClearCanvasCommand(InkCanvas inkCanvas, System.Windows.Controls.Canvas shapeCanvas, List<UIElement> shapeElementsList)
        {
            _inkCanvas = inkCanvas;
            _shapeCanvas = shapeCanvas;
            _shapeElementsList = shapeElementsList;

            _strokesSnapshot = inkCanvas.Strokes.Clone();
            _shapesSnapshot = new List<UIElement>(shapeElementsList);
        }

        public void Execute()
        {
            _inkCanvas.Strokes.Clear();
            _shapeCanvas.Children.Clear();
            _shapeElementsList.Clear();
        }

        public void Unexecute()
        {
            _inkCanvas.Strokes = _strokesSnapshot.Clone();
            _shapeCanvas.Children.Clear();
            _shapeElementsList.Clear();

            foreach (var shape in _shapesSnapshot)
            {
                _shapeCanvas.Children.Add(shape);
                _shapeElementsList.Add(shape);
            }
        }
    }

    public class DeleteElementsCommand : IUndoableCommand
    {
        private readonly System.Windows.Controls.Canvas _shapeCanvas;
        private readonly List<UIElement> _elements;
        private readonly List<UIElement> _shapeElementsList;

        public DeleteElementsCommand(System.Windows.Controls.Canvas shapeCanvas, IEnumerable<UIElement> elements, List<UIElement> shapeElementsList)
        {
            _shapeCanvas = shapeCanvas;
            _elements = new List<UIElement>(elements);
            _shapeElementsList = shapeElementsList;
        }

        public void Execute()
        {
            foreach (var element in _elements)
            {
                if (_shapeCanvas.Children.Contains(element))
                {
                    _shapeCanvas.Children.Remove(element);
                }
                if (_shapeElementsList.Contains(element))
                {
                    _shapeElementsList.Remove(element);
                }
            }
        }

        public void Unexecute()
        {
            foreach (var element in _elements)
            {
                if (!_shapeCanvas.Children.Contains(element))
                {
                    _shapeCanvas.Children.Add(element);
                }
                if (!_shapeElementsList.Contains(element))
                {
                    _shapeElementsList.Add(element);
                }
            }
        }
    }
}
