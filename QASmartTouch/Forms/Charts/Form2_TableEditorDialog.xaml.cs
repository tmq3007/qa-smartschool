using System;
using System.Windows;
using QASmartTouch.Controls;
using QASmartTouch.Models;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_TableEditorDialog : Window
    {
        public TableData? ResultTableData { get; private set; }

        public Form2_TableEditorDialog()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
        }

        public Form2_TableEditorDialog(TableData existingData)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            // ✅ QC_4.2_TABLE_EDIT_FIX: Nạp dữ liệu vào XAML instance đã có
            // (KHÔNG tạo instance mới — tránh lỗi orphaned control)
            tableEditor.LoadExistingData(existingData);
        }

        private void TableEditor_InsertRequested(object? sender, EventArgs e)
        {
            ResultTableData = tableEditor.TableData;
            DialogResult = true;
            this.Close();
        }

        private void TableEditor_CancelRequested(object? sender, EventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
