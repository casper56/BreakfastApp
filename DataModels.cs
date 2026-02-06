using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;

namespace BreakfastApp
{
    // 對應 JSON 根目錄
    public class MenuRoot
    {
        [JsonPropertyName("menu_name")]
        public string MenuName { get; set; } = string.Empty;

        [JsonPropertyName("source_files")]
        public List<string> SourceFiles { get; set; } = new List<string>();

        [JsonPropertyName("categories")]
        public List<Category> Categories { get; set; } = new List<Category>();
    }

    // 對應個別類別 (如: 土司類, 漢堡類)
    public class Category
    {
        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("items")]
        public List<MenuItem> Items { get; set; } = new List<MenuItem>();
    }

    // 購物車內的項目 (與 JSON 結構對應)
    public class CartItem
    {
        [JsonPropertyName("item_id")]
        public int ItemId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("option")]
        public string OptionName { get; set; } = "單點";

        [JsonPropertyName("unit_price")]
        public int Price { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("subtotal")]
        public int Subtotal => Price * Quantity;

        // 不參與 JSON 序列化的原始物件參考，方便程式操作
        [JsonIgnore]
        public MenuItem Item { get; set; } = new MenuItem();

        public override string ToString()
        {
            return $"[{ItemId:00}] {Name}({OptionName}) x{Quantity} ${Subtotal}";
        }
    }

    // 訂單模型 (整台購物車的 JSON 結構)
    public class Order
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty; // YYYYMMDDxxxx

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [JsonPropertyName("items")]
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        [JsonPropertyName("total_amount")]
        public int TotalAmount => Items.Sum(x => x.Subtotal);
    }

    // 對應個別商品 (包含所有可能的屬性)
    public class MenuItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // --- 價格欄位 (全部設為可空 int?) ---

        // 飲品/薯條類
        [JsonPropertyName("price_small")]
        public int? PriceSmall { get; set; }
        [JsonPropertyName("price_medium")]
        public int? PriceMedium { get; set; }
        [JsonPropertyName("price_large")]
        public int? PriceLarge { get; set; }
        [JsonPropertyName("price_single")]
        public int? PriceSingle { get; set; } // 研磨咖啡單價

        // 一般餐點/套餐
        [JsonPropertyName("price_regular")]
        public int? PriceRegular { get; set; }
        [JsonPropertyName("price")]
        public int? Price { get; set; } // 套餐總價

        // 加蛋類 (土司/漢堡/鐵板麵)
        [JsonPropertyName("price_with_egg")]
        public int? PriceWithEgg { get; set; }

        // 蛋餅/河粉類
        [JsonPropertyName("price_danbing")]
        public int? PriceDanbing { get; set; }
        [JsonPropertyName("price_hefen")]
        public int? PriceHefen { get; set; }

        // 數量類 (餃子/蛋)
        [JsonPropertyName("price_8pcs")]
        public int? Price8Pcs { get; set; }
        [JsonPropertyName("price_10pcs")]
        public int? Price10Pcs { get; set; }
        [JsonPropertyName("price_1pc")]
        public int? Price1Pc { get; set; }
        [JsonPropertyName("price_2pcs")]
        public int? Price2Pcs { get; set; }

        // 其他屬性
        [JsonPropertyName("content")]
        public string? Content { get; set; } // 套餐內容
        
        [JsonPropertyName("flavors")]
        public List<string>? Flavors { get; set; } // 口味選擇

        [JsonPropertyName("category_note")]
        public string? CategoryNote { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        // 方便顯示的 ToString 方法
        public override string ToString()
        {
            var details = new List<string>();
            
            if (Price.HasValue) details.Add($"${Price}");
            if (PriceRegular.HasValue) details.Add($"原${PriceRegular}");
            if (PriceSmall.HasValue) details.Add($"小${PriceSmall}");
            if (PriceMedium.HasValue) details.Add($"中${PriceMedium}");
            if (PriceLarge.HasValue) details.Add($"大${PriceLarge}");
            if (PriceSingle.HasValue) details.Add($"單${PriceSingle}");
            if (PriceWithEgg.HasValue) details.Add($"加蛋${PriceWithEgg}");
            if (PriceDanbing.HasValue) details.Add($"蛋餅${PriceDanbing}");
            if (PriceHefen.HasValue) details.Add($"河粉${PriceHefen}");
            
            string detailStr = details.Count > 0 ? $" ({string.Join(", ", details)})" : "";
            return $"{Name}{detailStr}";
        }
    }
}