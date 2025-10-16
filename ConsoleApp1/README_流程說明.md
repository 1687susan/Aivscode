# AI Agent 系統流程說明

## 🚀 系統整體架構

這個 AI Agent 系統採用模組化設計，提供四種不同專業領域的智能助手服務。

## 📋 主要流程步驟

### 1. 程式啟動流程
```
Program.Main() 
    ↓
顯示選單（1-5個選項）
    ↓
使用者選擇服務類型
    ↓
調用對應的 AgentManager 方法
```

### 2. Agent 管理器流程
```
AgentManager
    ├── RunCustomerServiceAgent()     → 客戶服務助手
    ├── RunWeatherServiceAgent()      → 天氣服務助手  
    ├── RunHRManagementAgent()        → 人力資源助手
    └── RunOrderManagementAgent()     → 訂單管理助手
```

### 3. 每個 Agent 的內部流程

#### 3.1 Kernel 建立與配置
```
1. 建立 Kernel Builder
2. 加入 OpenAI Chat Completion 服務
3. 透過 PluginManager 載入對應的 Plugin 工具集
4. 設定 Function 自動調用行為
```

#### 3.2 對話循環處理
```
while (使用者輸入 != "exit"):
    1. 接收使用者輸入
    2. 加入到對話歷史
    3. 調用 LLM 進行串流回應
    4. 自動判斷是否需要調用函數
    5. 執行函數（如果需要）
    6. 顯示回應結果
    7. 更新對話歷史
```

## 🔧 Plugin 管理流程

### Plugin Manager 配置策略
```
ConfigureForCustomerService():
    → 載入 CustomerServicePlugin
    → 載入 OrderManagementPlugin
    
ConfigureForWeatherService():
    → 載入 WeatherServicePlugin
    
ConfigureForHRService():
    → 載入 HRManagementPlugin
    
ConfigureForOrderManagement():
    → 載入 OrderManagementPlugin
```

### Function Calling 流程
```
1. 使用者提出問題
2. LLM 分析問題內容
3. 判斷是否需要外部工具協助
4. 如果需要：
   a. 識別合適的函數
   b. 提取必要參數
   c. 調用對應的 Plugin 方法
   d. 獲取執行結果
   e. 整合結果回應使用者
5. 如果不需要：
   a. 直接生成回應
```

## 📊 各 Agent 特色功能

### 1. 客戶服務助手 (CustomerServiceAgent)
**可用工具：**
- CustomerServicePlugin.GetCustomerInfo() - 查詢客戶資訊
- OrderManagementPlugin.GetOrderStatus() - 查詢訂單狀態  
- OrderManagementPlugin.ProcessRefundRequest() - 處理退換貨

**使用場景：**
- "查詢王小明的客戶資訊"
- "訂單A001的狀態如何？"
- "我要申請A002訂單退貨，商品有瑕疵"

### 2. 天氣服務助手 (WeatherServiceAgent)
**可用工具：**
- WeatherServicePlugin.Get_Today_Temperature() - 非同步溫度查詢

**特色功能：**
- 支援非同步操作（模擬 API 呼叫延遲）
- 台灣主要城市溫度查詢
- 英文城市名稱輸入

**使用場景：**
- "台北今天溫度多少？"
- "高雄和台中哪裡比較熱？"

### 3. 人力資源助手 (HRManagementAgent)
**可用工具：**
- HRManagementPlugin.SearchEmployee() - 員工資訊搜尋

**使用場景：**
- "幫我找張經理的聯絡方式"
- "技術部有哪些員工？"

### 4. 訂單管理助手 (OrderManagementAgent)
**可用工具：**
- OrderManagementPlugin.GetOrderStatus() - 詳細訂單查詢
- OrderManagementPlugin.ProcessRefundRequest() - 退換貨處理

**特色功能：**
- 完整訂單資訊（包含客戶名稱、金額）
- 自動生成退換貨申請編號
- JSON 格式詳細資料回傳

## 🔄 資料流向圖

```
使用者輸入
    ↓
ChatHistory (對話歷史)
    ↓  
LLM 分析 (OpenAI GPT)
    ↓
Function Calling 判斷
    ↓
Plugin 方法執行
    ↓
結果整合
    ↓
串流回應輸出
    ↓
更新對話歷史
```

## 💾 對話歷史管理

### ChatHistory 位置與特性
- **建立位置**：`BaseAgent.ProcessAsync()` 方法中
- **更新位置**：`ProcessConversationLoop()` 方法中
- **存儲方式**：記憶體暫存（非持久化）
- **生命週期**：單次代理人會話期間

### 對話歷史結構
```csharp
ChatHistory history = new ChatHistory();
history.AddDeveloperMessage(SystemPrompt);    // 系統角色提示
history.AddUserMessage(userInput);            // 使用者輸入
history.AddAssistantMessage(aiResponse);      // AI 助手回應
```

### 會話隔離機制
- 每次選擇代理人都會建立新的 ChatHistory
- 不同代理人之間的對話歷史完全獨立
- 使用 `exit` 或 `menu` 會銷毀當前 ChatHistory

## 💡 系統優勢

### 1. 模組化設計
- 每個 Agent 獨立運作
- Plug

- 自動函數調用
- 上下文感知
- 串流回應體驗

### 3. 專業化服務
- 角色明確分工
- 專業領域知識
- 個性化系統提示

### 4. 擴展性設計
- 新增 Agent 容易
- Plugin 架構彈性
- 配置管理統一

## 🚦 錯誤處理機制

1. **無效選項處理**：提示使用者重新選擇
2. **空輸入檢查**：防止程式異常
3. **函數調用失敗**：優雅降級處理
4. **JSON 解析錯誤**：fallback 到一般回應

這個系統提供了企業級的 AI 助手解決方案，可以根據不同業務需求提供專業服務。