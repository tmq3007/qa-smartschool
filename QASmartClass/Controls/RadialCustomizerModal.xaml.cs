using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Services;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// QC_4.2 Hộp thoại Tùy biến Vòng tròn Radial - 6 Múi Tùy Chọn
    /// </summary>
    public partial class RadialCustomizerModal : UserControl
    {
        public event EventHandler<string>? ConfigurationSaved;
        public event EventHandler? Closed;

        private readonly string[] _slots = new string[6];
        private int _selectedSlotIndex = 1; // Múi đang chọn để gán (0 = M1, 1 = M2...)

        public RadialCustomizerModal()
        {
            InitializeComponent();
            LoadCurrentConfiguration();
            AttachEvents();
        }

        private void AttachEvents()
        {
            btnClose.Click += (s, e) => Closed?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnClose, () => Closed?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnCancel.Click += (s, e) => Closed?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnCancel, () => Closed?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnResetDefault.Click += (s, e) => ResetToDefaults();
            ToolbarPointerHelper.AttachTap(btnResetDefault, ResetToDefaults, handleMouse: false);

            btnSave.Click += (s, e) => SaveConfiguration();
            ToolbarPointerHelper.AttachTap(btnSave, SaveConfiguration, handleMouse: false);
        }

        public void LoadCurrentConfiguration()
        {
            string config = AppSettings.CustomRadialWedges;
            var parts = config.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < 6; i++)
            {
                if (i < parts.Length && RadialToolDial.AvailableTools.ContainsKey(parts[i]))
                    _slots[i] = parts[i];
                else
                    _slots[i] = GetDefaultKey(i);
            }
            _slots[0] = "pen"; // Múi 1 luôn cố định là Bút viết

            RenderSlots();
            RenderCandidates();
        }

        private static string GetDefaultKey(int index) => index switch
        {
            0 => "pen",
            1 => "eraser",
            2 => "select",
            3 => "shapes",
            4 => "undo",
            5 => "redo",
            _ => "pen"
        };

        private void ResetToDefaults()
        {
            for (int i = 0; i < 6; i++) _slots[i] = GetDefaultKey(i);
            _selectedSlotIndex = 1;
            RenderSlots();
            RenderCandidates();
        }

        private void SaveConfiguration()
        {
            string config = string.Join(",", _slots);
            AppSettings.CustomRadialWedges = config;
            ConfigurationSaved?.Invoke(this, config);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void RenderSlots()
        {
            slotsHost.Children.Clear();
            txtCandidateHint.Text = $"Chạm để gán vào M{_selectedSlotIndex + 1}";

            for (int i = 0; i < 6; i++)
            {
                int slotIndex = i;
                string key = _slots[i];
                if (!RadialToolDial.AvailableTools.TryGetValue(key, out var toolInfo))
                {
                    toolInfo = RadialToolDial.AvailableTools["pen"];
                }

                bool isSelected = (_selectedSlotIndex == i);
                bool isM1 = (i == 0);

                var border = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = isSelected ? RadialToolDial.Hex("#4F46E5") : RadialToolDial.Hex("#E1E8ED"),
                    BorderThickness = new Thickness(isSelected ? 2 : 1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = isM1 ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand,
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // M badge
                var mBadge = new Border
                {
                    Width = 24,
                    Height = 24,
                    Background = RadialToolDial.Hex(toolInfo.HexColor + "20"),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Child = new TextBlock
                    {
                        Text = $"M{i + 1}",
                        FontSize = 10.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = RadialToolDial.Hex(toolInfo.HexColor),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetColumn(mBadge, 0);
                grid.Children.Add(mBadge);

                // Tool info
                var infoPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var icon = new Path
                {
                    Data = Geometry.Parse(toolInfo.Icon),
                    Fill = RadialToolDial.Hex(toolInfo.HexColor),
                    Stretch = Stretch.Uniform,
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                var label = new TextBlock
                {
                    Text = toolInfo.Label,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = RadialToolDial.TextBrush,
                    VerticalAlignment = VerticalAlignment.Center
                };
                infoPanel.Children.Add(icon);
                infoPanel.Children.Add(label);
                Grid.SetColumn(infoPanel, 1);
                grid.Children.Add(infoPanel);

                // Action tag/button
                if (isM1)
                {
                    var fixedBadge = new Border
                    {
                        Background = RadialToolDial.Hex("#F1F2F6"),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 2, 8, 2),
                        Child = new TextBlock
                        {
                            Text = "Cố định",
                            FontSize = 10,
                            FontWeight = FontWeights.Medium,
                            Foreground = RadialToolDial.SubTextBrush
                        }
                    };
                    Grid.SetColumn(fixedBadge, 2);
                    grid.Children.Add(fixedBadge);
                }
                else
                {
                    var changeBtn = new Button
                    {
                        Height = 26,
                        Padding = new Thickness(10, 0, 10, 0),
                        Background = isSelected ? RadialToolDial.Hex("#4F46E5") : RadialToolDial.Hex("#EEF2FF"),
                        Foreground = isSelected ? Brushes.White : RadialToolDial.Hex("#4F46E5"),
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Focusable = false,
                        Template = RadialToolDial.CreatePillTemplate(13),
                        Content = new TextBlock
                        {
                            Text = isSelected ? "Đang chọn" : "Đổi",
                            FontSize = 11,
                            FontWeight = FontWeights.Bold
                        }
                    };
                    changeBtn.Click += (s, e) =>
                    {
                        _selectedSlotIndex = slotIndex;
                        RenderSlots();
                    };
                    ToolbarPointerHelper.AttachTap(changeBtn, () =>
                    {
                        _selectedSlotIndex = slotIndex;
                        RenderSlots();
                    }, handleMouse: false);

                    Grid.SetColumn(changeBtn, 2);
                    grid.Children.Add(changeBtn);
                }

                border.Child = grid;

                if (!isM1)
                {
                    border.MouseLeftButtonDown += (s, e) =>
                    {
                        _selectedSlotIndex = slotIndex;
                        RenderSlots();
                    };
                    ToolbarPointerHelper.AttachTap(border, () =>
                    {
                        _selectedSlotIndex = slotIndex;
                        RenderSlots();
                    }, handleMouse: false);
                }

                slotsHost.Children.Add(border);
            }
        }

        private void RenderCandidates()
        {
            candidatesHost.Children.Clear();

            foreach (var kv in RadialToolDial.AvailableTools)
            {
                var tool = kv.Value;
                if (tool.Key == "pen") continue; // M1 cố định là pen, không cần chọn lại

                var btn = new Button
                {
                    Width = 138,
                    Height = 46,
                    Margin = new Thickness(3),
                    Padding = new Thickness(8, 0, 8, 0),
                    Background = Brushes.White,
                    BorderBrush = RadialToolDial.Hex("#E1E8ED"),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Focusable = false,
                    Template = RadialToolDial.CreatePillTemplate(12),
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new Viewbox
                            {
                                Width = 18,
                                Height = 18,
                                Margin = new Thickness(0, 0, 8, 0),
                                Child = new Canvas
                                {
                                    Width = 24,
                                    Height = 24,
                                    Children = { new Path { Data = Geometry.Parse(tool.Icon), Fill = RadialToolDial.Hex(tool.HexColor) } }
                                }
                            },
                            new StackPanel
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Children =
                                {
                                    new TextBlock { Text = tool.Label, FontSize = 11.5, FontWeight = FontWeights.Bold, Foreground = RadialToolDial.TextBrush },
                                    new TextBlock { Text = tool.SubLabel, FontSize = 9.5, Foreground = RadialToolDial.SubTextBrush }
                                }
                            }
                        }
                    }
                };

                string key = tool.Key;
                btn.Click += (s, e) => AssignToolToCurrentSlot(key);
                ToolbarPointerHelper.AttachTap(btn, () => AssignToolToCurrentSlot(key), handleMouse: false);

                candidatesHost.Children.Add(btn);
            }
        }

        private void AssignToolToCurrentSlot(string key)
        {
            if (_selectedSlotIndex <= 0 || _selectedSlotIndex >= 6) return;
            _slots[_selectedSlotIndex] = key;
            RenderSlots();
        }
    }
}
