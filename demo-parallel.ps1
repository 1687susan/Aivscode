# 併行執行功能演示
Write-Host "=== 併行執行功能演示 ===" -ForegroundColor Green
Write-Host ""
Write-Host "步驟說明：" -ForegroundColor Yellow
Write-Host "1. 選擇 4,3 (訂單管理助手 + 人力資源助手)" -ForegroundColor White
Write-Host "2. 選擇執行模式 2 (併行執行)" -ForegroundColor White
Write-Host "3. 觀察兩個代理人同時啟動" -ForegroundColor White
Write-Host "4. 分別在兩個代理人中輸入 'exit' 結束" -ForegroundColor White
Write-Host "5. 輸入 5 退出程式" -ForegroundColor White
Write-Host ""
Write-Host "預期效果：" -ForegroundColor Cyan
Write-Host "- 兩個代理人會同時啟動並等待輸入" -ForegroundColor White
Write-Host "- 可以分別與每個代理人互動" -ForegroundColor White
Write-Host "- 所有代理人結束後回到主選單" -ForegroundColor White
Write-Host ""

# 使用測試輸入文件
Get-Content "c:\Users\user\Aivscode\test-parallel.txt" | dotnet run --project c:\Users\user\Aivscode\ConsoleApp1\ConsoleApp1.csproj

Write-Host ""
Write-Host "演示完成！" -ForegroundColor Green