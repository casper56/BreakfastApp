using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq; // Added for string.Join convenience if needed

namespace BreakfastApp
{
    public class EditItemForm : Form
    {
        public MenuItem ResultItem { get; private set; }
        public string SelectedCategoryName { get; private set; }

        private TextBox txtName;
        private ComboBox cboCategory; // 新增分類選單
        private TextBox txtFlavors; 
        private TextBox txtImage;

        // 使用 Dictionary 管理所有價格欄位，方便批次處理
        private Dictionary<string, TextBox> priceInputs = new Dictionary<string, TextBox>();

        public EditItemForm(List<string> categories, MenuItem item = null, string currentCategory = "")
        {
            this.Text = item == null ? "新增商品" : "編輯商品";
            this.Size = new Size(500, 950); // 增加高度
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // 固定視窗大小
            this.MaximizeBox = false; // 停用最大化
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft JhengHei", 10);

            // 建立 UI
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(20);
            layout.AutoScroll = true;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35)); // Label
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); // Input
            this.Controls.Add(layout);

            // 1. 基本資訊
            AddHeader(layout, "基本資訊");
            
            // 新增分類選單
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.Controls.Add(new Label { Text = "所屬分類", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleLeft }, 0, layout.RowCount);
            cboCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            if (categories != null) cboCategory.Items.AddRange(categories.ToArray());
            if (!string.IsNullOrEmpty(currentCategory) && cboCategory.Items.Contains(currentCategory))
                cboCategory.SelectedItem = currentCategory;
            else if (cboCategory.Items.Count > 0)
                cboCategory.SelectedIndex = 0;
            layout.Controls.Add(cboCategory, 1, layout.RowCount);
            layout.RowCount++;

            txtName = AddRow(layout, "商品名稱", item?.Name);
            AddImageRow(layout, "圖片路徑", item?.Image);
            
            // 口味編輯
            string flavorsStr = item?.Flavors != null ? string.Join(",", item.Flavors) : "";
            txtFlavors = AddRow(layout, "口味 (逗號分隔)", flavorsStr);
            txtFlavors.PlaceholderText = "例如: 草莓,藍莓,巧克力";

            // 2. 一般價格
            AddHeader(layout, "一般價格");
            AddPriceRow(layout, "原價 (Regular)", "PriceRegular", item?.PriceRegular);
            AddPriceRow(layout, "加蛋 (With Egg)", "PriceWithEgg", item?.PriceWithEgg);
            AddPriceRow(layout, "套餐價 (Price)", "Price", item?.Price);
            AddPriceRow(layout, "單點 (Single)", "PriceSingle", item?.PriceSingle);

            // 3. 飲品/薯條價格
            AddHeader(layout, "分量價格");
            AddPriceRow(layout, "小杯 (S)", "PriceSmall", item?.PriceSmall);
            AddPriceRow(layout, "中杯 (M)", "PriceMedium", item?.PriceMedium);
            AddPriceRow(layout, "大杯 (L)", "PriceLarge", item?.PriceLarge);

            // 4. 蛋餅/河粉
            AddHeader(layout, "餅皮價格");
            AddPriceRow(layout, "蛋餅皮", "PriceDanbing", item?.PriceDanbing);
            AddPriceRow(layout, "河粉皮", "PriceHefen", item?.PriceHefen);

            // 5. 數量價格
            AddHeader(layout, "數量價格");
            AddPriceRow(layout, "8顆", "Price8Pcs", item?.Price8Pcs);
            AddPriceRow(layout, "10顆", "Price10Pcs", item?.Price10Pcs);

            // 底部按鈕
            var pnlBtns = new FlowLayoutPanel();
            pnlBtns.Dock = DockStyle.Bottom;
            pnlBtns.FlowDirection = FlowDirection.RightToLeft;
            pnlBtns.Height = 60;
            pnlBtns.Padding = new Padding(10);

            var btnCancel = new Button { Text = "取消", Height = 40, Width = 100 };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            var btnSave = new Button { Text = "儲存", Height = 40, Width = 100, BackColor = Color.Gold, FlatStyle = FlatStyle.Flat };
            btnSave.Click += (s, e) => Save(item);

            pnlBtns.Controls.Add(btnSave);
            pnlBtns.Controls.Add(btnCancel);
            this.Controls.Add(pnlBtns);
        }

        private void AddHeader(TableLayoutPanel panel, string text)
        {
            var lbl = new Label { Text = text, Font = new Font("Microsoft JhengHei", 11, FontStyle.Bold), ForeColor = Color.DarkBlue, AutoSize = true, Margin = new Padding(0, 15, 0, 5) };
            panel.Controls.Add(lbl, 0, panel.RowCount);
            panel.SetColumnSpan(lbl, 2);
            panel.RowCount++;
        }

        private void AddImageRow(TableLayoutPanel panel, string labelText, string value)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            panel.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleLeft }, 0, panel.RowCount);
            
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            txtImage = new TextBox { Text = value, Width = 160 };
            
            var btnBrowse = new Button { Text = "...", Width = 30, Height = 23 };
            btnBrowse.Click += (s, e) => 
            {
                // 開啟獨立預覽視窗
                using (var form = new ImagePreviewForm(txtImage.Text))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        txtImage.Text = form.SelectedPath;
                    }
                }
            };
            
            flow.Controls.Add(txtImage);
            flow.Controls.Add(btnBrowse);
            panel.Controls.Add(flow, 1, panel.RowCount);
            panel.RowCount++;
        }

        private TextBox AddRow(TableLayoutPanel panel, string labelText, string value = "")
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // 強制每行高度 40
            panel.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleLeft }, 0, panel.RowCount);
            var txt = new TextBox { Text = value, Width = 200, Anchor = AnchorStyles.Left | AnchorStyles.Right }; // 讓輸入框填滿寬度
            panel.Controls.Add(txt, 1, panel.RowCount);
            panel.RowCount++;
            return txt;
        }

        private void AddPriceRow(TableLayoutPanel panel, string labelText, string key, int? value)
        {
            var txt = AddRow(panel, labelText, value?.ToString() ?? "");
            txt.PlaceholderText = "未設定"; 
            priceInputs[key] = txt;
        }

        private void Save(MenuItem existingItem)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("商品名稱不能為空！");
                return;
            }

            // 如果是編輯，沿用舊物件，如果是新增則建立新物件
            ResultItem = existingItem ?? new MenuItem();
            ResultItem.Name = txtName.Text;
            ResultItem.Image = string.IsNullOrWhiteSpace(txtImage.Text) ? null : txtImage.Text;
            
            // 儲存選取的分類
            SelectedCategoryName = cboCategory.SelectedItem?.ToString() ?? "";

            // 儲存口味
            if (!string.IsNullOrWhiteSpace(txtFlavors.Text))
            {
                // 處理全形與半形逗號，並移除空白
                var flavors = txtFlavors.Text.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(f => f.Trim())
                                             .Where(f => !string.IsNullOrEmpty(f))
                                             .ToList();
                ResultItem.Flavors = flavors.Count > 0 ? flavors : null;
            }
            else
            {
                ResultItem.Flavors = null;
            }

            ResultItem.PriceRegular = ParseInt(priceInputs["PriceRegular"].Text);
            ResultItem.PriceWithEgg = ParseInt(priceInputs["PriceWithEgg"].Text);
            ResultItem.Price = ParseInt(priceInputs["Price"].Text);
            ResultItem.PriceSingle = ParseInt(priceInputs["PriceSingle"].Text);
            ResultItem.PriceSmall = ParseInt(priceInputs["PriceSmall"].Text);
            ResultItem.PriceMedium = ParseInt(priceInputs["PriceMedium"].Text);
            ResultItem.PriceLarge = ParseInt(priceInputs["PriceLarge"].Text);
            ResultItem.PriceDanbing = ParseInt(priceInputs["PriceDanbing"].Text);
            ResultItem.PriceHefen = ParseInt(priceInputs["PriceHefen"].Text);
            ResultItem.Price8Pcs = ParseInt(priceInputs["Price8Pcs"].Text);
            ResultItem.Price10Pcs = ParseInt(priceInputs["Price10Pcs"].Text);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private int? ParseInt(string input)
        {
            if (int.TryParse(input, out int result)) return result;
            return null;
        }
    }
}