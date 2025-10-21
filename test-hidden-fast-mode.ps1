# 測試隱藏快速模式的腳本
Write-Host "=== 測試隱藏快速模式功能 ===" -ForegroundColor Green

# 測試案例：選擇多個代理人，應該只看到兩種執行模式
$testInputs = @(
    "2,3"      # 選擇多個代理人
    "1"        # 選擇順序執行模式
    "n"        # 不調整執行順序
    "exit"     # 退出第一個代理人
    "n"        # 不繼續執行剩餘代理人
)

Write-Host "測試輸入序列："
$testInputs | ForEach-Object { Write-Host "  $_" }

Write-Host "`n開始測試..." -ForegroundColor Yellow

# 將輸入序列傳送給程式
$inputString = $testInputs -join "`n"
$inputString | dotnet run --project ConsoleApp1

Write-Host "`n=== 測試完成 ===" -ForegroundColor Green
Write-Host "預期結果：應該只看到兩種執行模式選項（順序執行和真正併行）" -ForegroundColor Cyan