using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BreakfastApp
{
    public class ImagePreviewForm : Form
    {
        public string SelectedPath { get; private set; }
        private PictureBox picPreview;
        private Label lblPath;
        private Label lblLoading; // 新增遮罩標籤

        public ImagePreviewForm(string initialPath)
        {
            this.Text = "圖片預覽與選擇";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Image
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Path label
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Buttons
            this.Controls.Add(layout);

            // 使用 Panel 包裝 PictureBox 方便覆蓋遮罩
            var pnlImg = new Panel { Dock = DockStyle.Fill };
            picPreview = new PictureBox 
            { 
                Dock = DockStyle.Fill, 
                SizeMode = PictureBoxSizeMode.Zoom, 
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };
            lblLoading = new Label
            {
                Text = "圖片處理中...",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(200, Color.White), // 半透明白
                Font = new Font("Microsoft JhengHei", 12, FontStyle.Bold),
                Visible = false
            };
            pnlImg.Controls.Add(lblLoading);
            pnlImg.Controls.Add(picPreview);
            lblLoading.BringToFront();

            layout.Controls.Add(pnlImg, 0, 0);

            lblPath = new Label 
            { 
                Text = "目前路徑: (未選擇)", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            layout.Controls.Add(lblPath, 0, 1);

            var pnlBtns = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Fill, 
                FlowDirection = FlowDirection.RightToLeft 
            };
            
            var btnConfirm = new Button { Text = "確認使用", AutoSize = true, BackColor = Color.Gold, FlatStyle = FlatStyle.Flat };
            btnConfirm.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };
            
            var btnCancel = new Button { Text = "取消", AutoSize = true };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            var btnBrowse = new Button { Text = "瀏覽檔案...", AutoSize = true };
            btnBrowse.Click += (s, e) => BrowseImage();

            pnlBtns.Controls.Add(btnConfirm);
            pnlBtns.Controls.Add(btnCancel);
            pnlBtns.Controls.Add(btnBrowse);
            layout.Controls.Add(pnlBtns, 0, 2);

            LoadImage(initialPath);
        }

        private void BrowseImage()
        {
            using (var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadImage(ofd.FileName);
                }
            }
        }

        private void LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            // 確保遮罩在任何處理前就顯示
            lblLoading.Visible = true;
            lblLoading.BringToFront();
            Application.DoEvents(); // 強制立即繪製遮罩

            Cursor.Current = Cursors.WaitCursor;
            this.SuspendLayout();
            
            try
            {
                string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                if (File.Exists(fullPath))
                {
                    // Release old image if exists
                    if (picPreview.Image != null) picPreview.Image.Dispose();

                    // Load and resize image safely
                    using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        using (var tempImg = Image.FromStream(fs))
                        {
                            // Resize for preview (max width 600)
                            int newWidth = 600;
                            int newHeight = (int)((double)tempImg.Height / tempImg.Width * newWidth);
                            
                            var thumb = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (var g = Graphics.FromImage(thumb))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.DrawImage(tempImg, 0, 0, newWidth, newHeight);
                            }
                            
                            picPreview.Image = thumb;
                        }
                    }
                    
                    // Calculate relative path
                    string relative = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, fullPath);
                    SelectedPath = relative.StartsWith("..") ? fullPath : relative;
                    lblPath.Text = $"目前路徑: {SelectedPath}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"無法載入圖片: {ex.Message}");
            }
            finally
            {
                lblLoading.Visible = false; // 隱藏遮罩
                this.ResumeLayout();
                Cursor.Current = Cursors.Default;
            }
        }
    }
}