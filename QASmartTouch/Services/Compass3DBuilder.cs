using System.Windows.Media;
using System.Windows.Media.Media3D;
using QASmartTouch.Models;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service to build and assemble the complete 3D compass with all 22 zones
    /// Following technical specification from 1.1.4.Compa_That.txt
    /// </summary>
    public class Compass3DBuilder
    {
        private Model3DGroup _compassModel = new Model3DGroup();
        private double _openingAngle = 60.0; // degrees (10-180 range)
        private double _radius = 50.0; // mm (10-200 range)

        public double OpeningAngle
        {
            get => _openingAngle;
            set
            {
                _openingAngle = Math.Clamp(value, 10.0, 180.0);
                UpdateGeometry();
            }
        }

        public double Radius
        {
            get => _radius;
            set
            {
                _radius = Math.Clamp(value, 10.0, 200.0);
                UpdateGeometry();
            }
        }

        public Model3DGroup Build()
        {
            _compassModel = new Model3DGroup();

            // Build Group A: Pencil Assembly (right arm)
            BuildPencilArm();

            // Build Group B: Needle Assembly (left arm)
            BuildNeedleArm();

            // Build Group C: Center Joint
            BuildCenterJoint();

            // Add lighting
            AddLighting();

            return _compassModel;
        }

        private void BuildPencilArm()
        {
            var armGroup = new Model3DGroup();

            // Zone 3: Pencil body
            var pencilBody = new PencilBodyPart();
            pencilBody.Build();

            // Zone 4: Brand name text
            var brandText = new BrandNameTextPart();
            brandText.Build();

            // Zone 5: Lead type text
            var leadTypeText = new LeadTypeTextPart();
            leadTypeText.Build();
            
            // Zone 2: Ferrule
            var ferrule = new FerrulePart();
            ferrule.Build();

            // Zone 1: Eraser
            var eraser = new EraserPart();
            eraser.Build();

            // Zone 6: Wood tip
            var woodTip = new WoodTipPart();
            woodTip.Build();

            // Zone 7: Lead tip
            var leadTip = new LeadTipPart();
            leadTip.Build();

            // Zone 8: Metal clamp body
            var metalClamp = new MetalClampBodyPart();
            metalClamp.Build();

            // Zone 9: Adjustment screw
            var adjScrew = new AdjustmentScrewPart();
            adjScrew.Build();

            // Zone 10: Spring mechanism
            var spring = new SpringMechanismPart();
            spring.Build();

            // Zone 11: Grip pads
            var gripPads = new GripPadsPart();
            gripPads.Build();

            // Position pencil components vertically
            // Pencil starts from center joint (Y=0) and extends upward
            var pencilTransform = new TranslateTransform3D(0, 0, 0);
            pencilBody.Model.Transform = pencilTransform;

            // Text markings on pencil body (no transform, already positioned)
            armGroup.Children.Add(brandText.Model);
            armGroup.Children.Add(leadTypeText.Model);

            // Ferrule at top of pencil (after 60mm body)
            var ferruleTransform = new TranslateTransform3D(0, 60, 0);
            ferrule.Model.Transform = ferruleTransform;

            // Eraser at very top
            var eraserTransform = new TranslateTransform3D(0, 63, 0);
            eraser.Model.Transform = eraserTransform;

            // Wood tip at bottom (below center joint)
            var woodTipTransform = new TranslateTransform3D(0, -8, 0);
            woodTip.Model.Transform = woodTipTransform;

            // Lead tip below wood tip
            var leadTipTransform = new TranslateTransform3D(0, -16, 0);
            leadTip.Model.Transform = leadTipTransform;

            // Position metal clamp near center joint (Y=3-5mm from center)
            var metalClampTransform = new TranslateTransform3D(0, 3, 0);
            metalClamp.Model.Transform = metalClampTransform;

            // Position adjustment screw on side of clamp
            var screwTransform = new TranslateTransform3D(6, 5, 0);
            adjScrew.Model.Transform = screwTransform;

            // Position spring inside clamp
            var springTransform = new TranslateTransform3D(0, 4, 0);
            spring.Model.Transform = springTransform;

            // Position grip pads inside clamp
            var gripTransform = new TranslateTransform3D(0, 4, 0);
            gripPads.Model.Transform = gripTransform;

            armGroup.Children.Add(pencilBody.Model);
            armGroup.Children.Add(ferrule.Model);
            armGroup.Children.Add(eraser.Model);
            armGroup.Children.Add(woodTip.Model);
            armGroup.Children.Add(leadTip.Model);
            armGroup.Children.Add(metalClamp.Model);
            armGroup.Children.Add(adjScrew.Model);
            armGroup.Children.Add(spring.Model);
            armGroup.Children.Add(gripPads.Model);

            // Position entire pencil arm based on opening angle
            PositionPencilArm(armGroup);

            _compassModel.Children.Add(armGroup);
        }

        private void BuildNeedleArm()
        {
            var armGroup = new Model3DGroup();

            // Zone 16: Compass arm (oval rod)
            var arm = new CompassArmPart();
            arm.Build();

            // Zone 12: Needle tip
            var needleTip = new NeedleTipPart();
            needleTip.Build();

            // Zone 15: Needle shaft extension
            var needleShaft = new NeedleShaftExtensionPart();
            needleShaft.Build();

            // Zone 13: Needle mount (adjustable holder)
            var needleMount = new NeedleMountPart();
            needleMount.Build();

            // Zone 14: Locking mechanism
            var lockingMech = new LockingMechanismPart();
            lockingMech.Build();

            // Zone 18: Needle arm mount
            var armMount = new NeedleArmMountPart();
            armMount.Build();

            // Zone 17: Hinge joint
            var hingeJoint = new HingeJointPart();
            hingeJoint.Build();

            // Zone 19: Hinge rotation disc
            var rotationDisc = new HingeRotationDiscPart();
            rotationDisc.Build();

            // Position components along needle arm
            // Arm at base (0mm)
            armGroup.Children.Add(arm.Model);

            // Needle mount at end of arm (67mm)
            var mountTransform = new TranslateTransform3D(0, 67, 0);
            needleMount.Model.Transform = mountTransform;
            armGroup.Children.Add(needleMount.Model);

            // Locking mechanism on side of mount (67mm Y, 2mm X offset)
            var lockTransform = new TranslateTransform3D(2, 70, 0);
            lockingMech.Model.Transform = lockTransform;
            armGroup.Children.Add(lockingMech.Model);

            // Needle shaft inside mount (67mm)
            var shaftTransform = new TranslateTransform3D(0, 67, 0);
            needleShaft.Model.Transform = shaftTransform;
            armGroup.Children.Add(needleShaft.Model);

            // Needle tip at end (72mm = 67 + 5 shaft)
            var needleTransform = new TranslateTransform3D(0, 72, 0);
            needleTip.Model.Transform = needleTransform;
            armGroup.Children.Add(needleTip.Model);

            // Arm mount at base of arm (0mm)
            var armMountTransform = new TranslateTransform3D(0, 0, 0);
            armMount.Model.Transform = armMountTransform;
            armGroup.Children.Add(armMount.Model);

            // Hinge joint through arm mount (-2mm to center in mount)
            var hingeTransform = new TranslateTransform3D(0, 2.5, 0);
            hingeJoint.Model.Transform = hingeTransform;
            armGroup.Children.Add(hingeJoint.Model);

            // Rotation disc at hinge base (-0.5mm to sit below hinge)
            var discTransform = new TranslateTransform3D(0, -0.5, 0);
            rotationDisc.Model.Transform = discTransform;
            armGroup.Children.Add(rotationDisc.Model);

            // Position entire needle arm based on opening angle
            PositionNeedleArm(armGroup);

            _compassModel.Children.Add(armGroup);
        }

        private void BuildCenterJoint()
        {
            var jointGroup = new Model3DGroup();

            // Zone 21: Bottom disc (Ø12mm × 2mm)
            var bottomDisc = new JointDiscPart(12.0, 2.0, false);
            bottomDisc.Build();

            // Zone 20: Top disc (Ø10mm × 1.5mm)
            var topDisc = new JointDiscPart(10.0, 1.5, true);
            topDisc.Build();

            // Zone 19: Main rotation axis (Ø2mm × 8mm)
            var rotationAxis = new RotationAxisPart();
            rotationAxis.Build();

            // Zone 22: Center screw
            var centerScrew = new CenterScrewPart();
            centerScrew.Build();

            // Position discs
            var bottomTransform = new TranslateTransform3D(0, -1, 0);
            bottomDisc.Model.Transform = bottomTransform;

            var topTransform = new TranslateTransform3D(0, 1, 0);
            topDisc.Model.Transform = topTransform;

            // Position rotation axis through center of discs
            var axisTransform = new TranslateTransform3D(0, -4, 0);
            rotationAxis.Model.Transform = axisTransform;

            // Position center screw through joint
            var screwTransform = new TranslateTransform3D(0, -2, 0);
            centerScrew.Model.Transform = screwTransform;

            jointGroup.Children.Add(bottomDisc.Model);
            jointGroup.Children.Add(topDisc.Model);
            jointGroup.Children.Add(rotationAxis.Model);
            jointGroup.Children.Add(centerScrew.Model);

            _compassModel.Children.Add(jointGroup);
        }

        private void PositionPencilArm(Model3DGroup armGroup)
        {
            // Pencil arm rotates around center joint at origin
            // Right side: rotate +halfAngle around Z axis
            double halfAngle = _openingAngle / 2.0;

            // Rotate around Z axis only - NO translation needed
            // Arm extends along +Y axis from center (0,0,0)
            var rotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), halfAngle));

            armGroup.Transform = rotation;
        }

        private void PositionNeedleArm(Model3DGroup armGroup)
        {
            // Needle arm rotates around center joint at origin
            // Left side: rotate -halfAngle around Z axis
            double halfAngle = _openingAngle / 2.0;

            // Rotate around Z axis only - NO translation needed
            // Arm extends along +Y axis from center (0,0,0)
            var rotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -halfAngle));

            armGroup.Transform = rotation;
        }

        private void UpdateGeometry()
        {
            // Rebuild the entire compass when parameters change
            Build();
        }

        private void AddLighting()
        {
            // Key light from top-left (as per spec)
            var keyLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
            _compassModel.Children.Add(keyLight);

            // Fill light from right
            var fillLight = new DirectionalLight(Color.FromRgb(200, 200, 200), new Vector3D(1, 0, 0.5));
            _compassModel.Children.Add(fillLight);

            // Ambient light
            var ambientLight = new AmbientLight(Color.FromRgb(80, 80, 80));
            _compassModel.Children.Add(ambientLight);
        }
    }

    /// <summary>
    /// State manager for 3D compass
    /// </summary>
    public class Compass3DState
    {
        public double CenterX { get; set; } = 0;
        public double CenterY { get; set; } = 0;
        public double CenterZ { get; set; } = 0;
        public double OpeningAngle { get; set; } = 60.0;
        public double Radius { get; set; } = 50.0;
        public double RotationX { get; set; } = -30.0; // Camera angle
        public double RotationY { get; set; } = 0.0;
        public double RotationZ { get; set; } = 0.0;
        public double Zoom { get; set; } = 1.0;
    }
}
