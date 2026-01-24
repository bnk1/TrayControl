using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CompactAppWinForms
{
    public class Form1 : Form
    {
        readonly DoubleBufferedListView appsList;
        readonly Panel detailsPanel;
        readonly Label lblName;
        readonly Label lblVersion;
        readonly Label lblPublisher;
        readonly Button btnOpen;
        readonly Button btnUninstall;
        readonly List<AppInfo> appData = new();

        public Form1()
        {
            // Window settings - fixed, centered, compact
            Text = "Apps";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(640, 420);
            Font = new Font("Segoe UI", 9F);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            Controls.Add(table);

            // Left: ListView (compact, details view)
            appsList = new DoubleBufferedListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                MultiSelect = false,
                HideSelection = false,
            };
            appsList.Columns.Add("Name", 260);
            appsList.Columns.Add("Version", 90);
            appsList.Columns.Add("Publisher", 180);
            appsList.SelectedIndexChanged += AppsList_SelectedIndexChanged;
            table.Controls.Add(appsList, 0, 0);

            // Right: Details panel
            detailsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            table.Controls.Add(detailsPanel, 1, 0);

            var heading = new Label { Text = "Details", Font = new Font(Font, FontStyle.Bold), Dock = DockStyle.Top, Height = 22 };
            detailsPanel.Controls.Add(heading);

            var detailsBox = new Panel { Dock = DockStyle.Top, Height = 160, BackColor = Color.WhiteSmoke, Padding = new Padding(8) };
            detailsPanel.Controls.Add(detailsBox);

            lblName = new Label { Text = "", Font = new Font(Font.FontFamily, 10, FontStyle.Bold), AutoSize = false, Dock = DockStyle.Top, Height = 24 };
            lblVersion = new Label { Text = "", ForeColor = Color.Gray, AutoSize = false, Dock = DockStyle.Top, Height = 20 };
            lblPublisher = new Label { Text = "", ForeColor = Color.Gray, AutoSize = false, Dock = DockStyle.Top, Height = 20 };

            detailsBox.Controls.Add(lblPublisher);
            detailsBox.Controls.Add(lblVersion);
            detailsBox.Controls.Add(lblName);

            var buttonsPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 36, Padding = new Padding(0, 8, 0, 0) };
            detailsPanel.Controls.Add(buttonsPanel);

            btnOpen = new Button { Text = "Open", Width = 80, Enabled = false };
            btnUninstall = new Button { Text = "Uninstall", Width = 90, Enabled = false };
            buttonsPanel.Controls.Add(btnOpen);
            buttonsPanel.Controls.Add(btnUninstall);

            // Sample data - replace with your real discovery code
            appData.Add(new AppInfo("Visual Studio", "17.9", "Microsoft", @"C:\Program Files\VS"));
            appData.Add(new AppInfo("Notepad++", "8.4", "Notepad++ Team", @"C:\Program Files\Notepad++"));
            appData.Add(new AppInfo("Sample App", "1.2.3", "Contoso", @"C:\Program Files\Sample"));

            PopulateList();
        }

        void PopulateList()
        {
            appsList.BeginUpdate();
            appsList.Items.Clear();
            foreach (var a in appData)
            {
                var li = new ListViewItem(a.Name);
                li.SubItems.Add(a.Version);
                li.SubItems.Add(a.Publisher);
                li.Tag = a;
                appsList.Items.Add(li);
            }
            appsList.EndUpdate();
        }

        void AppsList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (appsList.SelectedItems.Count == 0)
            {
                lblName.Text = lblVersion.Text = lblPublisher.Text = "";
                btnOpen.Enabled = btnUninstall.Enabled = false;
                return;
            }

            var ai = (AppInfo)appsList.SelectedItems[0].Tag!;
            lblName.Text = ai.Name;
            lblVersion.Text = $"Version: {ai.Version}";
            lblPublisher.Text = $"Publisher: {ai.Publisher}";
            btnOpen.Enabled = btnUninstall.Enabled = true;
        }
    }

    // Small model
    public class AppInfo
    {
        public AppInfo(string name, string version, string publisher, string path)
        {
            Name = name; Version = version; Publisher = publisher; InstallPath = path;
        }
        public string Name { get; }
        public string Version { get; }
        public string Publisher { get; }
        public string InstallPath { get; }
    }

    // Helper: enable double buffering on ListView to reduce flicker and make dense lists smoother
    public class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}
