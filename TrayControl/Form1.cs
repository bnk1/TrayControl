#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
using IOPath = System.IO.Path;
using Windows.Foundation.Metadata;

namespace TrayControl
{
    public partial class Form1 : Form
    {
        private static readonly string path1 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // --- Persistence of hidden state ---
        private readonly string _settingsPath = IOPath.Combine(path1, "TrayControl", "settings.json");

        private sealed class Settings
        {
            public Dictionary<string, bool> HiddenByPath { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        private Settings _settings          = new Settings();
        private bool _isLoading             = false;
        private readonly ImageList _iconsIL = new ImageList();

        public Form1()
        {
            InitializeComponent();

            LoadSettings();

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

                int width = _iconsIL.ImageSize.Width;
                int height = _iconsIL.ImageSize.Height;

                var items = TrayInterop.ListTrayIcons(width, height);

                foreach (var info in items)
                {
                    // APP ICON ONLY (ignore TrayIcon)
                    Image img      = BuildAppOnlyIcon(info.AppIcon, width, height);
                    int imageIndex = _iconsIL.Images.Count;

                    _iconsIL.Images.Add(img);

                    string displayName = !string.IsNullOrEmpty(info.AppPath) ?
                        Path.GetFileNameWithoutExtension(info.AppPath) :
                        (string.IsNullOrEmpty(info.Text) ? "(unknown)" : info.Text);

                    var lvi = new ListViewItem(displayName)
                    {
                        ImageIndex = imageIndex,
                        Tag = info
                    };

                    bool savedHide = (!string.IsNullOrEmpty(info.AppPath) && _settings.HiddenByPath.TryGetValue(info.AppPath, out var h)) ? h : info.IsHidden;
                    lvi.Checked = savedHide;

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

        private void IconsList_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_isLoading)
                return;
            if (e.Item?.Tag is not TrayIconInfo info)
                return;

            bool wantHide = e.Item.Checked; // CHECKED = HIDE
            bool ok = wantHide ? TrayInterop.HideIcon(info.IdCommand, info.Area) : TrayInterop.ShowIcon(info.IdCommand, info.Area);

            if (ok)
            {
                info.IsHidden = wantHide;
                if (!string.IsNullOrEmpty(info.AppPath))
                    _settings.HiddenByPath[info.AppPath] = wantHide;
                SaveSettings();
            }
            else
            {
                _isLoading = true;
                try
                { e.Item.Checked = !wantHide; }
                finally { _isLoading = false; }
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


        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var txt = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<Settings>(txt) ?? new Settings();
                }
                else
                {
                    _settings = new Settings();
                    SaveSettings();
                }
            }
            catch
            {
                _settings = new Settings();
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(IOPath.GetDirectoryName(_settingsPath)!);
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, opts));
            }
            catch { }
        }



        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int RegisterWindowMessage(string lpString);

        private static readonly int WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_TASKBARCREATED)
                ScheduleApplyRules();
        }

        private void ScheduleApplyRules()
        {
            ApplyTimer.Stop();
            ApplyTimer.Start();
        }

        private void ApplyVisibilityRules()
        {
            // Query current tray items and re-apply saved hidden/visible states by AppPath
            int w = _iconsIL.ImageSize.Width, h = _iconsIL.ImageSize.Height;

            var items = TrayInterop.ListTrayIcons(w, h);

            foreach (var info in items)
            {
                if (string.IsNullOrEmpty(info.AppPath))
                    continue;
                if (!_settings.HiddenByPath.TryGetValue(info.AppPath, out bool hide))
                    continue;

                bool ok = hide ? TrayInterop.HideIcon(info.IdCommand, info.Area) : TrayInterop.ShowIcon(info.IdCommand, info.Area);
                if (ok)
                    info.IsHidden = hide;
            }

            LoadTrayItems();    // Refresh UI
        }

        private void ApplyTimer_Tick(object sender, EventArgs e)
        {
            ApplyTimer.Stop();
            ApplyVisibilityRules();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try 
            { 
                SaveSettings(); 
            }
            catch 
            { 
            }
            // DO NOT call base.OnFormClosing(e) from an event handler
        }
    }
}

