# 測試首次多選自動執行功能的演示腳本

Write-Host "=== AI Agent 系統首次多選自動執行功能演示 ===" -ForegroundColor Green
Write-Host ""

Write-Host "功能說明：" -ForegroundColor Yellow
Write-Host "1. 首次選擇多個代理人時，如果某個代理人中途退出，系統會自動繼續執行剩餘代理人"
Write-Host "2. 非首次選擇（暫存模式）時，系統會詢問用戶是否繼續"
Write-Host ""

Write-Host "測試場景：選擇 1,2 （客戶服務 + 天氣服務）" -ForegroundColor Cyan
Write-Host "預期行為：即使天氣服務代理人中途退出，客戶服務會自動繼續執行"
Write-Host ""

Write-Host "請按任意鍵開始演示..." -ForegroundColor Magenta
Read-Host

# 運行實際程式
Write-Host "啟動 AI Agent 系統..." -ForegroundColor Green
Set-Location c:\Users\user\Aivscode\ConsoleApp1
dotnet run