# 測試多選自動執行功能
Write-Host "開始測試首次多選自動執行功能..." -ForegroundColor Green

# 創建輸入檔案
$inputFile = "c:\Users\user\Aivscode\test-input.txt"
@"
1,2
exit
exit
5
"@ | Out-File -FilePath $inputFile -Encoding UTF8

Write-Host "輸入檔案已創建: $inputFile" -ForegroundColor Yellow

# 顯示輸入內容
Write-Host "輸入內容:" -ForegroundColor Cyan
Get-Content $inputFile | ForEach-Object { Write-Host "  $_" -ForegroundColor White }

Write-Host "`n執行程式..." -ForegroundColor Green
Write-Host "預期行為: 首次選擇 1,2 應該自動連續執行兩個代理人，不詢問是否繼續" -ForegroundColor Yellow

# 執行程式
Get-Content $inputFile | dotnet run --project c:\Users\user\Aivscode\ConsoleApp1\ConsoleApp1.csproj

# 清理
Remove-Item $inputFile -ErrorAction SilentlyContinue
Write-Host "`n測試完成！" -ForegroundColor Green