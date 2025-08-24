using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TrayControl
{
    public partial class Form1 : Form
    {
        private List<TrayIconInfo> _icons = new List<TrayIconInfo>();

        public Form1()
        {
            InitializeComponent();

            RefreshIconsList();
        }

        private void RefreshIconsList()
        {
            _icons = TrayInterop.ListTrayIcons();
            IconsList.Items.Clear();

            foreach (var icon in _icons)
            {
                IconsList.Items.Add(
                    $"{icon.Area}  id={icon.IdCommand}  text=\"{icon.Text}\""
                );
            }
        }

        private void ShowSelectedIcon()
        {
            int idx = IconsList.SelectedIndex;
            if (idx < 0 || idx >= _icons.Count)
            {
                MessageBox.Show("Select an icon first.");
                return;
            }

            var icon = _icons[idx];
            TrayInterop.ShowIcon(icon.IdCommand, icon.Area);
            //RefreshIconsList();
        }

        private void HideSelectedIcon()
        {
            int idx = IconsList.SelectedIndex;
            if (idx < 0 || idx >= _icons.Count)
            {
                MessageBox.Show("Select an icon first.");
                return;
            }

            var icon = _icons[idx];
            TrayInterop.HideIcon(icon.IdCommand, icon.Area);
            //RefreshIconsList();
        }

        private void HideBtn_Click(object sender, EventArgs e)
        {
            HideSelectedIcon();
        }

        private void ShowBtn_Click(object sender, EventArgs e)
        {
            ShowSelectedIcon();
        }
    }
}
