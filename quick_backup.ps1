# 快速備份腳本 - 更新前使用
# 使用方法: .\quick_backup.ps1 "更新原因描述"

param([string]$Reason = "程式更新前備份")

Write-Host "🚀 執行快速備份..." -ForegroundColor Yellow
$BackupFolder = & "c:\Users\user\Aivscode\backup_before_update.ps1" -BackupReason $Reason
Write-Host "✅ 備份完成！備份位置: $BackupFolder" -ForegroundColor Green