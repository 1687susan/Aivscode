# 三種執行模式演示
Write-Host "=== 三種執行模式功能說明 ===" -ForegroundColor Green
Write-Host ""
Write-Host "現在系統提供三種執行模式：" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 順序執行（依序互動，可中途停止）：" -ForegroundColor Cyan
Write-Host "   - 一個代理人完成後詢問是否繼續下一個" -ForegroundColor White
Write-Host "   - 可以中途停止，適合需要精確控制的場景" -ForegroundColor White
Write-Host "   - 支援執行順序調整" -ForegroundColor White
Write-Host ""
Write-Host "2. 快速模式（連續執行，僅測試用）：" -ForegroundColor Cyan
Write-Host "   - 代理人依序連續執行，無需中途確認" -ForegroundColor White
Write-Host "   - 適合批量測試或演示用途" -ForegroundColor White
Write-Host "   - 仍然是依序執行，但更流暢" -ForegroundColor White
Write-Host ""
Write-Host "3. 真正併行（同時啟動，共享控制台）：" -ForegroundColor Cyan
Write-Host "   - 所有代理人同時啟動" -ForegroundColor White
Write-Host "   - 共享控制台輸入輸出" -ForegroundColor White
Write-Host "   - 適合需要真正併行處理的場景" -ForegroundColor White
Write-Host "   - 注意：輸出可能會混亂，需要謹慎使用" -ForegroundColor Red
Write-Host ""
Write-Host "現在讓我們測試快速模式：" -ForegroundColor Green
Write-Host ""

# 創建測試輸入
@"
2,3
2
exit
exit
5
"@ | Out-File -FilePath "test-modes.txt" -Encoding UTF8

Write-Host "測試步驟：選擇 2,3 → 選擇模式 2（快速模式）→ 觀察連續執行" -ForegroundColor Yellow
Write-Host ""

# 執行測試
Get-Content "test-modes.txt" | dotnet run --project c:\Users\user\Aivscode\ConsoleApp1\ConsoleApp1.csproj

# 清理
Remove-Item "test-modes.txt" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "測試完成！現在您可以看到三種模式的區別。" -ForegroundColor Green