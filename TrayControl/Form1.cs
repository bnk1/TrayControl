#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TrayControl
{
    public partial class Form1 : Form
    {
        private bool _isLoading = false;

        private readonly ImageList _iconsIL = new ImageList();

        public Form1()
        {
            InitializeComponent();

            // Minimal ListView setup
            IconsList.View = View.Details;
            IconsList.OwnerDraw = false;
            IconsList.UseCompatibleStateImageBehavior = false;

            // One persistent ImageList
            _iconsIL.ColorDepth = ColorDepth.Depth32Bit;
            _iconsIL.ImageSize = new Size(32, 32); // change if you want bigger rows
            _iconsIL.TransparentColor = Color.Transparent;
            IconsList.SmallImageList = _iconsIL;

            // Wire buttons
            ShowBtn.Click += ShowBtn_Click;
            HideBtn.Click += HideBtn_Click;
            RefreshBtn.Click += (s, e) => LoadTrayItems();

            // Initial load
            LoadTrayItems();
        }

        private void LoadTrayItems()
        {
            _isLoading = true;
            IconsList.BeginUpdate();
            try
            {
                IconsList.Items.Clear();
                _iconsIL.Images.Clear();

                int w = _iconsIL.ImageSize.Width;
                int h = _iconsIL.ImageSize.Height;

                var items = TrayInterop.ListTrayIcons(w, h);

                foreach (var info in items)
                {
                    // APP ICON ONLY (ignore TrayIcon)
                    Image img      = BuildAppOnlyIcon(info.AppIcon, w, h);
                    int imageIndex = _iconsIL.Images.Count;

                    _iconsIL.Images.Add(img);

                    // Fix: Replace erroneous usage of 'GetFileNameWithoutExtension' on 'ColumnHeader' with correct usage on 'info.AppPath'
                    string displayName = !string.IsNullOrEmpty(info.AppPath) ? Path.GetFileNameWithoutExtension(info.AppPath) : (string.IsNullOrEmpty(info.Text) ? "(unknown)" : info.Text);

                    var lvi = new ListViewItem(displayName)
                    {
                        ImageIndex = imageIndex,
                        Tag = info
                    };

                    lvi.Checked = info.IsHidden;

                    // Fill optional columns if you added them in the Designer
                    while (lvi.SubItems.Count < IconsList.Columns.Count)
                        lvi.SubItems.Add(string.Empty);
                    if (IconsList.Columns.Count > 1)
                        lvi.SubItems[1].Text = info.Text ?? string.Empty;     // "Name"
                    if (IconsList.Columns.Count > 2)
                        lvi.SubItems[2].Text = info.AppPath ?? string.Empty;  // "Path"

                    IconsList.Items.Add(lvi);
                }
            }
            finally
            {
                IconsList.EndUpdate();
                AutoResizeCol();
                _isLoading = false;
            }
        }

        private void ShowBtn_Click(object sender, EventArgs e)
        {
            if (IconsList.SelectedItems.Count == 0)
                return;

            if (IconsList.SelectedItems[0].Tag is not TrayIconInfo info)
                return;

            if (TrayInterop.ShowIcon(info.IdCommand, info.Area))
                LoadTrayItems();
        }

        private void HideBtn_Click(object sender, EventArgs e)
        {
            if (IconsList.SelectedItems.Count == 0)
                return;
            if (IconsList.SelectedItems[0].Tag is not TrayIconInfo info)
                return;

            if (TrayInterop.HideIcon(info.IdCommand, info.Area))
                LoadTrayItems();
        }

        // --- tiny helpers ---

        private static Image BuildAppOnlyIcon(Image appIcon, int canvasW, int canvasH)
        {
            if (appIcon != null)
                return FitIntoCanvasNoUpscale(appIcon, canvasW, canvasH, Color.Transparent);

            // Final fallback so there is always something visible
            using (var sysBmp = SystemIcons.Application.ToBitmap())
            {
                var clone = new Bitmap(sysBmp);
                return FitIntoCanvasNoUpscale(clone, canvasW, canvasH, Color.Transparent);
            }
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

            var bmp = new Bitmap(canvasW, canvasH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(back);
                g.InterpolationMode = (scale < 1f)
                    ? System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    : System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(src, new Rectangle(x, y, w, h));
            }
            return bmp;
        }

        private void IconsList_Resize(object sender, EventArgs e)
        {
            AutoResizeCol();

        }

        private void AutoResizeCol()
        {
            // Keep it simple: re-apply built-in autosize
            IconsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            //int total = 0;
            //for (int i = 0; i < IconsList.Columns.Count - 1; i++)
            //    total += IconsList.Columns[i].Width;

            //int remaining = IconsList.ClientSize.Width - total;
            //if (remaining > 50 && IconsList.Columns.Count > 0)
            //    IconsList.Columns[IconsList.Columns.Count - 1].Width = remaining;
        }

        private void RefreshBtn_Click(object sender, EventArgs e)
        {
            LoadTrayItems();
        }

        private void IconsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonsForSelection();
        }

        private void UpdateButtonsForSelection()
        {
            // default state
            ShowBtn.Enabled = false;
            HideBtn.Enabled = false;

            if (IconsList.SelectedItems.Count == 0)
                return;
            if (IconsList.SelectedItems[0].Tag is not TrayIconInfo info)
                return;

            // Ask Explorer right now
            bool? hidden = TrayInterop.IsIconHidden(info.IdCommand, info.Area);

            if (hidden == true)
            {
                // currently hidden -> allow Show
                ShowBtn.Enabled = true;
            }
            else if (hidden == false)
            {
                // currently visible -> allow Hide
                HideBtn.Enabled = true;
            }
            // if null -> not found; keep both disabled (or enable both if you prefer)
        }

        private void IconsList_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (_isLoading)
                return;

            if (e.Item?.Tag is not TrayIconInfo info)
                return;

            bool wantHide = e.Item.Checked;
            bool ok       = wantHide ? TrayInterop.HideIcon(info.IdCommand, info.Area) : TrayInterop.ShowIcon(info.IdCommand, info.Area);

            if (ok)
            {
                info.IsHidden = wantHide;
            }
            else
            {
                _isLoading = true;
                try
                {
                    e.Item.Checked = !wantHide;
                }
                finally
                {
                    _isLoading = false;
                }
            }
        }
    }
}
