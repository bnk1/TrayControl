namespace TrayControl
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            HideBtn = new Button();
            ShowBtn = new Button();
            RefreshBtn = new Button();
            IconsList = new ListView();
            App = new ColumnHeader();
            AppName = new ColumnHeader();
            AppPath = new ColumnHeader();
            _refreshTimer = new System.Windows.Forms.Timer(components);
            flowLayoutPanel1 = new FlowLayoutPanel();
            OpenSettingsFile = new Button();
            BtnSaveSettings = new Button();
            statusStrip1 = new StatusStrip();
            ApplyTimer = new System.Windows.Forms.Timer(components);
            tableLayoutPanel1 = new TableLayoutPanel();
            flowLayoutPanel1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // HideBtn
            // 
            HideBtn.Location = new Point(3, 49);
            HideBtn.Name = "HideBtn";
            HideBtn.Size = new Size(131, 40);
            HideBtn.TabIndex = 2;
            HideBtn.Text = "Hide";
            HideBtn.UseVisualStyleBackColor = true;
            HideBtn.Click += HideBtn_Click;
            // 
            // ShowBtn
            // 
            ShowBtn.Location = new Point(3, 95);
            ShowBtn.Name = "ShowBtn";
            ShowBtn.Size = new Size(131, 40);
            ShowBtn.TabIndex = 4;
            ShowBtn.Text = "Show";
            ShowBtn.UseVisualStyleBackColor = true;
            ShowBtn.Click += ShowBtn_Click;
            // 
            // RefreshBtn
            // 
            RefreshBtn.Location = new Point(3, 3);
            RefreshBtn.Name = "RefreshBtn";
            RefreshBtn.Size = new Size(131, 40);
            RefreshBtn.TabIndex = 6;
            RefreshBtn.Text = "Refresh";
            RefreshBtn.UseVisualStyleBackColor = true;
            RefreshBtn.Click += RefreshBtn_Click;
            // 
            // IconsList
            // 
            IconsList.BorderStyle = BorderStyle.None;
            IconsList.CheckBoxes = true;
            IconsList.Columns.AddRange(new ColumnHeader[] { App, AppName, AppPath });
            IconsList.Dock = DockStyle.Fill;
            IconsList.FullRowSelect = true;
            IconsList.Location = new Point(146, 3);
            IconsList.Name = "IconsList";
            IconsList.Size = new Size(1594, 1047);
            IconsList.TabIndex = 7;
            IconsList.UseCompatibleStateImageBehavior = false;
            IconsList.ItemChecked += IconsList_ItemChecked;
            IconsList.SelectedIndexChanged += IconsList_SelectedIndexChanged;
            IconsList.Resize += IconsList_Resize;
            // 
            // App
            // 
            App.Text = "";
            // 
            // AppName
            // 
            AppName.Text = "Name";
            // 
            // AppPath
            // 
            AppPath.Text = "Path";
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel1.Controls.Add(RefreshBtn);
            flowLayoutPanel1.Controls.Add(HideBtn);
            flowLayoutPanel1.Controls.Add(ShowBtn);
            flowLayoutPanel1.Controls.Add(OpenSettingsFile);
            flowLayoutPanel1.Controls.Add(BtnSaveSettings);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.Location = new Point(3, 3);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(137, 1047);
            flowLayoutPanel1.TabIndex = 11;
            // 
            // OpenSettingsFile
            // 
            OpenSettingsFile.Location = new Point(3, 141);
            OpenSettingsFile.Name = "OpenSettingsFile";
            OpenSettingsFile.Size = new Size(131, 40);
            OpenSettingsFile.TabIndex = 7;
            OpenSettingsFile.Text = "Settings";
            OpenSettingsFile.UseVisualStyleBackColor = true;
            OpenSettingsFile.Click += OpenSettingsFile_Click;
            // 
            // BtnSaveSettings
            // 
            BtnSaveSettings.Location = new Point(3, 187);
            BtnSaveSettings.Name = "BtnSaveSettings";
            BtnSaveSettings.Size = new Size(131, 40);
            BtnSaveSettings.TabIndex = 8;
            BtnSaveSettings.Text = "Save";
            BtnSaveSettings.UseVisualStyleBackColor = true;
            BtnSaveSettings.Click += BtnSaveSettings_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(28, 28);
            statusStrip1.Location = new Point(0, 1053);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1743, 22);
            statusStrip1.TabIndex = 15;
            statusStrip1.Text = "statusStrip1";
            // 
            // ApplyTimer
            // 
            ApplyTimer.Interval = 700;
            ApplyTimer.Tick += ApplyTimer_Tick;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(IconsList, 1, 0);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1743, 1053);
            tableLayoutPanel1.TabIndex = 16;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(168F, 168F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1743, 1075);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(statusStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            FormClosing += Form1_FormClosing;
            flowLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button HideBtn;
        private Button ShowBtn;
        private Button RefreshBtn;
        private ListView IconsList;
        private ColumnHeader App;
        private ColumnHeader AppName;
        private ColumnHeader AppPath;
        private System.Windows.Forms.Timer _refreshTimer;
        private FlowLayoutPanel flowLayoutPanel1;
        private StatusStrip statusStrip1;
        private System.Windows.Forms.Timer ApplyTimer;
        private Button OpenSettingsFile;
        private TableLayoutPanel tableLayoutPanel1;
        private Button BtnSaveSettings;
    }
}
