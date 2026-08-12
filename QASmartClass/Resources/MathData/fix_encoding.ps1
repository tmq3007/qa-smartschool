$path = "d:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\Resources\MathData\Phase4_Data"
$utf8BOM = New-Object System.Text.UTF8Encoding $true
$repChar = [char]0xFFFD

$files = @(
    "G01_CH01_So_Tu_Nhien.json",
    "G02_CH01_Do_Luong.json",
    "G03_CH01_Phan_So.json",
    "G04_CH01_Hinh_Hoc_CB.json",
    "G05_CH01_Toan_Loi_Van.json",
    "G07_CH01_Dai_So_PT.json"
)

foreach ($f in $files) {
    $filePath = Join-Path $path $f
    $content = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
    $r = "$repChar"

    # === PASS 1: Multi-char sequences (most specific first) ===
    
    # ố = FFFD followed by ' (apostrophe/quote context)
    $content = $content.Replace("$($r)'", [string][char]0x1ED1 + "'")
    # Actually the pattern is: the FFFD replaced the first byte, and ' is the mapped second byte
    # Let me use simpler string-based approach

    # Write checkpoint
    Write-Output "Processing $f..."

    # === Use contextual string replacement ===
    # Strategy: find FFFD + surrounding text, replace with correct Vietnamese
    
    # Common words with FFFD - sorted by specificity (longest match first)
    $replacements = @(
        # FFFD' patterns - ố (the ' is part of surrounding text, not the char)
        @("S$($r)' ", "Số "),
        @("s$($r)' ", "số "),
        @("s$($r)'.", "số."),
        @("s$($r)',", "số,"),
        @("s$($r)'""", "số"""),
        @("s$($r)'\", "số\"),

        # Điểm/điểm patterns  
        @("$($r)i$($r)fm", "điểm"),
        @("$($r)i$($r)fn", "điện"),
        @("Di$($r)fn", "Diện"),
        @("di$($r)fn", "diện"),
        @("Ki$($r)fm", "Kiểm"),
        @("ki$($r)fm", "kiểm"),
        @("Hi$($r)fu", "Hiệu"),
        @("hi$($r)fu", "hiệu"),
        @("Bi$($r)fu", "Biểu"),
        @("bi$($r)fu", "biểu"),
        @("Nhi$($r)fu", "Nhiều"),
        @("nhi$($r)fu", "nhiều"),
        @("chi$($r)fu", "chiều"),
        @("li$($r)fu", "liều"),
        @("Ti$($r)fu", "Tiểu"),
        @("ti$($r)fu", "tiểu"),
        @("nghi$($r)fm", "nghiệm"),
        @("nghi$($r)fu", "nghiều"),

        # đ patterns
        @("$($r)'ến", "đến"),
        @("$($r)'ều", "đều"),
        @("$($r)'ơn", "đơn"),
        @("$($r)'ôi", "đôi"),
        @("$($r)'ường", "đường"),
        @("$($r)'ứng", "đứng"),
        @("$($r)'ủ", "đủ"),
        @("$($r)'úng", "đúng"),
        @("$($r)'ó", "đó"),
        @("$($r)'ược", "được"),
        @("$($r)'ây", "đây"),
        @("$($r)'ại", "đại"),
        @("$($r)'ỉnh", "đỉnh"),
        @("$($r)'áy", "đáy"),
        @("$($r)'ổi", "đổi"),
        @("$($r)'oạn", "đoạn"),
        @("$($r)'oạ", "đoạ"),
        @("$($r)'ôn", "đôn"),
        @("$($r)'i ", "đi "),
        @("$($r)'i.", "đi."),

        # ộ patterns: cộng, một, hộp, gộp
        @("c$($r)Tng", "cộng"),
        @("C$($r)Tng", "Cộng"),
        @("C$($r)~NG", "CỘNG"),
        @("m$($r)Tt", "một"),
        @("M$($r)Tt", "Một"),
        @("h$($r)Tp", "hộp"),
        @("H$($r)Tp", "Hộp"),
        @("g$($r)Tp", "gộp"),
        @("G$($r)Tp", "Gộp"),
        @("g$($r)Tc", "gốc"),
        @("r$($r)Tng", "rộng"),

        # ổ patterns: tổng
        @("t$($r).ng", "tổng"),
        @("T$($r).ng", "Tổng"),

        # ỗ patterns: mỗi, đỗi
        @("m$($r)-i", "mỗi"),
        @("M$($r)-i", "Mỗi"),

        # ớ patterns: nhớ, lớn, lớp, với, bớt
        @("nh$($r)>", "nhớ"),
        @("l$($r)>n", "lớn"),
        @("L$($r)>n", "Lớn"),
        @("l$($r)>p", "lớp"),
        @("L$($r)>p", "Lớp"),
        @("v$($r)>i", "với"),
        @("V$($r)>i", "Với"),
        @("b$($r)>t", "bớt"),
        @("B$($r)>t", "Bớt"),

        # ị patterns
        @("v$($r)<", "vị"),

        # ở patterns
        @("$($r)Y ", "ở "),
        @("$($r)Y c", "ở c"),

        # × (multiplication sign)
        @(" $($r)- ", " × "),
        @("$($r)-2", "×2"),
        @("$($r)-3", "×3"),
        @("$($r)-4", "×4"),
        @("$($r)-5", "×5"),
        @("$($r)-6", "×6"),
        @("$($r)-7", "×7"),
        @("$($r)-8", "×8"),
        @("$($r)-9", "×9"),

        # ✓ (checkmark)
        @(" $($r)o""", " ✓"""),

        # ăn, ắ
        @("$($r)fn", "ăn"),
        @("$($r)fn.", "ăn."),
        
        # ướ patterns
        @("ư$($r)>c", "ước"),
        @("th$($r)>c", "thước"),

        # ũ patterns  
        @("du$($r)-i", "duỗi"),

        # Ô tô pattern
        @("$($r)"" tô", "Ô tô"),
        @("$($r)"" t$($r)T", "Ô tô"),

        # Specific word fixes
        @("t$($r)'c", "tốc"),
        @("n$($r)fng", "năng"),
        @("N$($r)fng", "Năng"),
        @("ph$($r)' ", "phố "),
        @("chuy$($r)fn", "chuyển"),
        @("Chuy$($r)fn", "Chuyển"),
        @("Tu$($r).i", "Tuổi"),
        @("tu$($r).i", "tuổi"),
        @("$($r)'$($r)""ng", "đồng"),
        @("xư$($r)Yng", "xưởng"),

        # Remaining single FFFD patterns
        @("s$($r)' ", "số "),
        @("V$($r)fn", "Văn"),
        @("v$($r)fn", "văn")
    )

    foreach ($rep in $replacements) {
        $content = $content.Replace($rep[0], $rep[1])
    }

    # Write back with UTF-8 BOM
    [System.IO.File]::WriteAllText($filePath, $content, $utf8BOM)
    
    # Count remaining
    $remaining = 0
    foreach ($c in $content.ToCharArray()) {
        if ([int]$c -eq 0xFFFD) { $remaining++ }
    }
    Write-Output "${f}: $remaining replacement chars remaining"
}

Write-Output "Done!"
