using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class ElectronConfigTool : BaseToolControl
    {
        // Aufbau order: (n, l, maxElectrons, label)
        private static readonly (int N, int L, int Max, string Label)[] Subshells = {
            (1,0,2,"1s"), (2,0,2,"2s"), (2,1,6,"2p"),
            (3,0,2,"3s"), (3,1,6,"3p"), (4,0,2,"4s"), (3,2,10,"3d"), (4,1,6,"4p"),
            (5,0,2,"5s"), (4,2,10,"4d"), (5,1,6,"5p"), (6,0,2,"6s"), (4,3,14,"4f"),
            (5,2,10,"5d"), (6,1,6,"6p"), (7,0,2,"7s"), (5,3,14,"5f"), (6,2,10,"6d"), (7,1,6,"7p"),
        };

        // 118 Elements dictionary (Vietnamese name, English IUPAC name, Symbol, Category)
        private static readonly Dictionary<int, (string Name, string NameEn, string Symbol, string Category)> Elements = new()
        {
            { 1, ("Hydro", "Hydrogen", "H", "Phi kim") },
            { 2, ("Heli", "Helium", "He", "Khí hiếm") },
            { 3, ("Liti", "Lithium", "Li", "Kim loại kiềm") },
            { 4, ("Berili", "Beryllium", "Be", "Kim loại kiềm thổ") },
            { 5, ("Bo", "Boron", "B", "Á kim") },
            { 6, ("Cacbon", "Carbon", "C", "Phi kim") },
            { 7, ("Nitơ", "Nitrogen", "N", "Phi kim") },
            { 8, ("Oxy", "Oxygen", "O", "Phi kim") },
            { 9, ("Flo", "Fluorine", "F", "Phi kim – Halogen") },
            { 10, ("Neon", "Neon", "Ne", "Khí hiếm") },
            { 11, ("Natri", "Sodium", "Na", "Kim loại kiềm") },
            { 12, ("Magie", "Magnesium", "Mg", "Kim loại kiềm thổ") },
            { 13, ("Nhôm", "Aluminium", "Al", "Kim loại") },
            { 14, ("Silic", "Silicon", "Si", "Bán kim loại") },
            { 15, ("Phốt pho", "Phosphorus", "P", "Phi kim") },
            { 16, ("Lưu huỳnh", "Sulfur", "S", "Phi kim") },
            { 17, ("Clo", "Chlorine", "Cl", "Halogen – Phi kim") },
            { 18, ("Argon", "Argon", "Ar", "Khí hiếm – Noble gas") },
            { 19, ("Kali", "Potassium", "K", "Kim loại kiềm") },
            { 20, ("Canxi", "Calcium", "Ca", "Kim loại kiềm thổ") },
            { 21, ("Scandi", "Scandium", "Sc", "Kim loại chuyển tiếp") },
            { 22, ("Titan", "Titanium", "Ti", "Kim loại chuyển tiếp") },
            { 23, ("Vanadi", "Vanadium", "V", "Kim loại chuyển tiếp") },
            { 24, ("Crom", "Chromium", "Cr", "Kim loại chuyển tiếp") },
            { 25, ("Mangan", "Manganese", "Mn", "Kim loại chuyển tiếp") },
            { 26, ("Sắt", "Iron", "Fe", "Kim loại chuyển tiếp") },
            { 27, ("Coban", "Cobalt", "Co", "Kim loại chuyển tiếp") },
            { 28, ("Niken", "Nickel", "Ni", "Kim loại chuyển tiếp") },
            { 29, ("Đồng", "Copper", "Cu", "Kim loại chuyển tiếp") },
            { 30, ("Kẽm", "Zinc", "Zn", "Kim loại chuyển tiếp") },
            { 31, ("Gallium", "Gallium", "Ga", "Kim loại chuyển tiếp") },
            { 32, ("Germani", "Germanium", "Ge", "Á kim") },
            { 33, ("Asen", "Arsenic", "As", "Á kim") },
            { 34, ("Selen", "Selenium", "Se", "Phi kim") },
            { 35, ("Brom", "Bromine", "Br", "Halogen") },
            { 36, ("Krypton", "Krypton", "Kr", "Khí hiếm") },
            { 37, ("Rubidi", "Rubidium", "Rb", "Kim loại kiềm") },
            { 38, ("Stronti", "Strontium", "Sr", "Kim loại kiềm thổ") },
            { 39, ("Yttri", "Yttrium", "Y", "Kim loại chuyển tiếp") },
            { 40, ("Zirconi", "Zirconium", "Zr", "Kim loại chuyển tiếp") },
            { 41, ("Niobi", "Niobium", "Nb", "Kim loại chuyển tiếp") },
            { 42, ("Molipđen", "Molybdenum", "Mo", "Kim loại chuyển tiếp") },
            { 43, ("Tecneti", "Technetium", "Tc", "Kim loại chuyển tiếp") },
            { 44, ("Rutheni", "Ruthenium", "Ru", "Kim loại chuyển tiếp") },
            { 45, ("Rhodium", "Rhodium", "Rh", "Kim loại chuyển tiếp") },
            { 46, ("Palladi", "Palladium", "Pd", "Kim loại chuyển tiếp") },
            { 47, ("Bạc", "Silver", "Ag", "Kim loại chuyển tiếp") },
            { 48, ("Cadmi", "Cadmium", "Cd", "Kim loại chuyển tiếp") },
            { 49, ("Indi", "Indium", "In", "Kim loại hậu chuyển tiếp") },
            { 50, ("Thiếc", "Tin", "Sn", "Kim loại hậu chuyển tiếp") },
            { 51, ("Antimon", "Antimony", "Sb", "Bán kim loại") },
            { 52, ("Telluri", "Tellurium", "Te", "Bán kim loại") },
            { 53, ("Iốt", "Iodine", "I", "Phi kim") },
            { 54, ("Xenon", "Xenon", "Xe", "Khí hiếm") },
            { 55, ("Xesi", "Cesium", "Cs", "Kim loại kiềm") },
            { 56, ("Bari", "Barium", "Ba", "Kim loại kiềm thổ") },
            { 57, ("Lantan", "Lanthanum", "La", "Kim loại đất hiếm") },
            { 58, ("Cer", "Cerium", "Ce", "Kim loại đất hiếm") },
            { 59, ("Praseodymi", "Praseodymium", "Pr", "Kim loại đất hiếm") },
            { 60, ("Neodymi", "Neodymium", "Nd", "Kim loại đất hiếm") },
            { 61, ("Promethi", "Promethium", "Pm", "Kim loại đất hiếm") },
            { 62, ("Samari", "Samarium", "Sm", "Kim loại đất hiếm") },
            { 63, ("Europi", "Europium", "Eu", "Kim loại đất hiếm") },
            { 64, ("Gadolini", "Gadolinium", "Gd", "Kim loại đất hiếm") },
            { 65, ("Terbi", "Terbium", "Tb", "Kim loại đất hiếm") },
            { 66, ("Dysprosi", "Dysprosium", "Dy", "Kim loại đất hiếm") },
            { 67, ("Holmi", "Holmium", "Ho", "Kim loại đất hiếm") },
            { 68, ("Erbi", "Erbium", "Er", "Kim loại đất hiếm (Lantanide)") },
            { 69, ("Thuli", "Thulium", "Tm", "Kim loại đất hiếm (Lantanide)") },
            { 70, ("Ytterbi", "Ytterbium", "Yb", "Kim loại đất hiếm (Lantanide)") },
            { 71, ("Luteti", "Lutetium", "Lu", "Kim loại đất hiếm (Lantanide)") },
            { 72, ("Hafni", "Hafnium", "Hf", "Kim loại chuyển tiếp") },
            { 73, ("Tantali", "Tantalum", "Ta", "Kim loại chuyển tiếp") },
            { 74, ("Wolfram", "Tungsten", "W", "Kim loại chuyển tiếp") },
            { 75, ("Rheni", "Rhenium", "Re", "Kim loại chuyển tiếp") },
            { 76, ("Osmi", "Osmium", "Os", "Kim loại chuyển tiếp") },
            { 77, ("Iridi", "Iridium", "Ir", "Kim loại chuyển tiếp") },
            { 78, ("Platinum", "Platinum", "Pt", "Kim loại chuyển tiếp") },
            { 79, ("Vàng", "Gold", "Au", "Kim loại chuyển tiếp") },
            { 80, ("Thủy ngân", "Mercury", "Hg", "Kim loại chuyển tiếp") },
            { 81, ("Thallium", "Thallium", "Tl", "Kim loại sau chuyển tiếp, kim loại độc") },
            { 82, ("Chì", "Lead", "Pb", "Kim loại sau chuyển tiếp") },
            { 83, ("Bismuth", "Bismuth", "Bi", "Kim loại nặng p-block") },
            { 84, ("Poloni", "Polonium", "Po", "Chất phóng xạ") },
            { 85, ("Astatin", "Astatine", "At", "Halogen phóng xạ") },
            { 86, ("Radon", "Radon", "Rn", "Khí hiếm phóng xạ") },
            { 87, ("Franci", "Francium", "Fr", "Kim loại kiềm phóng xạ") },
            { 88, ("Radium", "Radium", "Ra", "Kim loại kiềm thổ phóng xạ") },
            { 89, ("Actinium", "Actinium", "Ac", "Kim loại actinide") },
            { 90, ("Thorium", "Thorium", "Th", "Kim loại actinide") },
            { 91, ("Protactinium", "Protactinium", "Pa", "Kim loại actinide") },
            { 92, ("Uranium", "Uranium", "U", "Kim loại actinide") },
            { 93, ("Neptunium", "Neptunium", "Np", "Kim loại actinide") },
            { 94, ("Plutôni", "Plutonium", "Pu", "Actinide") },
            { 95, ("Americi", "Americium", "Am", "Actinide") },
            { 96, ("Curi", "Curium", "Cm", "Actinide") },
            { 97, ("Berkeli", "Berkelium", "Bk", "Actinide") },
            { 98, ("Californi", "Californium", "Cf", "Actinide") },
            { 99, ("Einsteini", "Einsteinium", "Es", "Actinide") },
            { 100, ("Fermi", "Fermium", "Fm", "Actinide") },
            { 101, ("Mendelevi", "Mendelevium", "Md", "Actinide") },
            { 102, ("Nobeli", "Nobelium", "No", "Actinide") },
            { 103, ("Lawrenci", "Lawrencium", "Lr", "Actinide") },
            { 104, ("Rutherfordi", "Rutherfordium", "Rf", "Kim loại chuyển tiếp") },
            { 105, ("Dubni", "Dubnium", "Db", "Kim loại chuyển tiếp") },
            { 106, ("Seaborgi", "Seaborgium", "Sg", "Kim loại chuyển tiếp") },
            { 107, ("Bohri", "Bohrium", "Bh", "Kim loại chuyển tiếp") },
            { 108, ("Hassi", "Hassium", "Hs", "Kim loại chuyển tiếp") },
            { 109, ("Meitneri", "Meitnerium", "Mt", "Kim loại chuyển tiếp") },
            { 110, ("Darmstadti", "Darmstadtium", "Ds", "Kim loại chuyển tiếp") },
            { 111, ("Roentgeni", "Roentgenium", "Rg", "Kim loại chuyển tiếp") },
            { 112, ("Copernici", "Copernicium", "Cn", "Kim loại chuyển tiếp") },
            { 113, ("Nihoni", "Nihonium", "Nh", "Kim loại hậu chuyển tiếp") },
            { 114, ("Flerovi", "Flerovium", "Fl", "Kim loại hậu chuyển tiếp – siêu nặng") },
            { 115, ("Moscovi", "Moscovium", "Mc", "Kim loại hậu chuyển tiếp – siêu nặng") },
            { 116, ("Livermo", "Livermorium", "Lv", "Kim loại hậu chuyển tiếp – siêu nặng") },
            { 117, ("Tennesin", "Tennessine", "Ts", "Halogen siêu nặng (nhóm 17)") },
            { 118, ("Oganeson", "Oganesson", "Og", "Khí hiếm siêu nặng – phi kim atypical") },
        };

        // Electron config exceptions overrides
        private static readonly Dictionary<int, Dictionary<string, int>> Exceptions = new()
        {
            { 24, new() { { "4s", 1 }, { "3d", 5 } } },
            { 29, new() { { "4s", 1 }, { "3d", 10 } } },
            { 41, new() { { "5s", 1 }, { "4d", 4 } } },
            { 42, new() { { "5s", 1 }, { "4d", 5 } } },
            { 44, new() { { "5s", 1 }, { "4d", 7 } } },
            { 45, new() { { "5s", 1 }, { "4d", 8 } } },
            { 46, new() { { "5s", 0 }, { "4d", 10 } } },
            { 47, new() { { "5s", 1 }, { "4d", 10 } } },
            { 57, new() { { "4f", 0 }, { "5d", 1 } } },
            { 58, new() { { "4f", 1 }, { "5d", 1 } } },
            { 64, new() { { "4f", 7 }, { "5d", 1 } } },
            { 78, new() { { "6s", 1 }, { "5d", 9 } } },
            { 79, new() { { "6s", 1 }, { "5d", 10 } } },
            { 89, new() { { "5f", 0 }, { "6d", 1 } } },
            { 90, new() { { "5f", 0 }, { "6d", 2 } } },
            { 91, new() { { "5f", 2 }, { "6d", 1 } } },
            { 92, new() { { "5f", 3 }, { "6d", 1 } } },
            { 93, new() { { "5f", 4 }, { "6d", 1 } } },
            { 96, new() { { "5f", 7 }, { "6d", 1 } } },
            { 103, new() { { "5f", 14 }, { "6d", 0 }, { "7s", 2 }, { "7p", 1 } } }
        };

        public ElectronConfigTool()
        {
            InitializeComponent();
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtZ, step: 1, allowDecimal: false);
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextSimulator != null) menuTextSimulator.Text = isVN ? "Trình mô phỏng" : "Simulator";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildPresets();
                Calc();
                LoadPracticalApps();
            };
        }

        private void Z_Changed(object sender, EventArgs e) { if (IsLoaded) Calc(); }

        private void Calc()
        {
            resultPanel.Children.Clear();
            if (orbitalPanel != null) orbitalPanel.Children.Clear();
            if (bohrCanvas != null) bohrCanvas.Children.Clear();

            if (cardOrbital != null) cardOrbital.Visibility = Visibility.Collapsed;
            if (cardBohr != null) cardBohr.Visibility = Visibility.Collapsed;

            if (!int.TryParse(txtZ?.Text, out int z) || z < 1 || z > 118)
            {
                txtElement.Text = "Nguyên tố chưa xác định";
                txtSymbol.Text = "";
                return;
            }

            // Get element information
            bool hasElem = Elements.TryGetValue(z, out var elem);
            if (hasElem)
            {
                txtElement.Text = $"{elem.Symbol} — {elem.Name}";
                txtSymbol.Text = $"Z = {z} ({elem.NameEn})";
            }
            else
            {
                txtElement.Text = $"Nguyên tố Z={z}";
                txtSymbol.Text = $"Z = {z}";
            }

            // 1. Build standard Aufbau configuration
            var aufbauConfig = new List<(string Label, int Count)>();
            int remaining = z;
            foreach (var sub in Subshells)
            {
                if (remaining <= 0) break;
                int fill = System.Math.Min(remaining, sub.Max);
                aufbauConfig.Add((sub.Label, fill));
                remaining -= fill;
            }

            // Convert to a dictionary for easy manipulation of exceptions
            var configDict = aufbauConfig.ToDictionary(c => c.Label, c => c.Count);

            // 2. Apply exceptions overrides
            if (Exceptions.TryGetValue(z, out var overrideDict))
            {
                foreach (var kvp in overrideDict)
                {
                    configDict[kvp.Key] = kvp.Value;
                }
            }

            // Convert back to a list, filtering out subshells with 0 electrons
            var finalConfig = configDict
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => (Label: kvp.Key, Count: kvp.Value))
                .ToList();

            // Sắp xếp các cấu hình:
            // a) Trật tự mức năng lượng (Aufbau order)
            var aufbauOrderMap = Subshells.Select((s, idx) => (s.Label, idx)).ToDictionary(x => x.Label, x => x.idx);
            var energyOrderConfig = finalConfig
                .OrderBy(c => aufbauOrderMap.ContainsKey(c.Label) ? aufbauOrderMap[c.Label] : 99)
                .ToList();
            string energyConfigStr = string.Join(" ", energyOrderConfig.Select(c => $"{c.Label}{ToSuperscript(c.Count)}"));

            // b) Cấu hình electron nguyên tử (Sorted by Shell number n, then by l)
            var sortedConfig = finalConfig
                .OrderBy(c => c.Label[0] - '0') // n
                .ThenBy(c => GetOrbitalLValue(c.Label[1])) // l
                .ToList();
            string fullConfigStr = string.Join(" ", sortedConfig.Select(c => $"{c.Label}{ToSuperscript(c.Count)}"));

            // Display Configurations
            UI.ResultRow($"Cấu hình e nguyên tử: {fullConfigStr}", "#1565C0", resultPanel);
            UI.ResultRow($"Trật tự mức năng lượng: {energyConfigStr}", "#757575", resultPanel);

            // c) Abbreviated Configuration (Lõi khí hiếm)
            string abbrevStr = GetAbbreviatedConfigString(z, sortedConfig);
            if (!string.IsNullOrEmpty(abbrevStr))
            {
                UI.ResultRow($"Cấu hình rút gọn: {abbrevStr}", "#1565C0", resultPanel);
            }

            // 3. Layer distribution
            var layers = new int[8]; // Shells 1 to 7
            foreach (var c in sortedConfig)
            {
                int shell = c.Label[0] - '0';
                if (shell >= 1 && shell <= 7)
                {
                    layers[shell] += c.Count;
                }
            }

            int numLayers = 0;
            for (int i = 7; i >= 1; i--)
            {
                if (layers[i] > 0)
                {
                    numLayers = i;
                    break;
                }
            }

            string[] shellNames = { "", "K", "L", "M", "N", "O", "P", "Q" };
            var layerStr = "";
            for (int i = 1; i <= numLayers; i++)
            {
                layerStr += $"Lớp {i} ({shellNames[i]}): {layers[i]}e   ";
            }
            UI.ResultRow($"Phân bố lớp: {layerStr.Trim()}", "#2E7D32", resultPanel);
            UI.ResultRow($"Chu kỳ: {numLayers} (do có {numLayers} lớp electron)", "#2E7D32", resultPanel);

            // 4. Block, Group, Valence electrons, Properties
            // The block is determined by the last subshell receiving electron in Aufbau filling
            var lastSubshellFilled = energyOrderConfig.Last();
            string block = lastSubshellFilled.Label[1].ToString();
            if (z >= 57 && z <= 71) block = "f";
            else if (z >= 89 && z <= 103) block = "f";
            UI.ResultRow($"Khối nguyên tố (Block): {block}", "#1565C0", resultPanel);

            int valenceShellElectrons = layers[numLayers];
            int valenceElectrons = valenceShellElectrons;

            // Group calculation
            string groupStr = "";
            if (block == "s" || block == "p")
            {
                int grpNum = valenceShellElectrons;
                if (z == 2) // Helium exception
                {
                    groupStr = "VIIIA";
                }
                else
                {
                    groupStr = ToRoman(grpNum) + "A";
                }
            }
            else if (block == "d")
            {
                // ns^a (n-1)d^b
                int nsElectrons = configDict.ContainsKey($"{numLayers}s") ? configDict[$"{numLayers}s"] : 0;
                int ndElectrons = configDict.ContainsKey($"{numLayers - 1}d") ? configDict[$"{numLayers - 1}d"] : 0;
                int sum = nsElectrons + ndElectrons;
                
                // Valence electrons for transition metals include ns and incomplete (n-1)d
                valenceElectrons = ndElectrons == 10 ? nsElectrons : sum;

                if (sum < 8)
                {
                    groupStr = ToRoman(sum) + "B";
                }
                else if (sum >= 8 && sum <= 10)
                {
                    groupStr = "VIIIB";
                }
                else if (sum == 11)
                {
                    groupStr = "IB";
                }
                else if (sum == 12)
                {
                    groupStr = "IIB";
                }
            }
            else if (block == "f")
            {
                valenceElectrons = 3; // Conventionally 3 valence e
                if (z >= 57 && z <= 71)
                {
                    groupStr = "IIIB (Họ Lantan)";
                }
                else if (z >= 89 && z <= 103)
                {
                    groupStr = "IIIB (Họ Actini)";
                }
                else
                {
                    groupStr = "IIIB";
                }
            }

            UI.ResultRow($"Nhóm: {groupStr}", "#1565C0", resultPanel);
            UI.ResultRow($"Số electron lớp ngoài cùng: {valenceShellElectrons}e", "#1565C0", resultPanel);
            UI.ResultRow($"Số electron hóa trị: {valenceElectrons}e", "#1565C0", resultPanel);

            // Properties classification
            string propertiesStr = "";
            // Safe IUPAC classification
            if (z == 2 || z == 10 || z == 18 || z == 36 || z == 54 || z == 86 || z == 118)
            {
                propertiesStr = "Khí hiếm (bền vững, trơ hóa học)";
            }
            else if (z == 5 || z == 14 || z == 32 || z == 33 || z == 51 || z == 52 || z == 84)
            {
                propertiesStr = "Á kim (bán dẫn)";
            }
            else if (z == 1 || z == 6 || z == 7 || z == 8 || z == 9 || z == 15 || z == 16 || z == 17 || z == 34 || z == 35 || z == 53 || z == 85 || z == 117)
            {
                propertiesStr = "Phi kim (dễ nhận electron)";
            }
            else
            {
                propertiesStr = "Kim loại (dễ nhường electron)";
            }
            UI.ResultRow($"Tính chất: {propertiesStr}", "#E65100", resultPanel);

            // 5. Draw Orbital Diagram
            DrawOrbitalDiagram(numLayers, sortedConfig);

            // 6. Draw Bohr Model
            DrawBohrModel(numLayers, layers, hasElem ? elem.Symbol : "?", z);
        }

        private int GetOrbitalLValue(char lChar)
        {
            return lChar switch
            {
                's' => 0, 'p' => 1, 'd' => 2, 'f' => 3, _ => 99
            };
        }

        private static string ToRoman(int number)
        {
            return number switch
            {
                1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII",
                9 => "IX", 10 => "X", 11 => "XI", 12 => "XII", _ => number.ToString()
            };
        }

        private void DrawOrbitalDiagram(int numLayers, List<(string Label, int Count)> sortedConfig)
        {
            if (orbitalPanel == null) return;
            orbitalPanel.Children.Clear();

            // Select valence subshells: 
            // 1. All subshells in outermost shell (n = numLayers)
            // 2. The d-subshell of shell (n = numLayers - 1) if present
            // 3. The f-subshell of shell (n = numLayers - 2) if present
            var valenceSubshells = sortedConfig
                .Where(c => {
                    int n = c.Label[0] - '0';
                    char l = c.Label[1];
                    if (n == numLayers) return true;
                    if (n == numLayers - 1 && l == 'd') return true;
                    if (n == numLayers - 2 && l == 'f') return true;
                    return false;
                })
                .OrderBy(c => c.Label[0] - '0')
                .ThenBy(c => GetOrbitalLValue(c.Label[1]))
                .ToList();

            if (valenceSubshells.Count > 0)
            {
                if (cardOrbital != null) cardOrbital.Visibility = Visibility.Visible;
                foreach (var sub in valenceSubshells)
                {
                    int eCount = sub.Count;
                    char l = sub.Label[1];
                    int oCount = l switch { 's' => 1, 'p' => 3, 'd' => 5, 'f' => 7, _ => 0 };

                    var subPanel = new StackPanel { Margin = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Center };
                    var labelText = new TextBlock 
                    { 
                        Text = $"{sub.Label}{ToSuperscript(eCount)}", 
                        FontSize = 14, 
                        FontWeight = FontWeights.Bold, 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4527A0")), 
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    subPanel.Children.Add(labelText);

                    var boxesPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                    for (int i = 0; i < oCount; i++)
                    {
                        var box = new Border
                        {
                            Width = 32,
                            Height = 36,
                            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7E57C2")),
                            BorderThickness = new Thickness(1.5),
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE7F6")),
                            CornerRadius = new CornerRadius(2),
                            Margin = new Thickness(1, 0, 1, 0)
                        };

                        // Hund's rule: fill singly first, then double up
                        string arrows = "";
                        SolidColorBrush arrowColor = Brushes.Black;
                        if (eCount <= oCount)
                        {
                            if (i < eCount)
                            {
                                arrows = "↑";
                                arrowColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0")); // Blue for unpaired
                            }
                        }
                        else
                        {
                            if (i < eCount - oCount)
                            {
                                arrows = "↑↓";
                                arrowColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D84315")); // Red-orange for paired
                            }
                            else
                            {
                                arrows = "↑";
                                arrowColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0"));
                            }
                        }

                        var textArrows = new TextBlock
                        {
                            Text = arrows,
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Foreground = arrowColor,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        box.Child = textArrows;
                        boxesPanel.Children.Add(box);
                    }
                    subPanel.Children.Add(boxesPanel);
                    orbitalPanel.Children.Add(subPanel);
                }
            }
        }

        private void DrawBohrModel(int numLayers, int[] layers, string symbol, int z)
        {
            if (bohrCanvas == null) return;
            bohrCanvas.Children.Clear();

            if (numLayers > 0)
            {
                if (cardBohr != null) cardBohr.Visibility = Visibility.Visible;

                // Center coordinates
                double x0 = 150;
                double y0 = 150;

                // 1. Draw Nucleus
                var nucleusGrid = new Grid { Width = 46, Height = 46 };
                var circle = new Ellipse 
                { 
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4527A0")), 
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1C4E9")), 
                    StrokeThickness = 2 
                };
                nucleusGrid.Children.Add(circle);
                
                var text = new TextBlock 
                { 
                    Text = $"{symbol}\n{z}+", 
                    FontSize = 11, 
                    FontWeight = FontWeights.Bold, 
                    Foreground = Brushes.White, 
                    TextAlignment = TextAlignment.Center, 
                    VerticalAlignment = VerticalAlignment.Center, 
                    HorizontalAlignment = HorizontalAlignment.Center 
                };
                nucleusGrid.Children.Add(text);
                Canvas.SetLeft(nucleusGrid, x0 - 23);
                Canvas.SetTop(nucleusGrid, y0 - 23);
                bohrCanvas.Children.Add(nucleusGrid);

                // 2. Draw orbits and electron dots
                for (int i = 1; i <= numLayers; i++)
                {
                    double r = 32 + i * 15; // Radial distance

                    // Orbit ellipse
                    var orbit = new Ellipse
                    {
                        Width = r * 2,
                        Height = r * 2,
                        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B39DDB")),
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection(new double[] { 3, 3 })
                    };
                    Canvas.SetLeft(orbit, x0 - r);
                    Canvas.SetTop(orbit, y0 - r);
                    bohrCanvas.Children.Add(orbit);

                    // Electron dots
                    int eCount = layers[i];
                    for (int j = 0; j < eCount; j++)
                    {
                        double angle = j * (2 * System.Math.PI / eCount);
                        double ex = x0 + r * System.Math.Cos(angle) - 4.5;
                        double ey = y0 + r * System.Math.Sin(angle) - 4.5;

                        var electron = new Ellipse
                        {
                            Width = 9,
                            Height = 9,
                            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100")),
                            Stroke = Brushes.White,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(electron, ex);
                        Canvas.SetTop(electron, ey);
                        bohrCanvas.Children.Add(electron);
                    }
                }
            }
        }

        private static string GetAbbreviatedConfigString(int z, List<(string Label, int Count)> sortedConfig)
        {
            var nobles = new (int Z, string Sym)[] 
            { 
                (2, "He"), (10, "Ne"), (18, "Ar"), (36, "Kr"), (54, "Xe"), (86, "Rn") 
            };
            
            string coreSym = "";
            int coreZ = 0;
            foreach (var (nz, sym) in nobles)
            {
                if (nz < z) 
                { 
                    coreZ = nz; 
                    coreSym = sym; 
                }
                else break;
            }

            if (coreZ == 0) return "";

            // Get core sorted configuration
            var coreConfig = new List<(string Label, int Count)>();
            int rem = coreZ;
            foreach (var sub in Subshells)
            {
                if (rem <= 0) break;
                int fill = System.Math.Min(rem, sub.Max);
                coreConfig.Add((sub.Label, fill));
                rem -= fill;
            }
            // Apply exceptions to core config if the core itself has exception (e.g. coreZ = 46 but noble gases don't have exception)
            // Noble gases (2, 10, 18, 36, 54, 86) do not have exceptions, so coreConfig is perfectly standard.

            var coreDict = coreConfig.ToDictionary(c => c.Label, c => c.Count);

            // Subtract core from sortedConfig
            var tail = new List<string>();
            foreach (var c in sortedConfig)
            {
                int coreCount = coreDict.ContainsKey(c.Label) ? coreDict[c.Label] : 0;
                int diff = c.Count - coreCount;
                if (diff > 0)
                {
                    tail.Add($"{c.Label}{ToSuperscript(diff)}");
                }
            }

            return $"[{coreSym}] {string.Join(" ", tail)}";
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            presetPanel.Children.Clear();
            var presets = new (string L, int Z)[]
            {
                ("H (1)", 1), ("C (6)", 6), ("O (8)", 8), ("Na (11)", 11),
                ("Cl (17)", 17), ("Ca (20)", 20), ("Cr (24)", 24), ("Fe (26)", 26),
                ("Cu (29)", 29), ("Br (35)", 35), ("Pd (46)", 46), ("Ag (47)", 47),
            };

            var c = (Color)ColorConverter.ConvertFromString("#4527A0");
            foreach (var (l, zv) in presets)
            {
                int cz = zv;
                UI.PresetButton(l, c, () => { txtZ.Text = cz.ToString(); }, presetPanel);
            }
        }

        // ═══ HELPERS ═══
        private static string ToSuperscript(int n)
        {
            var map = "⁰¹²³⁴⁵⁶⁷⁸⁹";
            return n < 10 ? map[n].ToString() : string.Concat(n.ToString().Select(ch => map[ch - '0']));
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Phát quang Đèn Neon (Z=10)" : "Neon Lamp Luminescence (Z=10)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_1_{suffix}.png",
                        Description = isVN 
                            ? "Khí hiếm Neon (1s² 2s² 2p⁶) có lớp ngoài bão hòa bền vững. Khi có dòng điện kích thích, các electron nhảy lên trạng thái năng lượng cao và giải phóng ánh sáng đỏ-cam đặc trưng khi quay lại trạng thái cơ bản." 
                            : "Neon noble gas (1s² 2s² 2p⁶) has a stable saturated outer shell. Under electric current excitation, electrons jump to higher energy levels and release characteristic red-orange light upon returning to the ground state."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💡",
                        Title = isVN ? "Dây tóc Vonfram (Z=74)" : "Tungsten Filament (Z=74)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_2_{suffix}.png",
                        Description = isVN 
                            ? "Kim loại chuyển tiếp Vonfram (W, [Xe] 4f¹⁴ 5d⁴ 6s²) có liên kết kim loại cực mạnh nhờ sự tham gia của electron hóa trị d và s, giúp nó có nhiệt độ nóng chảy cao nhất (3422°C) dùng làm dây tóc bóng đèn." 
                            : "Tungsten transition metal (W, [Xe] 4f¹⁴ 5d⁴ 6s²) has strong metallic bonding due to d and s valence electrons, giving it the highest melting point (3422°C) used in bulb filaments."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏗",
                        Title = isVN ? "️ Sắt & Luyện Thép (Z=26)" : "Iron & Steel Metallurgy (Z=26)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_3_{suffix}.png",
                        Description = isVN 
                            ? "Sắt (Fe, [Ar] 3d⁶ 4s²) là kim loại d-block có nhiều trạng thái oxy hóa (+2, +3), dễ dàng tạo hợp kim thép cường độ cao vững chắc, làm cốt lõi cho mọi công trình xây dựng lớn." 
                            : "Iron (Fe, [Ar] 3d⁶ 4s²) is a d-block metal with multiple oxidation states (+2, +3), making it easy to create high-strength steel alloys for construction foundations."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔋",
                        Title = isVN ? "Pin sạc Lithium-ion (Z=3)" : "Rechargeable Lithium-ion Battery (Z=3)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_4_{suffix}.png",
                        Description = isVN 
                            ? "Lithium (1s² 2s¹) là kim loại kiềm nhẹ nhất, có duy nhất 1e hóa trị lớp ngoài cùng cực kỳ dễ nhường, đem lại hiệu điện thế và mật độ năng lượng vượt trội cho pin sạc điện thoại, ô tô điện." 
                            : "Lithium (1s² 2s¹) is the lightest alkali metal, having a single outer valence electron that is easily donated, providing exceptional voltage and energy density for smartphones and EVs."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💻",
                        Title = isVN ? "Chip bán dẫn Silic (Z=14)" : "Silicon Semiconductor Chip (Z=14)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_5_{suffix}.png",
                        Description = isVN 
                            ? "Silic (Si, [Ne] 3s² 3p²) là á kim nhóm IVA. Với 4e lớp ngoài cùng, nó liên kết cộng hóa trị tạo mạng tinh thể dạng kim cương, có đặc tính bán dẫn tối ưu làm bộ vi xử lý máy tính." 
                            : "Silicon (Si, [Ne] 3s² 3p²) is a metalloid in group IVA. With 4 outer electrons, it forms covalent bonds in a diamond-like lattice, providing optimal semiconductor properties for computer microprocessors."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👑",
                        Title = isVN ? "Vàng trang sức trơ (Z=79)" : "Inert Jewelry Gold (Z=79)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_6_{suffix}.png",
                        Description = isVN 
                            ? "Vàng (Au, [Xe] 4f¹⁴ 5d¹⁰ 6s¹) có các phân lớp d và f bão hòa hoàn toàn che chắn hạt nhân mạnh, giữ electron ngoài cùng rất chặt. Vàng cực kỳ trơ về mặt hóa học, không bị gỉ sét." 
                            : "Gold (Au, [Xe] 4f¹⁴ 5d¹⁰ 6s¹) has completely saturated d and f subshells shielding the nucleus strongly, holding outer electrons tightly. Gold is extremely chemically inert and does not rust."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧲",
                        Title = isVN ? "Chế tạo nam châm vĩnh cửu" : "Permanent Magnet Fabrication",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_7_{suffix}.png",
                        Description = isVN 
                            ? "Sắp xếp các electron độc thân ở phân lớp d của các nguyên tố đất hiếm để tạo ra nam châm có từ tính cực mạnh." 
                            : "Align unpaired d-subshell electrons in rare earth elements to manufacture ultra-strong permanent magnets."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔋",
                        Title = isVN ? "Phát triển pin Lithium-ion" : "Lithium-ion Battery Development",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_electron_8_{suffix}.png",
                        Description = isVN 
                            ? "Tận dụng cấu hình electron lớp ngoài cùng dễ nhường 1e của Lithium để thiết kế điện cực có mật độ năng lượng cao." 
                            : "Leverage Lithium's outer shell electron structure (easy to lose 1e) to design high-energy density electrodes."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for ElectronConfigTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewSimulator == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewSimulator.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewSimulator.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
