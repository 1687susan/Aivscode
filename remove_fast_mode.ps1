# 移除快速模式的PowerShell腳本
$content = Get-Content "ConsoleApp1\Program.cs" -Raw

# 找到快速模式方法的開始和結束
$startPattern = "// 快速模式執行代理人（連續執行）"
$endPattern = "// 執行單一代理人（用於併行執行）"

$lines = Get-Content "ConsoleApp1\Program.cs"
$startLine = -1
$endLine = -1

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "快速模式執行代理人") {
        $startLine = $i
    }
    if ($lines[$i] -match "執行單一代理人" -and $startLine -ge 0) {
        $endLine = $i
        break
    }
}

Write-Host "找到快速模式方法：第 $($startLine + 1) 行到第 $($endLine) 行"

if ($startLine -ge 0 -and $endLine -gt $startLine) {
    # 創建新內容，跳過快速模式方法
    $newLines = @()
    $newLines += $lines[0..($startLine - 1)]
    $newLines += $lines[$endLine..($lines.Count - 1)]
    
    # 寫入新檔案
    $newLines | Set-Content "ConsoleApp1\Program.cs"
    Write-Host "✅ 已成功移除快速模式方法"
} else {
    Write-Host "❌ 無法找到快速模式方法的邊界"
}