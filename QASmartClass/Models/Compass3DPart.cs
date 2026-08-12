using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Base class for all 3D compass parts (22 zones)
    /// </summary>
    public abstract class Compass3DPart
    {
        public string Name { get; set; } = string.Empty;
        public int ZoneNumber { get; set; }
        public Model3DGroup Model { get; protected set; } = new Model3DGroup();
        public Transform3DGroup Transform { get; protected set; } = new Transform3DGroup();

        protected Compass3DPart(string name, int zoneNumber)
        {
            Name = name;
            ZoneNumber = zoneNumber;
        }

        public abstract void Build();

        protected GeometryModel3D CreateMesh(MeshGeometry3D mesh, Material material)
        {
            return new GeometryModel3D(mesh, material);
        }

        protected int GetOptimizedSegments(int segments)
        {
            int resolutionSetting = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            var appConfig = QASmartClass.Services.AppConfig.Load();
            if (resolutionSetting <= 20 || appConfig.Reduce3DMeshResolution || (appConfig.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                return Math.Max(6, segments / 2);
            }
            return segments;
        }

        protected Material CreateMaterial(Color color, double specular = 0.3, double shininess = 30)
        {
            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), shininess) 
            { 
                SpecularPower = specular * 100 
            });
            return materialGroup;
        }

        /// <summary>
        /// Helper method to create a cylinder mesh
        /// </summary>
        protected MeshGeometry3D CreateCylinder(double radius, double height, int segments)
        {
            segments = GetOptimizedSegments(segments);
            var mesh = new MeshGeometry3D();

            // Generate vertices for cylinder
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, height, z));
            }

            // Generate side triangles
            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);

                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            // Add top and bottom caps
            Point3D centerTop = new Point3D(0, height, 0);
            Point3D centerBottom = new Point3D(0, 0, 0);
            int centerTopIndex = mesh.Positions.Count;
            int centerBottomIndex = centerTopIndex + 1;
            mesh.Positions.Add(centerTop);
            mesh.Positions.Add(centerBottom);

            for (int i = 0; i < segments; i++)
            {
                // Top cap
                mesh.TriangleIndices.Add(centerTopIndex);
                mesh.TriangleIndices.Add(i * 2 + 1);
                mesh.TriangleIndices.Add((i + 1) * 2 + 1);

                // Bottom cap
                mesh.TriangleIndices.Add(centerBottomIndex);
                mesh.TriangleIndices.Add((i + 1) * 2);
                mesh.TriangleIndices.Add(i * 2);
            }

            return mesh;
        }
    }

    #region Group A: Pencil Assembly (Zones 1-11)

    /// <summary>
    /// Zone 1: Eraser (Red rubber hemisphere)
    /// Dimensions: R ~4mm, Length ~8mm, Color: #8B0000 → #A0522D
    /// </summary>
    public class EraserPart : Compass3DPart
    {
        public EraserPart() : base("Eraser", 1) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Create hemisphere mesh
            var mesh = new MeshGeometry3D();
            int segments = 16;
            double radius = 4.0;
            double height = 3.0;

            // Generate hemisphere vertices
            for (int i = 0; i <= segments; i++)
            {
                for (int j = 0; j <= segments / 2; j++)
                {
                    double theta = 2.0 * Math.PI * i / segments;
                    double phi = Math.PI * j / segments;
                    
                    double x = radius * Math.Sin(phi) * Math.Cos(theta);
                    double y = height * (1.0 - Math.Cos(phi));
                    double z = radius * Math.Sin(phi) * Math.Sin(theta);

                    mesh.Positions.Add(new Point3D(x, y, z));
                }
            }

            // Generate triangles
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments / 2; j++)
                {
                    int p1 = i * (segments / 2 + 1) + j;
                    int p2 = (i + 1) * (segments / 2 + 1) + j;
                    int p3 = i * (segments / 2 + 1) + (j + 1);
                    int p4 = (i + 1) * (segments / 2 + 1) + (j + 1);

                    mesh.TriangleIndices.Add(p1);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p3);

                    mesh.TriangleIndices.Add(p3);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p4);
                }
            }

            var material = CreateMaterial(Color.FromRgb(0xEF, 0x44, 0x44), 0.35, 20);
            Model.Children.Add(CreateMesh(mesh, material));
        }
    }

    /// <summary>
    /// Zone 2: Metal ferrule (Silver collar holding eraser)
    /// Dimensions: Ø9×h3mm, Color: #C0C0C0
    /// </summary>
    public class FerrulePart : Compass3DPart
    {
        public FerrulePart() : base("Ferrule", 2) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mesh = CreateCylinder(4.5, 3.0, 20);
            var material = CreateMaterial(Color.FromRgb(0xC0, 0xC0, 0xC0), 0.6, 60);
            Model.Children.Add(CreateMesh(mesh, material));
        }

        private MeshGeometry3D CreateCylinder(double radius, double height, int segments)
        {
            segments = GetOptimizedSegments(segments);
            var mesh = new MeshGeometry3D();

            // Top and bottom circles
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, height, z));
            }

            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);

                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            return mesh;
        }
    }

    /// <summary>
    /// Zone 3: Pencil body (Yellow-orange cylinder)
    /// Dimensions: Ø7×L80mm, Color: #FFA500
    /// </summary>
    public class PencilBodyPart : Compass3DPart
    {
        public PencilBodyPart() : base("PencilBody", 3) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mesh = CreateHexagonalCylinder(3.5, 60.0, 6);
            var material = CreateMaterial(Color.FromRgb(0xFF, 0xA5, 0x00), 0.7, 70);
            Model.Children.Add(CreateMesh(mesh, material));
        }

        private MeshGeometry3D CreateHexagonalCylinder(double radius, double height, int sides)
        {
            var mesh = new MeshGeometry3D();

            for (int i = 0; i <= sides; i++)
            {
                double angle = 2.0 * Math.PI * i / sides;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, height, z));
            }

            for (int i = 0; i < sides; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);

                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 4: Brand Name Text

    /// <summary>
    /// Zone 4: Brand name text on pencil body
    /// Specifications:
    /// - Text: "STAEDTLER" or brand name
    /// - Position: Side of pencil body (one face)
    /// - Style: Embossed/engraved text
    /// - Size: Height ~2mm, depth 0.3mm
    /// - Color: Dark text on yellow body
    /// - Technology: Simple 3D text boxes for letters
    /// </summary>
    public class BrandNameTextPart : Compass3DPart
    {
        private readonly string _text = "STAEDTLER";
        private readonly double _letterHeight = 2.0;
        private readonly double _letterWidth = 1.2;
        private readonly double _letterDepth = 0.3;
        private readonly double _spacing = 0.3;

        public BrandNameTextPart() : base("BrandNameText", 4) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Create simple 3D text using boxes for each letter
            var material = CreateMaterial(Color.FromRgb(0x30, 0x30, 0x30), 0.2, 20);
            
            double totalWidth = _text.Length * (_letterWidth + _spacing);
            double startX = -totalWidth / 2;
            double yPosition = 40.0; // Middle of pencil body

            for (int i = 0; i < _text.Length; i++)
            {
                var letterMesh = CreateLetterBox();
                var letter = CreateMesh(letterMesh, material);
                
                double xPos = startX + i * (_letterWidth + _spacing);
                var transform = new TranslateTransform3D(xPos, yPosition, 3.5); // On surface
                letter.Transform = transform;
                
                Model.Children.Add(letter);
            }
        }

        private MeshGeometry3D CreateLetterBox()
        {
            var mesh = new MeshGeometry3D();
            
            // Simple rectangular box for letter
            double hw = _letterWidth / 2;
            double hh = _letterHeight / 2;
            
            // 8 vertices for box
            mesh.Positions.Add(new Point3D(-hw, -hh, 0));
            mesh.Positions.Add(new Point3D(hw, -hh, 0));
            mesh.Positions.Add(new Point3D(hw, hh, 0));
            mesh.Positions.Add(new Point3D(-hw, hh, 0));
            mesh.Positions.Add(new Point3D(-hw, -hh, _letterDepth));
            mesh.Positions.Add(new Point3D(hw, -hh, _letterDepth));
            mesh.Positions.Add(new Point3D(hw, hh, _letterDepth));
            mesh.Positions.Add(new Point3D(-hw, hh, _letterDepth));

            // 6 faces (12 triangles)
            int[] indices = {
                0, 1, 2, 0, 2, 3,  // Front
                4, 7, 6, 4, 6, 5,  // Back
                0, 4, 5, 0, 5, 1,  // Bottom
                2, 6, 7, 2, 7, 3,  // Top
                0, 3, 7, 0, 7, 4,  // Left
                1, 5, 6, 1, 6, 2   // Right
            };

            foreach (var idx in indices)
                mesh.TriangleIndices.Add(idx);

            return mesh;
        }
    }

    #endregion

    #region Zone 5: Lead Type Text

    /// <summary>
    /// Zone 5: Lead type text on pencil body
    /// Specifications:
    /// - Text: "HB" or lead grade (2B, 3B, etc.)
    /// - Position: Near wood tip, on pencil body
    /// - Style: Embossed/engraved text
    /// - Size: Height ~2.5mm, larger than brand
    /// - Color: Dark text on yellow body
    /// - Technology: Simple 3D text boxes
    /// </summary>
    public class LeadTypeTextPart : Compass3DPart
    {
        private readonly string _text = "HB";
        private readonly double _letterHeight = 2.5;
        private readonly double _letterWidth = 1.8;
        private readonly double _letterDepth = 0.3;
        private readonly double _spacing = 0.5;

        public LeadTypeTextPart() : base("LeadTypeText", 5) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Create text near tip
            var material = CreateMaterial(Color.FromRgb(0x20, 0x20, 0x20), 0.2, 20);
            
            double totalWidth = _text.Length * (_letterWidth + _spacing);
            double startX = -totalWidth / 2;
            double yPosition = 65.0; // Near wood tip

            for (int i = 0; i < _text.Length; i++)
            {
                var letterMesh = CreateLetterBox();
                var letter = CreateMesh(letterMesh, material);
                
                double xPos = startX + i * (_letterWidth + _spacing);
                var transform = new TranslateTransform3D(xPos, yPosition, 3.5); // On surface
                letter.Transform = transform;
                
                Model.Children.Add(letter);
            }
        }

        private MeshGeometry3D CreateLetterBox()
        {
            var mesh = new MeshGeometry3D();
            
            double hw = _letterWidth / 2;
            double hh = _letterHeight / 2;
            
            // 8 vertices for box
            mesh.Positions.Add(new Point3D(-hw, -hh, 0));
            mesh.Positions.Add(new Point3D(hw, -hh, 0));
            mesh.Positions.Add(new Point3D(hw, hh, 0));
            mesh.Positions.Add(new Point3D(-hw, hh, 0));
            mesh.Positions.Add(new Point3D(-hw, -hh, _letterDepth));
            mesh.Positions.Add(new Point3D(hw, -hh, _letterDepth));
            mesh.Positions.Add(new Point3D(hw, hh, _letterDepth));
            mesh.Positions.Add(new Point3D(-hw, hh, _letterDepth));

            // 6 faces
            int[] indices = {
                0, 1, 2, 0, 2, 3,
                4, 7, 6, 4, 6, 5,
                0, 4, 5, 0, 5, 1,
                2, 6, 7, 2, 7, 3,
                0, 3, 7, 0, 7, 4,
                1, 5, 6, 1, 6, 2
            };

            foreach (var idx in indices)
                mesh.TriangleIndices.Add(idx);

            return mesh;
        }
    }

    #endregion

    #region Zone 6: Wood Tip

    /// <summary>
    /// Zone 6: Pencil wood tip (Conical)
    /// Dimensions: Øbase 7mm, height 8mm, Color: #D2B48C
    /// </summary>
    public class WoodTipPart : Compass3DPart
    {
        public WoodTipPart() : base("WoodTip", 6) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mesh = CreateCone(3.5, 8.0, 20);
            var material = CreateMaterial(Color.FromRgb(0xD2, 0xB4, 0x8C), 0.3, 30);
            Model.Children.Add(CreateMesh(mesh, material));
        }

        private MeshGeometry3D CreateCone(double baseRadius, double height, int segments)
        {
            var mesh = new MeshGeometry3D();

            // Apex
            mesh.Positions.Add(new Point3D(0, height, 0));

            // Base circle
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = baseRadius * Math.Cos(angle);
                double z = baseRadius * Math.Sin(angle);
                mesh.Positions.Add(new Point3D(x, 0, z));
            }

            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add(i + 2);
            }

            return mesh;
        }
    }

    /// <summary>
    /// Zone 7: Lead/Graphite tip
    /// Dimensions: Ø0.7mm, protruding 2mm, Color: #2F2F2F
    /// </summary>
    public class LeadTipPart : Compass3DPart
    {
        public LeadTipPart() : base("LeadTip", 7) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mesh = CreateCone(0.35, 2.0, 8);
            var material = CreateMaterial(Color.FromRgb(0x2F, 0x2F, 0x2F), 0.2, 10);
            Model.Children.Add(CreateMesh(mesh, material));
        }

        private MeshGeometry3D CreateCone(double baseRadius, double height, int segments)
        {
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(new Point3D(0, height, 0));

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = baseRadius * Math.Cos(angle);
                double z = baseRadius * Math.Sin(angle);
                mesh.Positions.Add(new Point3D(x, 0, z));
            }

            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add(i + 2);
            }

            return mesh;
        }
    }

    #endregion

    #region Group B: Needle Assembly (Zones 12-18)

    /// <summary>
    /// Zone 12: Needle tip (Super sharp cone)
    /// Dimensions: Øbase 1mm, length 3mm, angle ~10°, Color: Silver
    /// </summary>
    public class NeedleTipPart : Compass3DPart
    {
        public NeedleTipPart() : base("NeedleTip", 12) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mesh = CreateSharpCone(0.5, 3.0, 12);
            var material = CreateMaterial(Color.FromRgb(0xE5, 0xE5, 0xE5), 0.7, 80);
            Model.Children.Add(CreateMesh(mesh, material));
        }

        private MeshGeometry3D CreateSharpCone(double baseRadius, double height, int segments)
        {
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(new Point3D(0, height, 0));

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = baseRadius * Math.Cos(angle);
                double z = baseRadius * Math.Sin(angle);
                mesh.Positions.Add(new Point3D(x, 0, z));
            }

            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add(i + 2);
            }

            return mesh;
        }
    }

    /// <summary>
    /// Zone 16: Compass arm (Oval rod)
    /// Dimensions: Ø4×L70mm (oval 3.5×4.5mm), Color: #D3D3D3
    /// </summary>
    public class CompassArmPart : Compass3DPart
    {
        public CompassArmPart() : base("CompassArm", 16) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mesh = CreateOvalRod(1.75, 2.25, 70.0, 12);
            var material = CreateMaterial(Color.FromRgb(0xD3, 0xD3, 0xD3), 0.4, 40);
            Model.Children.Add(CreateMesh(mesh, material));
        }

        private MeshGeometry3D CreateOvalRod(double radiusX, double radiusZ, double height, int segments)
        {
            var mesh = new MeshGeometry3D();

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = radiusX * Math.Cos(angle);
                double z = radiusZ * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, height, z));
            }

            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);

                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            return mesh;
        }
    }

    #endregion

    #region Group C: Center Joint (Zones 19-22)

    /// <summary>
    /// Zone 20/21: Joint discs (top and bottom)
    /// Dimensions: Ø10-12mm × thickness 1.5-2mm
    /// </summary>
    public class JointDiscPart : Compass3DPart
    {
        private readonly double _radius;
        private readonly double _thickness;
        private readonly bool _isTop;

        public JointDiscPart(double radius, double thickness, bool isTop) 
            : base(isTop ? "TopDisc" : "BottomDisc", isTop ? 20 : 21)
        {
            _radius = radius;
            _thickness = thickness;
            _isTop = isTop;
        }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mesh = CreateDisc(_radius, _thickness, 24);
            var color = _isTop ? Color.FromRgb(0xBE, 0xBE, 0xBE) : Color.FromRgb(0x96, 0x96, 0x96);
            var material = CreateMaterial(color, 0.5, 50);
            Model.Children.Add(CreateMesh(mesh, material));
        }

        private MeshGeometry3D CreateDisc(double radius, double thickness, int segments)
        {
            var mesh = new MeshGeometry3D();

            // Top circle
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, thickness, z));
            }

            // Side
            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);

                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            // Top and bottom caps
            Point3D centerTop = new Point3D(0, thickness, 0);
            Point3D centerBottom = new Point3D(0, 0, 0);
            int centerTopIndex = mesh.Positions.Count;
            int centerBottomIndex = centerTopIndex + 1;
            mesh.Positions.Add(centerTop);
            mesh.Positions.Add(centerBottom);

            for (int i = 0; i < segments; i++)
            {
                // Top cap
                mesh.TriangleIndices.Add(centerTopIndex);
                mesh.TriangleIndices.Add(i * 2 + 1);
                mesh.TriangleIndices.Add((i + 1) * 2 + 1);

                // Bottom cap
                mesh.TriangleIndices.Add(centerBottomIndex);
                mesh.TriangleIndices.Add((i + 1) * 2);
                mesh.TriangleIndices.Add(i * 2);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 8: Metal Clamp Body

    /// <summary>
    /// Zone 8: Metal clamp body (main clamp that holds pencil)
    /// Specifications:
    /// - Position: Between ferrule (zone 2) and pencil body (zone 3)
    /// - Shape: Two-part hinged clamp with circular opening
    /// - Size: Outer Ø 12mm, Inner Ø 8mm (to fit pencil), Height 6mm
    /// - Material: Brushed aluminum/steel
    /// - Color: Silver metallic #A8A8A8 with high specular
    /// - Details: Split line in middle, small hinge rivets
    /// </summary>
    public class MetalClampBodyPart : Compass3DPart
    {
        private readonly double _outerRadius = 6.0;  // 12mm diameter
        private readonly double _innerRadius = 4.0;  // 8mm diameter (fit pencil)
        private readonly double _height = 6.0;
        private readonly double _splitGap = 0.3;     // Gap between two clamp halves

        public MetalClampBodyPart() : base("MetalClampBody", 8) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Left half of clamp
            var leftHalf = CreateClampHalf(_outerRadius, _innerRadius, _height, true);
            var material = CreateMaterial(Color.FromRgb(0xA8, 0xA8, 0xA8), 0.6, 80);
            var leftMesh = CreateMesh(leftHalf, material);
            var leftTransform = new TranslateTransform3D(-_splitGap / 2, 0, 0);
            leftMesh.Transform = leftTransform;
            Model.Children.Add(leftMesh);

            // Right half of clamp
            var rightHalf = CreateClampHalf(_outerRadius, _innerRadius, _height, false);
            var rightMesh = CreateMesh(rightHalf, material);
            var rightTransform = new TranslateTransform3D(_splitGap / 2, 0, 0);
            rightMesh.Transform = rightTransform;
            Model.Children.Add(rightMesh);

            // Hinge rivets (small cylinders)
            var rivetMesh = CreateRivet(0.8, _height);
            var rivetMaterial = CreateMaterial(Color.FromRgb(0x70, 0x70, 0x70), 0.4, 60);
            
            // Top rivet
            var topRivet = CreateMesh(rivetMesh, rivetMaterial);
            topRivet.Transform = new TranslateTransform3D(0, 0, _outerRadius);
            Model.Children.Add(topRivet);

            // Bottom rivet
            var bottomRivet = CreateMesh(rivetMesh, rivetMaterial);
            bottomRivet.Transform = new TranslateTransform3D(0, 0, -_outerRadius);
            Model.Children.Add(bottomRivet);
        }

        private MeshGeometry3D CreateClampHalf(double outerRadius, double innerRadius, double height, bool isLeft)
        {
            var mesh = new MeshGeometry3D();
            int segments = 16;
            double startAngle = isLeft ? Math.PI : 0;
            double endAngle = isLeft ? 2 * Math.PI : Math.PI;

            // Generate vertices
            for (int i = 0; i <= segments / 2; i++)
            {
                double angle = startAngle + (endAngle - startAngle) * i / (segments / 2);
                double cosA = Math.Cos(angle);
                double sinA = Math.Sin(angle);

                // Outer surface
                mesh.Positions.Add(new Point3D(outerRadius * cosA, 0, outerRadius * sinA));
                mesh.Positions.Add(new Point3D(outerRadius * cosA, height, outerRadius * sinA));

                // Inner surface
                mesh.Positions.Add(new Point3D(innerRadius * cosA, 0, innerRadius * sinA));
                mesh.Positions.Add(new Point3D(innerRadius * cosA, height, innerRadius * sinA));
            }

            // Create triangles
            for (int i = 0; i < segments / 2; i++)
            {
                int idx = i * 4;
                // Outer wall
                mesh.TriangleIndices.Add(idx);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 5);

                // Inner wall
                mesh.TriangleIndices.Add(idx + 2);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 6);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 7);
                mesh.TriangleIndices.Add(idx + 6);
            }

            return mesh;
        }

        private MeshGeometry3D CreateRivet(double radius, double height)
        {
            var mesh = new MeshGeometry3D();
            int segments = 8;

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, height, z));
            }

            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 9: Adjustment Screw

    /// <summary>
    /// Zone 9: Adjustment screw for clamp tightness
    /// Specifications:
    /// - Position: Side of metal clamp body
    /// - Shape: Cylindrical screw with knurled head
    /// - Size: Ø 3mm × Length 8mm, Head Ø 5mm × 2mm
    /// - Material: Brushed steel
    /// - Color: Dark silver #808080
    /// - Details: Knurled texture on head, thread lines on shaft
    /// </summary>
    public class AdjustmentScrewPart : Compass3DPart
    {
        private readonly double _shaftRadius = 1.5;
        private readonly double _shaftLength = 8.0;
        private readonly double _headRadius = 2.5;
        private readonly double _headThickness = 2.0;

        public AdjustmentScrewPart() : base("AdjustmentScrew", 9) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Screw shaft
            var shaftMesh = CreateCylinder(_shaftRadius, _shaftLength, 12);
            var shaftMaterial = CreateMaterial(Color.FromRgb(0x80, 0x80, 0x80), 0.5, 70);
            var shaft = CreateMesh(shaftMesh, shaftMaterial);
            Model.Children.Add(shaft);

            // Knurled head
            var headMesh = CreateKnurledHead(_headRadius, _headThickness, 16);
            var headMaterial = CreateMaterial(Color.FromRgb(0x70, 0x70, 0x70), 0.6, 60);
            var head = CreateMesh(headMesh, headMaterial);
            var headTransform = new TranslateTransform3D(0, _shaftLength, 0);
            head.Transform = headTransform;
            Model.Children.Add(head);
        }

        private MeshGeometry3D CreateKnurledHead(double radius, double thickness, int segments)
        {
            var mesh = new MeshGeometry3D();
            double knurlDepth = 0.2;

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                // Alternate radius for knurled effect
                double r = (i % 2 == 0) ? radius : radius - knurlDepth;
                double x = r * Math.Cos(angle);
                double z = r * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, thickness, z));
            }

            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 10: Spring Mechanism

    /// <summary>
    /// Zone 10: Spring mechanism inside clamp
    /// Specifications:
    /// - Position: Inside metal clamp, provides clamping pressure
    /// - Shape: Coil spring
    /// - Size: Ø 2mm wire, Outer Ø 6mm, Length 5mm, 8 coils
    /// - Material: Steel spring
    /// - Color: Dark gray #505050
    /// - Details: Visible through clamp gap
    /// </summary>
    public class SpringMechanismPart : Compass3DPart
    {
        private readonly double _wireRadius = 0.5;
        private readonly double _springRadius = 3.0;
        private readonly double _springLength = 5.0;
        private readonly int _coils = 8;

        public SpringMechanismPart() : base("SpringMechanism", 10) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var springMesh = CreateSpring(_springRadius, _wireRadius, _springLength, _coils);
            var material = CreateMaterial(Color.FromRgb(0x50, 0x50, 0x50), 0.3, 40);
            Model.Children.Add(CreateMesh(springMesh, material));
        }

        private MeshGeometry3D CreateSpring(double springRadius, double wireRadius, double length, int coils)
        {
            var mesh = new MeshGeometry3D();
            int segmentsPerCoil = 16;
            int wireSegments = 8;
            int totalSegments = coils * segmentsPerCoil;

            for (int i = 0; i <= totalSegments; i++)
            {
                double t = (double)i / totalSegments;
                double angle = 2.0 * Math.PI * coils * t;
                double y = length * t;

                // Spring centerline
                double centerX = springRadius * Math.Cos(angle);
                double centerZ = springRadius * Math.Sin(angle);

                // Wire cross-section
                for (int j = 0; j <= wireSegments; j++)
                {
                    double wireAngle = 2.0 * Math.PI * j / wireSegments;
                    double dx = wireRadius * Math.Cos(wireAngle) * Math.Cos(angle);
                    double dz = wireRadius * Math.Cos(wireAngle) * Math.Sin(angle);
                    double dy = wireRadius * Math.Sin(wireAngle);

                    mesh.Positions.Add(new Point3D(centerX + dx, y + dy, centerZ + dz));
                }
            }

            // Create triangles
            for (int i = 0; i < totalSegments; i++)
            {
                for (int j = 0; j < wireSegments; j++)
                {
                    int p1 = i * (wireSegments + 1) + j;
                    int p2 = p1 + 1;
                    int p3 = (i + 1) * (wireSegments + 1) + j;
                    int p4 = p3 + 1;

                    mesh.TriangleIndices.Add(p1);
                    mesh.TriangleIndices.Add(p3);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p3);
                    mesh.TriangleIndices.Add(p4);
                }
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 11: Grip Pads

    /// <summary>
    /// Zone 11: Rubber grip pads on clamp interior
    /// Specifications:
    /// - Position: Inside surfaces of metal clamp
    /// - Shape: Curved rubber pads matching inner clamp surface
    /// - Size: Arc length ~12mm, Width 4mm, Thickness 1mm
    /// - Material: Soft rubber
    /// - Color: Black rubber #2C2C2C
    /// - Details: Textured surface for grip
    /// </summary>
    public class GripPadsPart : Compass3DPart
    {
        private readonly double _radius = 4.5;  // Slightly larger than inner clamp radius
        private readonly double _thickness = 1.0;
        private readonly double _height = 4.0;

        public GripPadsPart() : base("GripPads", 11) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Left pad
            var leftPad = CreateGripPad(_radius, _thickness, _height, true);
            var material = CreateMaterial(Color.FromRgb(0x2C, 0x2C, 0x2C), 0.2, 10);
            Model.Children.Add(CreateMesh(leftPad, material));

            // Right pad
            var rightPad = CreateGripPad(_radius, _thickness, _height, false);
            Model.Children.Add(CreateMesh(rightPad, material));
        }

        private MeshGeometry3D CreateGripPad(double radius, double thickness, double height, bool isLeft)
        {
            var mesh = new MeshGeometry3D();
            int segments = 8;
            double startAngle = isLeft ? Math.PI * 0.7 : -Math.PI * 0.3;
            double endAngle = isLeft ? Math.PI * 1.3 : Math.PI * 0.3;

            for (int i = 0; i <= segments; i++)
            {
                double angle = startAngle + (endAngle - startAngle) * i / segments;
                double cosA = Math.Cos(angle);
                double sinA = Math.Sin(angle);

                // Inner surface
                mesh.Positions.Add(new Point3D(radius * cosA, 0, radius * sinA));
                mesh.Positions.Add(new Point3D(radius * cosA, height, radius * sinA));

                // Outer surface (with thickness)
                double outerRadius = radius + thickness;
                mesh.Positions.Add(new Point3D(outerRadius * cosA, 0, outerRadius * sinA));
                mesh.Positions.Add(new Point3D(outerRadius * cosA, height, outerRadius * sinA));
            }

            for (int i = 0; i < segments; i++)
            {
                int idx = i * 4;
                // Inner surface
                mesh.TriangleIndices.Add(idx);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 5);
                mesh.TriangleIndices.Add(idx + 4);

                // Outer surface
                mesh.TriangleIndices.Add(idx + 2);
                mesh.TriangleIndices.Add(idx + 6);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 6);
                mesh.TriangleIndices.Add(idx + 7);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 22: Center Screw

    /// <summary>
    /// Zone 22: Center screw (main bolt) holding joint together
    /// Specifications:
    /// - Position: Center of joint, through both discs
    /// - Shape: Screw with Phillips head on top
    /// - Size: Shaft Ø 2mm × Length 5mm, Head Ø 4mm × 1.5mm
    /// - Material: Stainless steel
    /// - Color: Bright silver #D0D0D0 with high specular
    /// - Details: Phillips cross pattern on head, visible from top
    /// </summary>
    public class CenterScrewPart : Compass3DPart
    {
        private readonly double _shaftRadius = 1.0;
        private readonly double _shaftLength = 5.0;
        private readonly double _headRadius = 2.0;
        private readonly double _headThickness = 1.5;

        public CenterScrewPart() : base("CenterScrew", 22) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Screw shaft
            var shaftMesh = CreateCylinder(_shaftRadius, _shaftLength, 16);
            var material = CreateMaterial(Color.FromRgb(0xD0, 0xD0, 0xD0), 0.7, 90);
            Model.Children.Add(CreateMesh(shaftMesh, material));

            // Screw head with Phillips pattern
            var headMesh = CreateScrewHead(_headRadius, _headThickness, 16);
            var head = CreateMesh(headMesh, material);
            var headTransform = new TranslateTransform3D(0, _shaftLength, 0);
            head.Transform = headTransform;
            Model.Children.Add(head);

            // Phillips cross slots
            var slotMaterial = CreateMaterial(Color.FromRgb(0x50, 0x50, 0x50), 0.3, 30);
            
            // Horizontal slot
            var hSlot = CreatePhillipsSlot(_headRadius * 0.9, 0.3, _headThickness * 0.5, true);
            var hSlotMesh = CreateMesh(hSlot, slotMaterial);
            hSlotMesh.Transform = new TranslateTransform3D(0, _shaftLength + _headThickness * 0.5, 0);
            Model.Children.Add(hSlotMesh);

            // Vertical slot
            var vSlot = CreatePhillipsSlot(_headRadius * 0.9, 0.3, _headThickness * 0.5, false);
            var vSlotMesh = CreateMesh(vSlot, slotMaterial);
            vSlotMesh.Transform = new TranslateTransform3D(0, _shaftLength + _headThickness * 0.5, 0);
            Model.Children.Add(vSlotMesh);
        }

        private MeshGeometry3D CreateScrewHead(double radius, double thickness, int segments)
        {
            var mesh = new MeshGeometry3D();

            // Slightly domed top
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, thickness, z));
            }

            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            // Top cap
            Point3D center = new Point3D(0, thickness, 0);
            int centerIndex = mesh.Positions.Count;
            mesh.Positions.Add(center);

            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(centerIndex);
                mesh.TriangleIndices.Add(i * 2 + 1);
                mesh.TriangleIndices.Add((i + 1) * 2 + 1);
            }

            return mesh;
        }

        private MeshGeometry3D CreatePhillipsSlot(double length, double width, double depth, bool isHorizontal)
        {
            var mesh = new MeshGeometry3D();

            // Simple box for slot
            double halfLen = length / 2;
            double halfWidth = width / 2;

            if (isHorizontal)
            {
                mesh.Positions.Add(new Point3D(-halfLen, -depth, -halfWidth));
                mesh.Positions.Add(new Point3D(-halfLen, 0, -halfWidth));
                mesh.Positions.Add(new Point3D(halfLen, -depth, -halfWidth));
                mesh.Positions.Add(new Point3D(halfLen, 0, -halfWidth));
                mesh.Positions.Add(new Point3D(-halfLen, -depth, halfWidth));
                mesh.Positions.Add(new Point3D(-halfLen, 0, halfWidth));
                mesh.Positions.Add(new Point3D(halfLen, -depth, halfWidth));
                mesh.Positions.Add(new Point3D(halfLen, 0, halfWidth));
            }
            else
            {
                mesh.Positions.Add(new Point3D(-halfWidth, -depth, -halfLen));
                mesh.Positions.Add(new Point3D(-halfWidth, 0, -halfLen));
                mesh.Positions.Add(new Point3D(halfWidth, -depth, -halfLen));
                mesh.Positions.Add(new Point3D(halfWidth, 0, -halfLen));
                mesh.Positions.Add(new Point3D(-halfWidth, -depth, halfLen));
                mesh.Positions.Add(new Point3D(-halfWidth, 0, halfLen));
                mesh.Positions.Add(new Point3D(halfWidth, -depth, halfLen));
                mesh.Positions.Add(new Point3D(halfWidth, 0, halfLen));
            }

            // Simple box triangulation
            int[] indices = { 0, 2, 1, 1, 2, 3, 4, 5, 6, 5, 7, 6, 0, 1, 4, 1, 5, 4, 2, 6, 3, 3, 6, 7, 0, 4, 2, 2, 4, 6, 1, 3, 5, 3, 7, 5 };
            foreach (var idx in indices)
                mesh.TriangleIndices.Add(idx);

            return mesh;
        }
    }

    #endregion

    #region Zone 13: Needle Mount (Adjustable Holder)

    /// <summary>
    /// Zone 13: Needle mount - adjustable holder for needle
    /// Specifications:
    /// - Position: End of compass arm, holds needle tip
    /// - Shape: Cylindrical holder with adjustment slot
    /// - Size: Outer Ø 4mm, Inner Ø 1.5mm, Length 8mm
    /// - Material: Metal (brass/steel)
    /// - Color: Brass tone #B8860B with medium specular
    /// - Details: Slot for needle adjustment, small set screw
    /// </summary>
    public class NeedleMountPart : Compass3DPart
    {
        private readonly double _outerRadius = 2.0;   // Ø4mm
        private readonly double _innerRadius = 0.75;  // Ø1.5mm (for needle)
        private readonly double _length = 8.0;
        private readonly double _slotWidth = 0.5;

        public NeedleMountPart() : base("NeedleMount", 13) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Main cylindrical holder
            var holderMesh = CreateHolderWithSlot(_outerRadius, _innerRadius, _length, _slotWidth);
            var material = CreateMaterial(Color.FromRgb(0xB8, 0x86, 0x0B), 0.5, 70);
            Model.Children.Add(CreateMesh(holderMesh, material));

            // Small set screw on side
            var setScrewMesh = CreateCylinder(0.4, 2.0, 8);
            var screwMaterial = CreateMaterial(Color.FromRgb(0x80, 0x80, 0x80), 0.6, 80);
            var setScrew = CreateMesh(setScrewMesh, screwMaterial);
            var screwTransform = new TranslateTransform3D(_outerRadius + 0.3, _length * 0.5, 0);
            setScrew.Transform = screwTransform;
            Model.Children.Add(setScrew);
        }

        private MeshGeometry3D CreateHolderWithSlot(double outerRadius, double innerRadius, double length, double slotWidth)
        {
            var mesh = new MeshGeometry3D();
            int segments = 16;

            // Create cylinder with vertical slot
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                
                // Skip vertices in slot region (front)
                bool inSlot = (angle > -0.2 && angle < 0.2);
                
                double cosA = Math.Cos(angle);
                double sinA = Math.Sin(angle);

                // Outer surface
                mesh.Positions.Add(new Point3D(outerRadius * cosA, 0, outerRadius * sinA));
                mesh.Positions.Add(new Point3D(outerRadius * cosA, length, outerRadius * sinA));

                // Inner surface (hole for needle)
                mesh.Positions.Add(new Point3D(innerRadius * cosA, 0, innerRadius * sinA));
                mesh.Positions.Add(new Point3D(innerRadius * cosA, length, innerRadius * sinA));
            }

            // Create triangles
            for (int i = 0; i < segments; i++)
            {
                int idx = i * 4;
                
                // Outer surface
                mesh.TriangleIndices.Add(idx);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 5);

                // Inner surface
                mesh.TriangleIndices.Add(idx + 2);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 6);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 7);
                mesh.TriangleIndices.Add(idx + 6);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 14: Locking Mechanism

    /// <summary>
    /// Zone 14: Locking mechanism for needle adjustment
    /// Specifications:
    /// - Position: Side of needle mount
    /// - Shape: Small thumbscrew with knurled head
    /// - Size: Shaft Ø 1.5mm × 3mm, Head Ø 3mm × 2mm
    /// - Material: Steel
    /// - Color: Dark silver #707070
    /// - Function: Locks needle in adjusted position
    /// </summary>
    public class LockingMechanismPart : Compass3DPart
    {
        private readonly double _shaftRadius = 0.75;
        private readonly double _shaftLength = 3.0;
        private readonly double _headRadius = 1.5;
        private readonly double _headThickness = 2.0;

        public LockingMechanismPart() : base("LockingMechanism", 14) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Screw shaft
            var shaftMesh = CreateCylinder(_shaftRadius, _shaftLength, 10);
            var material = CreateMaterial(Color.FromRgb(0x70, 0x70, 0x70), 0.5, 70);
            Model.Children.Add(CreateMesh(shaftMesh, material));

            // Knurled thumbscrew head
            var headMesh = CreateKnurledThumb(_headRadius, _headThickness, 12);
            var headMaterial = CreateMaterial(Color.FromRgb(0x60, 0x60, 0x60), 0.6, 60);
            var head = CreateMesh(headMesh, headMaterial);
            var headTransform = new TranslateTransform3D(0, _shaftLength, 0);
            head.Transform = headTransform;
            Model.Children.Add(head);
        }

        private MeshGeometry3D CreateKnurledThumb(double radius, double thickness, int segments)
        {
            segments = GetOptimizedSegments(segments);
            var mesh = new MeshGeometry3D();
            double knurlDepth = 0.15;

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double r = (i % 2 == 0) ? radius : radius - knurlDepth;
                double x = r * Math.Cos(angle);
                double z = r * Math.Sin(angle);

                mesh.Positions.Add(new Point3D(x, 0, z));
                mesh.Positions.Add(new Point3D(x, thickness, z));
            }

            for (int i = 0; i < segments; i++)
            {
                int p1 = i * 2;
                int p2 = i * 2 + 1;
                int p3 = (i + 1) * 2;
                int p4 = (i + 1) * 2 + 1;

                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);
                mesh.TriangleIndices.Add(p4);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 15: Needle Shaft Extension

    /// <summary>
    /// Zone 15: Needle shaft extension between mount and tip
    /// Specifications:
    /// - Position: Inside needle mount, connects to tip
    /// - Shape: Thin cylindrical shaft
    /// - Size: Ø 1.2mm × Length 5mm
    /// - Material: Steel needle
    /// - Color: Bright silver #C8C8C8
    /// - Details: Smooth polished surface
    /// </summary>
    public class NeedleShaftExtensionPart : Compass3DPart
    {
        private readonly double _radius = 0.6;    // Ø1.2mm
        private readonly double _length = 5.0;

        public NeedleShaftExtensionPart() : base("NeedleShaftExtension", 15) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var shaftMesh = CreateCylinder(_radius, _length, 12);
            var material = CreateMaterial(Color.FromRgb(0xC8, 0xC8, 0xC8), 0.7, 90);
            Model.Children.Add(CreateMesh(shaftMesh, material));
        }
    }

    #endregion

    #region Zone 17: Hinge Joint

    /// <summary>
    /// Zone 17: Hinge joint connecting needle arm to center
    /// Specifications:
    /// - Position: Between compass arm (zone 16) and center joint
    /// - Shape: Cylindrical hinge pin
    /// - Size: Ø 3mm × Length 4mm
    /// - Material: Steel
    /// - Color: Medium gray #909090
    /// - Function: Allows arm rotation
    /// </summary>
    public class HingeJointPart : Compass3DPart
    {
        private readonly double _radius = 1.5;    // Ø3mm
        private readonly double _length = 4.0;

        public HingeJointPart() : base("HingeJoint", 17) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Main hinge pin
            var pinMesh = CreateCylinder(_radius, _length, 16);
            var material = CreateMaterial(Color.FromRgb(0x90, 0x90, 0x90), 0.5, 70);
            Model.Children.Add(CreateMesh(pinMesh, material));

            // Small caps on both ends
            var capMesh = CreateCylinder(_radius * 1.2, 0.5, 16);
            var capMaterial = CreateMaterial(Color.FromRgb(0x80, 0x80, 0x80), 0.6, 80);
            
            // Bottom cap
            var bottomCap = CreateMesh(capMesh, capMaterial);
            var bottomTransform = new TranslateTransform3D(0, -0.5, 0);
            bottomCap.Transform = bottomTransform;
            Model.Children.Add(bottomCap);

            // Top cap
            var topCap = CreateMesh(capMesh, capMaterial);
            var topTransform = new TranslateTransform3D(0, _length, 0);
            topCap.Transform = topTransform;
            Model.Children.Add(topCap);
        }
    }

    #endregion

    #region Zone 18: Needle Arm Mount

    /// <summary>
    /// Zone 18: Mount connecting needle arm to hinge
    /// Specifications:
    /// - Position: End of compass arm, connects to hinge
    /// - Shape: Rectangular mount with hole
    /// - Size: 6mm × 5mm × 3mm with Ø3mm hole
    /// - Material: Metal
    /// - Color: Light gray #B0B0B0
    /// - Function: Attachment point for hinge
    /// </summary>
    public class NeedleArmMountPart : Compass3DPart
    {
        private readonly double _width = 6.0;
        private readonly double _height = 5.0;
        private readonly double _thickness = 3.0;
        private readonly double _holeRadius = 1.5;  // Ø3mm hole

        public NeedleArmMountPart() : base("NeedleArmMount", 18) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var mountMesh = CreateRectangularMount(_width, _height, _thickness, _holeRadius);
            var material = CreateMaterial(Color.FromRgb(0xB0, 0xB0, 0xB0), 0.5, 70);
            Model.Children.Add(CreateMesh(mountMesh, material));
        }

        private MeshGeometry3D CreateRectangularMount(double width, double height, double thickness, double holeRadius)
        {
            var mesh = new MeshGeometry3D();

            // Simple rectangular box with central hole
            double halfWidth = width / 2;
            double halfHeight = height / 2;

            // Outer rectangle vertices
            // Front face
            mesh.Positions.Add(new Point3D(-halfWidth, 0, -thickness / 2));
            mesh.Positions.Add(new Point3D(halfWidth, 0, -thickness / 2));
            mesh.Positions.Add(new Point3D(halfWidth, height, -thickness / 2));
            mesh.Positions.Add(new Point3D(-halfWidth, height, -thickness / 2));

            // Back face
            mesh.Positions.Add(new Point3D(-halfWidth, 0, thickness / 2));
            mesh.Positions.Add(new Point3D(halfWidth, 0, thickness / 2));
            mesh.Positions.Add(new Point3D(halfWidth, height, thickness / 2));
            mesh.Positions.Add(new Point3D(-halfWidth, height, thickness / 2));

            // Box faces
            int[] indices = {
                0, 1, 2, 0, 2, 3,  // Front
                4, 7, 6, 4, 6, 5,  // Back
                0, 4, 5, 0, 5, 1,  // Bottom
                2, 6, 7, 2, 7, 3,  // Top
                0, 3, 7, 0, 7, 4,  // Left
                1, 5, 6, 1, 6, 2   // Right
            };

            foreach (var idx in indices)
                mesh.TriangleIndices.Add(idx);

            return mesh;
        }
    }

    #endregion

    #region Zone 19: Hinge Rotation Disc

    /// <summary>
    /// Zone 19: Rotation disc at hinge point
    /// Specifications:
    /// - Position: Between hinge and center joint
    /// - Shape: Flat circular disc
    /// - Size: Ø 8mm × Thickness 1mm
    /// - Material: Metal
    /// - Color: Medium gray #A0A0A0
    /// - Function: Smooth rotation surface for hinge
    /// </summary>
    public class HingeRotationDiscPart : Compass3DPart
    {
        private readonly double _radius = 4.0;      // Ø8mm
        private readonly double _thickness = 1.0;
        private readonly double _holeRadius = 1.5;  // Ø3mm center hole

        public HingeRotationDiscPart() : base("HingeRotationDisc", 19) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            var discMesh = CreateDiscWithHole(_radius, _thickness, _holeRadius, 24);
            var material = CreateMaterial(Color.FromRgb(0xA0, 0xA0, 0xA0), 0.5, 70);
            Model.Children.Add(CreateMesh(discMesh, material));
        }

        private MeshGeometry3D CreateDiscWithHole(double outerRadius, double thickness, double holeRadius, int segments)
        {
            segments = GetOptimizedSegments(segments);
            var mesh = new MeshGeometry3D();

            // Generate vertices for disc with central hole
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                double cosA = Math.Cos(angle);
                double sinA = Math.Sin(angle);

                // Outer ring
                mesh.Positions.Add(new Point3D(outerRadius * cosA, 0, outerRadius * sinA));
                mesh.Positions.Add(new Point3D(outerRadius * cosA, thickness, outerRadius * sinA));

                // Inner ring (hole)
                mesh.Positions.Add(new Point3D(holeRadius * cosA, 0, holeRadius * sinA));
                mesh.Positions.Add(new Point3D(holeRadius * cosA, thickness, holeRadius * sinA));
            }

            // Create triangles
            for (int i = 0; i < segments; i++)
            {
                int idx = i * 4;

                // Outer surface
                mesh.TriangleIndices.Add(idx);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 5);

                // Inner surface
                mesh.TriangleIndices.Add(idx + 2);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 6);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 7);
                mesh.TriangleIndices.Add(idx + 6);

                // Top ring surface
                mesh.TriangleIndices.Add(idx + 1);
                mesh.TriangleIndices.Add(idx + 5);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 3);
                mesh.TriangleIndices.Add(idx + 5);
                mesh.TriangleIndices.Add(idx + 7);

                // Bottom ring surface
                mesh.TriangleIndices.Add(idx);
                mesh.TriangleIndices.Add(idx + 2);
                mesh.TriangleIndices.Add(idx + 4);
                mesh.TriangleIndices.Add(idx + 2);
                mesh.TriangleIndices.Add(idx + 6);
                mesh.TriangleIndices.Add(idx + 4);
            }

            return mesh;
        }
    }

    #endregion

    #region Zone 19: Main Rotation Axis

    /// <summary>
    /// Zone 19: Main rotation axis - central pivot shaft through joint
    /// Specifications:
    /// - Position: Through center of joint discs (zones 20-21)
    /// - Shape: Cylindrical shaft with end caps
    /// - Size: Ø 2mm diameter × 8mm length
    /// - Material: Hardened steel
    /// - Color: Bright silver #E0E0E0 with high shine
    /// - Details: Smooth polished surface, primary pivot point
    /// - Function: Main rotation axis for both compass arms
    /// </summary>
    public class RotationAxisPart : Compass3DPart
    {
        private readonly double _radius = 1.0;  // Ø2mm
        private readonly double _length = 8.0;
        private readonly int _segments = 24;

        public RotationAxisPart() : base("RotationAxis", 19) { }

        public override void Build()
        {
            Model = new Model3DGroup();

            // Main shaft cylinder - bright silver polished steel
            var shaftMesh = CreateCylinder(_radius, _length, _segments);
            var material = CreateMaterial(Color.FromRgb(0xE0, 0xE0, 0xE0), 0.9, 100);
            Model.Children.Add(CreateMesh(shaftMesh, material));

            // Top cap (slightly larger for assembly and visibility)
            var topCapMesh = CreateCylinder(_radius * 1.3, 0.4, _segments);
            var topCap = CreateMesh(topCapMesh, material);
            var topTransform = new TranslateTransform3D(0, _length, 0);
            topCap.Transform = topTransform;
            Model.Children.Add(topCap);

            // Bottom cap
            var bottomCapMesh = CreateCylinder(_radius * 1.3, 0.4, _segments);
            var bottomCap = CreateMesh(bottomCapMesh, material);
            var bottomTransform = new TranslateTransform3D(0, -0.4, 0);
            bottomCap.Transform = bottomTransform;
            Model.Children.Add(bottomCap);
        }
    }

    #endregion
}
