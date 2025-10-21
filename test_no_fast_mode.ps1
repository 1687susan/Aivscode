Write-Host "🧪 測試修復後的程式..."
Write-Host "📝 期待：只有兩個執行模式（順序執行、真正併行）"
Write-Host "🎯 測試：選擇多個代理人，確認沒有快速模式選項"
Write-Host ""

# 建立測試輸入檔案
@"
2,3
1
n
exit
"@ | Set-Content test_input.txt

Write-Host "▶️ 開始測試..."
Get-Content test_input.txt | dotnet run

Write-Host ""
Write-Host "✅ 測試完成！"