using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Drawing;

namespace BreakfastApp
{
    public class MenuService
    {
        private MenuRoot _menuData;
        private string _filePath;

        public List<MenuItem> AllItems { get; private set; }
        public List<Category> Categories => _menuData.Categories;

        public MenuService(string filePath)
        {
            _filePath = filePath;
            _menuData = new MenuRoot();
            AllItems = new List<MenuItem>();
        }

        // --- 影像處理工具 ---
        public Image GetThumbnail(string relativePath, int targetWidth = 300)
        {
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (!File.Exists(imgPath)) return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(imgPath);
                using (var ms = new MemoryStream(bytes))
                {
                    using (var tempImg = Image.FromStream(ms))
                    {
                        int newHeight = (int)((double)tempImg.Height / tempImg.Width * targetWidth);
                        var thumb = new Bitmap(targetWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (var g = Graphics.FromImage(thumb))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(tempImg, 0, 0, targetWidth, newHeight);
                        }
                        return thumb;
                    }
                }
            }
            catch { return null; }
        }

                public void LoadData()

                {

                    if (!File.Exists(_filePath))

                    {

                        throw new FileNotFoundException($"找不到檔案: {_filePath}");

                    }

        

                    try

                    {

                        string jsonString = File.ReadAllText(_filePath);

                        var options = new JsonSerializerOptions

                        {

                            PropertyNameCaseInsensitive = true,

                            ReadCommentHandling = JsonCommentHandling.Skip,

                            AllowTrailingCommas = true

                        };

                        

                        var root = JsonSerializer.Deserialize<MenuRoot>(jsonString, options);

                        

                        // 檢查是否為單一類別 JSON

                        if (root == null || (root.Categories == null || root.Categories.Count == 0))

                        {

                            try

                            {

                                var singleCat = JsonSerializer.Deserialize<Category>(jsonString, options);

                                if (singleCat != null && !string.IsNullOrEmpty(singleCat.CategoryName))

                                {

                                    root = new MenuRoot 

                                    {

                                        MenuName = "單一類別匯入", 

                                        Categories = new List<Category> { singleCat } 

                                    };

                                }

                            }

                            catch { /* 忽略 */ }

                        }

        

                        _menuData = root ?? new MenuRoot();

                        

                        // --- ID 初始化邏輯 ---

                        AllItems.Clear();

                        int maxId = 0;

                        if (_menuData.Categories != null)

                        {

                            foreach (var cat in _menuData.Categories)

                            {

                                if (cat.Items != null)

                                {

                                    foreach (var item in cat.Items)

                                    {

                                        if (item.Id > maxId) maxId = item.Id;

                                        AllItems.Add(item);

                                    }

                                }

                            }

                            

                            // 為沒有 ID 的項目補上 ID

                            foreach (var item in AllItems)

                            {

                                if (item.Id == 0)

                                {

                                    maxId++;

                                    item.Id = maxId;

                                }

                            }

                        }

                    }

                    catch (Exception ex)

                    {

                        throw new Exception($"讀取資料失敗: {ex.Message}");

                    }

                }

        

        public void SaveData(string targetPath)
        {
            // 簡單實作：將目前記憶體中的 AllItems 存檔
            // 因為 ID 機制已經修改了 _menuData 結構 (在 LoadData 時補上 ID)，
            // 所以直接序列化 _menuData 即可保留 ID
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(_menuData, options); 
            File.WriteAllText(targetPath, jsonString);
        }

        

                public void AddItem(MenuItem item, string categoryName)

                {

                    // 自動生成 ID

                    int maxId = AllItems.Any() ? AllItems.Max(x => x.Id) : 0;

                    item.Id = maxId + 1;

        

                    AllItems.Add(item);

                    

                    // ... (rest of logic) ...

                    // 尋找目標分類

                    var targetCat = _menuData.Categories.FirstOrDefault(c => c.CategoryName == categoryName);

                    if (targetCat != null)

                    {

                        targetCat.Items.Add(item);

                    }

                    else

                    {

                        // 如果找不到 (或是空字串)，則加到第一個或新建

                        if (_menuData.Categories.Count > 0)

                        {

                            _menuData.Categories[0].Items.Add(item);

                        }

                        else

                        {

                            _menuData.Categories.Add(new Category 

                            {

                                CategoryName = string.IsNullOrEmpty(categoryName) ? "新增項目" : categoryName, 

                                Items = new List<MenuItem> { item } 

                            });

                        }

                    }

                }

        

                // ... RemoveItem, UpdateItem ...

        

                public void SortCategory(string categoryName, bool ascending)

                {

                    var cat = _menuData.Categories.FirstOrDefault(c => c.CategoryName == categoryName);

                    if (cat != null && cat.Items != null)

                    {

                        int GetBasePrice(MenuItem item)

                        {

                            return item.PriceRegular ?? 

                                   item.PriceSmall ?? 

                                   item.PriceSingle ?? 

                                   item.Price ?? 0;

                        }

        

                        if (ascending)

                            cat.Items.Sort((x, y) => GetBasePrice(x).CompareTo(GetBasePrice(y)));

                        else

                            cat.Items.Sort((x, y) => GetBasePrice(y).CompareTo(GetBasePrice(x)));

                    }

                }

        

                public MenuItem? GetItemById(int id)

                {

                    return AllItems.FirstOrDefault(x => x.Id == id);

                }

        public void RemoveItem(MenuItem item)
        {
            if (AllItems.Contains(item))
            {
                AllItems.Remove(item);
                // 同步從巢狀結構移除
                foreach (var cat in _menuData.Categories)
                {
                    if (cat.Items.Contains(item))
                    {
                        cat.Items.Remove(item);
                        break; 
                    }
                }
            }
        }

        public void UpdateItem(int index, MenuItem newItem, string newCategoryName)
        {
            if (index >= 0 && index < AllItems.Count)
            {
                var oldItem = AllItems[index];
                AllItems[index] = newItem;

                // 1. 先從舊分類移除
                foreach (var cat in _menuData.Categories)
                {
                    if (cat.Items.Contains(oldItem))
                    {
                        cat.Items.Remove(oldItem);
                        break; 
                    }
                }

                // 2. 加入到新分類
                var targetCat = _menuData.Categories.FirstOrDefault(c => c.CategoryName == newCategoryName);
                if (targetCat == null)
                {
                    // 若找不到分類 (例如原本就在第一類且未變動，或新分類不存在)，則回退到第一類或新建
                     if (_menuData.Categories.Count > 0)
                        targetCat = _menuData.Categories[0];
                     else
                     {
                        targetCat = new Category { CategoryName = newCategoryName, Items = new List<MenuItem>() };
                        _menuData.Categories.Add(targetCat);
                     }
                }
                targetCat.Items.Add(newItem);
            }
        }

        public void ClearAll()
        {
            AllItems.Clear();
            _menuData.Categories.Clear();
        }

        // --- Sorting ---

        public void SortByName(bool ascending)
        {
            if (ascending)
                AllItems.Sort((x, y) => string.Compare(x.Name, y.Name));
            else
                AllItems.Sort((x, y) => string.Compare(y.Name, x.Name));
        }

        public void SortByPrice(bool ascending)
        {
            // 因為價格欄位很多，這裡以 "PriceRegular" 或 "PriceSmall" 做為排序基準示範
            // 若該欄位為 null 則視為 0
            int GetBasePrice(MenuItem item)
            {
                // 優先取一般價格，若無則取小杯價，再無則取單價...依此類推
                return item.PriceRegular ?? 
                       item.PriceSmall ?? 
                       item.PriceSingle ?? 
                       item.Price ?? 0;
            }

            if (ascending)
                AllItems.Sort((x, y) => GetBasePrice(x).CompareTo(GetBasePrice(y)));
            else
                AllItems.Sort((x, y) => GetBasePrice(y).CompareTo(GetBasePrice(x)));
        }

        // --- Checkout Calculation ---
        
        public int CalculateTotal(List<CartItem> cartItems)
        {
            return cartItems.Sum(x => x.Price);
        }
    }
}
