# ================================================================
# TEST COMPASS 3D TOOL - STANDALONE LAUNCHER
# ================================================================
# This script launches the 3D Compass Tool independently
# Usage: .\TEST_COMPASS_3D.ps1
# ================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPASS TOOL 3D - STANDALONE TEST" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
$projectPath = "d:\JOB\QA SmartSchool\QA SmartScreen v1.0\QASmartScreen"
Set-Location $projectPath

Write-Host "[1/3] Checking project files..." -ForegroundColor Green

# Check if all required files exist
$requiredFiles = @(
    "Models\Compass3DPart.cs",
    "Services\Compass3DBuilder.cs",
    "Forms\Form2_18_CompassTool_3D.xaml",
    "Forms\Form2_18_CompassTool_3D.xaml.cs",
    "Forms\CompassTool3D_Launcher.cs"
)

$allFilesExist = $true
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file" -ForegroundColor DarkGreen
    } else {
        Write-Host "  ✗ $file NOT FOUND!" -ForegroundColor Red
        $allFilesExist = $false
    }
}

if (-not $allFilesExist) {
    Write-Host ""
    Write-Host "ERROR: Missing required files!" -ForegroundColor Red
    Write-Host "Please ensure all files are created." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "[2/3] Building project..." -ForegroundColor Green

# Build the project
$buildResult = dotnet build --configuration Release --no-restore 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Build succeeded!" -ForegroundColor DarkGreen
} else {
    Write-Host "  ✗ Build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Build output:" -ForegroundColor Yellow
    Write-Host $buildResult
    exit 1
}

Write-Host ""
Write-Host "[3/3] Launching Compass 3D Tool..." -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONTROLS:" -ForegroundColor Yellow
Write-Host "  • Drag: Rotate view" -ForegroundColor White
Write-Host "  • Scroll: Zoom in/out" -ForegroundColor White
Write-Host "  • Sliders: Adjust angle & radius" -ForegroundColor White
Write-Host "  • Buttons: Change camera views" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Run the application
# Note: This will launch the full application. 
# To launch only the Compass 3D tool, you would need to modify App.xaml.cs
# For now, open the application and manually trigger the compass tool from menu

dotnet run --no-build --configuration Release

Write-Host ""
Write-Host "Application closed." -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "For more information, see:" -ForegroundColor Yellow
Write-Host "  • QUICKSTART_Compass3D.md" -ForegroundColor White
Write-Host "  • DOCUMENTATION_Compass3D_Complete.md" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
