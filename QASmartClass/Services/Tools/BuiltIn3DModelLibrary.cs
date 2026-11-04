using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace QASmartTouch.Services.Tools
{
    public class BuiltIn3DModelItem
    {
        public string Id { get; init; } = string.Empty;
        public string Icon { get; init; } = "🎲";
        public string Title { get; init; } = string.Empty;
        public string SubjectTag { get; init; } = string.Empty;
        public string AccentHex { get; init; } = "#8B5CF6";
        public Func<Model3DGroup> Builder { get; init; } = () => new Model3DGroup();
    }

    /// <summary>
    /// Kho mẫu 3D Giáo dục tích hợp sẵn (Offline, tạo bằng HelixToolkit MeshBuilder, tải tức thì)
    /// </summary>
    public static class BuiltIn3DModelLibrary
    {
        public static IReadOnlyList<BuiltIn3DModelItem> GetCatalog()
        {
            return new List<BuiltIn3DModelItem>
            {
                new BuiltIn3DModelItem
                {
                    Id = "h2o_molecule",
                    Icon = "⚛️",
                    Title = "Phân tử Nước (H₂O)",
                    SubjectTag = "Hóa học",
                    AccentHex = "#EF4444",
                    Builder = CreateWaterMoleculeModel
                },
                new BuiltIn3DModelItem
                {
                    Id = "dna_helix",
                    Icon = "🧬",
                    Title = "Chuỗi xoắn kép ADN",
                    SubjectTag = "Sinh học",
                    AccentHex = "#8B5CF6",
                    Builder = CreateDnaHelixModel
                },
                new BuiltIn3DModelItem
                {
                    Id = "solar_saturn",
                    Icon = "🪐",
                    Title = "Hệ Mặt Trời & Sao Thổ",
                    SubjectTag = "Địa lý - Thiên văn",
                    AccentHex = "#F59E0B",
                    Builder = CreateSolarSystemModel
                },
                new BuiltIn3DModelItem
                {
                    Id = "crystal_lattice",
                    Icon = "💎",
                    Title = "Mạng tinh thể Lập phương",
                    SubjectTag = "Vật lý - Hóa học",
                    AccentHex = "#0EA5E9",
                    Builder = CreateCrystalLatticeModel
                }
            };
        }

        private static Material CreateShinyMaterial(Color color, double specularPower = 65)
        {
            var group = new MaterialGroup();
            group.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), specularPower));
            return group;
        }

        /// <summary>
        /// 1. Phân tử Nước (H2O) với góc liên kết chuẩn 104.5 độ
        /// </summary>
        private static Model3DGroup CreateWaterMoleculeModel()
        {
            var group = new Model3DGroup();

            var oxygenPos = new Point3D(0, 0, 0.8);
            double bondLength = 3.2;
            double halfAngleRad = (104.5 / 2.0) * Math.PI / 180.0;

            var h1Pos = new Point3D(
                Math.Sin(halfAngleRad) * bondLength,
                0,
                oxygenPos.Z - Math.Cos(halfAngleRad) * bondLength);

            var h2Pos = new Point3D(
                -Math.Sin(halfAngleRad) * bondLength,
                0,
                oxygenPos.Z - Math.Cos(halfAngleRad) * bondLength);

            // Oxygen atom (Red)
            var oxygenBuilder = new MeshBuilder();
            oxygenBuilder.AddSphere(oxygenPos, 1.35, 36, 36);
            group.Children.Add(new GeometryModel3D(oxygenBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(239, 68, 68))));

            // Hydrogen atoms (Ice White / Cyan tint)
            var hydroBuilder = new MeshBuilder();
            hydroBuilder.AddSphere(h1Pos, 0.85, 32, 32);
            hydroBuilder.AddSphere(h2Pos, 0.85, 32, 32);
            group.Children.Add(new GeometryModel3D(hydroBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(226, 232, 240))));

            // Covalent bonds (Silver cylinders)
            var bondBuilder = new MeshBuilder();
            bondBuilder.AddCylinder(oxygenPos, h1Pos, 0.28, 24);
            bondBuilder.AddCylinder(oxygenPos, h2Pos, 0.28, 24);
            group.Children.Add(new GeometryModel3D(bondBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(148, 163, 184))));

            return group;
        }

        /// <summary>
        /// 2. Chuỗi xoắn kép ADN (DNA Double Helix)
        /// </summary>
        private static Model3DGroup CreateDnaHelixModel()
        {
            var group = new Model3DGroup();

            var strandABuilder = new MeshBuilder();
            var strandBBuilder = new MeshBuilder();
            var basePairBuilder1 = new MeshBuilder();
            var basePairBuilder2 = new MeshBuilder();

            int steps = 16;
            double radius = 2.0;
            double totalHeight = 8.0;
            double startZ = -totalHeight / 2.0;

            Point3D? prevA = null;
            Point3D? prevB = null;

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double angle = t * Math.PI * 2.5;
                double z = startZ + t * totalHeight;

                var ptA = new Point3D(radius * Math.Cos(angle), radius * Math.Sin(angle), z);
                var ptB = new Point3D(radius * Math.Cos(angle + Math.PI), radius * Math.Sin(angle + Math.PI), z);

                strandABuilder.AddSphere(ptA, 0.34, 18, 18);
                strandBBuilder.AddSphere(ptB, 0.34, 18, 18);

                if (prevA.HasValue && prevB.HasValue)
                {
                    strandABuilder.AddCylinder(prevA.Value, ptA, 0.16, 14);
                    strandBBuilder.AddCylinder(prevB.Value, ptB, 0.16, 14);
                }

                // Base pairs connecting the two strands
                var midPt = new Point3D(0, 0, z);
                if (i % 2 == 0)
                {
                    basePairBuilder1.AddCylinder(ptA, midPt, 0.14, 12);
                    basePairBuilder2.AddCylinder(midPt, ptB, 0.14, 12);
                }
                else
                {
                    basePairBuilder2.AddCylinder(ptA, midPt, 0.14, 12);
                    basePairBuilder1.AddCylinder(midPt, ptB, 0.14, 12);
                }

                prevA = ptA;
                prevB = ptB;
            }

            group.Children.Add(new GeometryModel3D(strandABuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(59, 130, 246)))); // Blue strand
            group.Children.Add(new GeometryModel3D(strandBBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(139, 92, 246)))); // Purple strand
            group.Children.Add(new GeometryModel3D(basePairBuilder1.ToMesh(), CreateShinyMaterial(Color.FromRgb(16, 185, 129)))); // Emerald base
            group.Children.Add(new GeometryModel3D(basePairBuilder2.ToMesh(), CreateShinyMaterial(Color.FromRgb(245, 158, 11)))); // Amber base

            return group;
        }

        /// <summary>
        /// 3. Hệ Mặt Trời & Sao Thổ có vành đai
        /// </summary>
        private static Model3DGroup CreateSolarSystemModel()
        {
            var group = new Model3DGroup();

            // Sun at center
            var sunBuilder = new MeshBuilder();
            sunBuilder.AddSphere(new Point3D(0, 0, 0), 1.6, 36, 36);
            group.Children.Add(new GeometryModel3D(sunBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(245, 158, 11), 90)));

            // Orbit rings
            var orbitBuilder = new MeshBuilder();
            orbitBuilder.AddTorus(6.4, 0.06, 48, 12);
            orbitBuilder.AddTorus(10.8, 0.06, 60, 12);
            group.Children.Add(new GeometryModel3D(orbitBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(148, 163, 184))));

            // Earth + Moon
            var earthPos = new Point3D(3.2, 0, 0);
            var earthBuilder = new MeshBuilder();
            earthBuilder.AddSphere(earthPos, 0.65, 28, 28);
            group.Children.Add(new GeometryModel3D(earthBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(14, 165, 233))));

            var moonBuilder = new MeshBuilder();
            moonBuilder.AddSphere(new Point3D(4.1, 0.7, 0.3), 0.22, 20, 20);
            group.Children.Add(new GeometryModel3D(moonBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(203, 213, 225))));

            // Saturn + Ring
            var saturnPos = new Point3D(-3.8, 3.8, 0);
            var saturnBuilder = new MeshBuilder();
            saturnBuilder.AddSphere(saturnPos, 0.95, 32, 32);
            group.Children.Add(new GeometryModel3D(saturnBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(234, 179, 8))));

            var ringBuilder = new MeshBuilder();
            ringBuilder.AddPipe(
                new Point3D(saturnPos.X, saturnPos.Y, saturnPos.Z - 0.05),
                new Point3D(saturnPos.X, saturnPos.Y, saturnPos.Z + 0.05),
                1.25, 2.15, 36);
            group.Children.Add(new GeometryModel3D(ringBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(217, 119, 6))));

            return group;
        }

        /// <summary>
        /// 4. Mạng tinh thể Lập phương (NaCl / Crystal Lattice)
        /// </summary>
        private static Model3DGroup CreateCrystalLatticeModel()
        {
            var group = new Model3DGroup();
            var ionABuilder = new MeshBuilder();
            var ionBBuilder = new MeshBuilder();
            var bondBuilder = new MeshBuilder();

            double spacing = 2.4;
            double offset = -spacing;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        var pt = new Point3D(offset + x * spacing, offset + y * spacing, offset + z * spacing);
                        bool isTypeA = (x + y + z) % 2 == 0;

                        if (isTypeA)
                            ionABuilder.AddSphere(pt, 0.48, 22, 22);
                        else
                            ionBBuilder.AddSphere(pt, 0.36, 22, 22);

                        if (x < 2)
                            bondBuilder.AddCylinder(pt, new Point3D(pt.X + spacing, pt.Y, pt.Z), 0.09, 12);
                        if (y < 2)
                            bondBuilder.AddCylinder(pt, new Point3D(pt.X, pt.Y + spacing, pt.Z), 0.09, 12);
                        if (z < 2)
                            bondBuilder.AddCylinder(pt, new Point3D(pt.X, pt.Y, pt.Z + spacing), 0.09, 12);
                    }
                }
            }

            group.Children.Add(new GeometryModel3D(ionABuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(14, 165, 233)))); // Sky Blue ions
            group.Children.Add(new GeometryModel3D(ionBBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(16, 185, 129)))); // Emerald ions
            group.Children.Add(new GeometryModel3D(bondBuilder.ToMesh(), CreateShinyMaterial(Color.FromRgb(148, 163, 184)))); // Lattice rods

            return group;
        }
    }
}
