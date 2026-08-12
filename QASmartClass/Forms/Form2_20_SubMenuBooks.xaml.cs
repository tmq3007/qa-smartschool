using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartTouch.Modules.InteractiveBooks.Models;
using QASmartTouch.Modules.InteractiveBooks.Services;

namespace QASmartTouch.Forms
{
    public partial class Form2_20_SubMenuBooks : Window
    {
        private readonly BookService _bookService;
        private List<Book> _allBooks;
        private List<Book> _filteredBooks;
        private int _selectedGrade = 0; // 0 = All grades

        public Form2_20_SubMenuBooks()
        {
            InitializeComponent();
            if (btnClose != null)
            {
                btnClose.PreviewTouchDown += (s, e) => { this.Close(); e.Handled = true; };
                btnClose.PreviewStylusDown += (s, e) => { this.Close(); e.Handled = true; };
            }
            _bookService = new BookService();
            _allBooks = new List<Book>();
            _filteredBooks = new List<Book>();
            
            Loaded += Form2_20_SubMenuBooks_Loaded;
        }

        private void Form2_20_SubMenuBooks_Loaded(object sender, RoutedEventArgs e)
        {
            CreateGradeTabs();
            LoadBooks();
            LoadSubjects();
            ApplyFilters();
        }

        /// <summary>
        /// Create grade filter tabs (Tất cả, Lớp 1-12)
        /// </summary>
        private void CreateGradeTabs()
        {
            gradeTabsPanel.Children.Clear();

            // Add "Tất cả" tab (not selected by default)
            var allTab = CreateGradeTab("Tất cả", 0, false);
            gradeTabsPanel.Children.Add(allTab);

            // Add grade 1-12 tabs (Lớp 1 is selected by default)
            for (int i = 1; i <= 12; i++)
            {
                var tab = CreateGradeTab($"Lớp {i}", i, i == 1); // Lớp 1 selected
                gradeTabsPanel.Children.Add(tab);
            }
            
            // Set initial selected grade to 1
            _selectedGrade = 1;
        }

        /// <summary>
        /// Create a single grade tab button
        /// </summary>
        private Button CreateGradeTab(string label, int grade, bool isSelected)
        {
            var button = new Button
            {
                Content = label,
                Tag = grade,
                Height = 32,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 12,
                FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                Background = isSelected ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A90E2")) : Brushes.White,
                Foreground = isSelected ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1E8ED")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            // Template for rounded corners
            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            factory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            factory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            factory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(contentPresenter);

            template.VisualTree = factory;
            button.Template = template;

            button.Click += GradeTab_Click;

            return button;
        }

        /// <summary>
        /// Handle grade tab click
        /// </summary>
        private void GradeTab_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = (Button)sender;
            _selectedGrade = (int)clickedButton.Tag;

            // Update all tab styles
            foreach (Button tab in gradeTabsPanel.Children)
            {
                bool isSelected = (int)tab.Tag == _selectedGrade;
                tab.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
                tab.Background = isSelected ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A90E2")) : Brushes.White;
                tab.Foreground = isSelected ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));
            }

            ApplyFilters();
        }

        /// <summary>
        /// Load all books from service
        /// </summary>
        private void LoadBooks()
        {
            try
            {
                _allBooks = _bookService.GetAllBooks();
                _filteredBooks = new List<Book>(_allBooks);
                
                UpdateBookGrid();
                
                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_allBooks.Count} books");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading books: {ex.Message}");
                MessageBox.Show($"Lỗi tải danh sách sách: {ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load subjects for filter dropdown
        /// </summary>
        private void LoadSubjects()
        {
            try
            {
                var subjects = _bookService.GetUniqueSubjects(_allBooks);
                
                // Clear existing items except "Tất cả môn học"
                while (cmbSubject.Items.Count > 1)
                {
                    cmbSubject.Items.RemoveAt(1);
                }
                
                // Add subjects
                foreach (var subject in subjects)
                {
                    cmbSubject.Items.Add(new ComboBoxItem { Content = subject });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading subjects: {ex.Message}");
            }
        }

        /// <summary>
        /// Update book grid display
        /// </summary>
        private void UpdateBookGrid()
        {
            booksGrid.ItemsSource = null;
            booksGrid.ItemsSource = _filteredBooks;
        }

        /// <summary>
        /// Apply all filters
        /// </summary>
        private void ApplyFilters()
        {
            _filteredBooks = new List<Book>(_allBooks);

            // Filter by grade
            if (_selectedGrade > 0)
            {
                _filteredBooks = _filteredBooks.Where(b => b.Grade == _selectedGrade).ToList();
            }

            // Filter by subject
            if (cmbSubject.SelectedIndex > 0)
            {
                var selectedSubject = ((ComboBoxItem)cmbSubject.SelectedItem).Content.ToString();
                _filteredBooks = _filteredBooks.Where(b => b.Subject == selectedSubject).ToList();
            }

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                var searchText = txtSearch.Text.ToLower();
                _filteredBooks = _filteredBooks.Where(b =>
                    b.Name.ToLower().Contains(searchText) ||
                    b.Subject.ToLower().Contains(searchText) ||
                    b.Publisher.ToLower().Contains(searchText)
                ).ToList();
            }

            UpdateBookGrid();
        }

        /// <summary>
        /// Open book in viewer
        /// </summary>
        private void OpenBook(Book book)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📖 Opening book: {book.Name}");

                // Open book viewer
                var bookViewer = new Form2_20_1_BookViewer(book);
                bookViewer.Topmost = true;
                bookViewer.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error opening book: {ex.Message}");
                MessageBox.Show($"Lỗi mở sách: {ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnViewBook_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var book = (Book)button.Tag;
            OpenBook(book);
        }

        private void BookCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var book = (Book)border.DataContext;
            OpenBook(book);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allBooks != null && _allBooks.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void cmbSubject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allBooks != null && _allBooks.Count > 0)
            {
                ApplyFilters();
            }
        }
    }
}
