# 早餐店點餐管理系統 (BreakfastApp)

![Platform](https://img.shields.io/badge/Platform-.NET%2010.0%20WinForms-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

這是一個專為台灣早餐店開發的桌面點餐管理系統。透過直覺的圖片化介面與智慧化的選項處理，旨在簡化繁瑣的點餐流程並提供高效的訂單管理。

## 🚀 核心功能

### 1. 智慧圖形化點餐
*   **分類分頁**：依據商品類別（土司、漢堡、蛋餅等）自動生成 Tab 分頁。
*   **直覺操作**：大圖示按鈕顯示品項、編號與價格。
*   **選項彈窗**：點擊具有多重規格（如加蛋、大小杯、口味）的品項時，系統會自動彈出選單供快速選擇。
*   **即時搜尋**：支援全品項關鍵字過濾（輸入後按 **Enter** 觸發，附帶過渡遮罩效果）。

### 2. 購物車管理
*   **自動合併**：相同品項與規格會自動累加數量。
*   **彈性編輯**：可直接在清單中加減數量或點擊刪除，支援鍵盤 `Delete` 鍵批次刪除。
*   **欄位排序**：點擊 DataGridView 標題（如品項名稱、單價）可進行即時排序。
*   **總額計算**：即時更新購物車總金額，顯示於結帳按鈕上方。

### 3. 專業列印服務
*   **客戶收據**：格式化收據列印預覽，包含明細與單號。
*   **廚房製作單**：排除價格資訊，僅顯示製作細節與數量，優化廚房作業。
*   **菜單清冊**：一鍵產生店內完整菜單價目表。

### 4. 後台管理
*   **商品 CRUD**：內建編輯視窗，可修改品項、價格、分類與對應圖片。
*   **資料持久化**：所有菜單與訂單資訊均以 JSON 格式儲存在本地端，方便備份與移植。
*   **排序功能**：支援依據價格或 ID 進行升降冪排序。

## 🏗 程式結構 (Program Structure)

本專案採用關注點分離 (SoC) 原則開發，主要分為以下層次：

*   **`DataModels.cs` (模型層)**: 定義選單 (`MenuRoot`)、類別 (`Category`)、商品 (`MenuItem`) 與訂單 (`Order`) 的資料結構，完整支援 JSON 序列化與反序列化。
*   **`Services/` (服務層)**: 
    *   `MenuService.cs`: 選單資料載入、儲存、CRUD 操作及圖片縮圖處理。
    *   `OrderService.cs`: 處理訂單儲存邏輯與流水號生成。
    *   `PrintService.cs`: 封裝所有列印邏輯與預覽視窗。
*   **`UI Forms/` (表現層)**:
    *   `Form1.cs`: 主控介面，處理動態選單生成、搜尋與購物車交互。
    *   `EditItemForm.cs`: 複合式商品編輯表單，支援多種價格規格設定。
    *   `OrderHistoryForm.cs`: 歷史訂單查詢與營收統計介面。
    *   `ImagePreviewForm.cs`: 商品圖片瀏覽器。

## 📂 資料結構說明

1.  **環境需求**:
    *   安裝 [.NET 10 SDK](https://dotnet.microsoft.com/download)
    *   Visual Studio 2022 (或更高版本)

2.  **複製專案**:
    ```bash
    git clone https://github.com/casper56/BreakfastApp.git
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
