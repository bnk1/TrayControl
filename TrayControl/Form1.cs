#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Windows.Foundation.Metadata;

namespace TrayControl
{
    public partial class Form1 : Form
    {
        private bool _isLoading = false;

        private readonly ImageList _iconsIL = new ImageList();

        public Form1()
        {
            InitializeComponent();

            // use the VersionAttribute helper to build the window title
            this.Text = VersionAttribute.GetAppTitleWithVersion();


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

                    AutoResizeCol();
                }
            }
            finally
            {
                IconsList.EndUpdate();
                _isLoading = false;
                AutoSizeListAndForm();
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

        // Change return type of BuildAppOnlyIcon from Image to Bitmap for improved performance (CA1859)
        private static Bitmap BuildAppOnlyIcon(Image appIcon, int canvasW, int canvasH)
        {
            if (appIcon != null)
                return FitIntoCanvasNoUpscale(appIcon, canvasW, canvasH, Color.Transparent);

            // Final fallback so there is always something visible
            using var sysBmp = SystemIcons.Application.ToBitmap();
            var clone = new Bitmap(sysBmp);
            return FitIntoCanvasNoUpscale(clone, canvasW, canvasH, Color.Transparent);
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

        // Fix CS8632: Remove nullable annotation from 'object?' since '#nullable disable' is set at the top
        private void IconsList_ItemChecked(object sender, ItemCheckedEventArgs e)
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

        private void AutoSizeListAndForm()
        {
            if (IconsList.View != View.Details)
                return;

            // 1) Let ListView compute column widths by content
            IconsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            // 2) Sum column widths
            int totalCols = 0;
            foreach (ColumnHeader ch in IconsList.Columns)
                totalCols += ch.Width;

            // Leave room for the image in column 0 (ListView's autosize ignores image width)
            int imagePad = (IconsList.SmallImageList != null && IconsList.Columns.Count > 0) ? IconsList.SmallImageList.ImageSize.Width + 6 : 0;

            // Account for vertical scrollbar if likely needed
            int rowH = (IconsList.SmallImageList != null) ? IconsList.SmallImageList.ImageSize.Height : (IconsList.Font.Height + 6);
            int visibleRows = Math.Max(1, IconsList.ClientSize.Height / Math.Max(1, rowH));
            bool needsVScroll = IconsList.Items.Count > visibleRows;
            int vScroll = needsVScroll ? SystemInformation.VerticalScrollBarWidth : 0;

            // Decorations = borders, padding, etc between Width and ClientSize
            int decorations = IconsList.Width - IconsList.ClientSize.Width;

            int desiredListWidth = totalCols + imagePad + vScroll + decorations;

            // 3) If we're inside a SplitContainer (common: buttons left / list right), grow the form so Panel2 fits
            SplitContainer sc = null;
            for (Control p = IconsList.Parent; p != null; p = p.Parent)
                if (p is SplitContainer s)
                { sc = s; break; }

            if (sc != null)
            {
                int delta = desiredListWidth - sc.Panel2.ClientSize.Width;
                if (delta > 0)
                    this.ClientSize = new Size(this.ClientSize.Width + delta, this.ClientSize.Height);
            }
            else
            {
                // No split container: grow form so the list fits from its Left edge
                int desiredClientWidth = IconsList.Left + desiredListWidth + IconsList.Margin.Right;
                int delta = desiredClientWidth - this.ClientSize.Width;
                if (delta > 0)
                    this.ClientSize = new Size(this.ClientSize.Width + delta, this.ClientSize.Height);
            }
        }

    }
}
