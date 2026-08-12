# 🚀 QUICK START GUIDE - Compass Tool 3D

## ⚡ BUILD & RUN (5 phút)

### **Step 1: Restore Packages**
```powershell
cd "d:\JOB\QA SmartSchool\QA SmartScreen v1.0\QASmartTouch"
dotnet restore
```

### **Step 2: Build Project**
```powershell
dotnet build --configuration Release
```

### **Step 3: Run Application**
```powershell
dotnet run
```

---

## 🧪 TEST STANDALONE

### **Option A: Từ Visual Studio**
1. Open `QASmartTouch.sln`
2. Set `QASmartTouch` as startup project
3. Thêm test code vào `App.xaml.cs`:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    // TEST: Launch Compass 3D directly
    var compass3D = new Forms.Form2_18_CompassTool_3D();
    compass3D.Show();
}
```

4. Press F5 hoặc Ctrl+F5

### **Option B: PowerShell Script**

Tạo file `test_compass3d.ps1`:

```powershell
# Navigate to project folder
Set-Location "d:\JOB\QA SmartSchool\QA SmartScreen v1.0\QASmartTouch"

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore

# Build
Write-Host "Building project..." -ForegroundColor Cyan
dotnet build --configuration Release

# Run
Write-Host "Launching Compass 3D..." -ForegroundColor Green
dotnet run
```

Chạy:
```powershell
.\test_compass3d.ps1
```

---

## 📋 CHECKLIST TRƯỚC KHI RUN

- [ ] .NET 8.0 SDK đã cài đặt
- [ ] HelixToolkit.Wpf package đã được thêm vào .csproj
- [ ] Tất cả files đã được tạo:
  - [ ] `Models/Compass3DPart.cs`
  - [ ] `Services/Compass3DBuilder.cs`
  - [ ] `Forms/Form2_18_CompassTool_3D.xaml`
  - [ ] `Forms/Form2_18_CompassTool_3D.xaml.cs`
  - [ ] `Forms/CompassTool3D_Launcher.cs`
- [ ] No compile errors

---

## 🎮 QUICK CONTROLS REFERENCE

| Action | Control |
|--------|---------|
| **Rotate** | Left-click + drag |
| **Zoom** | Mouse wheel |
| **Pan** | Ctrl + Left-click + drag |
| **Reset** | Click "Reset" button |
| **Change Angle** | Drag "Opening Angle" slider |
| **Change Radius** | Drag "Radius" slider |
| **Animate** | Click "🔄 Animate" button |
| **Export** | Click "📸 Export View" button |
| **Info** | Click "ℹ️ Info" button |

---

## 🔧 IF BUILD FAILS

### **Error: HelixToolkit.Wpf not found**
```powershell
dotnet add package HelixToolkit.Wpf --version 2.25.0
```

### **Error: Namespace not found**
Check file locations:
```powershell
# Should exist:
dir "Models\Compass3DPart.cs"
dir "Services\Compass3DBuilder.cs"
dir "Forms\Form2_18_CompassTool_3D.xaml"
dir "Forms\Form2_18_CompassTool_3D.xaml.cs"
```

### **Error: Cannot find type**
Clean and rebuild:
```powershell
dotnet clean
dotnet restore
dotnet build
```

---

## 🎯 EXPECTED RESULT

Khi run thành công, bạn sẽ thấy:

1. **Window xuất hiện** với:
   - Blue gradient title bar
   - "Professional Compass Tool - 3D Demo"
   - 3D viewport hiển thị compass
   - Grid helper
   - Control panel ở dưới

2. **Compass 3D** với:
   - 2 arms (pencil bên phải, needle bên trái)
   - Yellow pencil body với red eraser
   - Gray metal arms
   - Center joint
   - Góc mở mặc định: 60°
   - Radius: 50mm

3. **Interactive controls**:
   - Sliders để thay đổi angle và radius
   - 5 camera view buttons
   - 3 action buttons (Export, Animate, Info)

4. **Camera trackball**:
   - Drag để xoay
   - Scroll để zoom
   - ViewCube ở góc màn hình

---

## 📸 SCREENSHOTS

### **Expected Layout:**
```
┌─────────────────────────────────────────┐
│ 🔧 Professional Compass Tool - 3D  [✕] │ ← Title bar
├─────────────────────────────────────────┤
│                                         │
│          [3D Compass Viewport]          │ ← HelixViewport3D
│          with Grid & ViewCube           │
│                                         │
├─────────────────────────────────────────┤
│ Opening Angle: [========] 60°          │
│ Radius (mm):   [========] 50 mm        │ ← Sliders
│                                         │
│ Camera View: [Top][Front][Side][Iso]   │ ← Camera buttons
│                                         │
│  [📸 Export] [🔄 Animate] [ℹ️ Info]   │ ← Actions
└─────────────────────────────────────────┘
```

---

## 🎉 SUCCESS!

Nếu bạn thấy window như mô tả trên → **Congratulations!** 

Bạn đã có một công cụ Compass 3D chuyên nghiệp hoàn chỉnh!

**Next steps:**
1. ✅ Test all controls
2. ✅ Try different angles (10° - 180°)
3. ✅ Test camera views
4. ✅ Export a screenshot
5. ✅ Try animation mode
6. ✅ Read full documentation

---

## 📞 NEED HELP?

- Read: `DOCUMENTATION_Compass3D_Complete.md`
- Check: Specification file `1.1.4.Compa_That.txt`
- Debug: Add breakpoints in `Window_Loaded()` và `BuildCompassModel()`

**Happy coding! 🚀**
