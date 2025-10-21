# 備份系統使用說明

## 📋 備份系統概述

本備份系統可在每次更新程式碼前自動備份重要檔案，確保程式碼安全。所有備份檔案均為 `.txt` 格式，方便查看和還原。

## 🗂️ 備份目錄結構

```
Aivscode/
├── Backups/                    # 主備份目錄
│   ├── 2025-10-17/            # 按日期分組
│   │   ├── backup_0946/       # 按時間分組 (時分)
│   │   │   ├── *.backup.txt   # 備份檔案
│   │   │   └── backup_summary.txt  # 備份摘要
│   │   └── backup_1030/       # 其他時間點備份
│   └── .last_backup           # 最後備份日期記錄
├── backup_before_update.ps1   # 主備份腳本
├── quick_backup.ps1           # 快速備份腳本
└── auto_backup_check.ps1      # 自動備份檢查
```

## 🚀 使用方法

### 方法 1: 快速備份 (推薦)
```powershell
.\quick_backup.ps1 "更新原因描述"
```

### 方法 2: 完整備份
```powershell
.\backup_before_update.ps1 -BackupReason "詳細的更新說明"
```

### 方法 3: 自動檢查備份
```powershell
.\auto_backup_check.ps1
```

## 📁 備份的檔案清單

- `Program.cs` - 主程式檔案
- `CustomerServicePlugin.cs` - 客戶服務插件
- `WeatherServicePlugin.cs` - 天氣服務插件
- `HRManagementPlugin.cs` - 人資管理插件
- `OrderManagementPlugin.cs` - 訂單管理插件
- `DataStore.cs` - 資料存取層
- `agent-config.json` - 代理人設定檔
- `appsettings.json` - 應用程式設定
- `README_流程說明.md` - 流程說明文件
- `ConsoleApp1.csproj` - 專案檔案

## 🔧 備份檔案格式

每個備份檔案都包含：

```txt
# ================================================
# 檔案備份資訊
# ================================================
# 原始檔案: C:\Users\user\Aivscode\ConsoleApp1\Program.cs
# 備份時間: 2025-10-17 09:46:17
# 備份原因: 測試備份功能
# 檔案大小: 15234 bytes
# ================================================

[原始檔案內容...]
```

## 🔄 還原方法

1. 找到需要還原的備份檔案 (`.backup.txt`)
2. 複製檔案內容 (跳過標頭部分)
3. 貼上到原始檔案位置
4. 儲存檔案

## ⚡ 最佳實務

### 更新前備份流程
```powershell
# 1. 執行備份
.\quick_backup.ps1 "修復 XXX 功能"

# 2. 進行程式修改
# 3. 測試程式
# 4. 如有問題，從備份還原
```

### 定期清理
建議定期清理超過 30 天的備份檔案：
```powershell
Get-ChildItem "C:\Users\user\Aivscode\Backups" | 
    Where-Object {$_.CreationTime -lt (Get-Date).AddDays(-30)} | 
    Remove-Item -Recurse -Force
```

## 🚨 注意事項

1. **備份檔案為 UTF-8 編碼**，確保中文字元正確顯示
2. **每日第一次備份會建立新目錄**，同日多次備份會以時間區分
3. **備份不會覆蓋**，每次都會建立新的備份目錄
4. **檔案不存在時會顯示警告**，但不會中斷備份流程

## 📞 疑難排解

### 權限問題
如果遇到權限錯誤，請以管理員身分執行 PowerShell。

### 檔案鎖定
如果檔案被其他程式使用，請先關閉相關程式後再執行備份。

### 路徑問題
確保所有路徑都使用絕對路徑，避免相對路徑造成的問題。

---

**🎯 記住：在任何重要更新前，都要先執行備份！**