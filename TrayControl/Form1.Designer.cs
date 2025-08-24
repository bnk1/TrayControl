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
            HideBtn = new Button();
            ShowBtn = new Button();
            IconsList = new ListView();
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
            BtnList.Click += BtnList_Click;
            // 
            // HideBtn
            // 
            HideBtn.Location = new Point(35, 97);
            HideBtn.Name = "HideBtn";
            HideBtn.Size = new Size(131, 40);
            HideBtn.TabIndex = 2;
            HideBtn.Text = "Hide";
            HideBtn.UseVisualStyleBackColor = true;
            // 
            // ShowBtn
            // 
            ShowBtn.Location = new Point(35, 160);
            ShowBtn.Name = "ShowBtn";
            ShowBtn.Size = new Size(131, 40);
            ShowBtn.TabIndex = 4;
            ShowBtn.Text = "Show";
            ShowBtn.UseVisualStyleBackColor = true;
            // 
            // IconsList
            // 
            IconsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            IconsList.Location = new Point(224, 37);
            IconsList.Name = "IconsList";
            IconsList.Size = new Size(1464, 939);
            IconsList.TabIndex = 5;
            IconsList.UseCompatibleStateImageBehavior = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1729, 1011);
            Controls.Add(IconsList);
            Controls.Add(ShowBtn);
            Controls.Add(HideBtn);
            Controls.Add(BtnList);
            Name = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button BtnList;
        private Button HideBtn;
        private Button ShowBtn;
        private ListView IconsList;
    }
}
