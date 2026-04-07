using System.Drawing;
using System.Windows.Forms;


namespace WinStart
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code
        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            rtbTell = new RichTextBox();
            cmbView = new ComboBox();
            selector1 = new Selector();
            Btn_Go = new Button();
            SuspendLayout();
            // 
            // rtbTell
            // 
            rtbTell.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbTell.Location = new Point(416, 139);
            rtbTell.Name = "rtbTell";
            rtbTell.Size = new Size(574, 314);
            rtbTell.TabIndex = 0;
            rtbTell.Text = "";
            // 
            // cmbView
            // 
            cmbView.FormattingEnabled = true;
            cmbView.Items.AddRange(new object[] { "SmallIcon", "LargeIcon", "List", "Tile", "Details" });
            cmbView.Location = new Point(447, 14);
            cmbView.Name = "cmbView";
            cmbView.Size = new Size(139, 27);
            cmbView.TabIndex = 5;
            // 
            // selector1
            // 
            selector1.AllowDrop = true;
            selector1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            selector1.GridLines = true;
            selector1.LargeSize = 48;
            selector1.Location = new Point(3, 5);
            selector1.Name = "selector1";
            selector1.Size = new Size(398, 448);
            selector1.SmallSize = 16;
            selector1.TabIndex = 7;
            selector1.UseCompatibleStateImageBehavior = false;
            // 
            // Btn_Go
            // 
            Btn_Go.Location = new Point(614, 12);
            Btn_Go.Name = "Btn_Go";
            Btn_Go.Size = new Size(86, 26);
            Btn_Go.TabIndex = 8;
            Btn_Go.Text = "Go!!";
            Btn_Go.UseVisualStyleBackColor = true;
            Btn_Go.Click += Btn_Go_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1002, 460);
            Controls.Add(Btn_Go);
            Controls.Add(selector1);
            Controls.Add(cmbView);
            Controls.Add(rtbTell);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }
        #endregion

        private RichTextBox rtbTell;
        private ComboBox cmbView;
        private Selector selector1;
        private Button Btn_Go;
    }
}
