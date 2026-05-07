param(
    [string]$InputPath = ".\docs\Diplom_Moloko.md",
    [string]$OutputPath = ".\docs\Diplom_Moloko.docx"
)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

function Escape-Xml([string]$Text) {
    return [System.Security.SecurityElement]::Escape($Text)
}

function Run-Xml([string]$Text, [string]$Style = "Normal") {
    $escaped = Escape-Xml $Text
    if ($Style -eq "Code") {
        return '<w:r><w:rPr><w:rFonts w:ascii="Consolas" w:hAnsi="Consolas" w:cs="Consolas"/><w:sz w:val="20"/></w:rPr><w:t xml:space="preserve">' + $escaped + '</w:t></w:r>'
    }

    return '<w:r><w:t xml:space="preserve">' + $escaped + '</w:t></w:r>'
}

function Paragraph-Xml([string]$Text, [string]$Style = "Normal") {
    $styleXml = ""
    if ($Style -ne "Normal") {
        $styleXml = '<w:pPr><w:pStyle w:val="' + $Style + '"/></w:pPr>'
    }

    return '<w:p>' + $styleXml + (Run-Xml -Text $Text -Style $Style) + '</w:p>'
}

function PageBreak-Xml {
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'
}

function Image-Xml([string]$RelationshipId, [int64]$Cx, [int64]$Cy, [string]$AltText) {
    $escapedAlt = Escape-Xml $AltText
    return @"
<w:p>
  <w:pPr><w:jc w:val="center"/></w:pPr>
  <w:r>
    <w:drawing>
      <wp:inline distT="0" distB="0" distL="0" distR="0">
        <wp:extent cx="$Cx" cy="$Cy"/>
        <wp:effectExtent l="0" t="0" r="0" b="0"/>
        <wp:docPr id="$($RelationshipId.Replace('rIdImage',''))" name="$escapedAlt"/>
        <wp:cNvGraphicFramePr/>
        <a:graphic>
          <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture">
            <pic:pic>
              <pic:nvPicPr>
                <pic:cNvPr id="0" name="$escapedAlt"/>
                <pic:cNvPicPr/>
              </pic:nvPicPr>
              <pic:blipFill>
                <a:blip r:embed="$RelationshipId"/>
                <a:stretch><a:fillRect/></a:stretch>
              </pic:blipFill>
              <pic:spPr>
                <a:xfrm><a:off x="0" y="0"/><a:ext cx="$Cx" cy="$Cy"/></a:xfrm>
                <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
              </pic:spPr>
            </pic:pic>
          </a:graphicData>
        </a:graphic>
      </wp:inline>
    </w:drawing>
  </w:r>
</w:p>
"@
}

function Table-Xml([System.Collections.Generic.List[string]]$Lines) {
    $rows = New-Object System.Collections.Generic.List[string]
    foreach ($line in $Lines) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^\|[\s:\-]+\|') {
            continue
        }

        $cells = $trimmed.Trim('|').Split('|') | ForEach-Object { $_.Trim() }
        $cellXml = New-Object System.Collections.Generic.List[string]
        foreach ($cell in $cells) {
            [void]$cellXml.Add('<w:tc><w:tcPr><w:tcW w:w="2400" w:type="dxa"/></w:tcPr>' + (Paragraph-Xml -Text $cell -Style 'TableText') + '</w:tc>')
        }
        [void]$rows.Add('<w:tr>' + ($cellXml -join '') + '</w:tr>')
    }

    if ($rows.Count -eq 0) {
        return ''
    }

    return @"
<w:tbl>
  <w:tblPr>
    <w:tblW w:w="0" w:type="auto"/>
    <w:tblBorders>
      <w:top w:val="single" w:sz="4" w:space="0" w:color="808080"/>
      <w:left w:val="single" w:sz="4" w:space="0" w:color="808080"/>
      <w:bottom w:val="single" w:sz="4" w:space="0" w:color="808080"/>
      <w:right w:val="single" w:sz="4" w:space="0" w:color="808080"/>
      <w:insideH w:val="single" w:sz="4" w:space="0" w:color="808080"/>
      <w:insideV w:val="single" w:sz="4" w:space="0" w:color="808080"/>
    </w:tblBorders>
  </w:tblPr>
  $($rows -join "`n")
</w:tbl>
"@
}

$lines = Get-Content -LiteralPath $InputPath -Encoding UTF8
$body = New-Object System.Collections.Generic.List[string]
$imageRels = New-Object System.Collections.Generic.List[string]
$imageFiles = New-Object System.Collections.Generic.List[object]
$inputDirectory = Split-Path -Parent ([System.IO.Path]::GetFullPath($InputPath))
$inCode = $false
$tableLines = New-Object System.Collections.Generic.List[string]

function Flush-Table {
    if ($script:tableLines.Count -gt 0) {
        [void]$script:body.Add((Table-Xml -Lines $script:tableLines))
        $script:tableLines.Clear()
    }
}

foreach ($line in $lines) {
    if ($line.Trim() -eq "\page") {
        Flush-Table
        $body.Add((PageBreak-Xml))
        continue
    }

    if ($line.Trim().StartsWith('```')) {
        $inCode = -not $inCode
        continue
    }

    if ($inCode) {
        Flush-Table
        $paragraph = Paragraph-Xml -Text $line -Style 'Code'
        [void]$body.Add($paragraph)
        continue
    }

    if ([string]::IsNullOrWhiteSpace($line)) {
        Flush-Table
        [void]$body.Add('<w:p/>')
        continue
    }

    if ($line.Trim().StartsWith('|')) {
        [void]$tableLines.Add($line)
        continue
    }

    Flush-Table

    if ($line -match '^!\[(.+?)\]\((.+?)\)$') {
        $alt = $matches[1]
        $relativePath = $matches[2]
        $imagePath = if ([System.IO.Path]::IsPathRooted($relativePath)) {
            $relativePath
        } else {
            Join-Path $inputDirectory $relativePath
        }
        if (Test-Path -LiteralPath $imagePath) {
            $index = $imageFiles.Count + 1
            $relationshipId = "rIdImage$index"
            $extension = [System.IO.Path]::GetExtension($imagePath).TrimStart('.').ToLowerInvariant()
            $target = "media/image$index.$extension"
            $bitmap = [System.Drawing.Image]::FromFile($imagePath)
            $maxWidthEmu = 8600000
            $cx = $maxWidthEmu
            $cy = [int64]($maxWidthEmu * $bitmap.Height / $bitmap.Width)
            $bitmap.Dispose()
            [void]$imageFiles.Add([pscustomobject]@{ Path = $imagePath; Target = "word/$target" })
            [void]$imageRels.Add('<Relationship Id="' + $relationshipId + '" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="' + $target + '"/>')
            [void]$body.Add((Image-Xml -RelationshipId $relationshipId -Cx $cx -Cy $cy -AltText $alt))
            [void]$body.Add((Paragraph-Xml -Text $alt -Style 'Normal'))
            continue
        }
    }

    if ($line.StartsWith("### ")) {
        $paragraph = Paragraph-Xml -Text ($line.Substring(4)) -Style 'Heading3'
        [void]$body.Add($paragraph)
    }
    elseif ($line.StartsWith("## ")) {
        $paragraph = Paragraph-Xml -Text ($line.Substring(3)) -Style 'Heading2'
        [void]$body.Add($paragraph)
    }
    elseif ($line.StartsWith("# ")) {
        $paragraph = Paragraph-Xml -Text ($line.Substring(2)) -Style 'Heading1'
        [void]$body.Add($paragraph)
    }
    elseif ($line.StartsWith("- ")) {
        $paragraph = Paragraph-Xml -Text ('- ' + $line.Substring(2)) -Style 'Normal'
        [void]$body.Add($paragraph)
    }
    else {
        $paragraph = Paragraph-Xml -Text $line -Style 'Normal'
        [void]$body.Add($paragraph)
    }
}

Flush-Table

$documentXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"
            xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
            xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"
            xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture">
  <w:body>
    $($body -join "`n")
    <w:sectPr>
      <w:footerReference w:type="default" r:id="rIdFooter1"/>
      <w:titlePg/>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1134" w:right="567" w:bottom="1134" w:left="1701" w:header="708" w:footer="708" w:gutter="0"/>
      <w:cols w:space="708"/>
      <w:docGrid w:linePitch="360"/>
    </w:sectPr>
  </w:body>
</w:document>
"@

$stylesXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:qFormat/>
    <w:pPr>
      <w:spacing w:line="360" w:lineRule="auto"/>
      <w:ind w:firstLine="708"/>
      <w:jc w:val="both"/>
    </w:pPr>
    <w:rPr>
      <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
      <w:sz w:val="28"/>
      <w:szCs w:val="28"/>
    </w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="heading 1"/>
    <w:basedOn w:val="Normal"/>
    <w:next w:val="Normal"/>
    <w:qFormat/>
    <w:pPr>
      <w:keepNext/>
      <w:spacing w:before="240" w:after="160"/>
      <w:jc w:val="center"/>
    </w:pPr>
    <w:rPr>
      <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
      <w:b/>
      <w:caps/>
      <w:sz w:val="28"/>
      <w:szCs w:val="28"/>
    </w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="heading 2"/>
    <w:basedOn w:val="Normal"/>
    <w:next w:val="Normal"/>
    <w:qFormat/>
    <w:pPr>
      <w:keepNext/>
      <w:spacing w:before="200" w:after="120"/>
      <w:ind w:firstLine="708"/>
    </w:pPr>
    <w:rPr>
      <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
      <w:b/>
      <w:sz w:val="28"/>
      <w:szCs w:val="28"/>
    </w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Code">
    <w:name w:val="Code"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr>
      <w:spacing w:line="240" w:lineRule="auto"/>
      <w:ind w:firstLine="0" w:left="425"/>
    </w:pPr>
    <w:rPr>
      <w:rFonts w:ascii="Consolas" w:hAnsi="Consolas" w:cs="Consolas"/>
      <w:sz w:val="20"/>
    </w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading3">
    <w:name w:val="heading 3"/>
    <w:basedOn w:val="Normal"/>
    <w:next w:val="Normal"/>
    <w:qFormat/>
    <w:pPr>
      <w:keepNext/>
      <w:spacing w:before="160" w:after="100"/>
      <w:ind w:firstLine="708"/>
    </w:pPr>
    <w:rPr>
      <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
      <w:i/>
      <w:sz w:val="28"/>
      <w:szCs w:val="28"/>
    </w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="TableText">
    <w:name w:val="Table Text"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr>
      <w:spacing w:line="240" w:lineRule="auto"/>
      <w:ind w:firstLine="0"/>
      <w:jc w:val="left"/>
    </w:pPr>
    <w:rPr>
      <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
      <w:sz w:val="24"/>
      <w:szCs w:val="24"/>
    </w:rPr>
  </w:style>
</w:styles>
"@

$contentTypesXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Default Extension="png" ContentType="image/png"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/word/footer1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"/>
</Types>
"@

$relsXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
"@

$documentRelsXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rIdFooter1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer" Target="footer1.xml"/>
  $($imageRels -join "`n")
</Relationships>
"@

$footerXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:ftr xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:p>
    <w:pPr><w:jc w:val="center"/></w:pPr>
    <w:r>
      <w:fldChar w:fldCharType="begin"/>
    </w:r>
    <w:r>
      <w:instrText xml:space="preserve"> PAGE </w:instrText>
    </w:r>
    <w:r>
      <w:fldChar w:fldCharType="end"/>
    </w:r>
  </w:p>
</w:ftr>
"@

if (Test-Path -LiteralPath $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Force
}

$fullOutput = [System.IO.Path]::GetFullPath($OutputPath)
$zip = [System.IO.Compression.ZipFile]::Open($fullOutput, [System.IO.Compression.ZipArchiveMode]::Create)

function Add-ZipEntry($zipFile, [string]$Name, [string]$Content) {
    $entry = $zipFile.CreateEntry($Name)
    $stream = $entry.Open()
    $writer = New-Object System.IO.StreamWriter($stream, [System.Text.UTF8Encoding]::new($false))
    $writer.Write($Content)
    $writer.Dispose()
    $stream.Dispose()
}

function Add-ZipBytes($zipFile, [string]$Name, [byte[]]$Bytes) {
    $entry = $zipFile.CreateEntry($Name)
    $stream = $entry.Open()
    $stream.Write($Bytes, 0, $Bytes.Length)
    $stream.Dispose()
}

Add-ZipEntry $zip "[Content_Types].xml" $contentTypesXml
Add-ZipEntry $zip "_rels/.rels" $relsXml
Add-ZipEntry $zip "word/_rels/document.xml.rels" $documentRelsXml
Add-ZipEntry $zip "word/document.xml" $documentXml
Add-ZipEntry $zip "word/styles.xml" $stylesXml
Add-ZipEntry $zip "word/footer1.xml" $footerXml
foreach ($imageFile in $imageFiles) {
    Add-ZipBytes $zip $imageFile.Target ([System.IO.File]::ReadAllBytes($imageFile.Path))
}
$zip.Dispose()

Write-Output $fullOutput
