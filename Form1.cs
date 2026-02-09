using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.IO;

namespace BreakfastApp
{
    public partial class Form1 : Form
    {
        private MenuService _menuService;
        private OrderService _orderService; // 新增訂單服務
        private string _jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "category_all.json");

        private TabControl tabMenu;
        private DataGridView dgvCart; 
        private Label lblTotal;
        private Label lblStatus;
        private TextBox txtSearchMenu; // 商品搜尋框
        
        private List<CartItem> _cartItems = new List<CartItem>();
        private Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
        private string _cartSortColumn = "";
        private SortOrder _cartSortOrder = SortOrder.None;

        private Label lblLoading; 

        public Form1()
        {
            InitializeComponent();
            _menuService = new MenuService(_jsonPath);
            _orderService = new OrderService(); // 初始化
            SetupDynamicUI();
        }
        
        // 清除快取以釋放資源
        private void ClearImageCache()
        {
            foreach (var img in _imageCache.Values) img.Dispose();
            _imageCache.Clear();
            GC.Collect(); // 強制回收大圖佔用的記憶體
        }

        private void SetupDynamicUI()
        {
            this.Text = "早餐店點餐管理系統 (智慧選擇版) - v1.2";
            this.Size = new Size(1050, 900); // 調整寬度至 1050
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // 禁止調整大小
            this.MaximizeBox = false; // 停用最大化按鈕
            this.StartPosition = FormStartPosition.CenterScreen;

            // 頂部固定工具列
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(5) };
            this.Controls.Add(pnlHeader);

            // 主分割容器
            SplitContainer splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 500, // 預設上方高度
                SplitterWidth = 4,      // 縮小分割線寬度
                BorderStyle = BorderStyle.Fixed3D,
                IsSplitterFixed = true  // 關閉垂直方向 resize
            };
            this.Controls.Add(splitMain);
            splitMain.BringToFront(); // 確保在工具列下方

            // 工具列內容
            FlowLayoutPanel pnlToolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), AutoSize = true };
            Button CreateBtn(string text, EventHandler action) 
            {
                var btn = new Button { Text = text, AutoSize = true, Margin = new Padding(3), Font = new Font("Microsoft JhengHei", 9) };
                btn.Click += action;
                return btn;
            }
            
            pnlToolbar.Controls.Add(CreateBtn("📂 匯入資料", (s, e) => LoadData()));
            pnlToolbar.Controls.Add(CreateBtn("💾 儲存資料", (s, e) => SaveData()));
            pnlToolbar.Controls.Add(new Label { Text = " | ", AutoSize = true }); 
            pnlToolbar.Controls.Add(CreateBtn("➕ 新增", (s, e) => AddNewItem()));
            pnlToolbar.Controls.Add(CreateBtn("✏️ 修改", (s, e) => UpdateSelectedItem()));
            pnlToolbar.Controls.Add(CreateBtn("❌ 刪除", (s, e) => DeleteSelectedItem()));
            pnlToolbar.Controls.Add(new Label { Text = " | ", AutoSize = true }); 
            pnlToolbar.Controls.Add(CreateBtn("🔼 排序", (s, e) => SortItems(true)));
            pnlToolbar.Controls.Add(CreateBtn("🖨️ 預覽菜單", (s, e) => PrintOrderPreview())); 
            pnlToolbar.Controls.Add(CreateBtn("📜 歷史訂單", (s, e) => ShowOrderHistory())); 
            
            pnlToolbar.Controls.Add(new Label { Text = " |  🔍 搜尋:", AutoSize = true, Margin = new Padding(10, 8, 0, 0) });
            txtSearchMenu = new TextBox { Width = 120, Margin = new Padding(3, 5, 0, 0) };
            txtSearchMenu.KeyDown += (s, e) => 
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // 防止警告音
                    GenerateMenuTabs();
                }
            };
            pnlToolbar.Controls.Add(txtSearchMenu);
            pnlHeader.Controls.Add(pnlToolbar);

            // 上方：菜單區
            GroupBox grpMenu = new GroupBox { Text = "菜單區 (單擊點餐 / 若有加蛋或大杯等選項會自動彈出選單)", Dock = DockStyle.Fill, Font = new Font("Microsoft JhengHei", 10) };
            lblLoading = new Label 
            {
                Text = "資料載入中，請稍候...", 
                AutoSize = false, 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft JhengHei", 20, FontStyle.Bold),
                ForeColor = Color.DimGray,
                BackColor = Color.WhiteSmoke,
                Visible = false 
            };
            tabMenu = new TabControl 
            { 
                Dock = DockStyle.Fill, 
                Font = new Font("Microsoft JhengHei", 12, FontStyle.Bold), // 縮小至 12pt
                ItemSize = new Size(100, 38), // 縮小尺寸
                SizeMode = TabSizeMode.Fixed 
            };
            grpMenu.Controls.Add(lblLoading); 
            grpMenu.Controls.Add(tabMenu);
            splitMain.Panel1.Controls.Add(grpMenu);

            // 下方：底部區 (購物車與結帳)
            TableLayoutPanel bottomPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(10) };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70)); 
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            
            GroupBox grpCart = new GroupBox 
            { 
                Text = "選購清單 (可直接修改數量或刪除)", 
                Dock = DockStyle.Fill, 
                Font = new Font("Microsoft JhengHei", 10),
                Padding = new Padding(10, 25, 10, 10) 
            };
            
            dgvCart = new DataGridView 
            { 
                Dock = DockStyle.Fill, 
                AutoGenerateColumns = false, 
                AllowUserToAddRows = false, 
                MultiSelect = true, 
                RowHeadersVisible = true, 
                RowHeadersWidth = 30,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, 
                Font = new Font("Microsoft JhengHei", 10),
                ScrollBars = ScrollBars.Both 
            };
            
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "品項名稱", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true }); 
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OptionName", HeaderText = "規格/口味", Width = 120, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Price", HeaderText = "單價", Width = 60, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewButtonColumn { Text = "-", UseColumnTextForButtonValue = true, Width = 35, HeaderText = "" });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "數量", Width = 50 });
            dgvCart.Columns.Add(new DataGridViewButtonColumn { Text = "+", UseColumnTextForButtonValue = true, Width = 35, HeaderText = "" });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Subtotal", HeaderText = "小計", Width = 80, ReadOnly = true });
            dgvCart.Columns.Add(new DataGridViewButtonColumn { HeaderText = "操作", Text = "刪除", UseColumnTextForButtonValue = true, Width = 60 });
            
            // 支援標題點擊排序
            dgvCart.ColumnHeaderMouseClick += (s, e) => 
            {
                var col = dgvCart.Columns[e.ColumnIndex];
                if (string.IsNullOrEmpty(col.DataPropertyName)) return;

                if (_cartSortColumn == col.DataPropertyName)
                    _cartSortOrder = (_cartSortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
                else
                {
                    _cartSortColumn = col.DataPropertyName;
                    _cartSortOrder = SortOrder.Ascending;
                }

                ApplyCartSort();
            };

            dgvCart.CellValueChanged += (s, e) => { if (e.RowIndex >= 0) UpdateCartDisplay(); };
            dgvCart.CellContentClick += (s, e) => 
            {
                if (e.RowIndex < 0) return;
                var item = _cartItems[e.RowIndex];
                string headerText = dgvCart.Columns[e.ColumnIndex].HeaderText;
                if (headerText == "") 
                {
                    if (e.ColumnIndex == 3) // "-"
                    {
                        if (item.Quantity > 1) item.Quantity--;
                        else _cartItems.RemoveAt(e.RowIndex);
                        UpdateCartDisplay();
                    }
                    else if (e.ColumnIndex == 5) // "+"
                    {
                        item.Quantity++;
                        UpdateCartDisplay();
                    }
                }
                else if (e.ColumnIndex == dgvCart.Columns.Count - 1) // 刪除
                {
                    _cartItems.RemoveAt(e.RowIndex);
                    UpdateCartDisplay();
                }
            };

            dgvCart.KeyDown += (s, e) => 
            {
                if (e.KeyCode == Keys.Delete && dgvCart.SelectedRows.Count > 0)
                {
                    var itemsToRemove = new List<CartItem>();
                    foreach (DataGridViewRow row in dgvCart.SelectedRows)
                        if (row.DataBoundItem is CartItem ci) itemsToRemove.Add(ci);
                    foreach (var item in itemsToRemove) _cartItems.Remove(item);
                    UpdateCartDisplay();
                }
            };

            grpCart.Controls.Add(dgvCart);
            bottomPanel.Controls.Add(grpCart, 0, 0);

            Panel pnlCheckout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            lblTotal = new Label { Text = "總金額: $0", Font = new Font("Microsoft JhengHei", 24, FontStyle.Bold), Dock = DockStyle.Bottom, Height = 60, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.DarkRed };
            lblStatus = new Label { Text = "系統就緒", Dock = DockStyle.Bottom, AutoSize = true };
            Button btnClearCart = new Button { Text = "🧹 清空購物車", Dock = DockStyle.Bottom, Height = 35, BackColor = Color.WhiteSmoke, Font = new Font("Microsoft JhengHei", 10) };
            btnClearCart.Click += (s, e) => { if (MessageBox.Show("確定清空購物車？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes) { _cartItems.Clear(); UpdateCartDisplay(); } };
            Button btnCheckout = new Button { Text = "💰 結帳並出單", Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Gold, Font = new Font("Microsoft JhengHei", 14, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnCheckout.Click += (s, e) => PerformCheckout();
            
            // 依序加入 (最後加入的會在最上方)
            pnlCheckout.Controls.Add(lblStatus);
            pnlCheckout.Controls.Add(btnClearCart);
            pnlCheckout.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 10 });
            pnlCheckout.Controls.Add(btnCheckout);
            pnlCheckout.Controls.Add(lblTotal);
            
            bottomPanel.Controls.Add(pnlCheckout, 1, 0);
            
            splitMain.Panel2.Controls.Add(bottomPanel);
        }

        private void GenerateMenuTabs()
        {
            lblLoading.Text = "選單處理中...";
            lblLoading.Visible = true;
            lblLoading.BringToFront();
            tabMenu.Visible = false;
            Application.DoEvents();

            tabMenu.SuspendLayout(); 
            try
            {
                ClearImageCache();
                tabMenu.TabPages.Clear();
                string filter = txtSearchMenu?.Text?.Trim().ToLower() ?? "";

                foreach (var cat in _menuService.Categories)
                {
                    // 過濾該類別下的項目
                    var filteredItems = string.IsNullOrEmpty(filter) 
                        ? cat.Items 
                        : cat.Items.Where(i => i.Name.ToLower().Contains(filter)).ToList();

                    // 如果有搜尋且該分類沒東西，則不顯示該分頁 (除非是原本就沒搜尋)
                    if (!string.IsNullOrEmpty(filter) && filteredItems.Count == 0) continue;

                    TabPage tab = new TabPage(cat.CategoryName) { BackColor = Color.White };
                    FlowLayoutPanel pnl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10), WrapContents = true }; 
                    foreach (var item in filteredItems)
                    {
                        pnl.Controls.Add(CreateSmartButton(item));
                    }
                    
                    // 加入一個高度為 40 的隱形標籤作為底部間距，寬度設為 100 確保不會強制換行但能撐開高度
                    pnl.Controls.Add(new Label { Width = 100, Height = 40, Text = "", Margin = new Padding(0) }); 
                    
                    tab.Controls.Add(pnl);
                    tabMenu.TabPages.Add(tab);
                }
                lblStatus.Text = $"商品總數: {_menuService.AllItems.Count} | 圖片快取: {_imageCache.Count} | {DateTime.Now:HH:mm:ss}";
            }
            finally
            {
                tabMenu.ResumeLayout(); 
                lblLoading.Visible = false;
                tabMenu.Visible = true;
            }
        }

        private Button CreateSmartButton(MenuItem item)
        {
            Button btn = new Button();
            int basePrice = item.PriceRegular ?? item.PriceSmall ?? item.PriceSingle ?? item.Price ?? 0;
            
            bool hasMulti = HasMultipleOptions(item);
            string indicator = hasMulti ? " ☰" : "";

            btn.Text = $"[{item.Id:00}] {item.Name}\n${basePrice}{indicator}";
            btn.Size = new Size(190, 110);
            btn.BackColor = Color.AliceBlue;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.LightSkyBlue;
            btn.Margin = new Padding(6);
            btn.TextAlign = ContentAlignment.TopLeft; 
            btn.ForeColor = Color.Blue; 
            btn.Font = new Font("Microsoft JhengHei", 11, FontStyle.Bold);
            
            if (!string.IsNullOrEmpty(item.Image))
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, item.Image);
                if (!_imageCache.ContainsKey(fullPath))
                {
                    var thumb = _menuService.GetThumbnail(item.Image);
                    if (thumb != null) _imageCache[fullPath] = thumb;
                }

                if (_imageCache.ContainsKey(fullPath))
                {
                    btn.BackgroundImage = _imageCache[fullPath];
                    btn.BackgroundImageLayout = ImageLayout.Zoom;
                }
                else
                {
                    btn.Text += "\n(圖?)";
                }
            }

            // 核心邏輯：點擊判斷
            btn.Click += (s, e) => HandleItemClick(item, btn);
            // 右鍵快捷修改 (改用 ID 尋找)
            btn.MouseDown += (s, e) => { if (e.Button == MouseButtons.Right) PerformUpdate(item.Id); };
            
            return btn;
        }

        private bool HasMultipleOptions(MenuItem item)
        {
            // 如果有口味，或是多種價格，都視為多重選項
            if (item.Flavors != null && item.Flavors.Count > 0) return true;

            int count = 0;
            if (item.PriceRegular.HasValue) count++;
            if (item.PriceWithEgg.HasValue) count++;
            if (item.PriceSmall.HasValue) count++;
            if (item.PriceMedium.HasValue) count++;
            if (item.PriceLarge.HasValue) count++;
            if (item.PriceDanbing.HasValue) count++;
            if (item.PriceHefen.HasValue) count++;
            if (item.Price8Pcs.HasValue) count++;
            if (item.Price10Pcs.HasValue) count++;
            return count > 1;
        }

        private List<(string Name, int Price)> GetPriceOptions(MenuItem item)
        {
            var options = new List<(string Name, int Price)>();
            if (item.PriceRegular.HasValue) options.Add(("原價", item.PriceRegular.Value));
            if (item.PriceWithEgg.HasValue) options.Add(("加蛋", item.PriceWithEgg.Value));
            if (item.PriceSmall.HasValue) options.Add(("小杯/份", item.PriceSmall.Value));
            if (item.PriceMedium.HasValue) options.Add(("中杯/份", item.PriceMedium.Value));
            if (item.PriceLarge.HasValue) options.Add(("大杯/份", item.PriceLarge.Value));
            if (item.PriceDanbing.HasValue) options.Add(("蛋餅皮", item.PriceDanbing.Value));
            if (item.PriceHefen.HasValue) options.Add(("河粉皮", item.PriceHefen.Value));
            if (item.Price8Pcs.HasValue) options.Add(("8顆", item.Price8Pcs.Value));
            if (item.Price10Pcs.HasValue) options.Add(("10顆", item.Price10Pcs.Value));
            if (item.Price.HasValue) options.Add(("套餐價", item.Price.Value));
            if (item.PriceSingle.HasValue) options.Add(("單點", item.PriceSingle.Value));
            return options;
        }

        private void HandleItemClick(MenuItem item, Button btn)
        {
            var priceOpts = GetPriceOptions(item);

            // 1. 如果有口味 (Flavors)，優先顯示口味選單
            if (item.Flavors != null && item.Flavors.Count > 0)
            {
                ContextMenuStrip menu = CreateStyledMenu();
                foreach (var flavor in item.Flavors)
                {
                    var flavorItem = new ToolStripMenuItem(flavor);
                    flavorItem.Padding = new Padding(10, 8, 10, 8);

                    // 檢查這個口味底下是否有價格區分 (原價 vs 加蛋)
                    if (priceOpts.Count > 1)
                    {
                        // 建立子選單 (Submenu)
                        foreach (var opt in priceOpts)
                        {
                            var subItem = new ToolStripMenuItem($"{opt.Name} (${opt.Price})");
                            subItem.Padding = new Padding(10, 5, 10, 5);
                            subItem.Font = new Font("Microsoft JhengHei", 11);
                            subItem.Click += (s, e) => AddToCart(item, $"{flavor}/{opt.Name}", opt.Price);
                            flavorItem.DropDownItems.Add(subItem);
                        }
                    }
                    else
                    {
                        // 只有一種價格，點口味直接加入
                        int p = priceOpts.Count > 0 ? priceOpts[0].Price : 0;
                        string suffix = priceOpts.Count > 0 ? "" : "(未定價)"; // 預防無價格
                        flavorItem.Text += suffix; 
                        flavorItem.Click += (s, e) => AddToCart(item, flavor, p);
                    }
                    menu.Items.Add(flavorItem);
                }
                menu.Show(btn, new Point(0, btn.Height));
                return;
            }

            // 2. 沒有口味，但有多重價格 (原價/加蛋/大小杯)
            if (priceOpts.Count > 1)
            {
                ContextMenuStrip menu = CreateStyledMenu();
                foreach (var opt in priceOpts)
                {
                    var mItem = new ToolStripMenuItem($"{opt.Name} (${opt.Price})");
                    mItem.Padding = new Padding(10, 8, 10, 8);
                    mItem.Click += (s, e) => AddToCart(item, opt.Name, opt.Price);
                    menu.Items.Add(mItem);
                }
                menu.Show(btn, new Point(0, btn.Height));
            }
            // 3. 單一規格，直接加入
            else
            {
                int p = priceOpts.Count > 0 ? priceOpts[0].Price : 0;
                string n = priceOpts.Count > 0 ? priceOpts[0].Name : "單點";
                AddToCart(item, n, p);
            }
        }

        private ContextMenuStrip CreateStyledMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Microsoft JhengHei", 12, FontStyle.Bold);
            menu.ShowImageMargin = false;
            menu.Padding = new Padding(5);
            return menu;
        }

        private void AddToCart(MenuItem item, string option, int price)
        {
            // 找出該項目所屬分類
            string categoryName = _menuService.Categories.FirstOrDefault(c => c.Items.Contains(item))?.CategoryName ?? "其他";

            // 檢查購物車中是否已有相同品項與相同規格
            var existing = _cartItems.FirstOrDefault(x => x.ItemId == item.Id && x.OptionName == option);
            
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _cartItems.Add(new CartItem 
                { 
                    Item = item, 
                    ItemId = item.Id,
                    Name = item.Name,
                    CategoryName = categoryName,
                    OptionName = option, 
                    Price = price,
                    Quantity = 1
                });
            }
            UpdateCartDisplay();
        }

        private void UpdateCartDisplay()
        {
            if (!string.IsNullOrEmpty(_cartSortColumn) && _cartSortOrder != SortOrder.None)
            {
                switch (_cartSortColumn)
                {
                    case "Name":
                        _cartItems = _cartSortOrder == SortOrder.Ascending ? _cartItems.OrderBy(x => x.Name).ToList() : _cartItems.OrderByDescending(x => x.Name).ToList();
                        break;
                    case "OptionName":
                        _cartItems = _cartSortOrder == SortOrder.Ascending ? _cartItems.OrderBy(x => x.OptionName).ToList() : _cartItems.OrderByDescending(x => x.OptionName).ToList();
                        break;
                    case "Price":
                        _cartItems = _cartSortOrder == SortOrder.Ascending ? _cartItems.OrderBy(x => x.Price).ToList() : _cartItems.OrderByDescending(x => x.Price).ToList();
                        break;
                    case "Quantity":
                        _cartItems = _cartSortOrder == SortOrder.Ascending ? _cartItems.OrderBy(x => x.Quantity).ToList() : _cartItems.OrderByDescending(x => x.Quantity).ToList();
                        break;
                    case "Subtotal":
                        _cartItems = _cartSortOrder == SortOrder.Ascending ? _cartItems.OrderBy(x => x.Subtotal).ToList() : _cartItems.OrderByDescending(x => x.Subtotal).ToList();
                        break;
                }
            }

            dgvCart.DataSource = null;
            dgvCart.DataSource = _cartItems;
            lblTotal.Text = $"總金額: ${_cartItems.Sum(x => x.Subtotal)}";

            if (!string.IsNullOrEmpty(_cartSortColumn))
            {
                foreach (DataGridViewColumn col in dgvCart.Columns)
                {
                    if (col.DataPropertyName == _cartSortColumn)
                    {
                        col.HeaderCell.SortGlyphDirection = _cartSortOrder;
                        break;
                    }
                }
            }
        }

        private void ApplyCartSort()
        {
            UpdateCartDisplay();
        }

        private void RefreshState() 
        { 
            lblStatus.Text = $"商品總數: {_menuService.AllItems.Count} | {DateTime.Now:HH:mm:ss}"; 
            GenerateMenuTabs(); 
        }
        private void LoadData(bool autoLoad = false) 
        { 
            try 
            { 
                if (!autoLoad)
                {
                    using (OpenFileDialog ofd = new OpenFileDialog { Filter = "JSON Files|*.json" }) 
                    { 
                        if (ofd.ShowDialog() == DialogResult.OK) 
                            _jsonPath = ofd.FileName; 
                        else 
                            return; 
                    }
                }

                // 顯示載入畫面並隱藏選單，避免白框與閃爍
                Cursor.Current = Cursors.WaitCursor; 
                tabMenu.Visible = false;
                lblLoading.Visible = true;
                lblLoading.BringToFront(); // 確保蓋在最上層
                Application.DoEvents(); // 強制更新畫面顯示 Loading 文字

                _menuService = new MenuService(_jsonPath); 
                _menuService.LoadData(); 
                RefreshState(); 
            } 
            catch (Exception ex) 
            { 
                MessageBox.Show(ex.Message); 
            }
            finally
            {
                // 恢復顯示
                lblLoading.Visible = false;
                tabMenu.Visible = true;
                Cursor.Current = Cursors.Default; 
            }
        }
        private void SaveData() 
        { 
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "JSON Files|*.json" }) 
            { 
                if (sfd.ShowDialog() == DialogResult.OK) 
                { 
                    // 顯示遮罩避免白框
                    Cursor.Current = Cursors.WaitCursor;
                    tabMenu.Visible = false;
                    lblLoading.Text = "儲存資料中，請稍候...";
                    lblLoading.Visible = true;
                    lblLoading.BringToFront();
                    Application.DoEvents();

                    try
                    {
                        _menuService.SaveData(sfd.FileName); 
                    }
                    finally
                    {
                        // 恢復顯示
                        lblLoading.Visible = false;
                        tabMenu.Visible = true;
                        Cursor.Current = Cursors.Default;
                    }
                } 
            } 
        }
        
        // 修正：使用 EditItemForm
        private void AddNewItem() 
        { 
            // 取得所有分類名稱
            var categories = _menuService.Categories.Select(c => c.CategoryName).ToList();
            
            var form = new EditItemForm(categories);
            if (form.ShowDialog() == DialogResult.OK)
            {
                lblLoading.Visible = true; // 顯示遮罩
                lblLoading.BringToFront();
                Application.DoEvents();

                _menuService.AddItem(form.ResultItem, form.SelectedCategoryName);
                RefreshState();
                if (tabMenu.TabCount > 0) tabMenu.SelectedIndex = tabMenu.TabCount - 1;
            }
        }
        
        private void UpdateSelectedItem() 
        { 
            string input = Microsoft.VisualBasic.Interaction.InputBox("請輸入商品編號 (ID)", "修改商品"); 
            if (int.TryParse(input, out int id)) PerformUpdate(id);
        }

        private void PerformUpdate(int id)
        {
            var item = _menuService.GetItemById(id);
            if (item != null)
            {
                // 找出目前所屬分類
                string currentCat = _menuService.Categories.FirstOrDefault(c => c.Items.Contains(item))?.CategoryName ?? "";
                var categories = _menuService.Categories.Select(c => c.CategoryName).ToList();

                var form = new EditItemForm(categories, item, currentCat);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    lblLoading.Visible = true;
                    lblLoading.BringToFront();
                    Application.DoEvents();

                    int idx = _menuService.AllItems.IndexOf(item);
                    _menuService.UpdateItem(idx, form.ResultItem, form.SelectedCategoryName);
                    RefreshState();
                }
            }
            else MessageBox.Show("無效的編號 (ID)！");
        }

        private void DeleteSelectedItem() 
        { 
            string input = Microsoft.VisualBasic.Interaction.InputBox("請輸入商品編號 (ID)", "刪除"); 
            if (int.TryParse(input, out int id)) 
            { 
                var item = _menuService.GetItemById(id);
                if (item != null)
                {
                    _menuService.RemoveItem(item); 
                    RefreshState(); 
                }
                else MessageBox.Show("無效的編號 (ID)！");
            } 
        }
        
        private void PrintOrderPreview()
        {
            if (_menuService.AllItems.Count == 0)
            {
                MessageBox.Show("菜單是空的，無法列印！");
                return;
            }

            PrintService.PreviewMenu(_menuService.Categories, _menuService.AllItems.Count);
        }
        
        private void SortItems(bool asc) 
        { 
            if (tabMenu.SelectedTab != null)
            {
                lblLoading.Visible = true;
                lblLoading.BringToFront();
                Application.DoEvents();

                _menuService.SortCategory(tabMenu.SelectedTab.Text, asc); 
                RefreshState(); 
            }
        }

        private void ShowOrderHistory()
        {
            var form = new OrderHistoryForm(_orderService);
            form.ShowDialog();
        }

        private void PerformCheckout() 
        { 
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("購物車內無商品！");
                return;
            }

            if (MessageBox.Show($"確認結帳？金額: ${_cartItems.Sum(x => x.Subtotal)}", "結帳確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // 1. 生成訂單物件
                var order = new Order
                {
                    OrderId = _orderService.GenerateOrderId(),
                    Timestamp = DateTime.Now,
                    Items = new List<CartItem>(_cartItems)
                };

                // 2. 儲存至 JSON
                _orderService.AddOrder(order);

                // 3. 詢問列印選項
                var result = MessageBox.Show($"結帳成功！\n單號: {order.OrderId}\n\n[是]：預覽客戶收據\n[否]：預覽廚房製作單\n[取消]：不列印", "結帳完成", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                
                if (result == DialogResult.Yes)
                {
                    PrintService.PreviewReceipt(order, ReceiptType.Customer);
                }
                else if (result == DialogResult.No)
                {
                    PrintService.PreviewReceipt(order, ReceiptType.Kitchen);
                }
                
                // 4. 清空購物車
                _cartItems.Clear(); 
                UpdateCartDisplay(); 
            }
        }
    }
}
