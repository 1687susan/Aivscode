# AI 提供者切換指南

## 🔧 概述
您的 AI Agent 系統現在支援兩種 AI 提供者：
1. **Azure OpenAI** （公司版本）
2. **OpenAI 官方 API** （個人版本）

## 📋 切換步驟

### 方法 1：使用 Azure OpenAI（目前設定）
在 `appsettings.json` 中：
```json
{
  "AIProvider": "AzureOpenAI",
  "AzureOpenAI": {
    "ApiKey": "你的Azure OpenAI API Key",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

### 方法 2：切換到 OpenAI 官方 API
1. 修改 `appsettings.json` 中的 `AIProvider`：
```json
{
  "AIProvider": "OpenAI",
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key-here",
    "Model": "gpt-4",
    "BaseUrl": "https://api.openai.com/v1/"
  }
}
```

2. 取得 OpenAI API Key：
   - 前往 https://platform.openai.com/api-keys
   - 登入您的 OpenAI 帳戶
   - 創建新的 API Key
   - 將 API Key 填入上面的設定

## 💰 費用考量

### Azure OpenAI（公司版本）
- ✅ 公司負擔費用
- ✅ 企業級安全性
- ❌ 離職後無法使用

### OpenAI 官方 API（個人版本）  
- ✅ 個人擁有，可長期使用
- ✅ 最新模型優先支援
- ❌ 需要自行負擔費用
- 💡 費用參考：GPT-4 約 $0.03/1K tokens

## 🚀 GitHub Copilot 整合

目前 GitHub Copilot 主要用於程式碼編輯，並未提供直接的 Chat API。
建議的替代方案：

1. **OpenAI 官方 API**（推薦）
   - 使用相同的 GPT-4 模型
   - 完整的 API 功能
   - 按用量計費

2. **其他替代方案**
   - Anthropic Claude API
   - Google Gemini API
   - 需要額外程式碼修改

## 🔄 快速切換

只需要修改一行設定即可切換：
```json
"AIProvider": "OpenAI"    // 切換到 OpenAI
"AIProvider": "AzureOpenAI"  // 切換回 Azure OpenAI
```

## 🛠️ 故障排除

1. **API Key 無效**
   - 檢查 API Key 是否正確
   - 確認 API Key 有足夠的使用額度

2. **連線錯誤**
   - 檢查網路連線
   - 確認端點 URL 正確

3. **模型不存在**
   - 確認模型名稱正確
   - 檢查帳戶是否有該模型的存取權限

## 📞 技術支援

如有問題，可以檢查程式執行時的 Debug 訊息：
```
[Debug] 當前使用的 AI 提供者: OpenAI
[Info] 使用 OpenAI API
```

這樣可以確認系統正在使用哪個 AI 提供者。