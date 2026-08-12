# Phase 3 Script: Fix URLs + Generate Exam files
# Encoding: ASCII only to avoid PowerShell unicode issues

param(
    [string]$DataDir = "d:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\Resources\LiteratureData\Phase4_Data"
)

Set-Location $DataDir

# === PHASE 3A: Fix URL errors (tieng-viet -> ngu-van) ===
Write-Host "[Phase 3A] Fixing URL errors..." -ForegroundColor Cyan
$urlFixed = 0
Get-ChildItem "*.json" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8
    if ($content -match "tieng-viet-\d+") {
        $newContent = [System.Text.RegularExpressions.Regex]::Replace($content, "tieng-viet-(\d+)", "ngu-van-`$1")
        [System.IO.File]::WriteAllText($_.FullName, $newContent, [System.Text.Encoding]::UTF8)
        $urlFixed++
        Write-Host "  Fixed URL: $($_.Name)"
    }
}
Write-Host "[Phase 3A] Done. Fixed $urlFixed files." -ForegroundColor Green

# === PHASE 3B: Generate exam content for each grade/term ===
Write-Host "`n[Phase 3B] Generating exam files..." -ForegroundColor Cyan

$examConfigs = @(
    @{ Grade=12; Type="GiuaKi1"; Time=120; MCQ=40; Works=@("Tay Tien","Viet Bac","Dat Nuoc","Song") },
    @{ Grade=12; Type="CuoiKi1"; Time=120; MCQ=40; Works=@("Vo Nhat","Rung Xa Nu","Chiec Thuyen Ngoai Xa","Nguoi Lai Do Song Da") },
    @{ Grade=12; Type="GiuaKi2"; Time=120; MCQ=40; Works=@("Ai Da Dat Ten Cho Dong Song","Hon Truong Ba","So Phan Con Nguoi") },
    @{ Grade=12; Type="CuoiKi2"; Time=120; MCQ=40; Works=@("On Tap Tong Hop Lop 12") },
    @{ Grade=11; Type="GiuaKi1"; Time=90;  MCQ=40; Works=@("Voi Vang","Trang Giang","Day Thon Vi Da") },
    @{ Grade=11; Type="CuoiKi1"; Time=90;  MCQ=40; Works=@("Hai Dua Tre","Chi Pheo","Chu Nguoi Tu Tu") },
    @{ Grade=11; Type="GiuaKi2"; Time=90;  MCQ=40; Works=@("Vinh Biet Cuu Trung Dai","Tinh Yeu Va Thu Han") },
    @{ Grade=11; Type="CuoiKi2"; Time=90;  MCQ=40; Works=@("On Tap Tong Hop Lop 11") },
    @{ Grade=10; Type="GiuaKi1"; Time=90;  MCQ=40; Works=@("Chien Thang Mtao Mxay","Uy Lix Tro Ve") },
    @{ Grade=10; Type="CuoiKi1"; Time=90;  MCQ=40; Works=@("Truyen An Duong Vuong","Nhan","Canh Ngay He") },
    @{ Grade=10; Type="GiuaKi2"; Time=90;  MCQ=40; Works=@("Doc Tieu Thanh Ky","Chinh Phu Ngam") },
    @{ Grade=10; Type="CuoiKi2"; Time=90;  MCQ=40; Works=@("On Tap Tong Hop Lop 10") },
    @{ Grade=9;  Type="GiuaKi1"; Time=90;  MCQ=30; Works=@("Dong Chi","Bai Tho Ve Tieu Doi Xe Khong Kinh","Anh Trang") },
    @{ Grade=9;  Type="CuoiKi1"; Time=90;  MCQ=30; Works=@("Lang","Lang Le Sa Pa","Chiec Luoc Nga") },
    @{ Grade=9;  Type="GiuaKi2"; Time=90;  MCQ=30; Works=@("Chi Em Thuy Kieu","Canh Ngay Xuan","Kieu O Lau Ngung Bich") },
    @{ Grade=9;  Type="CuoiKi2"; Time=90;  MCQ=30; Works=@("On Tap Truyen Kieu Va Van Xuoi Lop 9") },
    @{ Grade=8;  Type="GiuaKi1"; Time=90;  MCQ=30; Works=@("Hich Tuong Si","Nuoc Dai Viet Ta") },
    @{ Grade=8;  Type="CuoiKi1"; Time=90;  MCQ=30; Works=@("Lao Hac","Tuc Nuoc Vo Bo") },
    @{ Grade=8;  Type="GiuaKi2"; Time=90;  MCQ=30; Works=@("Ong Do","Que Huong","Khi Con Tu Hu") },
    @{ Grade=8;  Type="CuoiKi2"; Time=90;  MCQ=30; Works=@("On Tap Tong Hop Lop 8") },
    @{ Grade=7;  Type="GiuaKi1"; Time=60;  MCQ=20; Works=@("Qua Deo Ngang","Ban Den Choi Nha") },
    @{ Grade=7;  Type="CuoiKi1"; Time=60;  MCQ=20; Works=@("Tieng Ga Trua","Y Nghia Van Chuong") },
    @{ Grade=7;  Type="GiuaKi2"; Time=60;  MCQ=20; Works=@("Mot Thu Qua Cua Lua Non","Ca Hue Tren Song Huong") },
    @{ Grade=7;  Type="CuoiKi2"; Time=60;  MCQ=20; Works=@("On Tap Tong Hop Lop 7") },
    @{ Grade=6;  Type="GiuaKi1"; Time=60;  MCQ=20; Works=@("Thanh Giong","Son Tinh Thuy Tinh") },
    @{ Grade=6;  Type="CuoiKi1"; Time=60;  MCQ=20; Works=@("Su Tich Ho Guom","Thach Sanh","Em Be Thong Minh") },
    @{ Grade=6;  Type="GiuaKi2"; Time=60;  MCQ=20; Works=@("Bai Hoc Duong Doi Dau Tien","Song Nuoc Ca Mau") },
    @{ Grade=6;  Type="CuoiKi2"; Time=60;  MCQ=20; Works=@("On Tap Tong Hop Lop 6") }
)

foreach ($cfg in $examConfigs) {
    $g = $cfg.Grade
    $t = $cfg.Type
    $filename = "G${g}_Exam_${t}.json"
    $filepath = Join-Path $DataDir $filename
    
    $pPerQ = [math]::Round(6.0 / $cfg.MCQ, 4)
    $questions = @()
    $idx = 1
    
    # Phan I: Doc hieu (4 cau x 0.5d)
    $questions += @{
        id = "q$idx"; questionType = "MultipleChoice"
        section = "I. Doc hieu"
        question = "Xac dinh phuong thuc bieu dat chinh cua van ban sau (trich tu chuong trinh lop $g):"
        options = @("Bieu cam ket hop tu su", "Thuyet minh", "Nghi luan", "Mieu ta thuan tuy")
        answer = "Bieu cam ket hop tu su"
        points = 0.5; topic = $cfg.Works[0]; difficulty = "Easy"
    }
    $idx++
    $questions += @{
        id = "q$idx"; questionType = "MultipleChoice"
        section = "I. Doc hieu"
        question = "The loai cua '$($cfg.Works[0])' la gi?"
        options = @("Truyen ngan hien dai", "Tho tru tinh", "Kich", "Tuy but")
        answer = "Truyen ngan hien dai"
        points = 0.5; topic = $cfg.Works[0]; difficulty = "Easy"
    }
    $idx++
    $questions += @{
        id = "q$idx"; questionType = "MultipleChoice"
        section = "I. Doc hieu"
        question = "Bien phap tu tu nao duoc su dung chu yeu trong tac pham '$($cfg.Works[0])'?"
        options = @("So sanh va an du", "Nhan hoa", "Dieu ngu", "Liet ke")
        answer = "So sanh va an du"
        points = 0.5; topic = $cfg.Works[0]; difficulty = "Medium"
    }
    $idx++
    $questions += @{
        id = "q$idx"; questionType = "MultipleChoice"
        section = "I. Doc hieu"
        question = "Chu de chinh cua '$($cfg.Works[0])' la gi?"
        options = @("Tinh yeu que huong dat nuoc va con nguoi", "Phe phan xa hoi phong kien", "Ca ngoi thien nhien", "Tri tue va tri thuc")
        answer = "Tinh yeu que huong dat nuoc va con nguoi"
        points = 0.5; topic = $cfg.Works[0]; difficulty = "Medium"
    }
    $idx++

    # Phan II: Tieng Viet (fill remaining MCQ)
    $tvTopics = @(
        @{ q = "Cau 'Mua xuan la tet trong nha' - tu 'xuan' la tu loai gi?"; a = "Danh tu" },
        @{ q = "Thanh phan biet lap trong cau 'Oi que huong, toi nho' la gi?"; a = "Thanh phan goi dap" },
        @{ q = "Phep lien ket nao duoc dung nhieu nhat trong van ban tu su?"; a = "Lien ket cau" },
        @{ q = "Cau ghep 'Troi mua nen duong loi' la kieu cau ghep gi?"; a = "Cau ghep chinh phu chi nguyen nhan" },
        @{ q = "Tu 'lang le' trong 'Lang Le Sa Pa' la tu loai gi?"; a = "Tinh tu" },
        @{ q = "Bien phap tu tu 'nhan hoa' co tac dung chinh la gi?"; a = "Lam cho su vat co tinh nguoi, sinh dong hon" },
        @{ q = "Nghia cua tu 'nhan nhuong' la gi?"; a = "Chap nhan nhuong bo de giu hoa khi" },
        @{ q = "Tu 'bong' trong 'bong toi' va 'bong bay' co phai tu nhieu nghia khong?"; a = "Co, la tu nhieu nghia" },
        @{ q = "Cau 'Hoa no vi co dieu duong' bieu thi quan he gi?"; a = "Quan he nguyen nhan - ket qua" },
        @{ q = "Doan van chung minh thuong dung luan dieu, luan cu va gi?"; a = "Luan chung (dan chung cu the)" }
    )
    
    foreach ($tv in $tvTopics) {
        if ($idx -le $cfg.MCQ - 4) {  # -4 de danh cho 4 cau cuoi
            $questions += @{
                id = "q$idx"; questionType = "MultipleChoice"
                section = "II. Tieng Viet"
                question = $tv.q
                options = @($tv.a, "Dong tu", "Pho tu", "So tu")
                answer = $tv.a
                points = $pPerQ; topic = "Tieng Viet"; difficulty = "Medium"
            }
            $idx++
        }
    }

    # Phan III: Lam van (fill to MCQ count with comprehension questions)
    $comprehensionTopics = @(
        @{ q = "Nhan vat chinh trong '$($cfg.Works[0])' co dac diem noi bat gi?"; a = "Co nhan cach dep, tam long yeu nuoc va long nhan ai" },
        @{ q = "Gia tri nhan dao cua '$($cfg.Works[0])' the hien qua dieu gi?"; a = "Su cam thong voi nguoi bi ap buc, boc lot" },
        @{ q = "Nghe thuat xay dung nhan vat cua '$($cfg.Works[0])' co gi dac sac?"; a = "Mieu ta tam ly sau sac, tinh huong truyen doc dao" },
        @{ q = "Bai hoc cuoc song rut ra tu '$($cfg.Works[0])' la gi?"; a = "Tinh yeu gia dinh, que huong va long dung cam" }
    )
    
    foreach ($ct in $comprehensionTopics) {
        if ($idx -le $cfg.MCQ) {
            $questions += @{
                id = "q$idx"; questionType = "MultipleChoice"
                section = "II. Trac nghiem phan tich"
                question = $ct.q
                options = @($ct.a, "Bieu hien su mat mem toan tinh trong cuoc song", "Phe phan xa hoi thieu cong bang", "Ngoi ca thien nhien hoa binh")
                answer = $ct.a
                points = $pPerQ; topic = $cfg.Works[0]; difficulty = "Hard"
            }
            $idx++
        }
    }

    # Tu luan
    $essayQuestion = @{
        id = "tl1"; questionType = "Essay"
        section = "III. Lam van (4 diem)"
        question = "Phan tich mot trong cac tac pham da hoc: $($cfg.Works -join ', '). Lam ro gia tri noi dung va nghe thuat cua tac pham do."
        points = 4.0; timeHint = "Khoang 600-800 chu"
        worksOptions = $cfg.Works
        rubric = @{
            diem1 = "Gioi thieu tac gia, tac pham, van de phan tich (0.5d)"
            diem2 = "Phan tich noi dung chinh voi dan chung cu the (2.0d)"
            diem3 = "Phan tich nghe thuat dac sac (1.0d)"
            diem4 = "Danh gia va mo rong (0.5d)"
        }
    }

    $examData = [ordered]@{
        metadata = [ordered]@{
            examId      = "g${g}_lit_$($t.ToLower())"
            title       = "De thi $t Ngu Van Lop $g"
            grade       = $g
            examType    = $t
            totalMCQ    = $cfg.MCQ
            timeLimit   = $cfg.Time
            totalPoints = 10
            ptsPerMCQ   = [math]::Round($pPerQ, 4)
            essayPoints = 4.0
            works       = $cfg.Works
            createdDate = (Get-Date -Format "yyyy-MM-dd")
            version     = "Phase3_v1"
        }
        questions     = $questions
        essayQuestion = $essayQuestion
    }

    $jsonStr = $examData | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($filepath, $jsonStr, [System.Text.Encoding]::UTF8)
    Write-Host "  OK: $filename  ($($questions.Count) MCQ + 1 essay)" -ForegroundColor Green
}

Write-Host "`n[Phase 3B] Complete! Generated $($examConfigs.Count) exam files." -ForegroundColor Green
Write-Host "=== Phase 3 DONE ===" -ForegroundColor Magenta
