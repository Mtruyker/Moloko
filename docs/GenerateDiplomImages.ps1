param(
    [string]$OutputDirectory = ".\docs\images"
)

Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null

function New-Canvas([int]$Width, [int]$Height) {
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $graphics.Clear([System.Drawing.Color]::FromArgb(244, 246, 243))
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

function Brush([int]$r, [int]$g, [int]$b) {
    return New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($r, $g, $b))
}

function Pen-Color([int]$r, [int]$g, [int]$b, [float]$width = 1) {
    return New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb($r, $g, $b), $width)
}

function Font-Segoe([float]$size, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular) {
    return New-Object System.Drawing.Font("Segoe UI", $size, $style)
}

function Draw-Text($g, [string]$text, [float]$x, [float]$y, [float]$size = 14, [System.Drawing.Color]$color = [System.Drawing.Color]::Black, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular, [float]$width = 1000) {
    $font = Font-Segoe $size $style
    $brush = New-Object System.Drawing.SolidBrush($color)
    $rect = New-Object System.Drawing.RectangleF($x, $y, $width, 200)
    $format = New-Object System.Drawing.StringFormat
    $format.Trimming = [System.Drawing.StringTrimming]::EllipsisWord
    $g.DrawString($text, $font, $brush, $rect, $format)
    $font.Dispose()
    $brush.Dispose()
    $format.Dispose()
}

function Fill-RoundedRect($g, [float]$x, [float]$y, [float]$w, [float]$h, [float]$r, $brush, $pen = $null) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.FillPath($brush, $path)
    if ($pen -ne $null) { $g.DrawPath($pen, $path) }
    $path.Dispose()
}

function Draw-Header($g, [string]$subtitle) {
    $dark = Brush 32 62 53
    $g.FillRectangle($dark, 0, 0, 1400, 92)
    Draw-Text $g "КФХ Жукушева Куанышкали Насимулловича" 28 18 21 ([System.Drawing.Color]::White) ([System.Drawing.FontStyle]::Bold) 900
    Draw-Text $g $subtitle 28 51 12 ([System.Drawing.Color]::FromArgb(221,233,228)) ([System.Drawing.FontStyle]::Regular) 1050
    $dark.Dispose()
}

function Draw-TabBar($g, [string]$active) {
    $tabs = @("Главная","Поступление","Партии","Качество","Склад","Отгрузка","Отчеты","Справочники","Аудит")
    $x = 22
    foreach ($tab in $tabs) {
        $isActive = $tab -eq $active
        $brush = if ($isActive) { Brush 40 92 77 } else { Brush 231 237 232 }
        $pen = Pen-Color 210 220 214
        Fill-RoundedRect $g $x 110 132 38 6 $brush $pen
        $color = if ($isActive) { [System.Drawing.Color]::White } else { [System.Drawing.Color]::FromArgb(30,43,37) }
        Draw-Text $g $tab ($x + 14) 119 11 $color ([System.Drawing.FontStyle]::Regular) 105
        $brush.Dispose()
        $pen.Dispose()
        $x += 142
    }
}

function Draw-Table($g, [float]$x, [float]$y, [float]$w, [string[]]$headers, [object[][]]$rows) {
    $border = Pen-Color 215 222 216
    $headerBrush = Brush 238 243 239
    $lineBrush = Brush 255 255 255
    $g.FillRectangle($lineBrush, $x, $y, $w, 40 + ($rows.Count * 34))
    $g.FillRectangle($headerBrush, $x, $y, $w, 36)
    $g.DrawRectangle($border, $x, $y, $w, 40 + ($rows.Count * 34))
    $colW = $w / $headers.Count
    for ($i=0; $i -lt $headers.Count; $i++) {
        Draw-Text $g $headers[$i] ($x + 10 + $i*$colW) ($y + 9) 10 ([System.Drawing.Color]::FromArgb(72,85,78)) ([System.Drawing.FontStyle]::Bold) ($colW - 12)
    }
    for ($r=0; $r -lt $rows.Count; $r++) {
        $ry = $y + 36 + ($r * 34)
        if ($r % 2 -eq 1) {
            $alt = Brush 248 250 248
            $g.FillRectangle($alt, $x, $ry, $w, 34)
            $alt.Dispose()
        }
        $g.DrawLine($border, $x, $ry, $x + $w, $ry)
        for ($c=0; $c -lt $headers.Count; $c++) {
            Draw-Text $g ([string]$rows[$r][$c]) ($x + 10 + $c*$colW) ($ry + 8) 10 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Regular) ($colW - 12)
        }
    }
    $border.Dispose()
    $headerBrush.Dispose()
    $lineBrush.Dispose()
}

function Draw-Dashboard {
    $canvas = New-Canvas 1400 850
    $g = $canvas.Graphics
    Draw-Header $g "Контроль качества, учет партий, склад, отгрузка и подготовка данных для ФГИС Меркурий"
    Draw-TabBar $g "Главная"
    $metrics = @(
        @("Остаток","1940,0 л",[System.Drawing.Color]::FromArgb(30,43,37)),
        @("Всего партий","10",[System.Drawing.Color]::FromArgb(30,43,37)),
        @("На анализе","2",[System.Drawing.Color]::FromArgb(30,43,37)),
        @("Заблокировано","1",[System.Drawing.Color]::FromArgb(158,47,56)),
        @("Просрочено","0",[System.Drawing.Color]::FromArgb(158,102,47)),
        @("Отгрузки сегодня","1",[System.Drawing.Color]::FromArgb(30,43,37))
    )
    $x=28
    foreach ($m in $metrics) {
        $white = Brush 255 255 255
        $pen = Pen-Color 215 222 216
        Fill-RoundedRect $g $x 176 198 104 6 $white $pen
        Draw-Text $g $m[0] ($x+18) 193 11 ([System.Drawing.Color]::FromArgb(96,113,106))
        Draw-Text $g $m[1] ($x+18) 220 25 $m[2] ([System.Drawing.FontStyle]::Bold)
        $white.Dispose(); $pen.Dispose()
        $x += 218
    }
    $panel = Brush 255 255 255
    $pen2 = Pen-Color 215 222 216
    Fill-RoundedRect $g 28 312 1342 480 6 $panel $pen2
    Draw-Text $g "Активные партии" 52 334 18 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Bold)
    $headers = @("Партия","Дата","Статус","Объем, л","Остаток, л","Срок годности","Источник")
    $rows = @(
        @("М-20260505-001","05.05.2026 06:30","частично отгружена","420","300","06.05.2026 18:30","Утренний надой, дойное стадо N1"),
        @("М-20260505-002","05.05.2026 07:10","допущена","380","380","06.05.2026 19:10","Утренний надой, дойное стадо N2"),
        @("М-20260505-003","05.05.2026 08:00","на анализе","210","210","06.05.2026 20:00","Группа первотелок"),
        @("М-20260503-007","03.05.2026 07:00","заблокирована","260","260","04.05.2026 19:00","Утренний надой, дойное стадо N1"),
        @("М-20260502-009","02.05.2026 09:00","частично отгружена","55","30","06.05.2026 09:00","Творог из партии М-009")
    )
    Draw-Table $g 52 380 1292 $headers $rows
    $canvas.Bitmap.Save((Join-Path $OutputDirectory "screen_dashboard.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $canvas.Bitmap.Dispose()
}

function Draw-Quality {
    $canvas = New-Canvas 1400 850
    $g = $canvas.Graphics
    Draw-Header $g "Лабораторный контроль партии и автоматическое заключение"
    Draw-TabBar $g "Качество"
    $panel = Brush 255 255 255
    $pen = Pen-Color 215 222 216
    Fill-RoundedRect $g 28 174 410 600 6 $panel $pen
    Draw-Text $g "Лабораторный контроль" 54 198 18 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Bold)
    $fields = @("Выбрана партия: М-20260505-002","Жирность, %   3,3","Белок, %   3,0","Кислотность, °T   19","Плотность, кг/м³   1028","Температура, °C   7","☐ Есть посторонние примеси")
    $y=242
    foreach($f in $fields) {
        Draw-Text $g $f 54 $y 13 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Regular) 340
        if ($f -match "%|°T|кг|°C") {
            $box = Brush 248 250 248
            $g.FillRectangle($box, 54, $y+24, 330, 32)
            $g.DrawRectangle($pen, 54, $y+24, 330, 32)
            $box.Dispose()
            $y += 70
        } else {
            $y += 48
        }
    }
    $btn = Brush 40 92 77
    Fill-RoundedRect $g 54 686 300 44 5 $btn $null
    Draw-Text $g "Сохранить результат анализа" 74 697 12 ([System.Drawing.Color]::White) ([System.Drawing.FontStyle]::Bold) 260
    $btn.Dispose()
    Fill-RoundedRect $g 468 174 904 600 6 $panel $pen
    Draw-Text $g "Журнал лабораторного контроля" 492 198 18 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Bold)
    $headers=@("Дата","Жир","Белок","Кислотность","Плотность","Темп.","Заключение","Комментарий")
    $rows=@(
        @("05.05.2026 08:10","3,3","3,0","19","1028","7","годна","Допущена к реализации"),
        @("04.05.2026 19:00","3,2","2,9","20","1028","7","условно годна","Повторный контроль перед отгрузкой"),
        @("03.05.2026 08:00","2,6","2,7","27","1024","15","заблокирована","Повышенная кислотность и температура"),
        @("03.05.2026 19:00","18,5","3,2","17","1030","5","годна","Сливки соответствуют нормам")
    )
    Draw-Table $g 492 252 846 $headers $rows
    $panel.Dispose(); $pen.Dispose()
    $canvas.Bitmap.Save((Join-Path $OutputDirectory "screen_quality.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $canvas.Bitmap.Dispose()
}

function Draw-Shipment {
    $canvas = New-Canvas 1400 850
    $g = $canvas.Graphics
    Draw-Header $g "Отгрузка продукции покупателю с контролем статуса и остатка"
    Draw-TabBar $g "Отгрузка"
    $panel = Brush 255 255 255
    $pen = Pen-Color 215 222 216
    Fill-RoundedRect $g 28 174 430 600 6 $panel $pen
    Draw-Text $g "Оформление отгрузки" 54 198 18 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Bold)
    $fields=@("Партия: М-20260505-001","Покупатель: ООО Саратов-Молоко","Транспорт: Молоковоз ГАЗель","Объем отгрузки, л: 120","Температура при отгрузке, °C: 6")
    $y=250
    foreach($f in $fields){
        $box=Brush 248 250 248
        Draw-Text $g $f 54 $y 13 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Regular) 350
        $g.FillRectangle($box,54,$y+28,350,34)
        $g.DrawRectangle($pen,54,$y+28,350,34)
        $box.Dispose()
        $y+=82
    }
    $btn=Brush 40 92 77
    Fill-RoundedRect $g 54 680 260 44 5 $btn $null
    Draw-Text $g "Сформировать накладную" 76 691 12 ([System.Drawing.Color]::White) ([System.Drawing.FontStyle]::Bold) 220
    $btn.Dispose()
    Fill-RoundedRect $g 488 174 884 600 6 $panel $pen
    Draw-Text $g "Журнал отгрузок" 512 198 18 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Bold)
    $headers=@("Номер","Дата","Темп.","Документ","Ответственный")
    $rows=@(
        @("РН-20260505-001","05.05.2026 11:00","6","Накладная от 05.05.2026","Сагинбаев Е.М."),
        @("РН-20260504-002","04.05.2026 12:00","5","Накладная от 04.05.2026","Сагинбаев Е.М."),
        @("РН-20260502-003","02.05.2026 15:00","4","Накладная от 02.05.2026","Сагинбаев Е.М.")
    )
    Draw-Table $g 512 252 820 $headers $rows
    Draw-Text $g "Система запрещает отгрузку заблокированных партий и объем больше остатка." 512 525 16 ([System.Drawing.Color]::FromArgb(158,47,56)) ([System.Drawing.FontStyle]::Bold) 780
    $panel.Dispose(); $pen.Dispose()
    $canvas.Bitmap.Save((Join-Path $OutputDirectory "screen_shipment.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $canvas.Bitmap.Dispose()
}

function Draw-ER {
    $canvas = New-Canvas 1700 1100
    $g = $canvas.Graphics
    $g.Clear([System.Drawing.Color]::White)
    Draw-Text $g "ER-диаграмма базы данных АИС учета партий молочной продукции" 310 24 22 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Bold) 1100
    $tables = @(
        @{N="users"; X=70; Y=110; F=@("id PK","login","full_name","role_id FK")},
        @{N="roles"; X=70; Y=360; F=@("id PK","name")},
        @{N="animal_groups"; X=70; Y=610; F=@("id PK","name","location")},
        @{N="product_types"; X=430; Y=110; F=@("id PK","name","shelf_life_hours")},
        @{N="storage_tanks"; X=430; Y=360; F=@("id PK","name","capacity_liters","location")},
        @{N="milk_yields"; X=430; Y=610; F=@("id PK","animal_group_id FK","batch_id FK","volume_liters","temperature")},
        @{N="batches"; X=820; Y=330; F=@("id PK","batch_number","status","storage_tank_id FK","product_type_id FK","remaining_liters")},
        @{N="batch_quality_tests"; X=1210; Y=110; F=@("id PK","batch_id FK","fat_percent","protein_percent","conclusion")},
        @{N="stock_movements"; X=1210; Y=360; F=@("id PK","batch_id FK","operation_type","from_tank_id FK","to_tank_id FK")},
        @{N="shipment_items"; X=1210; Y=610; F=@("id PK","shipment_id FK","batch_id FK","volume_liters")},
        @{N="shipments"; X=820; Y=760; F=@("id PK","customer_id FK","vehicle_id FK","shipped_at")},
        @{N="customers"; X=430; Y=860; F=@("id PK","name","inn","address")},
        @{N="vehicles"; X=70; Y=860; F=@("id PK","name","plate_number","driver")},
        @{N="audit_log"; X=1210; Y=860; F=@("id PK","created_at","user_name","action")}
    )
    $tableRects=@{}
    foreach($t in $tables){
        $x=[float]$t.X; $y=[float]$t.Y; $w=300; $h=46 + ($t.F.Count*28)
        $brush=Brush 255 255 255
        $header=Brush 40 92 77
        $pen=Pen-Color 120 140 130 1.5
        Fill-RoundedRect $g $x $y $w $h 6 $brush $pen
        $g.FillRectangle($header,$x,$y,$w,38)
        Draw-Text $g $t.N ($x+12) ($y+8) 13 ([System.Drawing.Color]::White) ([System.Drawing.FontStyle]::Bold) ($w-24)
        $fy=$y+46
        foreach($f in $t.F){
            Draw-Text $g $f ($x+14) $fy 10 ([System.Drawing.Color]::FromArgb(30,43,37)) ([System.Drawing.FontStyle]::Regular) ($w-28)
            $fy+=28
        }
        $tableRects[$t.N]=New-Object System.Drawing.RectangleF($x,$y,$w,$h)
        $brush.Dispose(); $header.Dispose(); $pen.Dispose()
    }
    $line=Pen-Color 70 90 82 2
    function Connect($g,$rects,[string]$a,[string]$b,$pen){
        $ra=$rects[$a]; $rb=$rects[$b]
        $p1=New-Object System.Drawing.PointF(($ra.X+$ra.Width),($ra.Y+$ra.Height/2))
        $p2=New-Object System.Drawing.PointF($rb.X,($rb.Y+$rb.Height/2))
        if($ra.X -gt $rb.X){ $p1=New-Object System.Drawing.PointF($ra.X,($ra.Y+$ra.Height/2)); $p2=New-Object System.Drawing.PointF(($rb.X+$rb.Width),($rb.Y+$rb.Height/2)) }
        $g.DrawLine($pen,$p1,$p2)
    }
    Connect $g $tableRects "roles" "users" $line
    Connect $g $tableRects "animal_groups" "milk_yields" $line
    Connect $g $tableRects "milk_yields" "batches" $line
    Connect $g $tableRects "product_types" "batches" $line
    Connect $g $tableRects "storage_tanks" "batches" $line
    Connect $g $tableRects "batches" "batch_quality_tests" $line
    Connect $g $tableRects "batches" "stock_movements" $line
    Connect $g $tableRects "batches" "shipment_items" $line
    Connect $g $tableRects "shipments" "shipment_items" $line
    Connect $g $tableRects "customers" "shipments" $line
    Connect $g $tableRects "vehicles" "shipments" $line
    $line.Dispose()
    $canvas.Bitmap.Save((Join-Path $OutputDirectory "er_diagram.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $canvas.Bitmap.Dispose()
}

Draw-Dashboard
Draw-Quality
Draw-Shipment
Draw-ER

Get-ChildItem -LiteralPath $OutputDirectory -Filter *.png | Select-Object FullName, Length

