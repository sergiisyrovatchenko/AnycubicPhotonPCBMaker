#nullable disable

namespace AnycubicPhotonPCB.Maker
{
    partial class MainForm
    {
        // Required designer variable
        private System.ComponentModel.IContainer components = null;

        // Clean up any resources being used.
        // disposing: true if managed resources should be disposed; otherwise, false
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // Required method for Designer support - do not modify
        // the contents of this method with the code editor
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menuStrip = new MenuStrip();
            openMenuItem = new ToolStripMenuItem();
            openFolderMenuItem = new ToolStripMenuItem();
            openFilesMenuItem = new ToolStripMenuItem();
            openZipMenuItem = new ToolStripMenuItem();
            optionsMenuItem = new ToolStripMenuItem();
            drillsMarksMenuItem = new ToolStripMenuItem();
            drillsSubtractMenuItem = new ToolStripMenuItem();
            outlineMenuItem = new ToolStripMenuItem();
            optionsSeparator = new ToolStripSeparator();
            photoresistMenuItem = new ToolStripMenuItem();
            positiveResistMenuItem = new ToolStripMenuItem();
            negativeResistMenuItem = new ToolStripMenuItem();
            printerMenuItem = new ToolStripMenuItem();
            reloadTimer = new System.Windows.Forms.Timer(components);
            tabs = new TabControl();
            boardTab = new TabPage();
            rightPanel = new Panel();
            boardLayout = new TableLayoutPanel();
            topCaption = new Label();
            topPicture = new PictureBox();
            bottomCaption = new Label();
            bottomPicture = new PictureBox();
            leftPanel = new TableLayoutPanel();
            fileList = new ListView();
            fileColumn = new ColumnHeader();
            layerColumn = new ColumnHeader();
            exportTab = new TabPage();
            exportView = new ExportView();
            statusStrip = new StatusStrip();
            boardStatusLabel = new ToolStripStatusLabel();
            printerStatusLabel = new ToolStripStatusLabel();
            screenStatusLabel = new ToolStripStatusLabel();
            previewStatusLabel = new ToolStripStatusLabel();
            progressStatusBar = new ToolStripProgressBar();
            menuStrip.SuspendLayout();
            tabs.SuspendLayout();
            boardTab.SuspendLayout();
            rightPanel.SuspendLayout();
            boardLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)topPicture).BeginInit();
            ((System.ComponentModel.ISupportInitialize)bottomPicture).BeginInit();
            leftPanel.SuspendLayout();
            exportTab.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.BackColor = SystemColors.Window;
            menuStrip.Items.AddRange(new ToolStripItem[] { openMenuItem, optionsMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1264, 24);
            menuStrip.TabIndex = 3;
            // 
            // openMenuItem
            // 
            openMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFolderMenuItem, openFilesMenuItem, openZipMenuItem });
            openMenuItem.Name = "openMenuItem";
            openMenuItem.Size = new Size(48, 20);
            openMenuItem.Text = "&Open";
            // 
            // openFolderMenuItem
            // 
            openFolderMenuItem.Name = "openFolderMenuItem";
            openFolderMenuItem.Size = new Size(204, 22);
            openFolderMenuItem.Text = "Folder with Gerber files…";
            openFolderMenuItem.Click += OpenFolderClicked;
            // 
            // openFilesMenuItem
            // 
            openFilesMenuItem.Name = "openFilesMenuItem";
            openFilesMenuItem.Size = new Size(204, 22);
            openFilesMenuItem.Text = "Gerber files…";
            openFilesMenuItem.Click += OpenFilesClicked;
            // 
            // openZipMenuItem
            // 
            openZipMenuItem.Name = "openZipMenuItem";
            openZipMenuItem.Size = new Size(204, 22);
            openZipMenuItem.Text = "ZIP archive…";
            openZipMenuItem.Click += OpenZipClicked;
            // 
            // optionsMenuItem
            // 
            optionsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { drillsMarksMenuItem, drillsSubtractMenuItem, outlineMenuItem, optionsSeparator, photoresistMenuItem, printerMenuItem });
            optionsMenuItem.Name = "optionsMenuItem";
            optionsMenuItem.Size = new Size(61, 20);
            optionsMenuItem.Text = "O&ptions";
            // 
            // drillsMarksMenuItem
            // 
            drillsMarksMenuItem.CheckOnClick = true;
            drillsMarksMenuItem.Name = "drillsMarksMenuItem";
            drillsMarksMenuItem.Size = new Size(180, 22);
            drillsMarksMenuItem.Text = "Drill &holes";
            drillsMarksMenuItem.ToolTipText = "Add a thin copper ring inside the holes that have no copper around them (mounting holes), which Subtract holes cannot show";
            drillsMarksMenuItem.CheckedChanged += DrillsMenuItemCheckedChanged;
            // 
            // drillsSubtractMenuItem
            // 
            drillsSubtractMenuItem.CheckOnClick = true;
            drillsSubtractMenuItem.Name = "drillsSubtractMenuItem";
            drillsSubtractMenuItem.Size = new Size(180, 22);
            drillsSubtractMenuItem.Text = "&Subtract holes";
            drillsSubtractMenuItem.ToolTipText = "Cut the drill holes out of the copper and soldermask layers, as they will be on the board (previews and export)";
            drillsSubtractMenuItem.CheckedChanged += DrillsMenuItemCheckedChanged;
            // 
            // outlineMenuItem
            // 
            outlineMenuItem.CheckOnClick = true;
            outlineMenuItem.Name = "outlineMenuItem";
            outlineMenuItem.Size = new Size(180, 22);
            outlineMenuItem.Text = "Board &outline";
            outlineMenuItem.ToolTipText = "Draw the board outline into the copper layers (previews and export)";
            outlineMenuItem.CheckedChanged += OutlineMenuItemCheckedChanged;
            // 
            // optionsSeparator
            // 
            optionsSeparator.Name = "optionsSeparator";
            optionsSeparator.Size = new Size(177, 6);
            // 
            // photoresistMenuItem
            // 
            photoresistMenuItem.DropDownItems.AddRange(new ToolStripItem[] { positiveResistMenuItem, negativeResistMenuItem });
            photoresistMenuItem.Name = "photoresistMenuItem";
            photoresistMenuItem.Size = new Size(180, 22);
            photoresistMenuItem.Text = "Photo&resist";
            photoresistMenuItem.ToolTipText = "Photoresist on the board; decides whether the export exposes the background or the features";
            // 
            // positiveResistMenuItem
            // 
            positiveResistMenuItem.Name = "positiveResistMenuItem";
            positiveResistMenuItem.Size = new Size(121, 22);
            positiveResistMenuItem.Text = "&Positive";
            positiveResistMenuItem.ToolTipText = "Exposed areas dissolve in the developer";
            positiveResistMenuItem.Click += PhotoresistMenuItemClicked;
            // 
            // negativeResistMenuItem
            // 
            negativeResistMenuItem.Name = "negativeResistMenuItem";
            negativeResistMenuItem.Size = new Size(121, 22);
            negativeResistMenuItem.Text = "&Negative";
            negativeResistMenuItem.ToolTipText = "Exposed areas stay after developing (dry film)";
            negativeResistMenuItem.Click += PhotoresistMenuItemClicked;
            // 
            // printerMenuItem
            // 
            printerMenuItem.Name = "printerMenuItem";
            printerMenuItem.Size = new Size(180, 22);
            printerMenuItem.Text = "&Printer";
            printerMenuItem.ToolTipText = "Printer the export is made for";
            // 
            // reloadTimer
            // 
            reloadTimer.Interval = 300;
            reloadTimer.Tick += ReloadTimerTick;
            // 
            // tabs
            // 
            tabs.Controls.Add(boardTab);
            tabs.Controls.Add(exportTab);
            tabs.Dock = DockStyle.Fill;
            tabs.Location = new Point(0, 24);
            tabs.Margin = new Padding(4, 3, 4, 3);
            tabs.Name = "tabs";
            tabs.Padding = new Point(12, 4);
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(1264, 733);
            tabs.TabIndex = 4;
            // 
            // boardTab
            // 
            boardTab.BackColor = Color.White;
            boardTab.Controls.Add(rightPanel);
            boardTab.Controls.Add(leftPanel);
            boardTab.Location = new Point(4, 26);
            boardTab.Margin = new Padding(0);
            boardTab.Name = "boardTab";
            boardTab.Size = new Size(1256, 703);
            boardTab.TabIndex = 0;
            boardTab.Text = "PCB";
            // 
            // rightPanel
            // 
            rightPanel.BackColor = Color.Black;
            rightPanel.Controls.Add(boardLayout);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(340, 0);
            rightPanel.Margin = new Padding(0);
            rightPanel.Name = "rightPanel";
            rightPanel.Size = new Size(916, 703);
            rightPanel.TabIndex = 1;
            // 
            // boardLayout
            // 
            boardLayout.ColumnCount = 1;
            boardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            boardLayout.Controls.Add(topCaption, 0, 0);
            boardLayout.Controls.Add(topPicture, 0, 1);
            boardLayout.Controls.Add(bottomCaption, 0, 2);
            boardLayout.Controls.Add(bottomPicture, 0, 3);
            boardLayout.Dock = DockStyle.Fill;
            boardLayout.Location = new Point(0, 0);
            boardLayout.Margin = new Padding(0);
            boardLayout.Name = "boardLayout";
            boardLayout.RowCount = 4;
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            boardLayout.Size = new Size(916, 703);
            boardLayout.TabIndex = 0;
            // 
            // topCaption
            // 
            topCaption.AutoSize = true;
            topCaption.BackColor = Color.White;
            topCaption.Dock = DockStyle.Fill;
            topCaption.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            topCaption.ForeColor = Color.Black;
            topCaption.Location = new Point(0, 0);
            topCaption.Margin = new Padding(0);
            topCaption.Name = "topCaption";
            topCaption.Size = new Size(916, 24);
            topCaption.TabIndex = 0;
            topCaption.Text = "Top";
            topCaption.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // topPicture
            // 
            topPicture.BackColor = Color.White;
            topPicture.Dock = DockStyle.Fill;
            topPicture.Location = new Point(0, 24);
            topPicture.Margin = new Padding(0);
            topPicture.Name = "topPicture";
            topPicture.Size = new Size(916, 327);
            topPicture.SizeMode = PictureBoxSizeMode.Zoom;
            topPicture.TabIndex = 1;
            topPicture.TabStop = false;
            // 
            // bottomCaption
            // 
            bottomCaption.AutoSize = true;
            bottomCaption.BackColor = Color.White;
            bottomCaption.Dock = DockStyle.Fill;
            bottomCaption.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            bottomCaption.ForeColor = Color.Black;
            bottomCaption.Location = new Point(0, 351);
            bottomCaption.Margin = new Padding(0);
            bottomCaption.Name = "bottomCaption";
            bottomCaption.Size = new Size(916, 24);
            bottomCaption.TabIndex = 0;
            bottomCaption.Text = "Bottom";
            bottomCaption.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // bottomPicture
            // 
            bottomPicture.BackColor = Color.White;
            bottomPicture.Dock = DockStyle.Fill;
            bottomPicture.Location = new Point(0, 375);
            bottomPicture.Margin = new Padding(0);
            bottomPicture.Name = "bottomPicture";
            bottomPicture.Size = new Size(916, 328);
            bottomPicture.SizeMode = PictureBoxSizeMode.Zoom;
            bottomPicture.TabIndex = 1;
            bottomPicture.TabStop = false;
            // 
            // leftPanel
            // 
            leftPanel.ColumnCount = 1;
            leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftPanel.Controls.Add(fileList, 0, 1);
            leftPanel.Dock = DockStyle.Left;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Margin = new Padding(0);
            leftPanel.Name = "leftPanel";
            leftPanel.RowCount = 2;
            leftPanel.RowStyles.Add(new RowStyle());
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftPanel.Size = new Size(340, 703);
            leftPanel.TabIndex = 1;
            // 
            // fileList
            // 
            fileList.BorderStyle = BorderStyle.None;
            fileList.CheckBoxes = true;
            fileList.Columns.AddRange(new ColumnHeader[] { fileColumn, layerColumn });
            fileList.Dock = DockStyle.Fill;
            fileList.ForeColor = Color.Gainsboro;
            fileList.FullRowSelect = true;
            fileList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            fileList.Location = new Point(0, 0);
            fileList.Margin = new Padding(0);
            fileList.Name = "fileList";
            fileList.Size = new Size(340, 703);
            fileList.TabIndex = 2;
            fileList.UseCompatibleStateImageBehavior = false;
            fileList.View = View.Details;
            fileList.ItemChecked += FileListItemChecked;
            // 
            // fileColumn
            // 
            fileColumn.Text = "File";
            fileColumn.Width = 190;
            // 
            // layerColumn
            // 
            layerColumn.Text = "Layer";
            layerColumn.Width = 130;
            // 
            // exportTab
            // 
            exportTab.Controls.Add(exportView);
            exportTab.Location = new Point(4, 26);
            exportTab.Margin = new Padding(0);
            exportTab.Name = "exportTab";
            exportTab.Size = new Size(1256, 703);
            exportTab.TabIndex = 1;
            exportTab.Text = "Export";
            exportTab.UseVisualStyleBackColor = true;
            // 
            // exportView
            // 
            exportView.Dock = DockStyle.Fill;
            exportView.Location = new Point(0, 0);
            exportView.Margin = new Padding(0);
            exportView.Name = "exportView";
            exportView.Size = new Size(1256, 703);
            exportView.TabIndex = 0;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { boardStatusLabel, printerStatusLabel, screenStatusLabel, previewStatusLabel, progressStatusBar });
            statusStrip.Location = new Point(0, 757);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1264, 24);
            statusStrip.TabIndex = 5;
            // 
            // boardStatusLabel
            // 
            boardStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            boardStatusLabel.Name = "boardStatusLabel";
            boardStatusLabel.Size = new Size(61, 19);
            boardStatusLabel.Text = "No board";
            // 
            // printerStatusLabel
            // 
            printerStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            printerStatusLabel.Name = "printerStatusLabel";
            printerStatusLabel.Size = new Size(46, 19);
            printerStatusLabel.Text = "Printer";
            // 
            // screenStatusLabel
            // 
            screenStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            screenStatusLabel.Name = "screenStatusLabel";
            screenStatusLabel.Size = new Size(46, 19);
            screenStatusLabel.Text = "Screen";
            // 
            // previewStatusLabel
            // 
            previewStatusLabel.ForeColor = SystemColors.ControlText;
            previewStatusLabel.Name = "previewStatusLabel";
            previewStatusLabel.Size = new Size(1096, 19);
            previewStatusLabel.Spring = true;
            previewStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // progressStatusBar
            // 
            progressStatusBar.Name = "progressStatusBar";
            progressStatusBar.Size = new Size(200, 18);
            progressStatusBar.Visible = false;
            // 
            // MainForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 781);
            Controls.Add(tabs);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(897, 594);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Anycubic Photon PCB Maker";
            DragDrop += MainFormDragDrop;
            DragEnter += MainFormDragEnter;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            tabs.ResumeLayout(false);
            boardTab.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            boardLayout.ResumeLayout(false);
            boardLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)topPicture).EndInit();
            ((System.ComponentModel.ISupportInitialize)bottomPicture).EndInit();
            leftPanel.ResumeLayout(false);
            exportTab.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip;
        private ToolStripMenuItem openMenuItem;
        private ToolStripMenuItem openFilesMenuItem;
        private ToolStripMenuItem openFolderMenuItem;
        private ToolStripMenuItem openZipMenuItem;
        private ToolStripMenuItem optionsMenuItem;
        private ToolStripMenuItem drillsSubtractMenuItem;
        private ToolStripMenuItem drillsMarksMenuItem;
        private ToolStripMenuItem outlineMenuItem;
        private ToolStripSeparator optionsSeparator;
        private ToolStripMenuItem photoresistMenuItem;
        private ToolStripMenuItem positiveResistMenuItem;
        private ToolStripMenuItem negativeResistMenuItem;
        private ToolStripMenuItem printerMenuItem;
        private System.Windows.Forms.Timer reloadTimer;
        private TabControl tabs;
        private TabPage boardTab;
        private Panel rightPanel;
        private TableLayoutPanel boardLayout;
        private Label topCaption;
        private PictureBox topPicture;
        private Label bottomCaption;
        private PictureBox bottomPicture;
        private TableLayoutPanel leftPanel;
        private ListView fileList;
        private ColumnHeader fileColumn;
        private ColumnHeader layerColumn;
        private TabPage exportTab;
        private ExportView exportView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel boardStatusLabel;
        private ToolStripStatusLabel printerStatusLabel;
        private ToolStripStatusLabel screenStatusLabel;
        private ToolStripStatusLabel previewStatusLabel;
        private ToolStripProgressBar progressStatusBar;
    }
}
