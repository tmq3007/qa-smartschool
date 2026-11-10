using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartTouch.Services.Tools
{
    public enum PhETSubject
    {
        All,
        Math,
        Physics,
        Chemistry,
        Biology
    }

    public enum PhETSchoolLevel
    {
        All,
        THCS,
        THPT
    }

    public class PhETSimulationItem
    {
        public string Subject { get; set; } = "math";
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Topic { get; set; } = "";
        public bool IsTHCS { get; set; }
        public bool IsTHPT { get; set; }

        public string SimUrl => $"https://phet.colorado.edu/sims/html/{Id}/latest/{Id}_all.html?locale=vi";
        public string WebUrl => $"https://phet.colorado.edu/vi/simulation/{Id}";

        public string SubjectName => Subject switch
        {
            "math" => "Toán học",
            "physics" => "Vật lí",
            "chemistry" => "Hóa học",
            "biology" => "Sinh học",
            _ => "Khác"
        };
    }

    public static class PhETCatalogService
    {
        private static readonly List<PhETSimulationItem> _items = new();

        static PhETCatalogService()
        {
            InitializeCatalog();
        }

        public static IReadOnlyList<PhETSimulationItem> GetAll() => _items;

        public static IEnumerable<PhETSimulationItem> Filter(PhETSubject subject, PhETSchoolLevel level, string keyword)
        {
            IEnumerable<PhETSimulationItem> query = _items;

            if (subject != PhETSubject.All)
            {
                string subjKey = subject.ToString().ToLowerInvariant();
                query = query.Where(i => i.Subject.Equals(subjKey, StringComparison.OrdinalIgnoreCase));
            }

            if (level == PhETSchoolLevel.THCS)
            {
                query = query.Where(i => i.IsTHCS);
            }
            else if (level == PhETSchoolLevel.THPT)
            {
                query = query.Where(i => i.IsTHPT);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLowerInvariant();
                query = query.Where(i =>
                    i.Title.ToLowerInvariant().Contains(kw) ||
                    i.Topic.ToLowerInvariant().Contains(kw) ||
                    i.Grade.ToLowerInvariant().Contains(kw) ||
                    i.Id.ToLowerInvariant().Contains(kw)
                );
            }

            return query;
        }

        private static void Add(string subject, string id, string title, string grade, string topic)
        {
            bool thcs = grade.Contains("KHTN") || grade.Contains("3") || grade.Contains("4") || grade.Contains("5") || grade.Contains("6") || grade.Contains("7") || grade.Contains("8") || grade.Contains("9");
            bool thpt = grade.Contains("10") || grade.Contains("11") || grade.Contains("12");
            _items.Add(new PhETSimulationItem
            {
                Subject = subject,
                Id = id,
                Title = title,
                Grade = grade,
                Topic = topic,
                IsTHCS = thcs,
                IsTHPT = thpt
            });
        }

        private static void InitializeCatalog()
        {
            Add("math", "fractions-intro", "Phân số", "Lớp 3–6", "Biểu diễn, so sánh và ý nghĩa phân số");
            Add("math", "equality-explorer", "Khám phá đẳng thức", "Lớp 6–8", "Đẳng thức và giải phương trình");
            Add("math", "graphing-lines", "Đồ thị đường thẳng", "Lớp 8–10", "Hệ số góc, hàm số bậc nhất");
            Add("math", "graphing-quadratics", "Đồ thị hàm bậc hai", "Lớp 9–10", "Parabol và ảnh hưởng của các hệ số");
            Add("math", "trig-tour", "Vòng tròn lượng giác", "Lớp 11", "Góc lượng giác, sin và cos");
            Add("math", "plinko-probability", "Thí nghiệm xác suất", "Lớp 7–11", "Tần suất thực nghiệm và phân bố kết quả");
            Add("physics", "forces-and-motion-basics", "Lực và chuyển động", "KHTN 6–8 · Lí 10", "Lực tổng hợp, ma sát, định luật Newton");
            Add("physics", "energy-skate-park", "Bảo toàn cơ năng", "Lí 10", "Động năng, thế năng, ma sát");
            Add("physics", "wave-on-a-string", "Sóng trên dây", "Lí 11", "Bước sóng, tần số, sóng dừng");
            Add("physics", "circuit-construction-kit-dc", "Lắp mạch điện một chiều", "KHTN · Lí 11", "Dòng điện, điện trở và đo trong mạch");
            Add("physics", "gas-properties", "Tính chất chất khí", "Lí 12", "Mô hình động học, các thông số trạng thái");
            Add("physics", "bending-light", "Khúc xạ ánh sáng", "KHTN 9", "Khúc xạ, phản xạ toàn phần");
            Add("chemistry", "build-an-atom", "Xây dựng nguyên tử", "KHTN 7 · Hóa 10", "Proton, neutron, electron và ion");
            Add("chemistry", "molecule-shapes", "Hình dạng phân tử", "Chuyên đề Hóa 10", "Mô hình hình học phân tử; học liệu bổ trợ");
            Add("chemistry", "balancing-chemical-equations", "Cân bằng phương trình hóa học", "KHTN 8", "Bảo toàn nguyên tử và hệ số phản ứng");
            Add("chemistry", "concentration", "Nồng độ dung dịch", "KHTN 8 · Hóa 11", "Pha loãng, lượng chất tan và nồng độ");
            Add("chemistry", "ph-scale", "Thang pH", "KHTN 8 · Hóa 11", "Acid, base và môi trường dung dịch");
            Add("chemistry", "reactants-products-and-leftovers", "Chất phản ứng và sản phẩm", "KHTN 8 · Hóa 10", "Tỉ lệ phản ứng và chất còn dư");
            Add("biology", "natural-selection", "Chọn lọc tự nhiên", "Sinh 12", "Biến dị, di truyền, tác động môi trường");
            Add("biology", "gene-expression-essentials", "Biểu hiện gene", "Sinh 12", "Phiên mã, dịch mã và tổng hợp protein");
            Add("biology", "neuron", "Tế bào thần kinh", "Sinh 11", "Mô hình điện thế hoạt động và truyền tin");
            Add("biology", "diffusion", "Mô hình khuếch tán", "Sinh 10 · bổ trợ", "Khuếch tán phân tử; không thay thế mô hình màng tế bào");
            Add("math", "function-builder", "Xây dựng hàm số", "Lớp 8–10", "Quy tắc hàm, đầu vào – đầu ra, hàm hợp");
            Add("math", "area-model-algebra", "Mô hình diện tích (đại số)", "Lớp 7–8", "Nhân đa thức, hằng đẳng thức bằng diện tích");
            Add("math", "unit-rates", "Tỉ lệ đơn vị", "Lớp 6–7", "Tỉ số, tỉ lệ thuận, đơn giá");
            Add("physics", "projectile-motion", "Chuyển động ném", "Lí 10", "Ném ngang, ném xiên, tầm xa và độ cao");
            Add("physics", "masses-and-springs", "Con lắc lò xo", "Lí 10 · Lí 11", "Định luật Hooke, dao động của con lắc lò xo");
            Add("physics", "pendulum-lab", "Con lắc đơn", "Lí 11", "Chu kì con lắc đơn, dao động điều hòa");
            Add("physics", "coulombs-law", "Định luật Coulomb", "Lí 11", "Lực tương tác giữa hai điện tích điểm");
            Add("physics", "faradays-law", "Định luật Faraday", "Lí 12", "Cảm ứng điện từ, suất điện động cảm ứng");
            Add("chemistry", "states-of-matter", "Trạng thái của chất", "KHTN 6 · Hóa 10", "Rắn, lỏng, khí; chuyển thể theo nhiệt độ");
            Add("chemistry", "molarity", "Nồng độ mol", "Hóa 10 · Hóa 11", "Số mol chất tan, thể tích, nồng độ mol");
            Add("chemistry", "acid-base-solutions", "Dung dịch acid – base", "Hóa 11", "Acid mạnh/yếu, base, pH và độ điện li");
            Add("math", "area-builder", "Diện tích và chu vi", "Lớp 6", "Diện tích, chu vi hình chữ nhật và hình ghép");
            Add("math", "area-model-multiplication", "Mô hình nhân bằng diện tích", "Lớp 6", "Phép nhân, tính chất phân phối");
            Add("math", "fractions-equality", "Phân số bằng nhau", "Lớp 6", "Tính chất cơ bản của phân số, rút gọn");
            Add("math", "fractions-mixed-numbers", "Phân số và hỗn số", "Lớp 6", "Hỗn số dương, quy đổi phân số");
            Add("math", "number-line-integers", "Số nguyên trên trục số", "Lớp 6", "Số nguyên âm, so sánh, số đối");
            Add("math", "number-line-operations", "Phép tính trên trục số", "Lớp 6–7", "Cộng, trừ số nguyên trên trục số");
            Add("math", "number-line-distance", "Khoảng cách trên trục số", "Lớp 6–7", "Khoảng cách hai điểm, giá trị tuyệt đối");
            Add("math", "ratio-and-proportion", "Tỉ số và tỉ lệ", "Lớp 6–7", "Tỉ số bằng nhau, đại lượng tỉ lệ");
            Add("math", "proportion-playground", "Tỉ lệ thức", "Lớp 7", "Tỉ lệ thức, đại lượng tỉ lệ thuận");
            Add("math", "mean-share-and-balance", "Số trung bình cộng", "Lớp 6–7", "Ý nghĩa số trung bình của dãy số liệu");
            Add("math", "expression-exchange", "Biểu thức đại số", "Lớp 7", "Đơn thức đồng dạng, thu gọn biểu thức");
            Add("math", "quadrilateral", "Tứ giác", "Lớp 8", "Hình bình hành, hình thang, hình chữ nhật, hình thoi");
            Add("math", "graphing-slope-intercept", "Hàm số y = ax + b", "Lớp 8–9", "Hệ số góc, tung độ gốc, đồ thị");
            Add("math", "equality-explorer-two-variables", "Phương trình hai ẩn", "Lớp 9", "Phương trình, hệ phương trình bậc nhất hai ẩn");
            Add("math", "center-and-variability", "Số đặc trưng của mẫu số liệu", "Lớp 9–10", "Trung bình, trung vị, khoảng biến thiên, tứ phân vị");
            Add("math", "vector-addition", "Cộng véc tơ", "Toán 10 · Lí 10", "Tổng, hiệu véc tơ, phân tích thành phần");
            Add("math", "vector-addition-equations", "Tổ hợp véc tơ", "Toán 10", "Tích một số với véc tơ, biểu thị véc tơ");
            Add("math", "least-squares-regression", "Đường thẳng phù hợp dữ liệu", "Toán 10 (bổ trợ)", "Tương quan, hồi quy tuyến tính");
            Add("math", "curve-fitting", "Khớp đường cong", "Toán 11–12 (bổ trợ)", "Mô hình hàm số từ số liệu thực nghiệm");
            Add("math", "calculus-grapher", "Đồ thị đạo hàm và tích phân", "Toán 11–12", "Đạo hàm, nguyên hàm, tích phân trên đồ thị");
            Add("physics", "friction", "Lực ma sát", "KHTN 6", "Ma sát, sự nóng lên khi cọ xát");
            Add("physics", "energy-forms-and-changes", "Các dạng năng lượng", "KHTN 6", "Chuyển hóa và truyền năng lượng");
            Add("physics", "gravity-force-lab-basics", "Lực hấp dẫn (cơ bản)", "KHTN 6", "Khối lượng, khoảng cách và lực hút");
            Add("physics", "density", "Khối lượng riêng", "KHTN 8", "Khối lượng, thể tích, khối lượng riêng");
            Add("physics", "buoyancy", "Lực đẩy Archimedes", "KHTN 8", "Lực đẩy, điều kiện vật nổi, vật chìm");
            Add("physics", "under-pressure", "Áp suất chất lỏng", "KHTN 8", "Áp suất theo độ sâu, áp suất khí quyển");
            Add("physics", "balloons-and-static-electricity", "Nhiễm điện do cọ xát", "KHTN 8", "Điện tích, lực hút và đẩy");
            Add("physics", "john-travoltage", "Hiện tượng phóng điện", "KHTN 8", "Tích điện và phóng điện");
            Add("physics", "sound-waves", "Sóng âm", "KHTN 8 · Lí 11", "Độ cao, độ to của âm");
            Add("physics", "balancing-act", "Cân bằng đòn bẩy", "KHTN 8 · Lí 10", "Moment lực, điều kiện cân bằng");
            Add("physics", "energy-skate-park-basics", "Động năng và thế năng", "KHTN 9", "Cơ năng, chuyển hóa năng lượng");
            Add("physics", "geometric-optics", "Thấu kính", "KHTN 9", "Ảnh qua thấu kính hội tụ, phân kì");
            Add("physics", "color-vision", "Ánh sáng màu", "KHTN 9", "Ánh sáng trắng, màu sắc vật");
            Add("physics", "ohms-law", "Định luật Ohm", "KHTN 9 · Lí 11", "Hiệu điện thế, cường độ, điện trở");
            Add("physics", "resistance-in-a-wire", "Điện trở của dây dẫn", "KHTN 9 · Lí 11", "Phụ thuộc chiều dài, tiết diện, vật liệu");
            Add("physics", "magnets-and-electromagnets", "Nam châm điện", "KHTN 9", "Từ trường của nam châm, ống dây");
            Add("physics", "faradays-electromagnetic-lab", "Cảm ứng điện từ", "KHTN 9 · Lí 12", "Dòng điện cảm ứng, máy phát, máy biến áp");
            Add("physics", "greenhouse-effect", "Hiệu ứng nhà kính", "KHTN 9", "Cân bằng năng lượng Trái Đất, khí nhà kính");
            Add("physics", "gravity-force-lab", "Định luật vạn vật hấp dẫn", "Lí 10", "Lực hấp dẫn giữa hai vật");
            Add("physics", "hookes-law", "Định luật Hooke", "Lí 10", "Lực đàn hồi, độ cứng lò xo");
            Add("physics", "collision-lab", "Va chạm", "Lí 10", "Động lượng, va chạm đàn hồi và mềm");
            Add("physics", "gravity-and-orbits", "Hấp dẫn và quỹ đạo", "Lí 10", "Chuyển động tròn của hành tinh, vệ tinh");
            Add("physics", "keplers-laws", "Các định luật Kepler", "Lí 10 (chuyên đề)", "Quỹ đạo elip, định luật diện tích");
            Add("physics", "my-solar-system", "Hệ Mặt Trời", "Lí 10 (bổ trợ)", "Quỹ đạo nhiều vật dưới lực hấp dẫn");
            Add("physics", "waves-intro", "Sóng cơ và sóng điện từ", "Lí 11", "Biên độ, tần số, bước sóng");
            Add("physics", "wave-interference", "Giao thoa sóng", "Lí 11", "Giao thoa mặt nước, âm, ánh sáng; khe Young");
            Add("physics", "normal-modes", "Dao động của hệ lò xo", "Lí 11 (bổ trợ)", "Các dạng dao động riêng");
            Add("physics", "fourier-making-waves", "Tổng hợp dao động", "Lí 11 (bổ trợ)", "Tổng hợp các dao động điều hòa");
            Add("physics", "charges-and-fields", "Điện trường", "Lí 11", "Điện tích, đường sức, điện thế");
            Add("physics", "capacitor-lab-basics", "Tụ điện", "Lí 11", "Điện dung, năng lượng tụ điện");
            Add("physics", "circuit-construction-kit-ac", "Mạch điện xoay chiều", "Lí 12", "Dòng điện xoay chiều, tụ điện, cuộn cảm");
            Add("physics", "gases-intro", "Chất khí", "Lí 12", "Áp suất, nhiệt độ, thể tích khí");
            Add("physics", "build-a-nucleus", "Cấu tạo hạt nhân", "Lí 12", "Proton, neutron, đồng vị, phóng xạ");
            Add("physics", "rutherford-scattering", "Thí nghiệm Rutherford", "Lí 12 · Hóa 10", "Tán xạ hạt alpha, hạt nhân nguyên tử");
            Add("physics", "models-of-the-hydrogen-atom", "Mẫu nguyên tử hydrogen", "Lí 12 (bổ trợ)", "Mẫu Bohr và quang phổ");
            Add("physics", "blackbody-spectrum", "Bức xạ nhiệt", "Lí 12 (bổ trợ)", "Phổ bức xạ theo nhiệt độ");
            Add("chemistry", "states-of-matter-basics", "Ba thể của chất", "KHTN 6", "Rắn, lỏng, khí và sự chuyển thể");
            Add("chemistry", "build-a-molecule", "Xây dựng phân tử", "KHTN 7", "Đơn chất, hợp chất, công thức phân tử");
            Add("chemistry", "ph-scale-basics", "Thang pH (cơ bản)", "KHTN 8", "Acid, base trong đời sống");
            Add("chemistry", "isotopes-and-atomic-mass", "Đồng vị và nguyên tử khối", "Hóa 10", "Đồng vị, nguyên tử khối trung bình");
            Add("chemistry", "atomic-interactions", "Tương tác giữa các nguyên tử", "Hóa 10", "Lực hút, đẩy và liên kết");
            Add("chemistry", "molecule-polarity", "Phân cực phân tử", "Hóa 10", "Độ âm điện, liên kết phân cực");
            Add("chemistry", "molecule-shapes-basics", "Hình dạng phân tử (cơ bản)", "Hóa 10", "Cặp electron và hình dạng phân tử");
            Add("chemistry", "beers-law-lab", "Nồng độ và hấp thụ ánh sáng", "Hóa 11 (bổ trợ)", "Định luật Beer, màu dung dịch");
            Add("biology", "membrane-transport", "Vận chuyển qua màng", "Sinh 10", "Khuếch tán, kênh, bơm protein");
        }
    }
}
