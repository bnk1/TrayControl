using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TrayControl
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            ConfigureIconsList();
            RefreshIconsList();

            ShowBtn.Click += (s, e) => ActOnChecked(show: true);
            HideBtn.Click += (s, e) => ActOnChecked(show: false);
        }

        private const int IconWidth = 64;
        private const int IconHeight = 64;

        // at the top of Form1
        private List<TrayIconInfo> _icons = new();
        private ImageList _smallImages = new();

        private void ConfigureIconsList()
        {
            // Make sure IconsList is a ListView (not ListBox) named exactly "IconsList"
            IconsList.View = View.Details;
            IconsList.CheckBoxes = true;
            IconsList.FullRowSelect = true;
            IconsList.GridLines = false;
            IconsList.HideSelection = false;

            // This line is important for showing SmallImageList icons in Details view
            IconsList.UseCompatibleStateImageBehavior = false;

            IconsList.Columns.Clear();
            IconsList.Columns.Add("", 26);                 // image/checkbox pad
            IconsList.Columns.Add("Area", 110);
            IconsList.Columns.Add("ID", 80, HorizontalAlignment.Right);
            IconsList.Columns.Add("Text", 400);

            // Prepare the SmallImageList *before* adding items
            _smallImages = new ImageList { ImageSize = new Size(IconWidth, IconHeight), ColorDepth = ColorDepth.Depth32Bit };
            IconsList.SmallImageList = _smallImages;

            // (optional) owner draw OFF
            IconsList.OwnerDraw = false;
        }

        private void RefreshIconsList()
        {
            _icons = TrayInterop.ListTrayIcons(IconWidth, IconHeight);

            // Rebuild imagelist first
            _smallImages.Images.Clear();

            IconsList.BeginUpdate();
            IconsList.Items.Clear();

            for (int i = 0; i < _icons.Count; i++)
            {
                var ic = _icons[i];

                int imageIndex = -1;
                if (ic.Icon != null)
                {
                    // Add returns index; use that as ImageIndex
                    _smallImages.Images.Add(ic.Icon);
                    imageIndex = _smallImages.Images.Count - 1;
                }

                // IMPORTANT: put the image on the ListViewItem itself (first column)
                var lvi = new ListViewItem(""); // leave text empty; image appears here
                lvi.ImageIndex = imageIndex;    // <- this actually shows the icon

                // add the rest of the columns as subitems
                lvi.SubItems.Add(ic.Area == TrayArea.NotificationArea ? "Notification" : "Overflow");
                lvi.SubItems.Add(ic.IdCommand.ToString());
                lvi.SubItems.Add(ic.Text ?? "");

                lvi.Tag = i; // keep index for actions
                IconsList.Items.Add(lvi);
            }

            // Auto-size after items added
            IconsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            IconsList.EndUpdate();
        }


        private void ActOnChecked(bool show)
        {
            // if nothing is checked, fall back to selected item
            var targets = new List<int>();

            foreach (ListViewItem item in IconsList.Items)
            {
                if (item.Checked)
                    targets.Add((int)(item.Tag ?? -1));
            }

            if (targets.Count == 0 && IconsList.SelectedItems.Count > 0)
            {
                if (IconsList.SelectedItems[0].Tag != null)
                    targets.Add((int)(IconsList.SelectedItems[0].Tag ?? -1));
            }

            if (targets.Count == 0)
            {
                MessageBox.Show("Check at least one row, or select a row.");
                return;
            }

            foreach (int idx in targets)
            {
                var ic = _icons[idx];
                if (show)
                    TrayInterop.ShowIcon(ic.IdCommand, ic.Area);
                else
                    TrayInterop.HideIcon(ic.IdCommand, ic.Area);
            }

            // Keep selection & checks if you want by NOT refreshing here.
            // If you want a manual refresh button, call RefreshIconsList() there.
            // RefreshIconsList();
        }

        private void BtnList_Click(object sender, EventArgs e)
        {
            ConfigureIconsList();
            RefreshIconsList();
        }
    }
}