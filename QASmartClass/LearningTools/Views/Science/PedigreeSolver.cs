using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.LearningTools.Views.Science
{
    public enum InheritanceMode
    {
        AutosomalRecessive,  // Lặn thường
        AutosomalDominant,   // Trội thường
        XLinkedRecessive,    // Lặn liên kết X
        XLinkedDominant,     // Trội liên kết X
        Undetermined         // Chưa xác định
    }

    public class PedigreeMember
    {
        public string Id { get; set; } = "";
        public string Sex { get; set; } = "Nam"; // "Nam" hoặc "Nữ"
        public bool IsAffected { get; set; } // Bị bệnh hay bình thường
        public int Row { get; set; } // Hàng thế hệ (I=1, II=2, III=3...)
        public double Col { get; set; } // Vị trí cột vẽ trên canvas
        public string ContradictionWarning { get; set; } = "";
        
        public PedigreeMember? Father { get; private set; }
        public PedigreeMember? Mother { get; private set; }
        public PedigreeMember? Spouse { get; private set; }
        public List<PedigreeMember> Children { get; } = new();

        public Dictionary<string, double> Genotypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public void SetParents(PedigreeMember? father, PedigreeMember? mother)
        {
            Father = father;
            Mother = mother;
            
            father?.Children.Add(this);
            mother?.Children.Add(this);
        }

        public void SetSpouse(PedigreeMember? spouse)
        {
            Spouse = spouse;
            if (spouse != null && spouse.Spouse != this)
            {
                spouse.SetSpouse(this);
            }
        }
    }

    public class PedigreeTree
    {
        public List<PedigreeMember> Members { get; } = new();

        public PedigreeMember CreateMember(string id, string sex, bool isAffected, int row, double col)
        {
            var existing = FindById(id);
            if (existing != null) return existing;

            var m = new PedigreeMember
            {
                Id = id,
                Sex = sex,
                IsAffected = isAffected,
                Row = row,
                Col = col
            };
            Members.Add(m);
            return m;
        }

        public void Clear()
        {
            Members.Clear();
        }

        public PedigreeMember? FindById(string id)
        {
            return Members.FirstOrDefault(m => m.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasCycle()
        {
            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();

            foreach (var m in Members)
            {
                if (DfsCheckCycle(m, visited, recStack)) return true;
            }
            return false;
        }

        private bool DfsCheckCycle(PedigreeMember m, HashSet<string> visited, HashSet<string> recStack)
        {
            if (recStack.Contains(m.Id)) return true;
            if (visited.Contains(m.Id)) return false;

            visited.Add(m.Id);
            recStack.Add(m.Id);

            var parents = new List<PedigreeMember>();
            if (m.Father != null) parents.Add(m.Father);
            if (m.Mother != null) parents.Add(m.Mother);

            foreach (var p in parents)
            {
                if (DfsCheckCycle(p, visited, recStack)) return true;
            }

            recStack.Remove(m.Id);
            return false;
        }

        public void CheckContradictions(InheritanceMode mode)
        {
            foreach (var m in Members)
            {
                m.ContradictionWarning = "";
            }

            foreach (var m in Members)
            {
                if (m.Father == null || m.Mother == null) continue;

                var f = m.Father;
                var mom = m.Mother;

                if (mode == InheritanceMode.AutosomalRecessive)
                {
                    // Bố bệnh x Mẹ bệnh -> Con phải bệnh
                    if (f.IsAffected && mom.IsAffected && !m.IsAffected)
                    {
                        m.ContradictionWarning = "Mâu thuẫn di truyền: Bố mẹ bị bệnh lặn thường không thể sinh con bình thường.";
                    }
                }
                else if (mode == InheritanceMode.AutosomalDominant)
                {
                    // Bố mẹ lành x Con bệnh -> Mâu thuẫn
                    if (!f.IsAffected && !mom.IsAffected && m.IsAffected)
                    {
                        m.ContradictionWarning = "Mâu thuẫn di truyền: Bố mẹ bình thường không thể sinh con bị bệnh trội thường.";
                    }
                }
                else if (mode == InheritanceMode.XLinkedRecessive)
                {
                    // Mẹ bị bệnh lặn liên kết X -> Con trai phải bị bệnh
                    if (mom.IsAffected && m.Sex == "Nam" && !m.IsAffected)
                    {
                        m.ContradictionWarning = "Mâu thuẫn di truyền: Mẹ bị bệnh lặn liên kết X phải sinh con trai bị bệnh.";
                    }
                    // Con gái bị bệnh lặn liên kết X -> Bố phải bị bệnh
                    if (m.Sex == "Nữ" && m.IsAffected && !f.IsAffected)
                    {
                        m.ContradictionWarning = "Mâu thuẫn di truyền: Con gái bị bệnh lặn liên kết X phải có bố bị bệnh.";
                    }
                }
                else if (mode == InheritanceMode.XLinkedDominant)
                {
                    // Bố bị bệnh trội liên kết X -> Con gái phải bị bệnh
                    if (f.IsAffected && m.Sex == "Nữ" && !m.IsAffected)
                    {
                        m.ContradictionWarning = "Mâu thuẫn di truyền: Bố bị bệnh trội liên kết X phải truyền bệnh cho con gái.";
                    }
                    // Bố mẹ bình thường -> Con phải bình thường
                    if (!f.IsAffected && !mom.IsAffected && m.IsAffected)
                    {
                        m.ContradictionWarning = "Mâu thuẫn di truyền: Bố mẹ bình thường không thể sinh con bị bệnh trội liên kết X.";
                    }
                    // Con trai bị bệnh trội liên kết X -> Mẹ phải bị bệnh
                    if (m.Sex == "Nam" && m.IsAffected && !mom.IsAffected)
                    {
                        m.ContradictionWarning = "Mâu thuẫn di truyền: Con trai bị bệnh trội liên kết X phải có mẹ bị bệnh.";
                    }
                }
            }
        }

        public void DeduceGenotypes(InheritanceMode mode)
        {
            if (mode == InheritanceMode.Undetermined)
            {
                foreach (var m in Members) m.Genotypes.Clear();
                return;
            }

            // Khởi tạo các kiểu gen khả dĩ theo kiểu hình và giới tính
            foreach (var m in Members)
            {
                m.Genotypes = GetInitialGenotypes(m, mode);
            }

            // Chạy vòng lặp truyền niềm tin (belief propagation) đơn giản để hội tụ kiểu gen
            for (int iter = 0; iter < 6; iter++)
            {
                foreach (var m in Members)
                {
                    // Cập nhật từ bố mẹ
                    if (m.Father != null && m.Mother != null)
                    {
                        var fatherGenos = m.Father.Genotypes;
                        var motherGenos = m.Mother.Genotypes;
                        var prior = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                        foreach (var fg in fatherGenos)
                        {
                            foreach (var mg in motherGenos)
                            {
                                double jointProb = fg.Value * mg.Value;
                                if (jointProb <= 0) continue;

                                var offspringGenos = GetOffspringGenotypes(fg.Key, mg.Key, mode);
                                foreach (var og in offspringGenos)
                                {
                                    if (!prior.ContainsKey(og.Key)) prior[og.Key] = 0;
                                    prior[og.Key] += og.Value * jointProb;
                                }
                            }
                        }

                        // Kết hợp với kiểu gen hiện tại của cá thể (phép nhân phân bố xác suất)
                        var updated = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kv in m.Genotypes)
                        {
                            if (prior.TryGetValue(kv.Key, out double priorProb))
                            {
                                double newProb = kv.Value * priorProb;
                                if (newProb > 0.0001) updated[kv.Key] = newProb;
                            }
                        }

                        Normalize(updated);
                        if (updated.Count > 0)
                        {
                            m.Genotypes = updated;
                        }
                    }

                    // Cập nhật ngược từ con cái
                    if (m.Children.Count > 0 && m.Spouse != null)
                    {
                        var spouse = m.Spouse;
                        var updated = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                        foreach (var g_m in m.Genotypes)
                        {
                            double sumChildProb = 0;
                            int validChildrenCount = 0;

                            foreach (var child in m.Children)
                            {
                                // Tìm con chung của m và spouse
                                if (child.Father != m && child.Mother != m) continue;
                                if (child.Father != spouse && child.Mother != spouse) continue;

                                validChildrenCount++;
                                double childMatchProb = 0;

                                string dadGeno = (m.Sex == "Nam") ? g_m.Key : "";
                                string momGeno = (m.Sex == "Nữ") ? g_m.Key : "";

                                foreach (var g_s in spouse.Genotypes)
                                {
                                    string dg = string.IsNullOrEmpty(dadGeno) ? g_s.Key : dadGeno;
                                    string mg = string.IsNullOrEmpty(momGeno) ? g_s.Key : momGeno;

                                    var offspringGenos = GetOffspringGenotypes(dg, mg, mode);
                                    foreach (var cg in child.Genotypes)
                                    {
                                        if (offspringGenos.TryGetValue(cg.Key, out double p))
                                        {
                                            childMatchProb += p * cg.Value * g_s.Value;
                                        }
                                    }
                                }
                                sumChildProb += childMatchProb;
                            }

                            double multiplier = (validChildrenCount > 0) ? (sumChildProb / validChildrenCount) : 1.0;
                            double newProb = g_m.Value * multiplier;
                            if (newProb > 0.0001) updated[g_m.Key] = newProb;
                        }

                        Normalize(updated);
                        if (updated.Count > 0)
                        {
                            m.Genotypes = updated;
                        }
                    }
                }
            }
        }

        public double CalculateOffspringProbability(PedigreeMember dad, PedigreeMember mom, string targetPhenotype, string targetSex, InheritanceMode mode)
        {
            double totalProb = 0;

            foreach (var fg in dad.Genotypes)
            {
                foreach (var mg in mom.Genotypes)
                {
                    double jointProb = fg.Value * mg.Value;
                    if (jointProb <= 0) continue;

                    var offspringGenos = GetOffspringGenotypes(fg.Key, mg.Key, mode);
                    foreach (var og in offspringGenos)
                     {
                        string geno = og.Key;
                        double prob = og.Value;

                        // Xác định kiểu hình của con
                        bool isAffected = IsGenotypeAffected(geno, mode);
                        string pheno = isAffected ? "Bị bệnh" : "Bình thường";

                        if (pheno.Equals(targetPhenotype, StringComparison.OrdinalIgnoreCase))
                        {
                            // Xác định giới tính của con
                            double sexMultiplier = 0.5; // Mặc định cho NST thường
                            bool sexMatch = false;

                            if (mode == InheritanceMode.XLinkedRecessive || mode == InheritanceMode.XLinkedDominant)
                            {
                                bool isMale = geno.Contains("Y", StringComparison.OrdinalIgnoreCase);
                                if (targetSex == "Con trai" && isMale) sexMatch = true;
                                else if (targetSex == "Con gái" && !isMale) sexMatch = true;
                                else if (targetSex == "Cả hai") sexMatch = true;

                                sexMultiplier = sexMatch ? 1.0 : 0.0;
                            }
                            else
                            {
                                if (targetSex == "Con trai" || targetSex == "Con gái")
                                {
                                    sexMatch = true;
                                    sexMultiplier = 0.5;
                                }
                                else if (targetSex == "Cả hai")
                                {
                                    sexMatch = true;
                                    sexMultiplier = 1.0;
                                }
                            }

                            if (sexMatch)
                            {
                                totalProb += prob * jointProb * sexMultiplier;
                            }
                        }
                    }
                }
            }

            return totalProb;
        }

        private static Dictionary<string, double> GetInitialGenotypes(PedigreeMember m, InheritanceMode mode)
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            bool isAffected = m.IsAffected;

            if (mode == InheritanceMode.AutosomalRecessive)
            {
                if (isAffected) dict["aa"] = 1.0;
                else
                {
                    dict["AA"] = 0.33;
                    dict["Aa"] = 0.67;
                }
            }
            else if (mode == InheritanceMode.AutosomalDominant)
            {
                if (!isAffected) dict["aa"] = 1.0;
                else
                {
                    dict["AA"] = 0.33;
                    dict["Aa"] = 0.67;
                }
            }
            else if (mode == InheritanceMode.XLinkedRecessive)
            {
                if (m.Sex == "Nam")
                {
                    if (isAffected) dict["X^a Y"] = 1.0;
                    else dict["X^A Y"] = 1.0;
                }
                else
                {
                    if (isAffected) dict["X^a X^a"] = 1.0;
                    else
                    {
                        dict["X^A X^A"] = 0.5;
                        dict["X^A X^a"] = 0.5;
                    }
                }
            }
            else if (mode == InheritanceMode.XLinkedDominant)
            {
                if (m.Sex == "Nam")
                {
                    if (isAffected) dict["X^A Y"] = 1.0;
                    else dict["X^a Y"] = 1.0;
                }
                else
                {
                    if (!isAffected) dict["X^a X^a"] = 1.0;
                    else
                    {
                        dict["X^A X^A"] = 0.5;
                        dict["X^A X^a"] = 0.5;
                    }
                }
            }

            return dict;
        }

        private static Dictionary<string, double> GetOffspringGenotypes(string dadGeno, string momGeno, InheritanceMode mode)
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (mode == InheritanceMode.AutosomalRecessive || mode == InheritanceMode.AutosomalDominant)
            {
                var dadAlleles = new[] { dadGeno[0].ToString(), dadGeno[1].ToString() };
                var momAlleles = new[] { momGeno[0].ToString(), momGeno[1].ToString() };

                foreach (var da in dadAlleles)
                {
                    foreach (var ma in momAlleles)
                    {
                        // Sắp xếp kiểu gen Aa thay vì aA
                        string child = (string.Compare(da, ma) <= 0) ? $"{da}{ma}" : $"{ma}{da}";
                        if (!dict.ContainsKey(child)) dict[child] = 0;
                        dict[child] += 0.25;
                    }
                }
            }
            else if (mode == InheritanceMode.XLinkedRecessive || mode == InheritanceMode.XLinkedDominant)
            {
                // dadGeno: e.g. "X^A Y", momGeno: e.g. "X^A X^a"
                var dadAlleles = dadGeno.Split(' ');
                var momAlleles = momGeno.Split(' ');

                foreach (var da in dadAlleles)
                {
                    foreach (var ma in momAlleles)
                    {
                        string child;
                        if (da.Contains("Y", StringComparison.OrdinalIgnoreCase))
                        {
                            // Con trai: nhận Y từ bố và X từ mẹ
                            child = $"{ma} Y";
                        }
                        else
                        {
                            // Con gái: nhận X từ bố và X từ mẹ, chuẩn hóa thứ tự chữ hoa đứng trước
                            child = (string.Compare(da, ma) <= 0) ? $"{da} {ma}" : $"{ma} {da}";
                        }

                        if (!dict.ContainsKey(child)) dict[child] = 0;
                        dict[child] += 0.25;
                    }
                }
            }

            return dict;
        }

        private static bool IsGenotypeAffected(string genotype, InheritanceMode mode)
        {
            if (mode == InheritanceMode.AutosomalRecessive)
            {
                return genotype.Equals("aa", StringComparison.OrdinalIgnoreCase);
            }
            if (mode == InheritanceMode.AutosomalDominant)
            {
                return genotype.Contains("A");
            }
            if (mode == InheritanceMode.XLinkedRecessive)
            {
                if (genotype.Contains("Y")) return genotype.Contains("X^a");
                return genotype.Equals("X^a X^a", StringComparison.OrdinalIgnoreCase);
            }
            if (mode == InheritanceMode.XLinkedDominant)
            {
                return genotype.Contains("X^A");
            }
            return false;
        }

        private static void Normalize(Dictionary<string, double> dict)
        {
            double sum = dict.Values.Sum();
            if (sum <= 0) return;
            var keys = dict.Keys.ToList();
            foreach (var k in keys)
            {
                dict[k] /= sum;
            }
        }
    }
}
