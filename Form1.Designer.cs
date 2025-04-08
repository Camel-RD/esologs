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
            tsiViewSelector = new ToolStripDropDownButton();
            tsiFights = new ToolStripMenuItem();
            tsiPlayers = new ToolStripMenuItem();
            tcPages = new TabControlWithoutHeader();
            tpFights = new TabPage();
            tpPlayers = new TabPage();
            tbPlayerData = new RichTextBox();
            panel1.SuspendLayout();
            toolStrip1.SuspendLayout();
            tcPages.SuspendLayout();
            tpFights.SuspendLayout();
            tpPlayers.SuspendLayout();
            SuspendLayout();
            // 
            // tbOut
            // 
            tbOut.BorderStyle = BorderStyle.None;
            tbOut.Dock = DockStyle.Fill;
            tbOut.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbOut.Location = new Point(0, 0);
            tbOut.Margin = new Padding(2);
            tbOut.Name = "tbOut";
            tbOut.Size = new Size(800, 286);
            tbOut.TabIndex = 0;
            tbOut.Text = "";
            tbOut.WordWrap = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(label1);
            panel1.Controls.Add(tbFileName);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 32);
            panel1.Margin = new Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new Size(808, 36);
            panel1.TabIndex = 1;
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
            tbFileName.Size = new Size(720, 29);
            tbFileName.TabIndex = 0;
            tbFileName.Leave += tbFileName_Leave;
            // 
            // toolStrip1
            // 
            toolStrip1.Font = new Font("Segoe UI", 12F);
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsiRead, tsiSelectFile, tsiClearFile, tsiViewSelector });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new Padding(11, 2, 0, 2);
            toolStrip1.Size = new Size(808, 32);
            toolStrip1.TabIndex = 2;
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
            // tsiViewSelector
            // 
            tsiViewSelector.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsiViewSelector.DropDownItems.AddRange(new ToolStripItem[] { tsiFights, tsiPlayers });
            tsiViewSelector.Image = (Image)resources.GetObject("tsiViewSelector.Image");
            tsiViewSelector.ImageTransparentColor = Color.Magenta;
            tsiViewSelector.Margin = new Padding(10, 1, 0, 2);
            tsiViewSelector.Name = "tsiViewSelector";
            tsiViewSelector.Size = new Size(65, 25);
            tsiViewSelector.Text = "Fights";
            // 
            // tsiFights
            // 
            tsiFights.Name = "tsiFights";
            tsiFights.Size = new Size(130, 26);
            tsiFights.Text = "Fights";
            tsiFights.Click += tsiFights_Click;
            // 
            // tsiPlayers
            // 
            tsiPlayers.Name = "tsiPlayers";
            tsiPlayers.Size = new Size(130, 26);
            tsiPlayers.Text = "Players";
            tsiPlayers.Click += tsiPlayers_Click;
            // 
            // tcPages
            // 
            tcPages.Controls.Add(tpFights);
            tcPages.Controls.Add(tpPlayers);
            tcPages.Dock = DockStyle.Fill;
            tcPages.Location = new Point(0, 68);
            tcPages.Name = "tcPages";
            tcPages.SelectedIndex = 0;
            tcPages.Size = new Size(808, 320);
            tcPages.TabIndex = 0;
            // 
            // tpFights
            // 
            tpFights.Controls.Add(tbOut);
            tpFights.Location = new Point(4, 30);
            tpFights.Margin = new Padding(0);
            tpFights.Name = "tpFights";
            tpFights.Size = new Size(800, 286);
            tpFights.TabIndex = 0;
            tpFights.Text = "Fights";
            tpFights.UseVisualStyleBackColor = true;
            // 
            // tpPlayers
            // 
            tpPlayers.Controls.Add(tbPlayerData);
            tpPlayers.Location = new Point(4, 28);
            tpPlayers.Margin = new Padding(0);
            tpPlayers.Name = "tpPlayers";
            tpPlayers.Size = new Size(773, 288);
            tpPlayers.TabIndex = 1;
            tpPlayers.Text = "Players";
            tpPlayers.UseVisualStyleBackColor = true;
            // 
            // tbPlayerData
            // 
            tbPlayerData.BorderStyle = BorderStyle.None;
            tbPlayerData.Dock = DockStyle.Fill;
            tbPlayerData.Font = new Font("Consolas", 12F);
            tbPlayerData.Location = new Point(0, 0);
            tbPlayerData.Name = "tbPlayerData";
            tbPlayerData.Size = new Size(773, 288);
            tbPlayerData.TabIndex = 0;
            tbPlayerData.Text = "";
            tbPlayerData.WordWrap = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(808, 388);
            Controls.Add(tcPages);
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
            tcPages.ResumeLayout(false);
            tpFights.ResumeLayout(false);
            tpPlayers.ResumeLayout(false);
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
        private TabControlWithoutHeader tcPages;
        private TabPage tpFights;
        private TabPage tpPlayers;
        private ToolStripDropDownButton tsiViewSelector;
        private ToolStripMenuItem tsiFights;
        private ToolStripMenuItem tsiPlayers;
        private RichTextBox tbPlayerData;
    }
}
