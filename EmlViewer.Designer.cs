namespace EmlFileViewer {
    partial class EmlViewer {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (loadingFile != null) { // LOADING FILE AND NOT FINISHED
                loadingFile.Stop(); // STOP LOADING!
                if (!emlLoaderWorker.IsBusy) {
                    emlLoaderWorker.CancelAsync();
                }
                loadingFile = null;
            }
            if (currentFile != null) {
                currentFile.Dispose();
                currentFile = null;
            }

            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EmlViewer));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.partsTree = new System.Windows.Forms.TreeView();
            this.messageLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.headerLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.subjectHeaderLabel = new System.Windows.Forms.Label();
            this.subjectHeaderBox = new System.Windows.Forms.TextBox();
            this.fromHeaderLabel = new System.Windows.Forms.Label();
            this.fromHeaderBox = new System.Windows.Forms.TextBox();
            this.dateHeaderLabel = new System.Windows.Forms.Label();
            this.dateHeaderBox = new System.Windows.Forms.TextBox();
            this.toHeaderLabel = new System.Windows.Forms.Label();
            this.toHeaderBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.savePartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker3 = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.messageLayoutPanel.SuspendLayout();
            this.headerLayoutPanel.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.partsTree);
            this.splitContainer1.Panel1.Cursor = System.Windows.Forms.Cursors.Default;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.messageLayoutPanel);
            this.splitContainer1.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer1.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel2_Paint);
            this.splitContainer1.Size = new System.Drawing.Size(844, 520);
            this.splitContainer1.SplitterDistance = 283;
            this.splitContainer1.TabIndex = 1;
            // 
            // partsTree
            // 
            this.partsTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.partsTree.Location = new System.Drawing.Point(0, 0);
            this.partsTree.Name = "partsTree";
            this.partsTree.ShowNodeToolTips = true;
            this.partsTree.Size = new System.Drawing.Size(283, 520);
            this.partsTree.TabIndex = 0;
            this.partsTree.NodeMouseHover += new System.Windows.Forms.TreeNodeMouseHoverEventHandler(this.treeView1_NodeMouseHover);
            this.partsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            this.partsTree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            // 
            // messageLayoutPanel
            // 
            this.messageLayoutPanel.AutoSize = true;
            this.messageLayoutPanel.ColumnCount = 1;
            this.messageLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.messageLayoutPanel.Controls.Add(this.headerLayoutPanel, 0, 0);
            this.messageLayoutPanel.Controls.Add(this.panel1, 0, 1);
            this.messageLayoutPanel.Controls.Add(this.webBrowser1, 0, 1);
            this.messageLayoutPanel.Controls.Add(this.textBox1, 0, 1);
            this.messageLayoutPanel.Controls.Add(this.progressBar1, 0, 2);
            this.messageLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messageLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.messageLayoutPanel.Name = "messageLayoutPanel";
            this.messageLayoutPanel.RowCount = 3;
            this.messageLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.messageLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.messageLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.messageLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.messageLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.messageLayoutPanel.Size = new System.Drawing.Size(557, 520);
            this.messageLayoutPanel.TabIndex = 0;
            this.messageLayoutPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.messageLayoutPanel_Paint);
            // 
            // headerLayoutPanel
            // 
            this.headerLayoutPanel.AutoSize = true;
            this.headerLayoutPanel.ColumnCount = 2;
            this.headerLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.headerLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerLayoutPanel.Controls.Add(this.subjectHeaderLabel, 0, 0);
            this.headerLayoutPanel.Controls.Add(this.subjectHeaderBox, 1, 0);
            this.headerLayoutPanel.Controls.Add(this.fromHeaderLabel, 0, 1);
            this.headerLayoutPanel.Controls.Add(this.fromHeaderBox, 1, 1);
            this.headerLayoutPanel.Controls.Add(this.dateHeaderLabel, 0, 2);
            this.headerLayoutPanel.Controls.Add(this.dateHeaderBox, 1, 2);
            this.headerLayoutPanel.Controls.Add(this.toHeaderLabel, 0, 3);
            this.headerLayoutPanel.Controls.Add(this.toHeaderBox, 1, 3);
            this.headerLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.headerLayoutPanel.Name = "headerLayoutPanel";
            this.headerLayoutPanel.RowCount = 4;
            this.headerLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.headerLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.headerLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.headerLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.headerLayoutPanel.Size = new System.Drawing.Size(551, 104);
            this.headerLayoutPanel.TabIndex = 0;
            this.headerLayoutPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.headerLayoutPanel_Paint);
            // 
            // subjectHeaderLabel
            // 
            this.subjectHeaderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.subjectHeaderLabel.AutoSize = true;
            this.subjectHeaderLabel.Location = new System.Drawing.Point(3, 5);
            this.subjectHeaderLabel.Name = "subjectHeaderLabel";
            this.subjectHeaderLabel.Size = new System.Drawing.Size(49, 15);
            this.subjectHeaderLabel.TabIndex = 0;
            this.subjectHeaderLabel.Text = "Subject:";
            this.subjectHeaderLabel.Visible = false;
            // 
            // subjectHeaderBox
            // 
            this.subjectHeaderBox.BackColor = System.Drawing.SystemColors.Window;
            this.subjectHeaderBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.subjectHeaderBox.Location = new System.Drawing.Point(63, 3);
            this.subjectHeaderBox.MaxLength = 50000000;
            this.subjectHeaderBox.Name = "subjectHeaderBox";
            this.subjectHeaderBox.ReadOnly = true;
            this.subjectHeaderBox.Size = new System.Drawing.Size(485, 23);
            this.subjectHeaderBox.TabIndex = 0;
            this.subjectHeaderBox.Visible = false;
            this.subjectHeaderBox.WordWrap = false;
            // 
            // fromHeaderLabel
            // 
            this.fromHeaderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.fromHeaderLabel.AutoSize = true;
            this.fromHeaderLabel.Location = new System.Drawing.Point(3, 30);
            this.fromHeaderLabel.Name = "fromHeaderLabel";
            this.fromHeaderLabel.Size = new System.Drawing.Size(38, 15);
            this.fromHeaderLabel.TabIndex = 1;
            this.fromHeaderLabel.Text = "From:";
            this.fromHeaderLabel.Visible = false;
            // 
            // fromHeaderBox
            // 
            this.fromHeaderBox.BackColor = System.Drawing.SystemColors.Window;
            this.fromHeaderBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fromHeaderBox.Location = new System.Drawing.Point(63, 28);
            this.fromHeaderBox.MaxLength = 50000000;
            this.fromHeaderBox.Name = "fromHeaderBox";
            this.fromHeaderBox.ReadOnly = true;
            this.fromHeaderBox.Size = new System.Drawing.Size(485, 23);
            this.fromHeaderBox.TabIndex = 0;
            this.fromHeaderBox.Visible = false;
            this.fromHeaderBox.WordWrap = false;
            // 
            // dateHeaderLabel
            // 
            this.dateHeaderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dateHeaderLabel.AutoSize = true;
            this.dateHeaderLabel.Location = new System.Drawing.Point(3, 55);
            this.dateHeaderLabel.Name = "dateHeaderLabel";
            this.dateHeaderLabel.Size = new System.Drawing.Size(34, 15);
            this.dateHeaderLabel.TabIndex = 2;
            this.dateHeaderLabel.Text = "Date:";
            this.dateHeaderLabel.Visible = false;
            // 
            // dateHeaderBox
            // 
            this.dateHeaderBox.BackColor = System.Drawing.SystemColors.Window;
            this.dateHeaderBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dateHeaderBox.Location = new System.Drawing.Point(63, 53);
            this.dateHeaderBox.MaxLength = 50000000;
            this.dateHeaderBox.Name = "dateHeaderBox";
            this.dateHeaderBox.ReadOnly = true;
            this.dateHeaderBox.Size = new System.Drawing.Size(485, 23);
            this.dateHeaderBox.TabIndex = 0;
            this.dateHeaderBox.Visible = false;
            this.dateHeaderBox.WordWrap = false;
            // 
            // toHeaderLabel
            // 
            this.toHeaderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.toHeaderLabel.AutoSize = true;
            this.toHeaderLabel.Location = new System.Drawing.Point(3, 82);
            this.toHeaderLabel.Name = "toHeaderLabel";
            this.toHeaderLabel.Size = new System.Drawing.Size(22, 15);
            this.toHeaderLabel.TabIndex = 3;
            this.toHeaderLabel.Text = "To:";
            this.toHeaderLabel.Visible = false;
            // 
            // toHeaderBox
            // 
            this.toHeaderBox.BackColor = System.Drawing.SystemColors.Window;
            this.toHeaderBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toHeaderBox.Location = new System.Drawing.Point(63, 78);
            this.toHeaderBox.MaxLength = 50000000;
            this.toHeaderBox.Name = "toHeaderBox";
            this.toHeaderBox.ReadOnly = true;
            this.toHeaderBox.Size = new System.Drawing.Size(485, 23);
            this.toHeaderBox.TabIndex = 0;
            this.toHeaderBox.Visible = false;
            this.toHeaderBox.WordWrap = false;
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.AutoSize = true;
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.BackColor = System.Drawing.SystemColors.Window;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 483);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(551, 14);
            this.panel1.TabIndex = 1;
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(3, 463);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(551, 14);
            this.webBrowser1.TabIndex = 2;
            this.webBrowser1.Visible = false;
            this.webBrowser1.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser1_DocumentCompleted);
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBox1.Location = new System.Drawing.Point(3, 113);
            this.textBox1.MaxLength = 50000000;
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(551, 344);
            this.textBox1.TabIndex = 1;
            this.textBox1.Visible = false;
            this.textBox1.WordWrap = false;
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar1.Location = new System.Drawing.Point(3, 503);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(551, 14);
            this.progressBar1.Step = 1;
            this.progressBar1.TabIndex = 0;
            this.progressBar1.Visible = false;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.savePartToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.ToolTipText = "\"Open EML File\"";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.EmlViewer_OpenClicked);
            // 
            // savePartToolStripMenuItem
            // 
            this.savePartToolStripMenuItem.Name = "savePartToolStripMenuItem";
            this.savePartToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.savePartToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.savePartToolStripMenuItem.Text = "Save";
            this.savePartToolStripMenuItem.ToolTipText = "\"Save EML Part\"";
            this.savePartToolStripMenuItem.Click += new System.EventHandler(this.EmlViewer_SavePartClicked);
            //
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.ToolTipText = "\"Close current EML file\"";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.EmlViewer_CloseClicked);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.EmlViewer_ExitClicked);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(844, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // EmlViewer
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(844, 544);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "EmlViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Eml Viewer";
            this.Load += new System.EventHandler(this.EmlViewer_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.EmlViewer_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.EmlViewer_DragEnter);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.messageLayoutPanel.ResumeLayout(false);
            this.messageLayoutPanel.PerformLayout();
            this.headerLayoutPanel.ResumeLayout(false);
            this.headerLayoutPanel.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SplitContainer splitContainer1;
        private TreeView partsTree;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem savePartToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private OpenFileDialog openFileDialog1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.ComponentModel.BackgroundWorker backgroundWorker2;
        private System.ComponentModel.BackgroundWorker backgroundWorker3;
        private volatile ProgressBar progressBar1;

        private Label fromHeaderLabel;
        private Label subjectHeaderLabel;
        private Label toHeaderLabel;
        private Label dateHeaderLabel;

        private TextBox fromHeaderBox;
        private TextBox subjectHeaderBox;
        private TextBox toHeaderBox;
        private TextBox dateHeaderBox;

        private TableLayoutPanel headerLayoutPanel;
        private TableLayoutPanel messageLayoutPanel;
        private Panel panel1;
        private TextBox textBox1;
        private WebBrowser webBrowser1;


    }
}