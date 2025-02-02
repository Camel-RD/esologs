using System.Drawing;
using System.Windows.Forms;

namespace ESOLogs
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            tbOut = new RichTextBox();
            panel1 = new Panel();
            label1 = new Label();
            tbFileName = new TextBox();
            toolStrip1 = new ToolStrip();
            tsiRead = new ToolStripButton();
            tsiSelectFile = new ToolStripButton();
            tsiClearFile = new ToolStripButton();
            panel1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tbOut
            // 
            tbOut.BorderStyle = BorderStyle.None;
            tbOut.Dock = DockStyle.Fill;
            tbOut.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbOut.Location = new Point(0, 68);
            tbOut.Margin = new Padding(2);
            tbOut.Name = "tbOut";
            tbOut.Size = new Size(655, 320);
            tbOut.TabIndex = 1;
            tbOut.Text = "";
            // 
            // panel1
            // 
            panel1.Controls.Add(label1);
            panel1.Controls.Add(tbFileName);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 32);
            panel1.Margin = new Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new Size(655, 36);
            panel1.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 8);
            label1.Name = "label1";
            label1.Size = new Size(64, 21);
            label1.TabIndex = 1;
            label1.Text = "Log file:";
            // 
            // tbFileName
            // 
            tbFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbFileName.Location = new Point(82, 4);
            tbFileName.Margin = new Padding(2);
            tbFileName.Name = "tbFileName";
            tbFileName.Size = new Size(567, 29);
            tbFileName.TabIndex = 0;
            tbFileName.Leave += tbFileName_Leave;
            // 
            // toolStrip1
            // 
            toolStrip1.Font = new Font("Segoe UI", 12F);
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsiRead, tsiSelectFile, tsiClearFile });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new Padding(11, 2, 0, 2);
            toolStrip1.Size = new Size(655, 32);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsiRead
            // 
            tsiRead.Image = (Image)resources.GetObject("tsiRead.Image");
            tsiRead.ImageTransparentColor = Color.Magenta;
            tsiRead.Name = "tsiRead";
            tsiRead.Size = new Size(91, 25);
            tsiRead.Text = "Read log";
            tsiRead.Click += tsiRead_Click;
            // 
            // tsiSelectFile
            // 
            tsiSelectFile.Image = (Image)resources.GetObject("tsiSelectFile.Image");
            tsiSelectFile.ImageTransparentColor = Color.Magenta;
            tsiSelectFile.Name = "tsiSelectFile";
            tsiSelectFile.Size = new Size(122, 25);
            tsiSelectFile.Text = "Select log file";
            tsiSelectFile.Click += tsiSelectFile_Click;
            // 
            // tsiClearFile
            // 
            tsiClearFile.Image = (Image)resources.GetObject("tsiClearFile.Image");
            tsiClearFile.ImageTransparentColor = Color.Magenta;
            tsiClearFile.Name = "tsiClearFile";
            tsiClearFile.Size = new Size(117, 25);
            tsiClearFile.Text = "Clear log file";
            tsiClearFile.Click += tsiClearFile_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(655, 388);
            Controls.Add(tbOut);
            Controls.Add(panel1);
            Controls.Add(toolStrip1);
            Font = new Font("Segoe UI", 12F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            Name = "Form1";
            Text = "ESO Encounter Log";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private RichTextBox tbOut;
        private Panel panel1;
        private TextBox tbFileName;
        private ToolStrip toolStrip1;
        private ToolStripButton tsiRead;
        private ToolStripButton tsiSelectFile;
        private ToolStripButton tsiClearFile;
        private Label label1;
    }
}
