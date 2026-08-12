using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents the complete table structure with rows, columns, and cells
    /// </summary>
    public class TableData : INotifyPropertyChanged
    {
        private string _borderColor = "#E1E8ED";
        private double _borderThickness = 1;
        private bool _autoExpandColumns = true;
        private bool _autoExpandRows = true;
        private double _cellPadding = 10;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<TableRow> Rows { get; set; }
        public ObservableCollection<TableColumn> Columns { get; set; }
        public TableCell[,] Cells { get; private set; }

        public int RowCount => Rows.Count;
        public int ColumnCount => Columns.Count;

        public string BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                OnPropertyChanged(nameof(BorderColor));
            }
        }

        public double BorderThickness
        {
            get => _borderThickness;
            set
            {
                _borderThickness = value;
                OnPropertyChanged(nameof(BorderThickness));
            }
        }

        public bool AutoExpandColumns
        {
            get => _autoExpandColumns;
            set
            {
                _autoExpandColumns = value;
                OnPropertyChanged(nameof(AutoExpandColumns));
            }
        }

        public bool AutoExpandRows
        {
            get => _autoExpandRows;
            set
            {
                _autoExpandRows = value;
                OnPropertyChanged(nameof(AutoExpandRows));
            }
        }

        public double CellPadding
        {
            get => _cellPadding;
            set
            {
                _cellPadding = value;
                OnPropertyChanged(nameof(CellPadding));
            }
        }

        public TableData(int rows = 2, int columns = 2)
        {
            Rows = new ObservableCollection<TableRow>();
            Columns = new ObservableCollection<TableColumn>();

            // Initialize rows
            for (int i = 0; i < rows; i++)
            {
                Rows.Add(new TableRow());
            }

            // Initialize columns
            for (int i = 0; i < columns; i++)
            {
                Columns.Add(new TableColumn());
            }

            // Initialize cells
            Cells = new TableCell[rows, columns];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cells[r, c] = new TableCell();
                }
            }
        }

        /// <summary>
        /// Add a new row at the end
        /// </summary>
        public void AddRow()
        {
            int newRowIndex = RowCount;
            Rows.Add(new TableRow());

            // Resize cells array
            var newCells = new TableCell[RowCount, ColumnCount];
            for (int r = 0; r < newRowIndex; r++)
            {
                for (int c = 0; c < ColumnCount; c++)
                {
                    newCells[r, c] = Cells[r, c];
                }
            }

            // Initialize new row cells
            for (int c = 0; c < ColumnCount; c++)
            {
                newCells[newRowIndex, c] = new TableCell();
            }

            Cells = newCells;
            OnPropertyChanged(nameof(RowCount));
        }

        /// <summary>
        /// Add a new column at the end
        /// </summary>
        public void AddColumn()
        {
            int newColIndex = ColumnCount;
            Columns.Add(new TableColumn());

            // Resize cells array
            var newCells = new TableCell[RowCount, ColumnCount];
            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < newColIndex; c++)
                {
                    newCells[r, c] = Cells[r, c];
                }
            }

            // Initialize new column cells
            for (int r = 0; r < RowCount; r++)
            {
                newCells[r, newColIndex] = new TableCell();
            }

            Cells = newCells;
            OnPropertyChanged(nameof(ColumnCount));
        }

        /// <summary>
        /// Delete a row at specified index
        /// </summary>
        public void DeleteRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= RowCount || RowCount <= 1)
                return;

            Rows.RemoveAt(rowIndex);

            // Resize cells array
            var newCells = new TableCell[RowCount, ColumnCount];
            int targetRow = 0;
            for (int r = 0; r < RowCount + 1; r++)
            {
                if (r == rowIndex)
                    continue;

                for (int c = 0; c < ColumnCount; c++)
                {
                    newCells[targetRow, c] = Cells[r, c];
                }
                targetRow++;
            }

            Cells = newCells;
            OnPropertyChanged(nameof(RowCount));
        }

        /// <summary>
        /// Delete a column at specified index
        /// </summary>
        public void DeleteColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= ColumnCount || ColumnCount <= 1)
                return;

            Columns.RemoveAt(columnIndex);

            // Resize cells array
            var newCells = new TableCell[RowCount, ColumnCount];
            for (int r = 0; r < RowCount; r++)
            {
                int targetCol = 0;
                for (int c = 0; c < ColumnCount + 1; c++)
                {
                    if (c == columnIndex)
                        continue;

                    newCells[r, targetCol] = Cells[r, c];
                    targetCol++;
                }
            }

            Cells = newCells;
            OnPropertyChanged(nameof(ColumnCount));
        }

        /// <summary>
        /// Get cell at specified position
        /// </summary>
        public TableCell GetCell(int row, int col)
        {
            if (row >= 0 && row < RowCount && col >= 0 && col < ColumnCount)
            {
                return Cells[row, col];
            }
            throw new ArgumentOutOfRangeException("Cell position out of bounds");
        }

        /// <summary>
        /// Set cell at specified position
        /// </summary>
        public void SetCell(int row, int col, TableCell cell)
        {
            if (row >= 0 && row < RowCount && col >= 0 && col < ColumnCount)
            {
                Cells[row, col] = cell;
            }
        }

        /// <summary>
        /// Calculate total table width
        /// </summary>
        public double GetTotalWidth()
        {
            return Columns.Sum(col => col.Width);
        }

        /// <summary>
        /// Calculate total table height
        /// </summary>
        public double GetTotalHeight()
        {
            return Rows.Sum(row => row.Height);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
