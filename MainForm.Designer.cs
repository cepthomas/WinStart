using Ephemera.NBagOfUis;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            rtbTell = new RichTextBox();
            selector = new VisualSelector();
            txtTrace = new TextBox();
            SuspendLayout();
            // 
            // rtbTell
            // 
            rtbTell.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            rtbTell.Location = new Point(409, 139);
            rtbTell.Name = "rtbTell";
            rtbTell.Size = new Size(581, 314);
            rtbTell.TabIndex = 0;
            rtbTell.Text = "";
            // 
            // selector
            // 
            selector.AllowDrop = true;
            selector.AllowExternalDrop = false;
            selector.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            selector.ImageSize = 32;
            selector.Location = new Point(3, 5);
            selector.MarkerColor = Color.Orange;
            selector.MultiSelect = false;
            selector.Name = "selector";
            selector.Size = new Size(389, 448);
            selector.Style = VisualSelector.SelectorStyle.Tile;
            selector.TabIndex = 7;
            selector.TileSize = 160;
            // 
            // txtTrace
            // 
            txtTrace.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtTrace.BorderStyle = BorderStyle.FixedSingle;
            txtTrace.Location = new Point(409, 76);
            txtTrace.Name = "txtTrace";
            txtTrace.ReadOnly = true;
            txtTrace.Size = new Size(581, 26);
            txtTrace.TabIndex = 9;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1002, 460);
            Controls.Add(txtTrace);
            Controls.Add(selector);
            Controls.Add(rtbTell);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private VisualSelector selector;
        private RichTextBox rtbTell;
        private TextBox txtTrace;
    }
}
