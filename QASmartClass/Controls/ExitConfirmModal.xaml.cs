using System;
using System.Windows.Controls;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// QC_4.2 Hộp thoại Xác nhận Thoát Ứng dụng
    /// </summary>
    public partial class ExitConfirmModal : UserControl
    {
        public event EventHandler? Cancelled;
        public event EventHandler? ExitWithoutSaveRequested;
        public event EventHandler? ExitWithSaveRequested;

        public ExitConfirmModal()
        {
            InitializeComponent();
            AttachEvents();
        }

        private void AttachEvents()
        {
            btnCancel.Click += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnCancel, () => Cancelled?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnExitWithoutSave.Click += (s, e) => ExitWithoutSaveRequested?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnExitWithoutSave, () => ExitWithoutSaveRequested?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnExitWithSave.Click += (s, e) => ExitWithSaveRequested?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnExitWithSave, () => ExitWithSaveRequested?.Invoke(this, EventArgs.Empty), handleMouse: false);
        }
    }
}
