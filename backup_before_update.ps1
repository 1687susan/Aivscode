# AI Agent 系統更新前備份腳本
# 作者: AI Assistant
# 日期: 2025-10-17
# 用途: 在每次更新程式碼前自動備份重要檔案

param(
    [string]$BackupReason = "Manual backup"
)

# 設定變數
$ProjectRoot = "c:\Users\user\Aivscode"
$BackupRoot = "$ProjectRoot\Backups"
$TodayFolder = Get-Date -Format "yyyy-MM-dd"
$TimeStamp = Get-Date -Format "HHmm"
$BackupFolder = "$BackupRoot\$TodayFolder\backup_$TimeStamp"

# 要備份的檔案清單
$FilesToBackup = @(
    "$ProjectRoot\ConsoleApp1\Program.cs",
    "$ProjectRoot\ConsoleApp1\CustomerServicePlugin.cs",
    "$ProjectRoot\ConsoleApp1\WeatherServicePlugin.cs", 
    "$ProjectRoot\ConsoleApp1\HRManagementPlugin.cs",
    "$ProjectRoot\ConsoleApp1\OrderManagementPlugin.cs",
    "$ProjectRoot\ConsoleApp1\DataStore.cs",
    "$ProjectRoot\ConsoleApp1\agent-config.json",
    "$ProjectRoot\ConsoleApp1\appsettings.json",
    "$ProjectRoot\ConsoleApp1\README_流程說明.md",
    "$ProjectRoot\ConsoleApp1\ConsoleApp1.csproj"
)

Write-Host "🔄 開始執行備份..." -ForegroundColor Yellow
Write-Host "備份原因: $BackupReason" -ForegroundColor Cyan
Write-Host "備份時間: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan

# 建立備份資料夾
if (!(Test-Path $BackupFolder)) {
    New-Item -ItemType Directory -Path $BackupFolder -Force | Out-Null
    Write-Host "✅ 建立備份資料夾: $BackupFolder" -ForegroundColor Green
}

# 執行備份
$BackupCount = 0
$SuccessCount = 0

foreach ($File in $FilesToBackup) {
    if (Test-Path $File) {
        $BackupCount++
        $FileName = Split-Path $File -Leaf
        $BackupFileName = "${FileName}.backup.txt"
        $BackupPath = "$BackupFolder\$BackupFileName"
        
        try {
            # 讀取原始檔案內容
            $Content = Get-Content $File -Raw -Encoding UTF8
            
            # 建立備份檔案標頭
            $Header = @"
# ================================================
# 檔案備份資訊
# ================================================
# 原始檔案: $File
# 備份時間: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
# 備份原因: $BackupReason
# 檔案大小: $((Get-Item $File).Length) bytes
# ================================================

"@
            
            # 寫入備份檔案
            Set-Content -Path $BackupPath -Value ($Header + $Content) -Encoding UTF8
            Write-Host "✅ 已備份: $FileName → $BackupFileName" -ForegroundColor Green
            $SuccessCount++
            
        } catch {
            Write-Host "❌ 備份失敗: $FileName - $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "⚠️  檔案不存在: $File" -ForegroundColor Yellow
    }
}

# 建立備份摘要檔案
$SummaryPath = "$BackupFolder\backup_summary.txt"
$Summary = @"
# AI Agent 系統備份摘要
# ========================================

備份時間: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
備份原因: $BackupReason
備份位置: $BackupFolder

檔案統計:
- 嘗試備份: $BackupCount 個檔案
- 成功備份: $SuccessCount 個檔案
- 失敗數量: $($BackupCount - $SuccessCount) 個檔案

備份檔案清單:
"@

Get-ChildItem $BackupFolder -Filter "*.backup.txt" | ForEach-Object {
    $Summary += "`n- $($_.Name)"
}

$Summary += @"

`n========================================
註: 所有備份檔案均為 .txt 格式，可直接開啟查看
如需還原，請手動複製內容回原始檔案
"@

Set-Content -Path $SummaryPath -Value $Summary -Encoding UTF8

Write-Host "`n📊 備份完成摘要:" -ForegroundColor Yellow
Write-Host "   備份位置: $BackupFolder" -ForegroundColor Cyan
Write-Host "   成功備份: $SuccessCount/$BackupCount 個檔案" -ForegroundColor Green
Write-Host "   摘要檔案: backup_summary.txt" -ForegroundColor Cyan
Write-Host "`n🎯 備份完成！可以安全進行更新了。" -ForegroundColor Green

return $BackupFolder