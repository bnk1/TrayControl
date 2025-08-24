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
            BtnList = new Button();
            TxtQuery = new TextBox();
            HideBtn = new Button();
            IconsList = new ListBox();
            ShowBtn = new Button();
            SuspendLayout();
            // 
            // BtnList
            // 
            BtnList.Location = new Point(35, 37);
            BtnList.Name = "BtnList";
            BtnList.Size = new Size(131, 40);
            BtnList.TabIndex = 0;
            BtnList.Text = "List";
            BtnList.UseVisualStyleBackColor = true;
            // 
            // TxtQuery
            // 
            TxtQuery.Location = new Point(298, 37);
            TxtQuery.Name = "TxtQuery";
            TxtQuery.Size = new Size(586, 35);
            TxtQuery.TabIndex = 1;
            // 
            // HideBtn
            // 
            HideBtn.Location = new Point(35, 97);
            HideBtn.Name = "HideBtn";
            HideBtn.Size = new Size(131, 40);
            HideBtn.TabIndex = 2;
            HideBtn.Text = "Hide";
            HideBtn.UseVisualStyleBackColor = true;
            HideBtn.Click += HideBtn_Click;
            // 
            // IconsList
            // 
            IconsList.FormattingEnabled = true;
            IconsList.ItemHeight = 30;
            IconsList.Location = new Point(298, 87);
            IconsList.Name = "IconsList";
            IconsList.Size = new Size(586, 484);
            IconsList.TabIndex = 3;
            // 
            // ShowBtn
            // 
            ShowBtn.Location = new Point(35, 160);
            ShowBtn.Name = "ShowBtn";
            ShowBtn.Size = new Size(131, 40);
            ShowBtn.TabIndex = 4;
            ShowBtn.Text = "Show";
            ShowBtn.UseVisualStyleBackColor = true;
            ShowBtn.Click += ShowBtn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1095, 724);
            Controls.Add(ShowBtn);
            Controls.Add(IconsList);
            Controls.Add(HideBtn);
            Controls.Add(TxtQuery);
            Controls.Add(BtnList);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button BtnList;
        private TextBox TxtQuery;
        private Button HideBtn;
        private ListBox IconsList;
        private Button ShowBtn;
    }
}
