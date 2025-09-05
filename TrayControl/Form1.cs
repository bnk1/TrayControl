#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace TrayControl
{
    public partial class Form1 : Form
    {
        // Designer already created:
        // public ListView IconsList;
        // public Button ShowBtn, HideBtn, RefreshBtn;

        public Form1()
        {
            InitializeComponent();

            // Optional: DPI-friendly
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96f, 96f);

            // Wire events if not already set in Designer
            this.Load += Form1_Load;
            ShowBtn.Click += ShowBtn_Click;
            HideBtn.Click += HideBtn_Click;
            RefreshBtn.Click += RefreshBtn_Click;

            // Double-buffer the ListView to reduce flicker
            var pi = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi?.SetValue(IconsList, true, null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AutoSizeColumnsToContent(IconsList, fillLastToControl: true);
            SetupIconsList();
            LoadTrayItems();
        }

        private void SetupIconsList()
        {
            IconsList.View = View.Details;
            IconsList.FullRowSelect = true;
            IconsList.HideSelection = false;

            // We are NOT owner-drawing anymore
            IconsList.OwnerDraw = false;
            IconsList.DrawColumnHeader -= (s, e) => e.DrawDefault = true;   // in case previously attached
            IconsList.DrawSubItem -= IconsList_DrawSubItem;                 // in case previously attached

            if (IconsList.SmallImageList == null)
                IconsList.SmallImageList = new ImageList();

            // Pick your row height (composite will fit into this)
            // Optionally scale with DPI:
            float scale = this.DeviceDpi / 96f;
            int basePx = 32;
            IconsList.SmallImageList.ImageSize = new Size(
                (int)Math.Round(basePx * scale),
                (int)Math.Round(basePx * scale)
            );

            // Ensure columns exist (App, Name, Path). Remove any "Tray" column.
            if (IconsList.Columns.Count == 0)
            {
                IconsList.Columns.Add("App", 220);     // composite icon + display name
                IconsList.Columns.Add("Name", 280);    // tray text
                IconsList.Columns.Add("Path", 400);    // exe path
            }
            else
            {
                // If designer already has columns, ensure there is no "Tray" column
                for (int i = IconsList.Columns.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(IconsList.Columns[i].Text, "Tray", StringComparison.OrdinalIgnoreCase))
                        IconsList.Columns.RemoveAt(i);
                }
            }
        }

        private void LoadTrayItems()
        {
            // Preserve selection across refresh
            var selectedKeys = new HashSet<string>();
            foreach (ListViewItem it in IconsList.SelectedItems)
                if (it.Tag is TrayIconInfo info)
                    selectedKeys.Add(Key(info));

            IconsList.BeginUpdate();
            try
            {
                IconsList.Items.Clear();
                IconsList.SmallImageList.Images.Clear();

                int w = IconsList.SmallImageList.ImageSize.Width;
                int h = IconsList.SmallImageList.ImageSize.Height;

                var items = TrayInterop.ListTrayIcons(w, h);

                foreach (var info in items)
                {
                    // build composite image for the 1st column (left=AppIcon, right=TrayIcon)
                    Image composite = ComposeSideBySide(info.AppIcon, info.TrayIcon, w, h, gap: 2, canvasBack: Color.Transparent);

                    IconsList.SmallImageList.Images.Add(composite);
                    int imgIndex = IconsList.SmallImageList.Images.Count - 1;

                    string displayName =
                        !string.IsNullOrEmpty(info.AppPath)
                            ? System.IO.Path.GetFileNameWithoutExtension(info.AppPath)
                            : (string.IsNullOrEmpty(info.Text) ? "(unknown)" : info.Text);

                    var lvi = new ListViewItem(displayName)
                    {
                        ImageIndex = imgIndex,
                        Tag = info
                    };

                    // Ensure at least two more columns exist (Name, Path). Create empty if not.
                    while (lvi.SubItems.Count < 3)
                        lvi.SubItems.Add(string.Empty);

                    lvi.SubItems[1].Text = info.Text ?? string.Empty;     // Name
                    lvi.SubItems[2].Text = info.AppPath ?? string.Empty;  // Path

                    IconsList.Items.Add(lvi);
                }

                // Restore selection
                foreach (ListViewItem it in IconsList.Items)
                    if (it.Tag is TrayIconInfo info && selectedKeys.Contains(Key(info)))
                        it.Selected = true;
            }
            finally
            {
                IconsList.EndUpdate();
            }

            AutoSizeColumnsToContent(IconsList, fillLastToControl: true);
        }

        private static string Key(TrayIconInfo info) =>
            ((int)info.Area).ToString() + ":" + info.IdCommand.ToString();

        private void RefreshBtn_Click(object sender, EventArgs e) => LoadTrayItems();

        private void ShowBtn_Click(object sender, EventArgs e)
        {
            var info = GetSelected();
            if (info == null)
                return;
            if (TrayInterop.ShowIcon(info.IdCommand, info.Area))
                LoadTrayItems();
        }

        private void HideBtn_Click(object sender, EventArgs e)
        {
            var info = GetSelected();
            if (info == null)
                return;
            if (TrayInterop.HideIcon(info.IdCommand, info.Area))
                LoadTrayItems();
        }

        private TrayIconInfo GetSelected()
        {
            if (IconsList.SelectedItems.Count == 0)
                return null;
            return IconsList.SelectedItems[0].Tag as TrayIconInfo;
        }

        // ===== Helpers to draw the composite icon for the 1st column =====

        private static Image ComposeSideBySide(Image left, Image right, int canvasW, int canvasH, int gap, Color canvasBack)
        {
            // Handle absence of one/both images
            if (left == null && right == null)
                return CreateBlankCanvas(canvasW, canvasH, canvasBack);
            if (right == null)
                return FitIntoCanvasNoUpscale(left, canvasW, canvasH, canvasBack);
            if (left == null)
                return FitIntoCanvasNoUpscale(right, canvasW, canvasH, canvasBack);

            // Split canvas into two boxes with a gap
            int halfW = (canvasW - gap) / 2;
            if (halfW < 1)
                halfW = canvasW / 2;

            var bmp = new Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(canvasBack);

                var leftBox  = new Rectangle(0, 0, halfW, canvasH);
                var rightBox = new Rectangle(halfW + gap, 0, canvasW - (halfW + gap), canvasH);

                DrawFitNoUpscale(g, left, leftBox);
                DrawFitNoUpscale(g, right, rightBox);
            }
            return bmp;
        }

        private static void DrawFitNoUpscale(Graphics g, Image src, Rectangle box)
        {
            if (src == null || box.Width <= 0 || box.Height <= 0)
                return;

            float scale = Math.Min(1f, Math.Min((float)box.Width / src.Width, (float)box.Height / src.Height));
            int w = Math.Max(1, (int)Math.Round(src.Width * scale));
            int h = Math.Max(1, (int)Math.Round(src.Height * scale));
            int x = box.X + (box.Width - w) / 2;
            int y = box.Y + (box.Height - h) / 2;

            g.InterpolationMode = (scale < 1f)
                ? System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                : System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.DrawImage(src, new Rectangle(x, y, w, h));
        }

        private static Bitmap CreateBlankCanvas(int w, int h, Color back)
        {
            var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
                g.Clear(back);
            return bmp;
        }

        private static Bitmap FitIntoCanvasNoUpscale(Image src, int canvasW, int canvasH, Color back)
        {
            int srcW = Math.Max(1, src.Width);
            int srcH = Math.Max(1, src.Height);

            float scale = Math.Min(1f, Math.Min((float)canvasW / srcW, (float)canvasH / srcH));
            int w = Math.Max(1, (int)Math.Round(srcW * scale));
            int h = Math.Max(1, (int)Math.Round(srcH * scale));
            int x = (canvasW - w) / 2;
            int y = (canvasH - h) / 2;

            var outBmp = new Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(outBmp))
            {
                g.Clear(back);
                g.InterpolationMode = (scale < 1f)
                    ? System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    : System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(src, new Rectangle(x, y, w, h));
            }
            return outBmp;
        }

        // (kept only so unsubscribing above compiles cleanly)
        private void IconsList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) { }

        // Put this inside Form1 class
        private void AutoSizeColumnsToContent(ListView lv, bool fillLastToControl = true, int padding = 12)
        {
            if (lv.Columns.Count == 0)
                return;

            int[] preferred = new int[lv.Columns.Count];

            // 1) Start with header widths
            for (int i = 0; i < lv.Columns.Count; i++)
            {
                int headerW = TextRenderer.MeasureText(lv.Columns[i].Text, lv.Font).Width + padding;
                preferred[i] = Math.Max(40, headerW); // minimum so headers don't collapse
            }

            // 2) Include content widths (account for the image in column 0)
            int imgW  = lv.SmallImageList != null ? lv.SmallImageList.ImageSize.Width : 0;
            int imgGap = imgW > 0 ? 6 : 0;

            foreach (ListViewItem item in lv.Items)
            {
                for (int col = 0; col < lv.Columns.Count; col++)
                {
                    string text = col == 0 ? item.Text
                                   : (col < item.SubItems.Count ? item.SubItems[col].Text : string.Empty);

                    int textW = TextRenderer.MeasureText(text ?? string.Empty, lv.Font).Width + padding;
                    if (col == 0)
                        textW += imgW + imgGap; // leave room for the image in the first column

                    if (textW > preferred[col])
                        preferred[col] = textW;
                }
            }

            // 3) Apply widths
            int total = 0;
            for (int i = 0; i < lv.Columns.Count; i++)
            {
                lv.Columns[i].Width = preferred[i];
                total += preferred[i];
            }

            // 4) Optionally stretch the last column to fill the control width
            if (fillLastToControl)
            {
                int target = lv.ClientSize.Width;
                if (target > total && lv.Columns.Count > 0)
                {
                    lv.Columns[lv.Columns.Count - 1].Width += (target - total);
                }
            }
        }

        private void BtnList_Click(object sender, EventArgs e)
        {

        }
    }
}
