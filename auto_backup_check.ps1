# 自動備份檢查器
# 檢查是否需要在程式啟動前執行備份

$LastBackupFile = "c:\Users\user\Aivscode\Backups\.last_backup"
$Today = Get-Date -Format "yyyy-MM-dd"

# 檢查今日是否已備份
if (Test-Path $LastBackupFile) {
    $LastBackup = Get-Content $LastBackupFile -ErrorAction SilentlyContinue
    if ($LastBackup -eq $Today) {
        Write-Host "✅ 今日已有備份記錄" -ForegroundColor Green
        exit 0
    }
}

# 執行備份
Write-Host "⚠️  建議在程式更新前先執行備份" -ForegroundColor Yellow
Write-Host "執行指令: .\quick_backup.ps1" -ForegroundColor Cyan
Write-Host "或按 Y 立即備份: " -NoNewline -ForegroundColor Yellow

$choice = Read-Host
if ($choice -eq "Y" -or $choice -eq "y") {
    & "c:\Users\user\Aivscode\quick_backup.ps1" "自動備份檢查觸發"
    Set-Content -Path $LastBackupFile -Value $Today
}