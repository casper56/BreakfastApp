# 早餐店點餐管理系統 (BreakfastApp)

![Platform](https://img.shields.io/badge/Platform-.NET%2010.0%20WinForms-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

這是一個專為台灣早餐店開發的桌面點餐管理系統。透過直覺的圖片化介面與智慧化的選項處理，旨在簡化繁瑣的點餐流程並提供高效的訂單管理。

## 🚀 核心功能

### 1. 智慧圖形化點餐
*   **分類分頁**：依據商品類別（土司、漢堡、蛋餅等）自動生成 Tab 分頁。
*   **直覺操作**：大圖示按鈕顯示品項、編號與價格。
*   **選項彈窗**：點擊具有多重規格（如加蛋、大小杯、口味）的品項時，系統會自動彈出選單供快速選擇。
*   **即時搜尋**：支援全品項關鍵字過濾。

### 2. 購物車管理
*   **自動合併**：相同品項與規格會自動累加數量。
*   **彈性編輯**：可直接在清單中加減數量或點擊刪除，支援鍵盤 `Delete` 鍵批次刪除。
*   **總額計算**：即時更新購物車總金額。

### 3. 專業列印服務
*   **客戶收據**：格式化收據列印預覽，包含明細與單號。
*   **廚房製作單**：排除價格資訊，僅顯示製作細節與數量，優化廚房作業。
*   **菜單清冊**：一鍵產生店內完整菜單價目表。

### 4. 後台管理
*   **商品 CRUD**：內建編輯視窗，可修改品項、價格、分類與對應圖片。
*   **資料持久化**：所有菜單與訂單資訊均以 JSON 格式儲存在本地端，方便備份與移植。
*   **排序功能**：支援依據價格或 ID 進行升降冪排序。

## 🛠 技術架構

*   **語言**: C# 13
*   **框架**: .NET 10.0 Windows Forms
*   **設計模式**: 服務化架構 (Service-Oriented Architecture)
    *   `MenuService`: 處理菜單邏輯與影像縮圖快取。
    *   `OrderService`: 管理訂單流水號與歷史紀錄。
    *   `PrintService`: 封裝 GDI+ 列印與預覽邏輯。
*   **UI 配置**: 採用 `SplitContainer` 實現響應式佈局，確保在不同解析度下操作一致性。

## 📦 安裝與執行

1.  **環境需求**:
    *   安裝 [.NET 10 SDK](https://dotnet.microsoft.com/download)
    *   Visual Studio 2022 (或更高版本)

2.  **複製專案**:
    ```bash
    git clone https://github.com/YourUsername/BreakfastApp.git
    cd BreakfastApp
    ```

3.  **執行程式**:
    使用 Visual Studio 開啟 `BreakfastApp.sln` 並按下 `F5`，或使用 CLI：
    ```bash
    dotnet run
    ```

## 📂 資料結構說明

本系統使用 `category_all.json` 管理菜單，結構如下：
```json
{
  "menu_name": "早餐店菜單",
  "categories": [
    {
      "category_name": "漢堡類",
      "items": [
        {
          "id": 1,
          "name": "牛肉漢堡",
          "price_regular": 35,
          "price_with_egg": 45,
          "image": "images/漢堡/牛肉.png"
        }
      ]
    }
  ]
}
```

## 📝 規格書
詳細的技術開發細節請參閱專案內的 [spec.md](./spec.md)。

## 📄 授權
本專案採用 [MIT License](LICENSE) 授權。
