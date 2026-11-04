using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Speech.Synthesis;

namespace QASmartTouch.Services.Tools
{
    #region Metadata Models

        public class EnglishCardData
    {
        public string Emoji { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
    }

    public class EnglishCardDefinition
    {
        public string Emoji { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
    }
    public class SubjectShapeDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
    }

    public class CategoryDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public List<SubjectShapeDefinition> Shapes { get; set; } = new();
    }

    public class SubjectDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<CategoryDefinition> Categories { get; set; } = new();
    }

    #endregion

    /// <summary>
    /// Service sinh vector hình học chuẩn hóa theo môn học (Toán, Vật lí, Hóa học, Sinh học).
    /// Kế thừa và chuẩn hóa thuật toán vector [0..1] x [0..1] từ NoteBoard Ink.
    /// Sinh ra System.Windows.Media.Geometry (PathGeometry) sắc nét trên màn hình cảm ứng 86" 4K,
    /// tương thích hoàn hảo với QuadTree, SelectionBox, ContextToolbar, Undo/Redo và Export.
    /// </summary>
    public static class SubjectShapeGeometryService
    {
        private static readonly List<SubjectDefinition> _subjects = new();
        private static readonly Dictionary<string, SubjectShapeDefinition> _shapeIndex = new(StringComparer.OrdinalIgnoreCase);

        static SubjectShapeGeometryService()
        {
            InitializeCatalog();
        }

                private static readonly Dictionary<string, List<EnglishCardDefinition>> _englishCards = new(StringComparer.OrdinalIgnoreCase);
        private static SpeechSynthesizer? _speechSynth;
        private static readonly object _speechLock = new();
        private static DateTime _lastSpeakTime = DateTime.MinValue;
        private static string? _lastSpokenWord = null;

        public static IReadOnlyDictionary<string, List<EnglishCardDefinition>> EnglishCards => _englishCards;

        private static void InitSpeechSynth()
        {
            try
            {
                _speechSynth = new SpeechSynthesizer();
                _speechSynth.Rate = 0;
                _speechSynth.Volume = 100;
                var enVoice = _speechSynth.GetInstalledVoices()
                    .FirstOrDefault(v => v.Enabled && v.VoiceInfo.Culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase));
                if (enVoice != null)
                {
                    _speechSynth.SelectVoice(enVoice.VoiceInfo.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Speech] Init exception: {ex.Message}");
            }
        }

        public static void SpeakWord(string? word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;

            lock (_speechLock)
            {
                // Chống gọi đúp liên tiếp trong 350ms (do TouchDown + MouseDown cùng bắn)
                var now = DateTime.UtcNow;
                if ((now - _lastSpeakTime).TotalMilliseconds < 350 && string.Equals(_lastSpokenWord, word, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                _lastSpeakTime = now;
                _lastSpokenWord = word;

                try
                {
                    if (_speechSynth == null)
                    {
                        InitSpeechSynth();
                    }

                    if (_speechSynth != null)
                    {
                        _speechSynth.SpeakAsyncCancelAll();
                        _speechSynth.SpeakAsync(word);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Speech] Speak error: {ex.Message}, recreating synthesizer...");
                    try
                    {
                        _speechSynth?.Dispose();
                        _speechSynth = null;
                        InitSpeechSynth();
                        _speechSynth?.SpeakAsync(word);
                    }
                    catch { }
                }
            }
        }

        public static IReadOnlyList<SubjectDefinition> Subjects => _subjects;

        public static bool IsSubjectShape(string? shapeName)
        {
            if (string.IsNullOrEmpty(shapeName)) return false;
            return _shapeIndex.ContainsKey(shapeName);
        }

        public static SubjectShapeDefinition? GetShapeDefinition(string? shapeName)
        {
            if (string.IsNullOrEmpty(shapeName)) return null;
            _shapeIndex.TryGetValue(shapeName, out var def);
            return def;
        }

        public static Geometry? GenerateGeometry(string shapeName, Rect bounds)
        {
            if (string.IsNullOrEmpty(shapeName) || bounds.Width <= 0 || bounds.Height <= 0)
                return null;

            var builder = new SubjectGeometryBuilder(bounds);
            builder.BuildShape(shapeName);
            return builder.GetGeometry();
        }

        private static void InitializeCatalog()
        {
            // 1. TOÁN HỌC
            var math = new SubjectDefinition { Id = "math", Name = "Toán học", Icon = "📐" };
            
            var mPlane = new CategoryDefinition { Id = "mPlane", Name = "Hình phẳng", SubjectId = "math" };
            AddShape(mPlane, "mLine", "Đường thẳng");
            AddShape(mPlane, "mDashedLine", "Nét đứt");
            AddShape(mPlane, "mTriRight", "Tam giác vuông");
            AddShape(mPlane, "mTriangle", "Tam giác ABC");
            AddShape(mPlane, "mIsosceles", "Tam giác cân");
            AddShape(mPlane, "mEquilateral", "Tam giác đều");
            AddShape(mPlane, "mSquare", "Hình vuông");
            AddShape(mPlane, "mCircle", "Đường tròn (O)");
            AddShape(mPlane, "mAltitude", "Đường cao");
            AddShape(mPlane, "mIncircle", "Đường tròn nội tiếp");
            AddShape(mPlane, "mInscribed", "Tam giác nội tiếp");
            AddShape(mPlane, "mTangent", "Tiếp tuyến");
            AddShape(mPlane, "mSector", "Hình quạt tròn");
            AddShape(mPlane, "mParallelogram", "Hình bình hành");
            AddShape(mPlane, "mTrapezoid", "Hình thang");
            AddShape(mPlane, "mRhombus", "Hình thoi");
            AddShape(mPlane, "mPentagon", "Ngũ giác đều");
            AddShape(mPlane, "mHexagon", "Lục giác đều");
            AddShape(mPlane, "mAngle", "Góc hình học");
            AddShape(mPlane, "mParallel", "Hai đường song song");
            math.Categories.Add(mPlane);

            var mSpace = new CategoryDefinition { Id = "mSpace", Name = "Không gian", SubjectId = "math" };
            AddShape(mSpace, "mCube", "Hình hộp · Lập phương");
            AddShape(mSpace, "mPyramid", "Hình chóp tứ giác");
            AddShape(mSpace, "mTetra", "Hình chóp tam giác");
            AddShape(mSpace, "mPrism", "Lăng trụ tam giác");
            AddShape(mSpace, "mPrism4", "Lăng trụ tứ giác");
            AddShape(mSpace, "mCylinder", "Hình trụ");
            AddShape(mSpace, "mCone", "Hình nón");
            AddShape(mSpace, "mFrustum", "Hình nón cụt");
            AddShape(mSpace, "mSphere", "Mặt cầu");
            AddShape(mSpace, "mPlaneAlpha", "Mặt phẳng (α)");
            math.Categories.Add(mSpace);

            var mGraph = new CategoryDefinition { Id = "mGraph", Name = "Đồ thị", SubjectId = "math" };
            AddShape(mGraph, "axes", "Trục Oxy (Dương)");
            AddShape(mGraph, "axesFull", "Trục Oxy (Đầy đủ)");
            AddShape(mGraph, "mAxes3d", "Hệ trục Oxyz");
            AddShape(mGraph, "mParabola", "Parabol y = ax²");
            AddShape(mGraph, "mCubic", "Đồ thị bậc ba");
            AddShape(mGraph, "mHyperbola", "Đồ thị y = a/x");
            AddShape(mGraph, "mSin", "Đồ thị y = sin x");
            AddShape(mGraph, "mExp", "Đồ thị y = aˣ");
            AddShape(mGraph, "mLog", "Đồ thị logarit");
            AddShape(mGraph, "mUnitCircle", "Đường tròn lượng giác");
            AddShape(mGraph, "mNumberLine", "Trục số");
            AddShape(mGraph, "mGrid", "Lưới tọa độ");
            math.Categories.Add(mGraph);

            var mStat = new CategoryDefinition { Id = "mStat", Name = "Thống kê & Dụng cụ", SubjectId = "math" };
            AddShape(mStat, "mBarChart", "Biểu đồ cột");
            AddShape(mStat, "mPieChart", "Biểu đồ quạt");
            AddShape(mStat, "mLineChart", "Biểu đồ đoạn thẳng");
            AddShape(mStat, "mTree", "Sơ đồ cây");
            AddShape(mStat, "mVenn", "Biểu đồ Venn");
            AddShape(mStat, "mRuler", "Thước thẳng");
            AddShape(mStat, "mSetSquare", "Ê ke");
            AddShape(mStat, "mProtractor", "Thước đo góc");
            AddShape(mStat, "mCompass", "Com-pa");
            math.Categories.Add(mStat);

            _subjects.Add(math);

            // 2. VẬT LÍ
            var physics = new SubjectDefinition { Id = "physics", Name = "Vật lí", Icon = "🧲" };

            var basic = new CategoryDefinition { Id = "basic", Name = "Cơ bản & Lực", SubjectId = "physics" };
            AddShape(basic, "arrow", "Vectơ lực");
            AddShape(basic, "doubleArrow", "Hai đầu mũi tên");
            AddShape(basic, "spring", "Lò xo nằm ngang");
            AddShape(basic, "springMass", "Con lắc lò xo");
            AddShape(basic, "pendulum", "Con lắc đơn");
            AddShape(basic, "incline", "Mặt phẳng nghiêng");
            AddShape(basic, "block", "Khối vật / Hộp");
            AddShape(basic, "pulley", "Ròng rọc");
            physics.Categories.Add(basic);

            var electric = new CategoryDefinition { Id = "electric", Name = "Điện học", SubjectId = "physics" };
            AddShape(electric, "resistor", "Điện trở");
            AddShape(electric, "battery", "Nguồn điện (Pin)");
            AddShape(electric, "capacitor", "Tụ điện");
            AddShape(electric, "bulb", "Bóng đèn");
            AddShape(electric, "switch", "Công tắc");
            AddShape(electric, "coil", "Cuộn cảm");
            AddShape(electric, "diode", "Đi-ốt");
            AddShape(electric, "ground", "Nối đất");
            AddShape(electric, "ammeter", "Ampe kế");
            AddShape(electric, "voltmeter", "Vôn kế");
            AddShape(electric, "galvanometer", "Điện kế G");
            AddShape(electric, "rheostat", "Biến trở");
            AddShape(electric, "acSource", "Nguồn xoay chiều");
            physics.Categories.Add(electric);

            var magnet = new CategoryDefinition { Id = "magnet", Name = "Từ · Nhiệt", SubjectId = "physics" };
            AddShape(magnet, "barMagnet", "Nam châm thẳng");
            AddShape(magnet, "uMagnet", "Nam châm chữ U");
            AddShape(magnet, "solenoid", "Ống dây Solenoid");
            AddShape(magnet, "fieldLines", "Đường sức từ");
            AddShape(magnet, "thermometer", "Nhiệt kế");
            AddShape(magnet, "forceMeter", "Lực kế lò xo");
            physics.Categories.Add(magnet);

            var optics = new CategoryDefinition { Id = "optics", Name = "Quang · Sóng", SubjectId = "physics" };
            AddShape(optics, "convexLens", "Thấu kính hội tụ");
            AddShape(optics, "concaveLens", "Thấu kính phân kì");
            AddShape(optics, "mirror", "Gương phẳng");
            AddShape(optics, "sine", "Sóng hình sin");
            AddShape(optics, "ray", "Tia sáng");
            AddShape(optics, "wavefront", "Mặt sóng");
            physics.Categories.Add(optics);

            _subjects.Add(physics);

            // 3. HÓA HỌC
            var chem = new SubjectDefinition { Id = "chem", Name = "Hóa học", Icon = "🧪" };

            var cWare = new CategoryDefinition { Id = "cWare", Name = "Dụng cụ", SubjectId = "chem" };
            AddShape(cWare, "cTestTube", "Ống nghiệm");
            AddShape(cWare, "cBeaker", "Cốc thủy tinh");
            AddShape(cWare, "cFlask", "Bình tam giác");
            AddShape(cWare, "cRoundFlask", "Bình cầu");
            AddShape(cWare, "cCylinder", "Ống đong");
            AddShape(cWare, "cFunnel", "Phễu thủy tinh");
            AddShape(cWare, "cSepFunnel", "Phễu chiết");
            AddShape(cWare, "cDropper", "Ống nhỏ giọt");
            AddShape(cWare, "cDish", "Chậu thủy tinh");
            AddShape(cWare, "cCrucible", "Chén sứ");
            AddShape(cWare, "cBurette", "Buret");
            AddShape(cWare, "cBurner", "Đèn cồn");
            AddShape(cWare, "cThermo", "Nhiệt kế hóa học");
            chem.Categories.Add(cWare);

            var cSetup = new CategoryDefinition { Id = "cSetup", Name = "Lắp thí nghiệm", SubjectId = "chem" };
            AddShape(cSetup, "cStand", "Giá thí nghiệm");
            AddShape(cSetup, "cTripod", "Kiềng · Lưới thép");
            AddShape(cSetup, "cDelivery", "Ống dẫn khí");
            AddShape(cSetup, "cDistill", "Bộ chưng cất");
            AddShape(cSetup, "cFilter", "Bộ lọc");
            AddShape(cSetup, "cElectrolysis", "Bộ điện phân");
            AddShape(cSetup, "cWaterTrough", "Thu khí qua nước");
            AddShape(cSetup, "cGasUp", "Thu khí đẩy không khí");
            chem.Categories.Add(cSetup);

            var cStruct = new CategoryDefinition { Id = "cStruct", Name = "Cấu tạo chất", SubjectId = "chem" };
            AddShape(cStruct, "cBenzene", "Vòng benzen");
            AddShape(cStruct, "cAtom", "Mô hình nguyên tử");
            AddShape(cStruct, "cBohr", "Lớp electron Bohr");
            AddShape(cStruct, "cOrbital", "Ô lượng tử");
            AddShape(cStruct, "cWater", "Phân tử H₂O");
            AddShape(cStruct, "cMethane", "Phân tử CH₄");
            AddShape(cStruct, "cChain", "Mạch carbon");
            AddShape(cStruct, "cGalvanic", "Pin điện hóa");
            AddShape(cStruct, "cElementCell", "Ô nguyên tố");
            AddShape(cStruct, "cEquilibrium", "Phản ứng thuận nghịch");
            chem.Categories.Add(cStruct);

            _subjects.Add(chem);

            // 4. SINH HỌC
            var bio = new SubjectDefinition { Id = "bio", Name = "Sinh học", Icon = "🧬" };

            var bCell = new CategoryDefinition { Id = "bCell", Name = "Tế bào", SubjectId = "bio" };
            AddShape(bCell, "bAnimalCell", "Tế bào động vật");
            AddShape(bCell, "bPlantCell", "Tế bào thực vật");
            AddShape(bCell, "bMito", "Ti thể");
            AddShape(bCell, "bChloro", "Lục lạp");
            AddShape(bCell, "bNucleus", "Nhân tế bào");
            AddShape(bCell, "bMembrane", "Màng sinh chất");
            AddShape(bCell, "bNeuron", "Tế bào thần kinh (Nơron)");
            AddShape(bCell, "bBacteria", "Vi khuẩn");
            AddShape(bCell, "bVirus", "Virus");
            bio.Categories.Add(bCell);

            var bGene = new CategoryDefinition { Id = "bGene", Name = "Di truyền", SubjectId = "bio" };
            AddShape(bGene, "bDNA", "ADN xoắn kép");
            AddShape(bGene, "bRNA", "ARN");
            AddShape(bGene, "bChromosome", "Nhiễm sắc thể kép");
            AddShape(bGene, "bMitosis", "Nguyên phân");
            AddShape(bGene, "bCross", "Sơ đồ lai");
            AddShape(bGene, "bPedigree", "Sơ đồ phả hệ");
            AddShape(bGene, "bPunnett", "Bảng Punnett");
            bio.Categories.Add(bGene);

            var bBody = new CategoryDefinition { Id = "bBody", Name = "Cơ thể & Sinh thái", SubjectId = "bio" };
            AddShape(bBody, "bHeart", "Tim");
            AddShape(bBody, "bLungs", "Phổi");
            AddShape(bBody, "bEye", "Mắt");
            AddShape(bBody, "bTooth", "Răng");
            AddShape(bBody, "bLeaf", "Lá cây & Gân lá");
            AddShape(bBody, "bFlower", "Cấu tạo hoa");
            AddShape(bBody, "bFoodChain", "Chuỗi thức ăn");
            AddShape(bBody, "bFoodWeb", "Lưới thức ăn");
            AddShape(bBody, "bPyramid", "Tháp sinh thái");
            AddShape(bBody, "bCycle", "Chu trình sinh thái");
            bio.Categories.Add(bBody);

            _subjects.Add(bio);

            // 5. TIẾNG ANH
            var eng = new SubjectDefinition { Id = "eng", Name = "Tiếng Anh", Icon = "🔤" };

            var eAnimals = new CategoryDefinition { Id = "eAnimals", Name = "Con vật", SubjectId = "eng" };
            AddEnglishCategory(eAnimals, new[]
            {
                ("🐱", "cat", "con mèo"), ("🐶", "dog", "con chó"), ("🐦", "bird", "con chim"),
                ("🐟", "fish", "con cá"), ("🐰", "rabbit", "con thỏ"), ("🐘", "elephant", "con voi"),
                ("🦁", "lion", "sư tử"), ("🐯", "tiger", "con hổ"), ("🐒", "monkey", "con khỉ"),
                ("🐄", "cow", "con bò"), ("🐷", "pig", "con lợn"), ("🦆", "duck", "con vịt"),
                ("🐔", "chicken", "con gà"), ("🐴", "horse", "con ngựa"), ("🐻", "bear", "con gấu"),
                ("🐸", "frog", "con ếch"), ("🦋", "butterfly", "con bướm"), ("🦒", "giraffe", "hươu cao cổ")
            });
            eng.Categories.Add(eAnimals);

            var eFood = new CategoryDefinition { Id = "eFood", Name = "Đồ ăn", SubjectId = "eng" };
            AddEnglishCategory(eFood, new[]
            {
                ("🍎", "apple", "quả táo"), ("🍌", "banana", "quả chuối"), ("🍊", "orange", "quả cam"),
                ("🍇", "grapes", "chùm nho"), ("🍉", "watermelon", "dưa hấu"), ("🍓", "strawberry", "dâu tây"),
                ("🥭", "mango", "quả xoài"), ("🥕", "carrot", "cà rốt"), ("🍞", "bread", "bánh mì"),
                ("🥚", "egg", "quả trứng"), ("🥛", "milk", "sữa"), ("🍚", "rice", "cơm"),
                ("🎂", "cake", "bánh kem"), ("🍦", "ice cream", "kem")
            });
            eng.Categories.Add(eFood);

            var eSchool = new CategoryDefinition { Id = "eSchool", Name = "Đồ dùng học tập", SubjectId = "eng" };
            AddEnglishCategory(eSchool, new[]
            {
                ("📖", "book", "quyển sách"), ("✏️", "pencil", "bút chì"), ("🖊️", "pen", "bút bi"),
                ("📏", "ruler", "thước kẻ"), ("✂️", "scissors", "cái kéo"), ("🎒", "bag", "cặp sách"),
                ("🖍️", "crayon", "bút sáp"), ("📒", "notebook", "quyển vở"), ("🖌️", "paintbrush", "bút lông"),
                ("⏰", "clock", "đồng hồ"), ("💻", "computer", "máy tính"), ("🏫", "school", "trường học")
            });
            eng.Categories.Add(eSchool);

            var eToys = new CategoryDefinition { Id = "eToys", Name = "Đồ chơi · Xe cộ", SubjectId = "eng" };
            AddEnglishCategory(eToys, new[]
            {
                ("⚽", "ball", "quả bóng"), ("🧸", "teddy bear", "gấu bông"), ("🪁", "kite", "con diều"),
                ("🎈", "balloon", "bóng bay"), ("🚗", "car", "ô tô"), ("🚲", "bike", "xe đạp"),
                ("🚌", "bus", "xe buýt"), ("🚆", "train", "tàu hỏa"), ("✈️", "plane", "máy bay"),
                ("⛵", "boat", "thuyền"), ("🏠", "house", "ngôi nhà"), ("🪑", "chair", "cái ghế"),
                ("🛏️", "bed", "cái giường"), ("☂️", "umbrella", "cái ô"), ("👟", "shoes", "đôi giày"),
                ("🧢", "cap", "mũ lưỡi trai")
            });
            eng.Categories.Add(eToys);

            var eNature = new CategoryDefinition { Id = "eNature", Name = "Thiên nhiên", SubjectId = "eng" };
            AddEnglishCategory(eNature, new[]
            {
                ("☀️", "sun", "mặt trời"), ("🌙", "moon", "mặt trăng"), ("⭐", "star", "ngôi sao"),
                ("☁️", "cloud", "đám mây"), ("🌧️", "rain", "mưa"), ("🌈", "rainbow", "cầu vồng"),
                ("🌳", "tree", "cái cây"), ("🌸", "flower", "bông hoa"), ("🍃", "leaf", "chiếc lá"),
                ("🌊", "sea", "biển"), ("⛰️", "mountain", "ngọn núi"), ("❄️", "snow", "tuyết")
            });
            eng.Categories.Add(eNature);

            _subjects.Add(eng);

        }

        private static void AddShape(CategoryDefinition cat, string id, string name)
        {
            var shape = new SubjectShapeDefinition
            {
                Id = id,
                Name = name,
                CategoryId = cat.Id,
                SubjectId = cat.SubjectId
            };
            cat.Shapes.Add(shape);
            _shapeIndex[id] = shape;
        }

        private static void AddEnglishCategory(CategoryDefinition cat, IEnumerable<(string Emoji, string Word, string Meaning)> cards)
        {
            var list = new List<EnglishCardDefinition>();
            foreach (var item in cards)
            {
                var card = new EnglishCardDefinition
                {
                    Emoji = item.Emoji,
                    Word = item.Word,
                    Meaning = item.Meaning,
                    CategoryId = cat.Id
                };
                list.Add(card);
            }
            _englishCards[cat.Id] = list;
        }
    }

    /// <summary>
    /// Dựng hình học vector theo tọa độ tương đối chuẩn hóa [0..1] x [0..1]
    /// </summary>
    internal class SubjectGeometryBuilder
    {
        private readonly double _x;
        private readonly double _y;
        private readonly double _w;
        private readonly double _h;
        private readonly double _ar;
        private readonly double _s;
        private readonly double _m;
        private readonly PathGeometry _geometry = new();

        public SubjectGeometryBuilder(Rect bounds)
        {
            _x = bounds.Left;
            _y = bounds.Top;
            _w = Math.Max(1.0, bounds.Width);
            _h = Math.Max(1.0, bounds.Height);
            _ar = _w / _h;
            _s = Math.Max(_w, _h);
            _m = (_w + _h) / 2.0;
        }

        public Geometry GetGeometry() => _geometry;

        private Point B(double u, double v) => new Point(_x + u * _w, _y + v * _h);

        private static double Hypot(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);

        #region Geometry Primitives

        public void Line(double u0, double v0, double u1, double v1)
        {
            LinePx(B(u0, v0), B(u1, v1));
        }

        public void LinePx(Point p0, Point p1)
        {
            var figure = new PathFigure { StartPoint = p0, IsClosed = false, IsFilled = false };
            figure.Segments.Add(new LineSegment(p1, true));
            _geometry.Figures.Add(figure);
        }

        public void Path(params Point[] pts)
        {
            if (pts == null || pts.Length < 2) return;
            var figure = new PathFigure { StartPoint = pts[0], IsClosed = false, IsFilled = false };
            for (int i = 1; i < pts.Length; i++)
            {
                figure.Segments.Add(new LineSegment(pts[i], true));
            }
            _geometry.Figures.Add(figure);
        }

        public void Path(IEnumerable<Point> pts)
        {
            var list = pts.ToList();
            Path(list.ToArray());
        }

        public void PathPx(params Point[] pts) => Path(pts);

        public void Closed(params Point[] pts)
        {
            if (pts == null || pts.Length < 2) return;
            var figure = new PathFigure { StartPoint = pts[0], IsClosed = true, IsFilled = false };
            for (int i = 1; i < pts.Length; i++)
            {
                figure.Segments.Add(new LineSegment(pts[i], true));
            }
            _geometry.Figures.Add(figure);
        }

        public void Closed(IEnumerable<Point> pts)
        {
            var list = pts.ToList();
            Closed(list.ToArray());
        }

        public void ClosedPx(params Point[] pts) => Closed(pts);
        public void ClosedPx(IEnumerable<Point> pts) => Closed(pts);

        public List<Point> ArcPts(double cx, double cy, double rx, double ry, double s, double e, int n = 32)
        {
            var list = new List<Point>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                double t = s + (e - s) * i / n;
                list.Add(B(cx + rx * Math.Cos(t), cy + ry * Math.Sin(t)));
            }
            return list;
        }

        public void Ellipse(double cx, double cy, double rx, double ry, double s = 0, double e = Math.PI * 2, int n = 36)
        {
            var pts = ArcPts(cx, cy, rx, ry, s, e, n);
            if (Math.Abs(e - s) >= Math.PI * 2 - 0.05)
                Closed(pts);
            else
                Path(pts);
        }

        public void Circ(double cx, double cy, double r, double s = 0, double e = Math.PI * 2)
        {
            Ellipse(cx, cy, r, r * _ar, s, e, 36);
        }

        public void CircPx(Point center, double radius, double s = 0, double e = Math.PI * 2, int n = 32)
        {
            var pts = new List<Point>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                double t = s + (e - s) * i / n;
                pts.Add(new Point(center.X + radius * Math.Cos(t), center.Y + radius * Math.Sin(t)));
            }
            if (Math.Abs(e - s) >= Math.PI * 2 - 0.05)
                Closed(pts);
            else
                Path(pts);
        }

        public void Arrow(double u0, double v0, double u1, double v1, bool both = false, bool inward = false, bool mid = false)
        {
            var p0 = B(u0, v0);
            var p1 = B(u1, v1);
            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            double len = Hypot(dx, dy);
            if (len < 1e-4) return;

            double ux = dx / len;
            double uy = dy / len;
            double ang = Math.Atan2(dy, dx);

            double lw = 2.0;
            double pt = 1.66;
            double l0 = 3 * pt + 4.5 * lw;
            double k = Math.Min(1.0, len / ((both || inward ? 2.6 : 1.8) * l0));
            double l = l0 * k;
            double w = 0.75 * l;
            double ins = 0.325 * l;

            if (mid)
            {
                LinePx(p0, p1);
                var midPt = new Point(p0.X + ux * (len / 2 + l / 2), p0.Y + uy * (len / 2 + l / 2));
                DrawTikzHead(midPt, ang, l, w, ins);
            }
            else if (inward)
            {
                LinePx(p0, p1);
                var h1 = new Point(p1.X - ux * l, p1.Y - uy * l);
                var h2 = new Point(p0.X + ux * l, p0.Y + uy * l);
                DrawTikzHead(h1, ang + Math.PI, l, w, ins);
                DrawTikzHead(h2, ang, l, w, ins);
            }
            else
            {
                double back = l - ins;
                var startPt = both ? new Point(p0.X + ux * back, p0.Y + uy * back) : p0;
                var endPt = new Point(p1.X - ux * back, p1.Y - uy * back);
                LinePx(startPt, endPt);
                DrawTikzHead(p1, ang, l, w, ins);
                if (both)
                {
                    DrawTikzHead(p0, ang + Math.PI, l, w, ins);
                }
            }
        }

        private void DrawTikzHead(Point tip, double ang, double l, double w, double ins)
        {
            double c = Math.Cos(ang);
            double s = Math.Sin(ang);
            Point At(double u, double v) => new Point(tip.X + u * c - v * s, tip.Y + u * s + v * c);

            var pTip = tip;
            var pLeft = At(-l, w / 2);
            var pIndent = At(-l + ins, 0);
            var pRight = At(-l, -w / 2);

            Closed(pTip, pLeft, pIndent, pRight);
        }

        public void ArcArrow(double cx, double cy, double rx, double ry, double a0, double a1)
        {
            var pts = ArcPts(cx, cy, rx, ry, a0, a1, 24);
            if (pts.Count < 2) return;
            Path(pts.Take(pts.Count - 1));
            var end = pts[^1];
            var prev = pts[^2];
            double ang = Math.Atan2(end.Y - prev.Y, end.X - prev.X);
            double l = 12.0;
            DrawTikzHead(end, ang, l, l * 0.75, l * 0.325);
        }

        public void Spring(double left, double right, double cy, double amp)
        {
            var pts = new List<Point> { B(left, cy), B(left + (right - left) * 0.12, cy) };
            int coils = 24;
            for (int i = 0; i <= coils; i++)
            {
                double u = left + (right - left) * (0.12 + i * 0.76 / coils);
                double v = cy + (i == 0 || i == coils ? 0 : (i % 2 == 1 ? amp : -amp));
                pts.Add(B(u, v));
            }
            pts.Add(B(right, cy));
            Path(pts);
        }

        public void DashPath(params Point[] pts)
        {
            if (pts == null || pts.Length < 2) return;
            double on = Math.Max(2.5, Math.Min(11.0, _s * 0.035));
            double off = on * 0.8;

            bool draw = true;
            double left = on;
            Point? currentStart = null;

            for (int i = 1; i < pts.Length; i++)
            {
                var a = pts[i - 1];
                var b = pts[i];
                double seg = Hypot(b.X - a.X, b.Y - a.Y);

                while (seg > 1e-6)
                {
                    double t = Math.Min(seg, left);
                    var n = new Point(a.X + (b.X - a.X) * t / seg, a.Y + (b.Y - a.Y) * t / seg);

                    if (draw)
                    {
                        if (currentStart == null) currentStart = a;
                        LinePx(currentStart.Value, n);
                        currentStart = null;
                    }

                    seg -= t;
                    left -= t;
                    a = n;

                    if (left <= 1e-6)
                    {
                        draw = !draw;
                        left = draw ? on : off;
                    }
                }
            }
        }

        public void DashPath(IEnumerable<Point> pts) => DashPath(pts.ToArray());

        public void DashLine(double u0, double v0, double u1, double v1)
        {
            DashPath(B(u0, v0), B(u1, v1));
        }

        public void RightMark(double u, double v, double dx, double dy)
        {
            double k = Math.Max(4.0, Math.Min(16.0, _s * 0.05));
            double au = dx * k / _w;
            double av = dy * k / _h;
            Path(B(u + au, v), B(u + au, v + av), B(u, v + av));
        }

        public void Tick(double u0, double v0, double u1, double v1, int n = 1)
        {
            var a = B(u0, v0);
            var b = B(u1, v1);
            double mx = (a.X + b.X) / 2.0;
            double my = (a.Y + b.Y) / 2.0;
            double d = Math.Max(1e-4, Hypot(b.X - a.X, b.Y - a.Y));
            double ux = (b.X - a.X) / d;
            double uy = (b.Y - a.Y) / d;
            double len = Math.Max(4.0, Math.Min(10.0, _s * 0.035));

            for (int i = 0; i < n; i++)
            {
                double o = (i - (n - 1) / 2.0) * len * 0.7;
                var p1 = new Point(mx + ux * o - uy * len, my + uy * o + ux * len);
                var p2 = new Point(mx + ux * o + uy * len, my + uy * o - ux * len);
                LinePx(p1, p2);
            }
        }

        public void Dot(double u, double v, double k = 1.0)
        {
            DotAt(B(u, v), k);
        }

        public void DotAt(Point c, double k = 1.0)
        {
            double r = Math.Max(1.5, Math.Min(5.5, _s * 0.016)) * k;
            CircPx(c, r, 0, Math.PI * 2, 16);
        }

        public List<Point> Fn(double a, double b, Func<double, double> f, int n = 60)
        {
            var list = new List<Point>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                double t = a + (b - a) * i / n;
                list.Add(B(t, f(t)));
            }
            return list;
        }

        public void Letter(char ch, double cu, double cv, double size)
        {
            double sx = size / _ar;
            switch (char.ToUpperInvariant(ch))
            {
                case 'A':
                    Path(B(cu - 0.35 * sx, cv + 0.5 * size), B(cu, cv - 0.5 * size), B(cu + 0.35 * sx, cv + 0.5 * size));
                    Line(cu - 0.23 * sx, cv + 0.14 * size, cu + 0.23 * sx, cv + 0.14 * size);
                    break;
                case 'V':
                    Path(B(cu - 0.35 * sx, cv - 0.5 * size), B(cu, cv + 0.5 * size), B(cu + 0.35 * sx, cv - 0.5 * size));
                    break;
                case 'N':
                    Path(B(cu - 0.35 * sx, cv + 0.5 * size), B(cu - 0.35 * sx, cv - 0.5 * size), B(cu + 0.35 * sx, cv + 0.5 * size), B(cu + 0.35 * sx, cv - 0.5 * size));
                    break;
                case 'S':
                    Path(
                        B(cu + 0.31 * sx, cv - 0.36 * size), B(cu + 0.17 * sx, cv - 0.48 * size), B(cu - 0.15 * sx, cv - 0.48 * size),
                        B(cu - 0.31 * sx, cv - 0.34 * size), B(cu - 0.29 * sx, cv - 0.14 * size), B(cu, cv),
                        B(cu + 0.29 * sx, cv + 0.14 * size), B(cu + 0.33 * sx, cv + 0.36 * size), B(cu + 0.17 * sx, cv + 0.48 * size),
                        B(cu - 0.15 * sx, cv + 0.48 * size), B(cu - 0.32 * sx, cv + 0.36 * size)
                    );
                    break;
                case 'O':
                    Circ(cu, cv, size * 0.4);
                    break;
                case 'G':
                    Path(ArcPts(cu, cv, size * 0.4 / _ar, size * 0.4, -0.6, -Math.PI * 2 + 0.35, 20));
                    Line(cu + 0.37 * sx, cv + 0.05 * size, cu + 0.09 * sx, cv + 0.05 * size);
                    break;
                case 'C':
                    Path(ArcPts(cu, cv, size * 0.4 / _ar, size * 0.4, -0.65, -Math.PI * 2 + 0.65, 20));
                    break;
                case 'H':
                    Line(cu - 0.35 * sx, cv - 0.5 * size, cu - 0.35 * sx, cv + 0.5 * size);
                    Line(cu + 0.35 * sx, cv - 0.5 * size, cu + 0.35 * sx, cv + 0.5 * size);
                    Line(cu - 0.35 * sx, cv, cu + 0.35 * sx, cv);
                    break;
            }
        }

        public void Meter(char ch)
        {
            double r = 0.24 / _ar;
            Line(0, 0.5, 0.5 - r, 0.5);
            Circ(0.5, 0.5, r);
            Line(0.5 + r, 0.5, 1.0, 0.5);
            Letter(ch, 0.5, 0.5, 0.24);
        }

        public void Thermo(double cx = 0.5)
        {
            Path(B(cx - 0.06, 0.12), B(cx - 0.06, 0.76));
            Path(B(cx + 0.06, 0.12), B(cx + 0.06, 0.76));
            Path(ArcPts(cx, 0.12, 0.06, 0.06 * _ar, Math.PI, Math.PI * 2, 12));
            Circ(cx, 0.85, 0.11);
            Closed(B(cx - 0.025, 0.42), B(cx + 0.025, 0.42), B(cx + 0.025, 0.8), B(cx - 0.025, 0.8));
            for (int i = 0; i < 8; i++)
            {
                Line(cx + 0.06, 0.18 + i * 0.07, cx + (i % 2 == 1 ? 0.1 : 0.13), 0.18 + i * 0.07);
            }
        }

        public void RotEllipse(double cx, double cy, double rx, double ry, double rot, int n = 36)
        {
            var c = B(cx, cy);
            var pts = new List<Point>(n + 1);
            double cosR = Math.Cos(rot);
            double sinR = Math.Sin(rot);
            for (int i = 0; i <= n; i++)
            {
                double t = i * Math.PI * 2 / n;
                double a = rx * _m * Math.Cos(t);
                double b = ry * _m * Math.Sin(t);
                pts.Add(new Point(c.X + a * cosR - b * sinR, c.Y + a * sinR + b * cosR));
            }
            Closed(pts);
        }

        public void RotDot(double cx, double cy, double rx, double ry, double rot, double t)
        {
            var c = B(cx, cy);
            double cosR = Math.Cos(rot);
            double sinR = Math.Sin(rot);
            double a = rx * _m * Math.Cos(t);
            double b = ry * _m * Math.Sin(t);
            DotAt(new Point(c.X + a * cosR - b * sinR, c.Y + a * sinR + b * cosR));
        }

        public List<Point> Blob(double cx, double cy, double rx, double ry, Func<double, double> k, int n = 60)
        {
            var list = new List<Point>(n + 1);
            for (int i = 0; i < n; i++)
            {
                double t = i * Math.PI * 2 / n;
                double r = k(t);
                list.Add(B(cx + rx * r * Math.Cos(t), cy + ry * r * Math.Sin(t)));
            }
            return list;
        }

        public List<Point> RoundRectPts(double x0, double y0, double x1, double y1, double r)
        {
            var pts = new List<Point>();
            pts.AddRange(ArcPts(x1 - r, y0 + r, r, r, -Math.PI / 2, 0, 8));
            pts.AddRange(ArcPts(x1 - r, y1 - r, r, r, 0, Math.PI / 2, 8));
            pts.AddRange(ArcPts(x0 + r, y1 - r, r, r, Math.PI / 2, Math.PI, 8));
            pts.AddRange(ArcPts(x0 + r, y0 + r, r, r, Math.PI, Math.PI * 1.5, 8));
            return pts;
        }

        public void Tube(List<Point> center, double rr)
        {
            double r = rr * _m;
            var l = new List<Point>();
            var rt = new List<Point>();
            for (int i = 0; i < center.Count; i++)
            {
                var a = center[Math.Max(0, i - 1)];
                var b = center[Math.Min(center.Count - 1, i + 1)];
                double d = Math.Max(1e-4, Hypot(b.X - a.X, b.Y - a.Y));
                double nx = -(b.Y - a.Y) / d;
                double ny = (b.X - a.X) / d;
                l.Add(new Point(center[i].X + nx * r, center[i].Y + ny * r));
                rt.Add(new Point(center[i].X - nx * r, center[i].Y - ny * r));
            }
            rt.Reverse();
            var all = new List<Point>(l);
            all.AddRange(rt);
            Closed(all);
        }

        #endregion

        #region Shape Dispatcher

        public void BuildShape(string kind)
        {
            switch (kind)
            {
                // TOÁN: HÌNH PHẲNG
                case "mLine":
                    Line(0.08, 0.5, 0.92, 0.5);
                    break;
                case "mDashedLine":
                    DashLine(0.08, 0.5, 0.92, 0.5);
                    break;
                case "mTriRight":
                    Closed(B(0.12, 0.9), B(0.12, 0.1), B(0.9, 0.9));
                    RightMark(0.12, 0.9, 1, -1);
                    break;
                case "mTriangle":
                    Closed(B(0.5, 0.08), B(0.92, 0.9), B(0.08, 0.9));
                    Dot(0.5, 0.08); Dot(0.92, 0.9); Dot(0.08, 0.9);
                    break;
                case "mIsosceles":
                    Closed(B(0.5, 0.06), B(0.92, 0.92), B(0.08, 0.92));
                    Tick(0.5, 0.06, 0.08, 0.92); Tick(0.5, 0.06, 0.92, 0.92);
                    break;
                case "mEquilateral":
                    Closed(B(0.5, 0.06), B(0.95, 0.9), B(0.05, 0.9));
                    Tick(0.5, 0.06, 0.05, 0.9); Tick(0.5, 0.06, 0.95, 0.9); Tick(0.05, 0.9, 0.95, 0.9);
                    break;
                case "mSquare":
                    Closed(B(0.08, 0.08), B(0.92, 0.08), B(0.92, 0.92), B(0.08, 0.92));
                    DashLine(0.08, 0.08, 0.92, 0.92);
                    DashLine(0.92, 0.08, 0.08, 0.92);
                    RightMark(0.08, 0.92, 1, -1);
                    break;
                case "mCircle":
                    Ellipse(0.5, 0.5, 0.42, 0.42);
                    Dot(0.5, 0.5);
                    Line(0.5, 0.5, 0.864, 0.29);
                    Dot(0.864, 0.29);
                    break;
                case "mAltitude":
                    Closed(B(0.34, 0.08), B(0.95, 0.9), B(0.05, 0.9));
                    DashLine(0.34, 0.08, 0.34, 0.9);
                    RightMark(0.34, 0.9, 1, -1);
                    Dot(0.34, 0.08); Dot(0.05, 0.9); Dot(0.95, 0.9); Dot(0.34, 0.9);
                    break;
                case "mIncircle":
                    {
                        var A = B(0.42, 0.06); var Bp = B(0.04, 0.92); var C = B(0.96, 0.92);
                        Closed(A, Bp, C);
                        double a = Hypot(C.X - Bp.X, C.Y - Bp.Y);
                        double b = Hypot(A.X - C.X, A.Y - C.Y);
                        double c = Hypot(A.X - Bp.X, A.Y - Bp.Y);
                        double p = Math.Max(1e-4, a + b + c);
                        var I = new Point((a * A.X + b * Bp.X + c * C.X) / p, (a * A.Y + b * Bp.Y + c * C.Y) / p);
                        double s2 = p / 2.0;
                        double area = Math.Sqrt(Math.Max(0, s2 * (s2 - a) * (s2 - b) * (s2 - c)));
                        CircPx(I, area / s2);
                        DotAt(I);
                    }
                    break;
                case "mInscribed":
                    Ellipse(0.5, 0.5, 0.42, 0.42);
                    Closed(B(0.5, 0.08), B(0.864, 0.71), B(0.136, 0.71));
                    Dot(0.5, 0.5);
                    break;
                case "mTangent":
                    {
                        double r = Math.Min(_w, _h) * 0.32;
                        var O = B(0.36, 0.55);
                        double t = -Math.PI / 4;
                        var T = new Point(O.X + r * Math.Cos(t), O.Y + r * Math.Sin(t));
                        double dx = -Math.Sin(t);
                        double dy = Math.Cos(t);
                        double L = r * 1.5;
                        CircPx(O, r);
                        DotAt(O);
                        LinePx(O, T);
                        LinePx(new Point(T.X - dx * L, T.Y - dy * L), new Point(T.X + dx * L, T.Y + dy * L));
                        DotAt(T);
                        double k = Math.Max(4.0, Math.Min(12.0, r * 0.15));
                        double ux = Math.Cos(t + Math.PI);
                        double uy = Math.Sin(t + Math.PI);
                        Path(new Point(T.X + ux * k, T.Y + uy * k), new Point(T.X + ux * k + dx * k, T.Y + uy * k + dy * k), new Point(T.X + dx * k, T.Y + dy * k));
                    }
                    break;
                case "mSector":
                    {
                        var O = B(0.1, 0.9);
                        double r = Math.Min(_w * 0.85, _h * 0.85);
                        Path(new Point(O.X + r, O.Y), O, new Point(O.X + r * Math.Cos(-1.05), O.Y + r * Math.Sin(-1.05)));
                        CircPx(O, r, -1.05, 0, 32);
                        CircPx(O, r * 0.18, -1.05, 0, 10);
                        DotAt(O);
                    }
                    break;
                case "mParallelogram":
                    Closed(B(0.27, 0.12), B(0.95, 0.12), B(0.73, 0.88), B(0.05, 0.88));
                    break;
                case "mTrapezoid":
                    Closed(B(0.3, 0.12), B(0.72, 0.12), B(0.95, 0.88), B(0.05, 0.88));
                    break;
                case "mRhombus":
                    Closed(B(0.5, 0.05), B(0.93, 0.5), B(0.5, 0.95), B(0.07, 0.5));
                    DashLine(0.5, 0.05, 0.5, 0.95);
                    DashLine(0.07, 0.5, 0.93, 0.5);
                    RightMark(0.5, 0.5, 1, -1);
                    break;
                case "mPentagon":
                    {
                        var pts = new List<Point>();
                        for (int i = 0; i < 5; i++)
                        {
                            double ang = -Math.PI / 2 + i * 2 * Math.PI / 5;
                            pts.Add(B(0.5 + 0.46 * Math.Cos(ang), 0.54 + 0.46 * Math.Sin(ang)));
                        }
                        Closed(pts);
                    }
                    break;
                case "mHexagon":
                    {
                        var pts = new List<Point>();
                        for (int i = 0; i < 6; i++)
                        {
                            double ang = i * Math.PI / 3;
                            pts.Add(B(0.5 + 0.46 * Math.Cos(ang), 0.5 + 0.46 * Math.Sin(ang)));
                        }
                        Closed(pts);
                        Dot(0.5, 0.5);
                    }
                    break;
                case "mAngle":
                    Line(0.08, 0.88, 0.96, 0.88);
                    Line(0.08, 0.88, 0.74, 0.1);
                    Path(ArcPts(0.08, 0.88, 0.24, 0.24, -0.87, 0, 16));
                    Dot(0.08, 0.88);
                    break;
                case "mParallel":
                    Line(0.02, 0.3, 0.98, 0.3);
                    Line(0.02, 0.72, 0.98, 0.72);
                    Line(0.22, 0.04, 0.78, 0.98);
                    Path(B(0.84, 0.25), B(0.88, 0.3), B(0.84, 0.35));
                    Path(B(0.84, 0.67), B(0.88, 0.72), B(0.84, 0.77));
                    break;

                // TOÁN: KHÔNG GIAN
                case "mCube":
                    Closed(B(0.06, 0.32), B(0.68, 0.32), B(0.68, 0.94), B(0.06, 0.94));
                    Path(B(0.06, 0.32), B(0.32, 0.06), B(0.94, 0.06), B(0.68, 0.32));
                    Path(B(0.94, 0.06), B(0.94, 0.68), B(0.68, 0.94));
                    DashPath(B(0.32, 0.06), B(0.32, 0.68), B(0.94, 0.68));
                    DashLine(0.06, 0.94, 0.32, 0.68);
                    break;
                case "mPyramid":
                    Path(B(0.05, 0.82), B(0.62, 0.82), B(0.95, 0.62));
                    Path(B(0.05, 0.82), B(0.45, 0.05), B(0.62, 0.82));
                    Line(0.45, 0.05, 0.95, 0.62);
                    DashPath(B(0.05, 0.82), B(0.38, 0.62), B(0.95, 0.62));
                    DashLine(0.45, 0.05, 0.38, 0.62);
                    DashLine(0.05, 0.82, 0.95, 0.62);
                    DashLine(0.62, 0.82, 0.38, 0.62);
                    DashLine(0.45, 0.05, 0.5, 0.72);
                    Dot(0.5, 0.72);
                    break;
                case "mTetra":
                    Path(B(0.08, 0.82), B(0.6, 0.95), B(0.92, 0.7));
                    Path(B(0.08, 0.82), B(0.42, 0.06), B(0.6, 0.95));
                    Line(0.42, 0.06, 0.92, 0.7);
                    DashLine(0.08, 0.82, 0.92, 0.7);
                    break;
                case "mPrism":
                    Closed(B(0.06, 0.16), B(0.52, 0.26), B(0.82, 0.1));
                    Path(B(0.06, 0.86), B(0.52, 0.96), B(0.82, 0.8));
                    Line(0.06, 0.16, 0.06, 0.86);
                    Line(0.52, 0.26, 0.52, 0.96);
                    Line(0.82, 0.1, 0.82, 0.8);
                    DashLine(0.06, 0.86, 0.82, 0.8);
                    break;
                case "mPrism4":
                    Closed(B(0.06, 0.3), B(0.62, 0.3), B(0.62, 0.94), B(0.06, 0.94));
                    Path(B(0.06, 0.3), B(0.38, 0.06), B(0.94, 0.06), B(0.62, 0.3));
                    Path(B(0.94, 0.06), B(0.94, 0.7), B(0.62, 0.94));
                    DashPath(B(0.38, 0.06), B(0.38, 0.7), B(0.94, 0.7));
                    DashLine(0.06, 0.94, 0.38, 0.7);
                    DashLine(0.06, 0.94, 0.94, 0.7);
                    break;
                case "mCylinder":
                    Ellipse(0.5, 0.14, 0.4, 0.1);
                    Line(0.1, 0.14, 0.1, 0.86);
                    Line(0.9, 0.14, 0.9, 0.86);
                    Path(ArcPts(0.5, 0.86, 0.4, 0.1, 0, Math.PI, 32));
                    DashPath(ArcPts(0.5, 0.86, 0.4, 0.1, Math.PI, Math.PI * 2, 32));
                    break;
                case "mCone":
                    Path(B(0.08, 0.84), B(0.5, 0.06), B(0.92, 0.84));
                    Path(ArcPts(0.5, 0.84, 0.42, 0.11, 0, Math.PI, 32));
                    DashPath(ArcPts(0.5, 0.84, 0.42, 0.11, Math.PI, Math.PI * 2, 32));
                    DashLine(0.5, 0.06, 0.5, 0.84);
                    DashLine(0.5, 0.84, 0.92, 0.84);
                    Dot(0.5, 0.84);
                    break;
                case "mFrustum":
                    Ellipse(0.5, 0.14, 0.24, 0.07);
                    Line(0.26, 0.14, 0.08, 0.84);
                    Line(0.74, 0.14, 0.92, 0.84);
                    Path(ArcPts(0.5, 0.84, 0.42, 0.1, 0, Math.PI, 32));
                    DashPath(ArcPts(0.5, 0.84, 0.42, 0.1, Math.PI, Math.PI * 2, 32));
                    DashLine(0.5, 0.14, 0.5, 0.84);
                    break;
                case "mSphere":
                    Ellipse(0.5, 0.5, 0.44, 0.44);
                    Path(ArcPts(0.5, 0.5, 0.44, 0.12, 0, Math.PI, 32));
                    DashPath(ArcPts(0.5, 0.5, 0.44, 0.12, Math.PI, Math.PI * 2, 32));
                    Dot(0.5, 0.5);
                    break;
                case "mPlaneAlpha":
                    Closed(B(0.24, 0.12), B(0.98, 0.12), B(0.76, 0.88), B(0.02, 0.88));
                    break;

                // TOÁN: ĐỒ THỊ
                case "axes":
                case "axesFull":
                    {
                        bool full = kind == "axesFull";
                        double ox = full ? 0.5 : 0.12;
                        double oy = full ? 0.5 : 0.86;
                        Arrow(full ? 0.02 : ox, oy, 0.98, oy);
                        Arrow(ox, full ? 0.98 : oy, ox, 0.02);
                        Letter('O', ox - 0.045 / _ar, oy + 0.055, 0.055);
                        Letter('X', 0.94, oy + 0.035, 0.045);
                        Letter('Y', ox + 0.035 / _ar, 0.04, 0.045);
                        int[] nums = full ? new[] { -2, -1, 1, 2 } : new[] { 1, 2 };
                        foreach (var n in nums)
                        {
                            double d = full ? 0.15 : 0.29;
                            double u = ox + n * d;
                            double v = oy - n * d;
                            Line(u, oy - 0.012, u, oy + 0.012);
                            Line(ox - 0.012 / _ar, v, ox + 0.012 / _ar, v);
                        }
                    }
                    break;
                case "mAxes3d":
                    Arrow(0.36, 0.64, 0.36, 0.03);
                    Arrow(0.36, 0.64, 0.98, 0.64);
                    Arrow(0.36, 0.64, 0.05, 0.97);
                    Dot(0.36, 0.64);
                    break;
                case "mParabola":
                    Arrow(0.03, 0.62, 0.98, 0.62);
                    Arrow(0.5, 0.98, 0.5, 0.02);
                    Path(Fn(0.14, 0.86, t => 0.8 - 4.5 * Math.Pow(t - 0.5, 2)));
                    break;
                case "mCubic":
                    Arrow(0.03, 0.5, 0.98, 0.5);
                    Arrow(0.5, 0.98, 0.5, 0.02);
                    Path(Fn(0.1, 0.9, t =>
                    {
                        double u = (t - 0.5) * 3.2;
                        return 0.5 - 0.14 * (u * u * u - 3 * u);
                    }));
                    break;
                case "mHyperbola":
                    Arrow(0.03, 0.5, 0.98, 0.5);
                    Arrow(0.5, 0.98, 0.5, 0.02);
                    Path(Fn(0.55, 0.95, t => 0.5 - 0.07 / (t - 0.5)));
                    Path(Fn(0.05, 0.45, t => 0.5 - 0.07 / (t - 0.5)));
                    break;
                case "mSin":
                    Arrow(0.02, 0.5, 0.98, 0.5);
                    Arrow(0.08, 0.98, 0.08, 0.02);
                    Path(Fn(0.08, 0.94, t => 0.5 - 0.32 * Math.Sin((t - 0.08) / 0.43 * Math.PI)));
                    break;
                case "mExp":
                    Arrow(0.02, 0.86, 0.98, 0.86);
                    Arrow(0.42, 0.98, 0.42, 0.02);
                    Path(Fn(0.04, 0.82, t => 0.86 - 0.12 * Math.Pow(2.6, (t - 0.42) * 5)));
                    Dot(0.42, 0.74);
                    break;
                case "mLog":
                    Arrow(0.02, 0.62, 0.98, 0.62);
                    Arrow(0.14, 0.98, 0.14, 0.02);
                    Path(Fn(0.16, 0.96, t => 0.62 - 0.2 * Math.Log(Math.Max(1e-4, (t - 0.14) / 0.2))));
                    Dot(0.34, 0.62);
                    break;
                case "mUnitCircle":
                    {
                        Arrow(0.02, 0.5, 0.98, 0.5);
                        Arrow(0.5, 0.98, 0.5, 0.02);
                        Circ(0.5, 0.5, 0.36 / _ar);
                        double t = -0.9;
                        var P = new Point(0.5 + 0.36 / _ar * Math.Cos(t), 0.5 + 0.36 * Math.Sin(t));
                        Line(0.5, 0.5, P.X, P.Y);
                        DashLine(P.X, P.Y, P.X, 0.5);
                        DashLine(P.X, P.Y, 0.5, P.Y);
                        Dot(P.X, P.Y);
                        Path(ArcPts(0.5, 0.5, 0.08 / _ar, 0.08, t, 0, 10));
                    }
                    break;
                case "mNumberLine":
                    Arrow(0.02, 0.55, 0.98, 0.55);
                    for (int i = 1; i < 10; i++) Line(i / 10.0, 0.45, i / 10.0, 0.65);
                    Dot(0.5, 0.55, 1.2);
                    break;
                case "mGrid":
                    for (int i = 1; i < 10; i++)
                    {
                        DashLine(i / 10.0, 0.02, i / 10.0, 0.98);
                        DashLine(0.02, i / 10.0, 0.98, i / 10.0);
                    }
                    Arrow(0.02, 0.5, 0.99, 0.5);
                    Arrow(0.5, 0.98, 0.5, 0.01);
                    break;

                // TOÁN: THỐNG KÊ & DỤNG CỤ
                case "mBarChart":
                    Arrow(0.08, 0.94, 0.98, 0.94);
                    Arrow(0.08, 0.94, 0.08, 0.02);
                    double[] barH = { 0.55, 0.32, 0.7, 0.45 };
                    for (int i = 0; i < barH.Length; i++)
                    {
                        Closed(B(0.17 + i * 0.2, 0.94), B(0.17 + i * 0.2, 0.94 - barH[i]), B(0.31 + i * 0.2, 0.94 - barH[i]), B(0.31 + i * 0.2, 0.94));
                    }
                    break;
                case "mPieChart":
                    Circ(0.5, 0.5, 0.44 / _ar);
                    foreach (double ang in new[] { -Math.PI / 2, 0.35, 2.2 })
                    {
                        Line(0.5, 0.5, 0.5 + 0.44 / _ar * Math.Cos(ang), 0.5 + 0.44 * Math.Sin(ang));
                    }
                    break;
                case "mLineChart":
                    Arrow(0.08, 0.94, 0.98, 0.94);
                    Arrow(0.08, 0.94, 0.08, 0.02);
                    var linePts = new[] { B(0.18, 0.7), B(0.36, 0.45), B(0.54, 0.55), B(0.72, 0.25), B(0.9, 0.35) };
                    Path(linePts);
                    foreach (var p in linePts) DotAt(p);
                    break;
                case "mTree":
                    var root = B(0.06, 0.5);
                    var l1 = new[] { B(0.45, 0.25), B(0.45, 0.75) };
                    var l2 = new[] { B(0.9, 0.12), B(0.9, 0.38), B(0.9, 0.62), B(0.9, 0.88) };
                    DotAt(root);
                    for (int i = 0; i < l1.Length; i++)
                    {
                        LinePx(root, l1[i]);
                        DotAt(l1[i]);
                        LinePx(l1[i], l2[i * 2]);
                        DotAt(l2[i * 2]);
                        LinePx(l1[i], l2[i * 2 + 1]);
                        DotAt(l2[i * 2 + 1]);
                    }
                    break;
                case "mVenn":
                    Circ(0.38, 0.5, 0.28 / _ar);
                    Circ(0.62, 0.5, 0.28 / _ar);
                    break;
                case "mRuler":
                    Closed(B(0.02, 0.32), B(0.98, 0.32), B(0.98, 0.72), B(0.02, 0.72));
                    for (int i = 1; i <= 20; i++)
                    {
                        double fx = 0.02 + i * 0.046;
                        Line(fx, 0.32, fx, i % 5 == 0 ? 0.5 : 0.42);
                    }
                    break;
                case "mSetSquare":
                    Closed(B(0.05, 0.95), B(0.05, 0.05), B(0.95, 0.95));
                    Closed(B(0.18, 0.82), B(0.18, 0.4), B(0.6, 0.82));
                    break;
                case "mProtractor":
                    Path(ArcPts(0.5, 0.92, 0.47, 0.84, Math.PI, Math.PI * 2, 48));
                    Path(ArcPts(0.5, 0.92, 0.3, 0.54, Math.PI, Math.PI * 2, 32));
                    Line(0.03, 0.92, 0.97, 0.92);
                    for (int k = 1; k < 18; k++)
                    {
                        double t = Math.PI + k * Math.PI / 18.0;
                        double r = k % 3 == 0 ? 0.09 : 0.04;
                        Line(0.5 + 0.47 * Math.Cos(t), 0.92 + 0.84 * Math.Sin(t), 0.5 + (0.47 - r) * Math.Cos(t), 0.92 + (0.84 - r * 1.8) * Math.Sin(t));
                    }
                    Dot(0.5, 0.92);
                    break;
                case "mCompass":
                    Ellipse(0.5, 0.08, 0.05, 0.05);
                    Line(0.5, 0, 0.5, 0.03);
                    Line(0.47, 0.12, 0.2, 0.86);
                    Line(0.53, 0.12, 0.76, 0.8);
                    Line(0.2, 0.86, 0.2, 0.98);
                    Path(B(0.76, 0.8), B(0.82, 0.98), B(0.72, 0.86), B(0.76, 0.8));
                    break;

                // VẬT LÍ: CƠ BẢN & LỰC
                case "arrow":
                    Arrow(0, 0.5, 1.0, 0.5);
                    break;
                case "doubleArrow":
                    Arrow(0, 0.5, 1.0, 0.5, both: true);
                    break;
                case "spring":
                    Spring(0, 1.0, 0.5, 0.3);
                    break;
                case "pendulum":
                    Line(0.1, 0.04, 0.7, 0.04);
                    for (int i = 0; i < 6; i++) Line(0.12 + i * 0.1, 0.04, 0.19 + i * 0.1, 0);
                    Line(0.4, 0.04, 0.73, 0.74);
                    Ellipse(0.77, 0.83, 0.11, 0.14);
                    break;
                case "incline":
                    Closed(B(0.03, 0.92), B(0.95, 0.92), B(0.95, 0.08));
                    for (int i = 0; i < 9; i++) Line(0.08 + i * 0.1, 0.92, 0.03 + i * 0.1, 1.0);
                    break;
                case "block":
                    Closed(B(0.05, 0.28), B(0.7, 0.28), B(0.7, 0.94), B(0.05, 0.94));
                    Path(B(0.05, 0.28), B(0.29, 0.05), B(0.96, 0.05), B(0.7, 0.28));
                    Path(B(0.96, 0.05), B(0.96, 0.7), B(0.7, 0.94));
                    break;
                case "pulley":
                    Ellipse(0.5, 0.34, 0.23, 0.23);
                    Ellipse(0.5, 0.34, 0.025, 0.025);
                    Line(0.5, 0, 0.5, 0.11);
                    Line(0.27, 0.34, 0.27, 0.96);
                    Line(0.73, 0.34, 0.73, 0.96);
                    break;
                case "springMass":
                    Line(0.05, 0.08, 0.05, 0.92);
                    for (int i = 0; i < 7; i++) Line(0.05, 0.12 + i * 0.11, 0, 0.17 + i * 0.11);
                    Spring(0.05, 0.7, 0.5, 0.17);
                    Closed(B(0.7, 0.27), B(0.98, 0.27), B(0.98, 0.73), B(0.7, 0.73));
                    Line(0, 0.78, 1.0, 0.78);
                    break;

                // VẬT LÍ: ĐIỆN HỌC
                case "resistor":
                    Line(0, 0.5, 0.24, 0.5);
                    Closed(B(0.24, 0.25), B(0.76, 0.25), B(0.76, 0.75), B(0.24, 0.75));
                    Line(0.76, 0.5, 1.0, 0.5);
                    break;
                case "battery":
                    Line(0, 0.5, 0.4, 0.5);
                    Line(0.4, 0.08, 0.4, 0.92);
                    Line(0.6, 0.27, 0.6, 0.73);
                    Line(0.6, 0.5, 1.0, 0.5);
                    Line(0.22, 0.08, 0.32, 0.08);
                    Line(0.27, 0.01, 0.27, 0.15);
                    break;
                case "capacitor":
                    Line(0, 0.5, 0.43, 0.5);
                    Line(0.43, 0.12, 0.43, 0.88);
                    Line(0.57, 0.12, 0.57, 0.88);
                    Line(0.57, 0.5, 1.0, 0.5);
                    break;
                case "bulb":
                    Line(0, 0.5, 0.27, 0.5);
                    Ellipse(0.5, 0.5, 0.23, 0.35);
                    Line(0.34, 0.25, 0.66, 0.75);
                    Line(0.34, 0.75, 0.66, 0.25);
                    Line(0.73, 0.5, 1.0, 0.5);
                    break;
                case "switch":
                    Line(0, 0.65, 0.3, 0.65);
                    Ellipse(0.3, 0.65, 0.025, 0.04);
                    Line(0.32, 0.61, 0.7, 0.13);
                    Ellipse(0.74, 0.65, 0.025, 0.04);
                    Line(0.76, 0.65, 1.0, 0.65);
                    break;
                case "coil":
                    Line(0, 0.7, 0.16, 0.7);
                    for (int i = 0; i < 4; i++)
                    {
                        Path(ArcPts(0.245 + i * 0.17, 0.7, 0.085, 0.42, Math.PI, Math.PI * 2, 20));
                    }
                    Line(0.84, 0.7, 1.0, 0.7);
                    break;
                case "diode":
                    Line(0, 0.5, 0.33, 0.5);
                    Closed(B(0.33, 0.15), B(0.33, 0.85), B(0.7, 0.5));
                    Line(0.7, 0.12, 0.7, 0.88);
                    Line(0.7, 0.5, 1.0, 0.5);
                    break;
                case "ground":
                    Line(0.5, 0, 0.5, 0.54);
                    Line(0.1, 0.54, 0.9, 0.54);
                    Line(0.25, 0.73, 0.75, 0.73);
                    Line(0.4, 0.92, 0.6, 0.92);
                    break;
                case "ammeter":
                    Meter('A');
                    break;
                case "voltmeter":
                    Meter('V');
                    break;
                case "galvanometer":
                    Meter('G');
                    break;
                case "rheostat":
                    Line(0, 0.6, 0.2, 0.6);
                    Closed(B(0.2, 0.45), B(0.8, 0.45), B(0.8, 0.75), B(0.2, 0.75));
                    Line(0.8, 0.6, 1.0, 0.6);
                    Arrow(0.28, 0.95, 0.74, 0.2);
                    break;
                case "acSource":
                    {
                        double r = 0.24 / _ar;
                        Line(0, 0.5, 0.5 - r, 0.5);
                        Circ(0.5, 0.5, r);
                        Line(0.5 + r, 0.5, 1.0, 0.5);
                        Path(Fn(0.5 - 0.15 / _ar, 0.5 + 0.15 / _ar, t => 0.5 - 0.09 * Math.Sin((t - 0.5) / (0.3 / _ar) * Math.PI * 2), 24));
                    }
                    break;

                // VẬT LÍ: TỪ · NHIỆT
                case "barMagnet":
                    Closed(B(0.04, 0.3), B(0.96, 0.3), B(0.96, 0.7), B(0.04, 0.7));
                    Line(0.5, 0.3, 0.5, 0.7);
                    Letter('N', 0.27, 0.5, 0.22);
                    Letter('S', 0.73, 0.5, 0.22);
                    break;
                case "uMagnet":
                    {
                        var pts = new List<Point> { B(0.08, 0.04), B(0.08, 0.6) };
                        pts.AddRange(ArcPts(0.5, 0.6, 0.42, 0.36, Math.PI, 0, 24).Skip(1));
                        pts.Add(B(0.92, 0.04));
                        pts.Add(B(0.66, 0.04));
                        pts.Add(B(0.66, 0.6));
                        pts.AddRange(ArcPts(0.5, 0.6, 0.16, 0.14, 0, Math.PI, 16).Skip(1));
                        pts.Add(B(0.34, 0.04));
                        Closed(pts);
                        Line(0.08, 0.24, 0.34, 0.24);
                        Line(0.66, 0.24, 0.92, 0.24);
                        Letter('N', 0.21, 0.13, 0.12);
                        Letter('S', 0.79, 0.13, 0.12);
                    }
                    break;
                case "solenoid":
                    for (int i = 0; i < 7; i++)
                    {
                        Path(ArcPts(0.12 + i * 0.11, 0.5, 0.06, 0.35, Math.PI, 0, 16));
                        DashPath(ArcPts(0.12 + i * 0.11, 0.5, 0.06, 0.35, 0, Math.PI, 16));
                    }
                    Line(0.06, 0.5, 0.12, 0.5);
                    Line(0.78, 0.5, 0.94, 0.5);
                    break;
                case "fieldLines":
                    Closed(B(0.36, 0.44), B(0.64, 0.44), B(0.64, 0.56), B(0.36, 0.56));
                    Line(0.5, 0.44, 0.5, 0.56);
                    Letter('S', 0.43, 0.5, 0.08);
                    Letter('N', 0.57, 0.5, 0.08);
                    for (int k = 1; k <= 3; k++)
                    {
                        double rx = 0.15 + 0.11 * k;
                        double ry = 0.06 + 0.13 * k;
                        ArcArrow(0.5, 0.5, rx, ry, -0.05, -Math.PI / 2);
                        Path(ArcPts(0.5, 0.5, rx, ry, -Math.PI / 2, -Math.PI + 0.05, 16));
                        ArcArrow(0.5, 0.5, rx, ry, 0.05, Math.PI / 2);
                        Path(ArcPts(0.5, 0.5, rx, ry, Math.PI / 2, Math.PI - 0.05, 16));
                    }
                    break;
                case "thermometer":
                    Thermo();
                    break;
                case "forceMeter":
                    Closed(B(0.3, 0.1), B(0.7, 0.1), B(0.7, 0.76), B(0.3, 0.76));
                    Circ(0.5, 0.05, 0.05);
                    var fspring = new List<Point>();
                    for (int i = 0; i < 17; i++)
                    {
                        fspring.Add(B(0.5 + (i % 2 == 1 ? 0.08 : -0.08) * (i > 0 && i < 16 ? 1 : 0), 0.14 + i * 0.025));
                    }
                    Path(fspring);
                    for (int i = 0; i < 9; i++) Line(0.62, 0.18 + i * 0.06, 0.66, 0.18 + i * 0.06);
                    Line(0.5, 0.54, 0.5, 0.86);
                    Path(ArcPts(0.5, 0.9, 0.06, 0.05, -Math.PI / 2, Math.PI, 14));
                    break;

                // VẬT LÍ: QUANG · SÓNG
                case "convexLens":
                    Arrow(0.5, 0.97, 0.5, 0.03, both: true);
                    Line(0, 0.5, 1.0, 0.5);
                    break;
                case "concaveLens":
                    Arrow(0.5, 0.97, 0.5, 0.03, inward: true);
                    Line(0, 0.5, 1.0, 0.5);
                    break;
                case "mirror":
                    Line(0.48, 0.02, 0.48, 0.98);
                    for (int i = 0; i < 9; i++) Line(0.48, 0.06 + i * 0.1, 0.68, i * 0.1);
                    break;
                case "sine":
                    {
                        var pts = new List<Point>();
                        for (int i = 0; i <= 180; i++)
                        {
                            double t = i / 180.0;
                            pts.Add(B(t, 0.5 - 0.38 * Math.Sin(t * Math.PI * 4)));
                        }
                        Path(pts);
                    }
                    break;
                case "ray":
                    Arrow(0, 0.95, 1.0, 0.05, mid: true);
                    break;
                case "wavefront":
                    for (int i = 1; i <= 4; i++)
                    {
                        Path(ArcPts(0, 0.5, 0.23 * i, 0.12 * i, -Math.PI / 2, Math.PI / 2, 24));
                    }
                    break;

                // HÓA HỌC: DỤNG CỤ
                case "cTestTube":
                    {
                        var pts = new List<Point> { B(0.33, 0.04), B(0.38, 0.08), B(0.38, 0.78) };
                        pts.AddRange(ArcPts(0.5, 0.78, 0.12, 0.16, Math.PI, 0, 20));
                        pts.Add(B(0.62, 0.78)); pts.Add(B(0.62, 0.08)); pts.Add(B(0.67, 0.04));
                        Path(pts);
                        Line(0.38, 0.55, 0.62, 0.55);
                    }
                    break;
                case "cBeaker":
                    Path(B(0.06, 0.04), B(0.16, 0.12), B(0.16, 0.9), B(0.2, 0.94), B(0.8, 0.94), B(0.84, 0.9), B(0.84, 0.08));
                    Line(0.84, 0.3, 0.76, 0.3);
                    Line(0.84, 0.5, 0.72, 0.5);
                    Line(0.84, 0.7, 0.76, 0.7);
                    Line(0.16, 0.6, 0.84, 0.6);
                    break;
                case "cFlask":
                    Path(B(0.38, 0.03), B(0.42, 0.06), B(0.42, 0.34), B(0.08, 0.9), B(0.12, 0.95), B(0.88, 0.95), B(0.92, 0.9), B(0.58, 0.34), B(0.58, 0.06), B(0.62, 0.03));
                    Line(0.25, 0.68, 0.75, 0.68);
                    break;
                case "cRoundFlask":
                    {
                        var pts = new List<Point> { B(0.4, 0.03), B(0.43, 0.06), B(0.43, 0.365) };
                        pts.AddRange(ArcPts(0.5, 0.66, 0.38, 0.3, -1.756, -Math.PI * 2 - 1.38, 48));
                        pts.Add(B(0.57, 0.365)); pts.Add(B(0.57, 0.06)); pts.Add(B(0.6, 0.03));
                        Path(pts);
                        Line(0.16, 0.78, 0.84, 0.78);
                    }
                    break;
                case "cCylinder":
                    Path(B(0.3, 0.03), B(0.36, 0.07), B(0.36, 0.86), B(0.64, 0.86), B(0.64, 0.07), B(0.7, 0.03));
                    Closed(B(0.2, 0.86), B(0.8, 0.86), B(0.8, 0.95), B(0.2, 0.95));
                    for (int i = 0; i < 8; i++) Line(0.36, 0.2 + i * 0.08, (i % 2 == 1 ? 0.42 : 0.47), 0.2 + i * 0.08);
                    Line(0.36, 0.62, 0.64, 0.62);
                    break;
                case "cFunnel":
                    Ellipse(0.5, 0.08, 0.42, 0.05);
                    Path(B(0.08, 0.08), B(0.43, 0.56), B(0.43, 0.97));
                    Path(B(0.92, 0.08), B(0.57, 0.56), B(0.57, 0.97));
                    break;
                case "cSepFunnel":
                    Ellipse(0.5, 0.08, 0.38, 0.06);
                    Path(B(0.12, 0.08), B(0.44, 0.6), B(0.44, 0.96));
                    Path(B(0.88, 0.08), B(0.56, 0.6), B(0.56, 0.96));
                    Line(0.38, 0.72, 0.62, 0.72);
                    break;
                case "cDropper":
                    Path(B(0.44, 0.05), B(0.44, 0.75), B(0.48, 0.95), B(0.52, 0.95), B(0.56, 0.75), B(0.56, 0.05));
                    Closed(B(0.4, 0.05), B(0.6, 0.05), B(0.56, 0.16), B(0.44, 0.16));
                    break;
                case "cDish":
                    Path(B(0.05, 0.5), B(0.12, 0.9), B(0.88, 0.9), B(0.95, 0.5));
                    Line(0.05, 0.5, 0.95, 0.5);
                    break;
                case "cCrucible":
                    Closed(B(0.15, 0.2), B(0.85, 0.2), B(0.75, 0.9), B(0.25, 0.9));
                    Ellipse(0.5, 0.2, 0.35, 0.08);
                    break;
                case "cBurette":
                    Line(0.44, 0.02, 0.44, 0.74);
                    Line(0.56, 0.02, 0.56, 0.74);
                    Closed(B(0.42, 0.74), B(0.58, 0.74), B(0.58, 0.82), B(0.42, 0.82));
                    Line(0.58, 0.78, 0.72, 0.78);
                    Path(B(0.46, 0.82), B(0.49, 0.97));
                    Path(B(0.54, 0.82), B(0.51, 0.97));
                    for (int i = 0; i < 9; i++) Line(0.44, 0.08 + i * 0.07, (i % 2 == 1 ? 0.48 : 0.51), 0.08 + i * 0.07);
                    break;
                case "cBurner":
                    Closed(B(0.2, 0.95), B(0.1, 0.72), B(0.26, 0.52), B(0.74, 0.52), B(0.9, 0.72), B(0.8, 0.95));
                    Closed(B(0.42, 0.52), B(0.42, 0.4), B(0.58, 0.4), B(0.58, 0.52));
                    Line(0.5, 0.4, 0.5, 0.32);
                    {
                        var flame = new List<Point> { B(0.5, 0.03), B(0.56, 0.14) };
                        flame.AddRange(ArcPts(0.5, 0.22, 0.07, 0.08, -0.3, Math.PI + 0.3, 16));
                        flame.Add(B(0.44, 0.14));
                        Closed(flame);
                    }
                    break;
                case "cThermo":
                    Thermo(0.5);
                    break;

                // HÓA HỌC: LẮP THÍ NGHIỆM
                case "cStand":
                    Closed(B(0.04, 0.9), B(0.62, 0.9), B(0.62, 0.98), B(0.04, 0.98));
                    Line(0.18, 0.9, 0.18, 0.03);
                    Closed(B(0.15, 0.3), B(0.21, 0.3), B(0.21, 0.38), B(0.15, 0.38));
                    Line(0.21, 0.34, 0.62, 0.34);
                    Path(B(0.62, 0.27), B(0.74, 0.27));
                    Path(B(0.62, 0.41), B(0.74, 0.41));
                    Line(0.62, 0.27, 0.62, 0.41);
                    Closed(B(0.15, 0.58), B(0.21, 0.58), B(0.21, 0.66), B(0.15, 0.66));
                    Line(0.21, 0.62, 0.45, 0.62);
                    Ellipse(0.62, 0.62, 0.17, 0.04);
                    break;
                case "cTripod":
                    Closed(B(0.1, 0.3), B(0.9, 0.3), B(0.9, 0.37), B(0.1, 0.37));
                    for (int i = 1; i < 8; i++) Line(0.1 + i * 0.1, 0.3, 0.1 + i * 0.1, 0.37);
                    Line(0.24, 0.37, 0.1, 0.98);
                    Line(0.76, 0.37, 0.9, 0.98);
                    Line(0.5, 0.37, 0.5, 0.9);
                    break;
                case "cDelivery":
                    Path(B(0.12, 0.95), B(0.12, 0.12), B(0.88, 0.12), B(0.88, 0.82));
                    Path(B(0.2, 0.95), B(0.2, 0.2), B(0.8, 0.2), B(0.8, 0.82));
                    Closed(B(0.06, 0.8), B(0.26, 0.8), B(0.26, 0.95), B(0.06, 0.95));
                    break;
                case "cDistill":
                    Closed(B(0.1, 0.4), B(0.3, 0.4), B(0.3, 0.8), B(0.1, 0.8));
                    Line(0.2, 0.4, 0.2, 0.15);
                    Line(0.2, 0.15, 0.7, 0.45);
                    Line(0.7, 0.45, 0.7, 0.85);
                    Closed(B(0.65, 0.85), B(0.85, 0.85), B(0.8, 0.95), B(0.7, 0.95));
                    break;
                case "cFilter":
                    Closed(B(0.1, 0.9), B(0.9, 0.9), B(0.85, 0.96), B(0.15, 0.96));
                    Line(0.2, 0.9, 0.2, 0.1);
                    Path(B(0.4, 0.2), B(0.7, 0.5), B(0.55, 0.5), B(0.55, 0.8));
                    break;
                case "cElectrolysis":
                    Closed(B(0.1, 0.3), B(0.9, 0.3), B(0.85, 0.95), B(0.15, 0.95));
                    Closed(B(0.25, 0.15), B(0.35, 0.15), B(0.35, 0.8), B(0.25, 0.8));
                    Closed(B(0.65, 0.15), B(0.75, 0.15), B(0.75, 0.8), B(0.65, 0.8));
                    Line(0.3, 0.15, 0.3, 0.05);
                    Line(0.7, 0.15, 0.7, 0.05);
                    Line(0.3, 0.05, 0.7, 0.05);
                    break;
                case "cWaterTrough":
                    Path(B(0.02, 0.42), B(0.02, 0.96), B(0.98, 0.96), B(0.98, 0.42));
                    DashLine(0.02, 0.56, 0.98, 0.56);
                    {
                        var pts = new List<Point> { B(0.56, 0.9), B(0.56, 0.22) };
                        pts.AddRange(ArcPts(0.66, 0.22, 0.1, 0.1, Math.PI, Math.PI * 2, 12));
                        pts.Add(B(0.76, 0.22)); pts.Add(B(0.76, 0.9));
                        Path(pts);
                    }
                    Path(B(0.1, 0.04), B(0.1, 0.86), B(0.6, 0.86));
                    Ellipse(0.66, 0.7, 0.02, 0.02);
                    Ellipse(0.68, 0.55, 0.025, 0.025);
                    Ellipse(0.65, 0.42, 0.02, 0.02);
                    break;
                case "cGasUp":
                    Closed(B(0.35, 0.1), B(0.65, 0.1), B(0.65, 0.8), B(0.35, 0.8));
                    Line(0.5, 0.95, 0.5, 0.25);
                    Arrow(0.5, 0.6, 0.5, 0.3);
                    break;

                // HÓA HỌC: CẤU TẠO CHẤT
                case "cBenzene":
                    {
                        var pts = new List<Point>();
                        for (int k = 0; k < 6; k++)
                        {
                            double ang = k * Math.PI / 3 - Math.PI / 2;
                            pts.Add(B(0.5 + 0.46 * Math.Cos(ang), 0.5 + 0.46 * Math.Sin(ang)));
                        }
                        Closed(pts);
                        Ellipse(0.5, 0.5, 0.27, 0.27);
                    }
                    break;
                case "cAtom":
                    for (int k = 0; k < 3; k++)
                    {
                        RotEllipse(0.5, 0.5, 0.46, 0.16, k * Math.PI / 3);
                    }
                    Dot(0.5, 0.5, 2.2);
                    Dot(0.5 + 0.46, 0.5, 1.1);
                    RotDot(0.5, 0.5, 0.46, 0.16, Math.PI / 3, 1.9);
                    RotDot(0.5, 0.5, 0.46, 0.16, 2 * Math.PI / 3, 4.2);
                    break;
                case "cBohr":
                    Ellipse(0.5, 0.5, 0.08, 0.08);
                    foreach (double r in new[] { 0.22, 0.34, 0.46 }) Ellipse(0.5, 0.5, r, r);
                    Dot(0.72, 0.5); Dot(0.28, 0.5);
                    for (int k = 0; k < 4; k++)
                    {
                        Dot(0.5 + 0.34 * Math.Cos(k * Math.PI / 2 + 0.4), 0.5 + 0.34 * Math.Sin(k * Math.PI / 2 + 0.4));
                    }
                    Dot(0.5, 0.04);
                    break;
                case "cOrbital":
                    for (int i = 0; i < 3; i++)
                    {
                        Closed(B(0.05 + 0.3 * i, 0.3), B(0.35 + 0.3 * i, 0.3), B(0.35 + 0.3 * i, 0.7), B(0.05 + 0.3 * i, 0.7));
                    }
                    Arrow(0.15, 0.64, 0.15, 0.36);
                    Arrow(0.25, 0.36, 0.25, 0.64);
                    Arrow(0.5, 0.64, 0.5, 0.36);
                    break;
                case "cWater":
                    Circ(0.5, 0.36, 0.14 / _ar);
                    Circ(0.17, 0.78, 0.11 / _ar);
                    Circ(0.83, 0.78, 0.11 / _ar);
                    Line(0.39, 0.47, 0.25, 0.69);
                    Line(0.61, 0.47, 0.75, 0.69);
                    Letter('O', 0.5, 0.36, 0.14);
                    Letter('H', 0.17, 0.78, 0.1);
                    Letter('H', 0.83, 0.78, 0.1);
                    break;
                case "cMethane":
                    Circ(0.5, 0.5, 0.14 / _ar);
                    Line(0.5, 0.36, 0.5, 0.12);
                    Line(0.64, 0.5, 0.88, 0.5);
                    Line(0.36, 0.5, 0.12, 0.5);
                    Line(0.5, 0.64, 0.5, 0.88);
                    foreach (var p in new[] { (0.5, 0.08), (0.92, 0.5), (0.08, 0.5), (0.5, 0.92) })
                    {
                        Circ(p.Item1, p.Item2, 0.07 / _ar);
                    }
                    Letter('C', 0.5, 0.5, 0.12);
                    break;
                case "cChain":
                    Path(B(0.04, 0.66), B(0.23, 0.34), B(0.42, 0.66), B(0.61, 0.34), B(0.8, 0.66), B(0.97, 0.38));
                    Line(0.27, 0.38, 0.38, 0.62);
                    break;
                case "cGalvanic":
                    Path(B(0.03, 0.42), B(0.03, 0.95), B(0.4, 0.95), B(0.4, 0.42));
                    Path(B(0.6, 0.42), B(0.6, 0.95), B(0.97, 0.95), B(0.97, 0.42));
                    DashLine(0.03, 0.52, 0.4, 0.52);
                    DashLine(0.6, 0.52, 0.97, 0.52);
                    Closed(B(0.15, 0.3), B(0.21, 0.3), B(0.21, 0.86), B(0.15, 0.86));
                    Closed(B(0.79, 0.3), B(0.85, 0.3), B(0.85, 0.86), B(0.79, 0.86));
                    Path(B(0.3, 0.8), B(0.3, 0.38), B(0.7, 0.38), B(0.7, 0.8));
                    Path(B(0.34, 0.8), B(0.34, 0.43), B(0.66, 0.43), B(0.66, 0.8));
                    Path(B(0.18, 0.3), B(0.18, 0.1), B(0.5 - 0.07 / _ar, 0.1));
                    Circ(0.5, 0.1, 0.07 / _ar);
                    Letter('V', 0.5, 0.1, 0.07);
                    Path(B(0.5 + 0.07 / _ar, 0.1), B(0.82, 0.1), B(0.82, 0.3));
                    break;
                case "cElementCell":
                    Closed(B(0.1, 0.1), B(0.9, 0.1), B(0.9, 0.9), B(0.1, 0.9));
                    Letter('O', 0.5, 0.45, 0.3);
                    Line(0.2, 0.75, 0.8, 0.75);
                    break;
                case "cEquilibrium":
                    Line(0.05, 0.42, 0.95, 0.42);
                    Path(B(0.95, 0.42), B(0.8, 0.3));
                    Line(0.95, 0.58, 0.05, 0.58);
                    Path(B(0.05, 0.58), B(0.2, 0.7));
                    break;

                // SINH HỌC: TẾ BÀO
                case "bAnimalCell":
                    Closed(Blob(0.5, 0.5, 0.46, 0.42, t => 1 + 0.06 * Math.Sin(3 * t) + 0.04 * Math.Cos(5 * t + 1)));
                    Ellipse(0.44, 0.48, 0.16, 0.15);
                    Dot(0.47, 0.46, 1.8);
                    RotEllipse(0.72, 0.3, 0.09, 0.04, 0.5);
                    RotEllipse(0.68, 0.72, 0.08, 0.035, -0.4);
                    foreach (var p in new[] { (0.25, 0.25), (0.3, 0.75), (0.2, 0.55), (0.8, 0.55) }) Dot(p.Item1, p.Item2, 0.8);
                    for (int i = 0; i < 3; i++) Path(ArcPts(0.24, 0.66 + i * 0.05, 0.09, 0.05, -2.6, -0.5, 10));
                    break;
                case "bPlantCell":
                    Closed(RoundRectPts(0.03, 0.05, 0.97, 0.95, 0.08));
                    Closed(RoundRectPts(0.08, 0.11, 0.92, 0.89, 0.06));
                    Ellipse(0.58, 0.55, 0.27, 0.24);
                    Ellipse(0.24, 0.3, 0.1, 0.1);
                    Dot(0.24, 0.3, 1.6);
                    RotEllipse(0.2, 0.72, 0.07, 0.035, 0.4);
                    RotEllipse(0.48, 0.2, 0.07, 0.035, -0.2);
                    RotEllipse(0.82, 0.2, 0.07, 0.035, 0.6);
                    RotEllipse(0.85, 0.82, 0.07, 0.035, -0.5);
                    break;
                case "bMito":
                    Ellipse(0.5, 0.5, 0.46, 0.3);
                    Ellipse(0.5, 0.5, 0.4, 0.24);
                    Path(Fn(0.14, 0.86, t => 0.5 + 0.17 * Math.Sin((t - 0.14) * 26) * Math.Sqrt(Math.Max(0, 1 - Math.Pow((t - 0.5) / 0.38, 2)))));
                    break;
                case "bChloro":
                    Ellipse(0.5, 0.5, 0.46, 0.3);
                    Ellipse(0.5, 0.5, 0.42, 0.26);
                    foreach (double c in new[] { 0.25, 0.5, 0.75 })
                    {
                        for (int i = 0; i < 4; i++) Ellipse(c, 0.4 + i * 0.06, 0.06, 0.022);
                    }
                    Line(0.31, 0.49, 0.44, 0.49);
                    Line(0.56, 0.49, 0.69, 0.49);
                    break;
                case "bNucleus":
                    Ellipse(0.5, 0.5, 0.45, 0.42);
                    Ellipse(0.5, 0.5, 0.4, 0.37);
                    Closed(Blob(0.56, 0.44, 0.12, 0.11, t => 1 + 0.1 * Math.Sin(3 * t), 24));
                    break;
                case "bMembrane":
                    for (int i = 0; i < 12; i++)
                    {
                        double u = 0.06 + i * 0.08;
                        if (i == 5 || i == 6) continue;
                        Circ(u, 0.2, 0.032 / _ar);
                        Line(u - 0.012, 0.235, u - 0.012, 0.44);
                        Line(u + 0.012, 0.235, u + 0.012, 0.44);
                        Circ(u, 0.8, 0.032 / _ar);
                        Line(u - 0.012, 0.765, u - 0.012, 0.56);
                        Line(u + 0.012, 0.765, u + 0.012, 0.56);
                    }
                    Closed(Blob(0.5, 0.5, 0.1, 0.42, t => 1 + 0.08 * Math.Sin(4 * t)));
                    break;
                case "bNeuron":
                    Ellipse(0.2, 0.5, 0.1, 0.13);
                    Dot(0.2, 0.5, 1.6);
                    Line(0.12, 0.4, 0.04, 0.2);
                    Line(0.1, 0.5, 0.01, 0.48);
                    Line(0.13, 0.6, 0.04, 0.82);
                    Line(0.2, 0.37, 0.24, 0.12);
                    Line(0.21, 0.63, 0.27, 0.9);
                    Line(0.3, 0.5, 0.84, 0.5);
                    for (int i = 0; i < 4; i++) Ellipse(0.4 + 0.12 * i, 0.5, 0.045, 0.04);
                    foreach (double e in new[] { 0.32, 0.5, 0.68 })
                    {
                        Line(0.84, 0.5, 0.97, e);
                        Dot(0.97, e, 1.1);
                    }
                    break;
                case "bBacteria":
                    {
                        var pts = new List<Point>();
                        pts.AddRange(ArcPts(0.27, 0.5, 0.17, 0.24, Math.PI / 2, Math.PI * 1.5, 16));
                        pts.AddRange(ArcPts(0.63, 0.5, 0.17, 0.24, -Math.PI / 2, Math.PI / 2, 16));
                        Closed(pts);
                        Path(Fn(0.25, 0.65, t => 0.5 + 0.08 * Math.Sin((t - 0.25) * 32)));
                        Path(Fn(0.8, 0.99, t => 0.5 + 0.07 * Math.Sin((t - 0.8) * 45)));
                    }
                    break;
                case "bVirus":
                    {
                        var pts = new List<Point>();
                        for (int k = 0; k < 6; k++)
                        {
                            double ang = k * Math.PI / 3 + Math.PI / 6;
                            pts.Add(B(0.5 + 0.22 * Math.Cos(ang) / _ar, 0.24 + 0.22 * Math.Sin(ang)));
                        }
                        Closed(pts);
                        Closed(B(0.46, 0.46), B(0.54, 0.46), B(0.54, 0.78), B(0.46, 0.78));
                        for (int i = 1; i < 6; i++) Line(0.46, 0.46 + i * 0.053, 0.54, 0.46 + i * 0.053);
                        Line(0.42, 0.78, 0.58, 0.78);
                        foreach (int s in new[] { -1, 1 })
                        {
                            Path(B(0.5 + s * 0.04, 0.78), B(0.5 + s * 0.2, 0.9), B(0.5 + s * 0.16, 0.99));
                            Path(B(0.5 + s * 0.02, 0.78), B(0.5 + s * 0.1, 0.94), B(0.5 + s * 0.06, 0.99));
                        }
                    }
                    break;

                // SINH HỌC: DI TRUYỀN
                case "bDNA":
                    {
                        Func<double, double> f = t => 0.5 + 0.34 * Math.Sin((t - 0.05) * Math.PI * 2 * 1.6);
                        Path(Fn(0.05, 0.95, f));
                        Path(Fn(0.05, 0.95, t => 1.0 - f(t)));
                        for (int i = 0; i < 12; i++)
                        {
                            double t = 0.08 + i * 0.075;
                            double a = f(t);
                            if (Math.Abs(a - 0.5) > 0.16) Line(t, a, t, 1.0 - a);
                        }
                    }
                    break;
                case "bRNA":
                    {
                        Func<double, double> f = t => 0.62 + 0.08 * Math.Sin((t - 0.04) * Math.PI * 2 * 1.4);
                        Path(Fn(0.04, 0.96, f));
                        for (int i = 0; i < 14; i++)
                        {
                            double t = 0.07 + i * 0.064;
                            Line(t, f(t), t, f(t) - 0.3);
                        }
                    }
                    break;
                case "bChromosome":
                    foreach (int sg in new[] { -1, 1 })
                    {
                        var pts = new List<Point>();
                        for (int i = 0; i <= 24; i++)
                        {
                            double t = i / 24.0;
                            pts.Add(B(0.5 + sg * (0.075 + 0.15 * Math.Pow(2 * t - 1, 2)), 0.06 + 0.88 * t));
                        }
                        Tube(pts, 0.068);
                    }
                    Dot(0.5, 0.5, 1.4);
                    break;
                case "bMitosis":
                    Ellipse(0.5, 0.5, 0.46, 0.44);
                    Dot(0.08, 0.5, 1.3);
                    Dot(0.92, 0.5, 1.3);
                    foreach (double v in new[] { 0.22, 0.36, 0.5, 0.64, 0.78 })
                    {
                        Line(0.08, 0.5, 0.46, v);
                        Line(0.92, 0.5, 0.54, v);
                    }
                    foreach (double v in new[] { 0.3, 0.5, 0.7 })
                    {
                        Path(B(0.46, v - 0.07), B(0.54, v + 0.07));
                        Path(B(0.54, v - 0.07), B(0.46, v + 0.07));
                    }
                    break;
                case "bCross":
                    Circ(0.18, 0.14, 0.1 / _ar);
                    Circ(0.82, 0.14, 0.1 / _ar);
                    Path(B(0.45, 0.08), B(0.55, 0.2));
                    Path(B(0.55, 0.08), B(0.45, 0.2));
                    Arrow(0.5, 0.3, 0.5, 0.5);
                    Line(0.12, 0.56, 0.88, 0.56);
                    foreach (double u in new[] { 0.12, 0.37, 0.63, 0.88 })
                    {
                        Line(u, 0.56, u, 0.66);
                        Circ(u, 0.78, 0.09 / _ar);
                    }
                    break;
                case "bPedigree":
                    Closed(B(0.12, 0.08), B(0.38, 0.08), B(0.38, 0.36), B(0.12, 0.36));
                    Ellipse(0.75, 0.22, 0.13, 0.14);
                    Line(0.38, 0.22, 0.62, 0.22);
                    Line(0.5, 0.22, 0.5, 0.52);
                    Line(0.25, 0.52, 0.75, 0.52);
                    Line(0.25, 0.52, 0.25, 0.64);
                    Line(0.75, 0.52, 0.75, 0.64);
                    Closed(B(0.12, 0.64), B(0.38, 0.64), B(0.38, 0.92), B(0.12, 0.92));
                    Ellipse(0.75, 0.78, 0.13, 0.14);
                    break;
                case "bPunnett":
                    Closed(B(0.02, 0.02), B(0.98, 0.02), B(0.98, 0.98), B(0.02, 0.98));
                    Line(0.34, 0.02, 0.34, 0.98);
                    Line(0.66, 0.02, 0.66, 0.98);
                    Line(0.02, 0.34, 0.98, 0.34);
                    Line(0.02, 0.66, 0.98, 0.66);
                    break;

                // SINH HỌC: CƠ THỂ & SINH THÁI
                case "bHeart":
                    {
                        var pts = new List<Point>();
                        for (int i = 0; i <= 72; i++)
                        {
                            double t = i * Math.PI * 2 / 72.0;
                            double x = 16 * Math.Pow(Math.Sin(t), 3);
                            double y = 13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t);
                            pts.Add(B(0.5 + x / 36.0, 0.5 - y / 34.0));
                        }
                        Closed(pts);
                        Line(0.5, 0.22, 0.5, 0.9);
                        Line(0.18, 0.5, 0.82, 0.5);
                    }
                    break;
                case "bLungs":
                    Line(0.5, 0.02, 0.5, 0.34);
                    Path(B(0.5, 0.34), B(0.36, 0.46), B(0.3, 0.58));
                    Path(B(0.5, 0.34), B(0.64, 0.46), B(0.7, 0.58));
                    Closed(Blob(0.24, 0.6, 0.2, 0.36, t => 1 + 0.06 * Math.Cos(2 * t)));
                    Closed(Blob(0.76, 0.6, 0.2, 0.36, t => 1 + 0.06 * Math.Cos(2 * t)));
                    break;
                case "bEye":
                    Circ(0.46, 0.5, 0.4 / _ar);
                    Path(ArcPts(0.46 + 0.4 / _ar, 0.5, 0.08 / _ar, 0.22, -Math.PI / 2, Math.PI / 2, 14));
                    Ellipse(0.46 + 0.31 / _ar, 0.5, 0.05 / _ar, 0.16);
                    Path(B(0.46 - 0.39 / _ar, 0.44), B(0.02, 0.4));
                    Path(B(0.46 - 0.39 / _ar, 0.56), B(0.02, 0.6));
                    Dot(0.46 - 0.25 / _ar, 0.5, 1.2);
                    break;
                case "bTooth":
                    Closed(B(0.2, 0.38), B(0.16, 0.16), B(0.3, 0.04), B(0.42, 0.12), B(0.5, 0.08), B(0.58, 0.12), B(0.7, 0.04), B(0.84, 0.16), B(0.8, 0.38), B(0.74, 0.62), B(0.66, 0.96), B(0.56, 0.7), B(0.5, 0.64), B(0.44, 0.7), B(0.34, 0.96), B(0.26, 0.62));
                    DashLine(0.18, 0.4, 0.82, 0.4);
                    Path(B(0.38, 0.24), B(0.4, 0.5), B(0.38, 0.82));
                    Path(B(0.62, 0.24), B(0.6, 0.5), B(0.62, 0.82));
                    break;
                case "bLeaf":
                    {
                        var pts = new List<Point>();
                        for (int i = 0; i <= 24; i++)
                        {
                            double s = i / 24.0;
                            double w = 0.24 * Math.Pow(Math.Sin(Math.PI * s), 0.85);
                            pts.Add(B(0.12 + 0.8 * s + 0.707 * w, 0.88 - 0.8 * s + 0.707 * w));
                        }
                        for (int i = 23; i >= 0; i--)
                        {
                            double s = i / 24.0;
                            double w = 0.24 * Math.Pow(Math.Sin(Math.PI * s), 0.85);
                            pts.Add(B(0.12 + 0.8 * s - 0.707 * w, 0.88 - 0.8 * s - 0.707 * w));
                        }
                        Closed(pts);
                        Line(0.03, 0.97, 0.92, 0.08);
                        foreach (double s in new[] { 0.22, 0.38, 0.54, 0.7 })
                        {
                            double w = 0.24 * Math.Pow(Math.Sin(Math.PI * (s + 0.1)), 0.85) * 0.85;
                            Line(0.12 + 0.8 * s, 0.88 - 0.8 * s, 0.12 + 0.8 * (s + 0.1) + 0.707 * w, 0.88 - 0.8 * (s + 0.1) + 0.707 * w);
                            Line(0.12 + 0.8 * s, 0.88 - 0.8 * s, 0.12 + 0.8 * (s + 0.1) - 0.707 * w, 0.88 - 0.8 * (s + 0.1) - 0.707 * w);
                        }
                    }
                    break;
                case "bFlower":
                    for (int k = 0; k < 6; k++)
                    {
                        double t = k * Math.PI / 3;
                        RotEllipse(0.5 + 0.17 * Math.Cos(t), 0.34 + 0.17 * Math.Sin(t) * _ar, 0.13, 0.07, t);
                    }
                    Ellipse(0.5, 0.34, 0.07, 0.07 * _ar);
                    Line(0.5, 0.52, 0.5, 0.98);
                    RotEllipse(0.62, 0.78, 0.1, 0.04, -0.6);
                    break;
                case "bFoodChain":
                    foreach (double c in new[] { 0.12, 0.5, 0.88 }) Ellipse(c, 0.5, 0.1, 0.16);
                    Arrow(0.24, 0.5, 0.37, 0.5);
                    Arrow(0.62, 0.5, 0.75, 0.5);
                    break;
                case "bFoodWeb":
                    {
                        var nodes = new[]
                        {
                            (0.5, 0.1), (0.2, 0.42), (0.8, 0.42),
                            (0.14, 0.84), (0.5, 0.74), (0.86, 0.84)
                        };
                        foreach (var n in nodes) Circ(n.Item1, n.Item2, 0.07 / _ar);
                        var edges = new[] { (3, 1), (4, 1), (4, 2), (5, 2), (1, 0), (2, 0), (4, 0) };
                        foreach (var e in edges)
                        {
                            var p = nodes[e.Item1];
                            var q = nodes[e.Item2];
                            double d = Math.Max(1e-4, Hypot((q.Item1 - p.Item1) * _ar, q.Item2 - p.Item2));
                            double k = 0.08 / d;
                            Arrow(p.Item1 + (q.Item1 - p.Item1) * k, p.Item2 + (q.Item2 - p.Item2) * k,
                                  q.Item1 - (q.Item1 - p.Item1) * k, q.Item2 - (q.Item2 - p.Item2) * k);
                        }
                    }
                    break;
                case "bPyramid":
                    Closed(B(0.5, 0.04), B(0.96, 0.94), B(0.04, 0.94));
                    foreach (double v in new[] { 0.28, 0.5, 0.72 })
                    {
                        double d = (v - 0.04) / 0.9 * 0.46;
                        Line(0.5 - d, v, 0.5 + d, v);
                    }
                    break;
                case "bCycle":
                    for (int k = 0; k < 4; k++)
                    {
                        ArcArrow(0.5, 0.5, 0.38, 0.38, k * Math.PI / 2 + 0.25, k * Math.PI / 2 + Math.PI / 2 - 0.25);
                    }
                    break;

                default:
                    // Fallback to bounding box rectangle
                    Closed(B(0, 0), B(1, 0), B(1, 1), B(0, 1));
                    break;
            }
        }

        #endregion
    }
}
