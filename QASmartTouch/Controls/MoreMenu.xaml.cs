using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Controls
{
    public partial class MoreMenu : UserControl
    {
        #region Events

        // Transform Operations
        public event EventHandler? FlipHorizontalClicked;
        public event EventHandler? FlipVerticalClicked;
        public event EventHandler? Rotate180Clicked;
        public event EventHandler? RotateLeft90Clicked;
        public event EventHandler? RotateCustomClicked;

        // Text Operations
        public event EventHandler? UpperCaseClicked;
        public event EventHandler? LowerCaseClicked;
        public event EventHandler? TitleCaseClicked;
        public event EventHandler? EditTextClicked;

        // Advanced Actions
        public event EventHandler? GroupClicked;
        public event EventHandler? UngroupClicked;
        public event EventHandler? DuplicateClicked;
        public event EventHandler? CreateLinkClicked;
        public event EventHandler? ExportImageClicked;

        // Smart Features
        public event EventHandler? OCRClicked;
        public event EventHandler? TranslateClicked;
        public event EventHandler? TextToSpeechClicked;
        public event EventHandler? VoiceInputClicked;
        public event EventHandler? FormulaRecognitionClicked;
        public event EventHandler? HandwritingClicked;
        public event EventHandler? WebSearchClicked;
        public event EventHandler? AskAIClicked;

        // Properties
        public event EventHandler? ObjectInfoClicked;
        public event EventHandler? SizePositionClicked;
        public event EventHandler? StrokeStyleClicked;

        #endregion

        #region Constructor

        public MoreMenu()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Event Handlers - Transform Operations

        private void btnFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            FlipHorizontalClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnFlipVertical_Click(object sender, RoutedEventArgs e)
        {
            FlipVerticalClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnRotate180_Click(object sender, RoutedEventArgs e)
        {
            Rotate180Clicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnRotateLeft90_Click(object sender, RoutedEventArgs e)
        {
            RotateLeft90Clicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnRotateCustom_Click(object sender, RoutedEventArgs e)
        {
            RotateCustomClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        #endregion

        #region Event Handlers - Text Operations

        private void btnUpperCase_Click(object sender, RoutedEventArgs e)
        {
            UpperCaseClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnLowerCase_Click(object sender, RoutedEventArgs e)
        {
            LowerCaseClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnTitleCase_Click(object sender, RoutedEventArgs e)
        {
            TitleCaseClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnEditText_Click(object sender, RoutedEventArgs e)
        {
            EditTextClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        #endregion

        #region Event Handlers - Advanced Actions

        private void btnGroup_Click(object sender, RoutedEventArgs e)
        {
            GroupClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnUngroup_Click(object sender, RoutedEventArgs e)
        {
            UngroupClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            DuplicateClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        private void btnCreateLink_Click(object sender, RoutedEventArgs e)
        {
            CreateLinkClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        private void btnExportImage_Click(object sender, RoutedEventArgs e)
        {
            ExportImageClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        #endregion

        #region Event Handlers - Smart Features

        private void btnOCR_Click(object sender, RoutedEventArgs e)
        {
            OCRClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            TranslateClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        private void btnTextToSpeech_Click(object sender, RoutedEventArgs e)
        {
            TextToSpeechClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnVoiceInput_Click(object sender, RoutedEventArgs e)
        {
            VoiceInputClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnFormulaRecognition_Click(object sender, RoutedEventArgs e)
        {
            FormulaRecognitionClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnHandwriting_Click(object sender, RoutedEventArgs e)
        {
            HandwritingClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void btnWebSearch_Click(object sender, RoutedEventArgs e)
        {
            WebSearchClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let handler control visibility
        }

        private void btnAskAI_Click(object sender, RoutedEventArgs e)
        {
            AskAIClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let handler control visibility
        }

        #endregion

        #region Event Handlers - Properties

        private void btnObjectInfo_Click(object sender, RoutedEventArgs e)
        {
            ObjectInfoClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        private void btnSizePosition_Click(object sender, RoutedEventArgs e)
        {
            SizePositionClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        private void btnStrokeStyle_Click(object sender, RoutedEventArgs e)
        {
            StrokeStyleClicked?.Invoke(this, EventArgs.Empty);
            // Don't hide - let dialog handler control visibility
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ✅ BUG #11 FIX: Đơn giản hóa ShowAt — loại bỏ double-clamping.
        /// Caller (OnToolbarMoreClicked) chịu trách nhiệm tính toán vị trí hợp lệ
        /// trong biên Canvas, bao gồm logic smart Above/Below positioning.
        /// </summary>
        public void ShowAt(Point position)
        {
            Canvas.SetLeft(this, position.X);
            Canvas.SetTop(this, position.Y);
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
