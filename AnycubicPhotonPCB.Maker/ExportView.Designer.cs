#nullable disable

namespace AnycubicPhotonPCB.Maker
{
    partial class ExportView
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

        #region Component Designer generated code

        // Required method for Designer support - do not modify
        // the contents of this method with the code editor
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mainLayout = new TableLayoutPanel();
            optionsGroup = new GroupBox();
            optionsPanel = new FlowLayoutPanel();
            layersList = new LayerExportList();
            layersFileColumn = new ColumnHeader();
            layersLayerColumn = new ColumnHeader();
            layersFlipHorizontalColumn = new ColumnHeader();
            layersFlipVerticalColumn = new ColumnHeader();
            exposurePanel = new FlowLayoutPanel();
            exposureLabel = new Label();
            exposureNumeric = new NumericUpDown();
            exposureUnitLabel = new Label();
            anchorPanel = new FlowLayoutPanel();
            anchorLabel = new Label();
            anchorGrid = new TableLayoutPanel();
            topLeftRadio = new RadioButton();
            topRightRadio = new RadioButton();
            centerRadio = new RadioButton();
            bottomLeftRadio = new RadioButton();
            bottomRightRadio = new RadioButton();
            printerFrontLabel = new Label();
            offsetPanel = new FlowLayoutPanel();
            offsetLabel = new Label();
            offsetXLabel = new Label();
            offsetXNumeric = new NumericUpDown();
            offsetYLabel = new Label();
            offsetYNumeric = new NumericUpDown();
            offsetUnitLabel = new Label();
            rotationPanel = new FlowLayoutPanel();
            rotationLabel = new Label();
            rotateNoneRadio = new RadioButton();
            rotateLeftRadio = new RadioButton();
            rotateRightRadio = new RadioButton();
            copiesPanel = new FlowLayoutPanel();
            copiesLabel = new Label();
            copyMatrix = new CopyMatrix();
            copySpacingLabel = new Label();
            copySpacingNumeric = new NumericUpDown();
            copySpacingUnitLabel = new Label();
            saveButton = new Button();
            previewGroup = new GroupBox();
            previewLayout = new TableLayoutPanel();
            previewPicture = new PictureBox();
            toolTip = new ToolTip(components);
            mainLayout.SuspendLayout();
            optionsGroup.SuspendLayout();
            optionsPanel.SuspendLayout();
            exposurePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)exposureNumeric).BeginInit();
            anchorPanel.SuspendLayout();
            anchorGrid.SuspendLayout();
            offsetPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)offsetXNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)offsetYNumeric).BeginInit();
            rotationPanel.SuspendLayout();
            copiesPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)copySpacingNumeric).BeginInit();
            previewGroup.SuspendLayout();
            previewLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)previewPicture).BeginInit();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(optionsGroup, 0, 0);
            mainLayout.Controls.Add(previewGroup, 1, 0);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(1184, 780);
            mainLayout.TabIndex = 0;
            // 
            // optionsGroup
            // 
            optionsGroup.Controls.Add(optionsPanel);
            optionsGroup.Dock = DockStyle.Fill;
            optionsGroup.Location = new Point(3, 3);
            optionsGroup.Name = "optionsGroup";
            optionsGroup.Padding = new Padding(4);
            optionsGroup.Size = new Size(434, 774);
            optionsGroup.TabIndex = 0;
            optionsGroup.TabStop = false;
            optionsGroup.Text = "Export options";
            // 
            // optionsPanel
            // 
            optionsPanel.AutoScroll = true;
            optionsPanel.Controls.Add(layersList);
            optionsPanel.Controls.Add(exposurePanel);
            optionsPanel.Controls.Add(anchorPanel);
            optionsPanel.Controls.Add(offsetPanel);
            optionsPanel.Controls.Add(rotationPanel);
            optionsPanel.Controls.Add(copiesPanel);
            optionsPanel.Controls.Add(saveButton);
            optionsPanel.Dock = DockStyle.Fill;
            optionsPanel.FlowDirection = FlowDirection.TopDown;
            optionsPanel.Location = new Point(4, 20);
            optionsPanel.Name = "optionsPanel";
            optionsPanel.Padding = new Padding(4);
            optionsPanel.Size = new Size(426, 750);
            optionsPanel.TabIndex = 0;
            optionsPanel.WrapContents = false;
            // 
            // layersList
            // 
            layersList.BorderStyle = BorderStyle.FixedSingle;
            layersList.CheckBoxes = true;
            layersList.Columns.AddRange(new ColumnHeader[] { layersFileColumn, layersLayerColumn, layersFlipHorizontalColumn, layersFlipVerticalColumn });
            layersList.ForeColor = Color.Gainsboro;
            layersList.FullRowSelect = true;
            layersList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            layersList.Location = new Point(7, 7);
            layersList.MultiSelect = false;
            layersList.Name = "layersList";
            layersList.Size = new Size(408, 101);
            layersList.TabIndex = 1;
            layersList.UseCompatibleStateImageBehavior = false;
            layersList.View = View.Details;
            layersList.SelectedLayerChanged += LayersListSelectedLayerChanged;
            layersList.CheckedLayersChanged += LayersListCheckedLayersChanged;
            layersList.FlipsChanged += OptionsChanged;
            // 
            // layersFileColumn
            // 
            layersFileColumn.Text = "File";
            layersFileColumn.Width = 140;
            // 
            // layersLayerColumn
            // 
            layersLayerColumn.Text = "Layer";
            layersLayerColumn.Width = 100;
            // 
            // layersFlipHorizontalColumn
            // 
            layersFlipHorizontalColumn.Text = "Flip horizontal";
            layersFlipHorizontalColumn.TextAlign = HorizontalAlignment.Center;
            layersFlipHorizontalColumn.Width = 90;
            // 
            // layersFlipVerticalColumn
            // 
            layersFlipVerticalColumn.Text = "Flip vertical";
            layersFlipVerticalColumn.TextAlign = HorizontalAlignment.Center;
            layersFlipVerticalColumn.Width = 75;
            // 
            // exposurePanel
            // 
            exposurePanel.AutoSize = true;
            exposurePanel.Controls.Add(exposureLabel);
            exposurePanel.Controls.Add(exposureNumeric);
            exposurePanel.Controls.Add(exposureUnitLabel);
            exposurePanel.Location = new Point(7, 114);
            exposurePanel.Name = "exposurePanel";
            exposurePanel.Size = new Size(175, 29);
            exposurePanel.TabIndex = 3;
            exposurePanel.WrapContents = false;
            // 
            // exposureLabel
            // 
            exposureLabel.Anchor = AnchorStyles.Left;
            exposureLabel.AutoSize = true;
            exposureLabel.Location = new Point(3, 7);
            exposureLabel.Name = "exposureLabel";
            exposureLabel.Size = new Size(82, 15);
            exposureLabel.TabIndex = 2;
            exposureLabel.Text = "Exposure time";
            // 
            // exposureNumeric
            // 
            exposureNumeric.Anchor = AnchorStyles.Left;
            exposureNumeric.DecimalPlaces = 1;
            exposureNumeric.Location = new Point(91, 3);
            exposureNumeric.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            exposureNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            exposureNumeric.Name = "exposureNumeric";
            exposureNumeric.Size = new Size(63, 23);
            exposureNumeric.TabIndex = 0;
            exposureNumeric.Value = new decimal(new int[] { 10, 0, 0, 0 });
            exposureNumeric.ValueChanged += ExposureNumericValueChanged;
            // 
            // exposureUnitLabel
            // 
            exposureUnitLabel.Anchor = AnchorStyles.Left;
            exposureUnitLabel.AutoSize = true;
            exposureUnitLabel.Location = new Point(160, 7);
            exposureUnitLabel.Name = "exposureUnitLabel";
            exposureUnitLabel.Size = new Size(12, 15);
            exposureUnitLabel.TabIndex = 1;
            exposureUnitLabel.Text = "s";
            // 
            // anchorPanel
            // 
            anchorPanel.AutoSize = true;
            anchorPanel.Controls.Add(anchorLabel);
            anchorPanel.Controls.Add(anchorGrid);
            anchorPanel.Controls.Add(printerFrontLabel);
            anchorPanel.FlowDirection = FlowDirection.TopDown;
            anchorPanel.Location = new Point(7, 149);
            anchorPanel.Name = "anchorPanel";
            anchorPanel.Size = new Size(186, 153);
            anchorPanel.TabIndex = 10;
            anchorPanel.WrapContents = false;
            // 
            // anchorLabel
            // 
            anchorLabel.Anchor = AnchorStyles.Top;
            anchorLabel.AutoSize = true;
            anchorLabel.Location = new Point(51, 10);
            anchorLabel.Margin = new Padding(3, 10, 3, 3);
            anchorLabel.Name = "anchorLabel";
            anchorLabel.Size = new Size(83, 15);
            anchorLabel.TabIndex = 9;
            anchorLabel.Text = "Anchor corner";
            // 
            // anchorGrid
            // 
            anchorGrid.BackColor = SystemColors.Window;
            anchorGrid.BorderStyle = BorderStyle.FixedSingle;
            anchorGrid.ColumnCount = 3;
            anchorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            anchorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            anchorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            anchorGrid.Controls.Add(topLeftRadio, 0, 0);
            anchorGrid.Controls.Add(topRightRadio, 2, 0);
            anchorGrid.Controls.Add(centerRadio, 1, 1);
            anchorGrid.Controls.Add(bottomLeftRadio, 0, 2);
            anchorGrid.Controls.Add(bottomRightRadio, 2, 2);
            anchorGrid.Location = new Point(3, 31);
            anchorGrid.Name = "anchorGrid";
            anchorGrid.RowCount = 3;
            anchorGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            anchorGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            anchorGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            anchorGrid.Size = new Size(180, 96);
            anchorGrid.TabIndex = 0;
            // 
            // topLeftRadio
            // 
            topLeftRadio.Anchor = AnchorStyles.None;
            topLeftRadio.AutoSize = true;
            topLeftRadio.Location = new Point(22, 9);
            topLeftRadio.Name = "topLeftRadio";
            topLeftRadio.Size = new Size(14, 13);
            topLeftRadio.TabIndex = 0;
            toolTip.SetToolTip(topLeftRadio, "Top left");
            topLeftRadio.UseVisualStyleBackColor = true;
            topLeftRadio.CheckedChanged += OptionsChanged;
            // 
            // topRightRadio
            // 
            topRightRadio.Anchor = AnchorStyles.None;
            topRightRadio.AutoSize = true;
            topRightRadio.Location = new Point(141, 9);
            topRightRadio.Name = "topRightRadio";
            topRightRadio.Size = new Size(14, 13);
            topRightRadio.TabIndex = 1;
            toolTip.SetToolTip(topRightRadio, "Top right");
            topRightRadio.UseVisualStyleBackColor = true;
            topRightRadio.CheckedChanged += OptionsChanged;
            // 
            // centerRadio
            // 
            centerRadio.Anchor = AnchorStyles.None;
            centerRadio.AutoSize = true;
            centerRadio.Location = new Point(81, 40);
            centerRadio.Name = "centerRadio";
            centerRadio.Size = new Size(14, 13);
            centerRadio.TabIndex = 2;
            toolTip.SetToolTip(centerRadio, "Center");
            centerRadio.UseVisualStyleBackColor = true;
            centerRadio.CheckedChanged += OptionsChanged;
            // 
            // bottomLeftRadio
            // 
            bottomLeftRadio.Anchor = AnchorStyles.None;
            bottomLeftRadio.AutoSize = true;
            bottomLeftRadio.Location = new Point(22, 71);
            bottomLeftRadio.Name = "bottomLeftRadio";
            bottomLeftRadio.Size = new Size(14, 13);
            bottomLeftRadio.TabIndex = 3;
            toolTip.SetToolTip(bottomLeftRadio, "Bottom left");
            bottomLeftRadio.UseVisualStyleBackColor = true;
            bottomLeftRadio.CheckedChanged += OptionsChanged;
            // 
            // bottomRightRadio
            // 
            bottomRightRadio.Anchor = AnchorStyles.None;
            bottomRightRadio.AutoSize = true;
            bottomRightRadio.Location = new Point(141, 71);
            bottomRightRadio.Name = "bottomRightRadio";
            bottomRightRadio.Size = new Size(14, 13);
            bottomRightRadio.TabIndex = 4;
            toolTip.SetToolTip(bottomRightRadio, "Bottom right");
            bottomRightRadio.UseVisualStyleBackColor = true;
            bottomRightRadio.CheckedChanged += OptionsChanged;
            // 
            // printerFrontLabel
            // 
            printerFrontLabel.ForeColor = SystemColors.GrayText;
            printerFrontLabel.Location = new Point(3, 130);
            printerFrontLabel.Name = "printerFrontLabel";
            printerFrontLabel.Size = new Size(180, 23);
            printerFrontLabel.TabIndex = 1;
            printerFrontLabel.Text = "Printer front";
            printerFrontLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // offsetPanel
            // 
            offsetPanel.AutoSize = true;
            offsetPanel.Controls.Add(offsetLabel);
            offsetPanel.Controls.Add(offsetXLabel);
            offsetPanel.Controls.Add(offsetXNumeric);
            offsetPanel.Controls.Add(offsetYLabel);
            offsetPanel.Controls.Add(offsetYNumeric);
            offsetPanel.Controls.Add(offsetUnitLabel);
            offsetPanel.Location = new Point(7, 308);
            offsetPanel.Name = "offsetPanel";
            offsetPanel.Size = new Size(366, 29);
            offsetPanel.TabIndex = 12;
            offsetPanel.WrapContents = false;
            // 
            // offsetLabel
            // 
            offsetLabel.Anchor = AnchorStyles.Left;
            offsetLabel.AutoSize = true;
            offsetLabel.Location = new Point(3, 7);
            offsetLabel.Name = "offsetLabel";
            offsetLabel.Size = new Size(79, 15);
            offsetLabel.TabIndex = 11;
            offsetLabel.Text = "Anchor offset";
            // 
            // offsetXLabel
            // 
            offsetXLabel.Anchor = AnchorStyles.Left;
            offsetXLabel.AutoSize = true;
            offsetXLabel.Location = new Point(88, 7);
            offsetXLabel.Name = "offsetXLabel";
            offsetXLabel.Size = new Size(14, 15);
            offsetXLabel.TabIndex = 0;
            offsetXLabel.Text = "X";
            // 
            // offsetXNumeric
            // 
            offsetXNumeric.Anchor = AnchorStyles.Left;
            offsetXNumeric.DecimalPlaces = 2;
            offsetXNumeric.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            offsetXNumeric.Location = new Point(108, 3);
            offsetXNumeric.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            offsetXNumeric.Minimum = new decimal(new int[] { 500, 0, 0, int.MinValue });
            offsetXNumeric.Name = "offsetXNumeric";
            offsetXNumeric.Size = new Size(80, 23);
            offsetXNumeric.TabIndex = 1;
            offsetXNumeric.ValueChanged += OptionsChanged;
            // 
            // offsetYLabel
            // 
            offsetYLabel.Anchor = AnchorStyles.Left;
            offsetYLabel.AutoSize = true;
            offsetYLabel.Location = new Point(194, 7);
            offsetYLabel.Name = "offsetYLabel";
            offsetYLabel.Size = new Size(48, 15);
            offsetYLabel.TabIndex = 2;
            offsetYLabel.Text = "mm    Y";
            // 
            // offsetYNumeric
            // 
            offsetYNumeric.Anchor = AnchorStyles.Left;
            offsetYNumeric.DecimalPlaces = 2;
            offsetYNumeric.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            offsetYNumeric.Location = new Point(248, 3);
            offsetYNumeric.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            offsetYNumeric.Minimum = new decimal(new int[] { 500, 0, 0, int.MinValue });
            offsetYNumeric.Name = "offsetYNumeric";
            offsetYNumeric.Size = new Size(80, 23);
            offsetYNumeric.TabIndex = 3;
            offsetYNumeric.ValueChanged += OptionsChanged;
            // 
            // offsetUnitLabel
            // 
            offsetUnitLabel.Anchor = AnchorStyles.Left;
            offsetUnitLabel.AutoSize = true;
            offsetUnitLabel.Location = new Point(334, 7);
            offsetUnitLabel.Name = "offsetUnitLabel";
            offsetUnitLabel.Size = new Size(29, 15);
            offsetUnitLabel.TabIndex = 4;
            offsetUnitLabel.Text = "mm";
            // 
            // rotationPanel
            // 
            rotationPanel.AutoSize = true;
            rotationPanel.Controls.Add(rotationLabel);
            rotationPanel.Controls.Add(rotateNoneRadio);
            rotationPanel.Controls.Add(rotateLeftRadio);
            rotationPanel.Controls.Add(rotateRightRadio);
            rotationPanel.Location = new Point(7, 343);
            rotationPanel.Name = "rotationPanel";
            rotationPanel.Size = new Size(285, 25);
            rotationPanel.TabIndex = 19;
            rotationPanel.WrapContents = false;
            // 
            // rotationLabel
            // 
            rotationLabel.Anchor = AnchorStyles.Left;
            rotationLabel.AutoSize = true;
            rotationLabel.Location = new Point(3, 5);
            rotationLabel.Name = "rotationLabel";
            rotationLabel.Size = new Size(75, 15);
            rotationLabel.TabIndex = 18;
            rotationLabel.Text = "Rotate board";
            // 
            // rotateNoneRadio
            // 
            rotateNoneRadio.Anchor = AnchorStyles.Left;
            rotateNoneRadio.AutoSize = true;
            rotateNoneRadio.Checked = true;
            rotateNoneRadio.Location = new Point(84, 3);
            rotateNoneRadio.Name = "rotateNoneRadio";
            rotateNoneRadio.Size = new Size(54, 19);
            rotateNoneRadio.TabIndex = 0;
            rotateNoneRadio.TabStop = true;
            rotateNoneRadio.Text = "None";
            rotateNoneRadio.UseVisualStyleBackColor = true;
            rotateNoneRadio.CheckedChanged += OptionsChanged;
            // 
            // rotateLeftRadio
            // 
            rotateLeftRadio.Anchor = AnchorStyles.Left;
            rotateLeftRadio.AutoSize = true;
            rotateLeftRadio.Location = new Point(144, 3);
            rotateLeftRadio.Name = "rotateLeftRadio";
            rotateLeftRadio.Size = new Size(62, 19);
            rotateLeftRadio.TabIndex = 1;
            rotateLeftRadio.Text = "90° left";
            toolTip.SetToolTip(rotateLeftRadio, "Turn the board 90° counter-clockwise on the printer screen");
            rotateLeftRadio.UseVisualStyleBackColor = true;
            rotateLeftRadio.CheckedChanged += OptionsChanged;
            // 
            // rotateRightRadio
            // 
            rotateRightRadio.Anchor = AnchorStyles.Left;
            rotateRightRadio.AutoSize = true;
            rotateRightRadio.Location = new Point(212, 3);
            rotateRightRadio.Name = "rotateRightRadio";
            rotateRightRadio.Size = new Size(70, 19);
            rotateRightRadio.TabIndex = 2;
            rotateRightRadio.Text = "90° right";
            toolTip.SetToolTip(rotateRightRadio, "Turn the board 90° clockwise on the printer screen");
            rotateRightRadio.UseVisualStyleBackColor = true;
            rotateRightRadio.CheckedChanged += OptionsChanged;
            // 
            // copiesPanel
            // 
            copiesPanel.AutoSize = true;
            copiesPanel.Controls.Add(copiesLabel);
            copiesPanel.Controls.Add(copyMatrix);
            copiesPanel.Controls.Add(copySpacingLabel);
            copiesPanel.Controls.Add(copySpacingNumeric);
            copiesPanel.Controls.Add(copySpacingUnitLabel);
            copiesPanel.Location = new Point(7, 374);
            copiesPanel.Name = "copiesPanel";
            copiesPanel.Size = new Size(289, 68);
            copiesPanel.TabIndex = 20;
            copiesPanel.WrapContents = false;
            // 
            // copiesLabel
            // 
            copiesLabel.Anchor = AnchorStyles.Left;
            copiesLabel.AutoSize = true;
            copiesLabel.Location = new Point(3, 26);
            copiesLabel.Name = "copiesLabel";
            copiesLabel.Size = new Size(43, 15);
            copiesLabel.TabIndex = 0;
            copiesLabel.Text = "Copies";
            //
            // copyMatrix
            //
            copyMatrix.Anchor = AnchorStyles.Left;
            copyMatrix.Location = new Point(52, 3);
            copyMatrix.Name = "copyMatrix";
            copyMatrix.Size = new Size(78, 62);
            copyMatrix.TabIndex = 1;
            toolTip.SetToolTip(copyMatrix, "Cells that get a copy of the board; the block of copies is placed by the anchor corner");
            copyMatrix.CellsChanged += OptionsChanged;
            // 
            // copySpacingLabel
            // 
            copySpacingLabel.Anchor = AnchorStyles.Left;
            copySpacingLabel.AutoSize = true;
            copySpacingLabel.Location = new Point(136, 26);
            copySpacingLabel.Name = "copySpacingLabel";
            copySpacingLabel.Size = new Size(39, 15);
            copySpacingLabel.TabIndex = 2;
            copySpacingLabel.Text = "Offset";
            // 
            // copySpacingNumeric
            // 
            copySpacingNumeric.Anchor = AnchorStyles.Left;
            copySpacingNumeric.DecimalPlaces = 1;
            copySpacingNumeric.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            copySpacingNumeric.Location = new Point(181, 22);
            copySpacingNumeric.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            copySpacingNumeric.Name = "copySpacingNumeric";
            copySpacingNumeric.Size = new Size(70, 23);
            copySpacingNumeric.TabIndex = 3;
            toolTip.SetToolTip(copySpacingNumeric, "Gap between neighbouring copies");
            copySpacingNumeric.Value = new decimal(new int[] { 2, 0, 0, 0 });
            copySpacingNumeric.ValueChanged += OptionsChanged;
            // 
            // copySpacingUnitLabel
            // 
            copySpacingUnitLabel.Anchor = AnchorStyles.Left;
            copySpacingUnitLabel.AutoSize = true;
            copySpacingUnitLabel.Location = new Point(257, 26);
            copySpacingUnitLabel.Name = "copySpacingUnitLabel";
            copySpacingUnitLabel.Size = new Size(29, 15);
            copySpacingUnitLabel.TabIndex = 4;
            copySpacingUnitLabel.Text = "mm";
            // 
            // saveButton
            // 
            saveButton.AutoSize = true;
            saveButton.Location = new Point(7, 448);
            saveButton.Name = "saveButton";
            saveButton.Padding = new Padding(12, 4, 12, 4);
            saveButton.Size = new Size(88, 35);
            saveButton.TabIndex = 0;
            saveButton.Text = "Save…";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += SaveClicked;
            // 
            // previewGroup
            // 
            previewGroup.Controls.Add(previewLayout);
            previewGroup.Dock = DockStyle.Fill;
            previewGroup.Location = new Point(443, 3);
            previewGroup.Name = "previewGroup";
            previewGroup.Padding = new Padding(4);
            previewGroup.Size = new Size(738, 774);
            previewGroup.TabIndex = 1;
            previewGroup.TabStop = false;
            previewGroup.Text = "Preview";
            // 
            // previewLayout
            // 
            previewLayout.ColumnCount = 1;
            previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            previewLayout.Controls.Add(previewPicture, 0, 1);
            previewLayout.Dock = DockStyle.Fill;
            previewLayout.Location = new Point(4, 20);
            previewLayout.Name = "previewLayout";
            previewLayout.RowCount = 3;
            previewLayout.RowStyles.Add(new RowStyle());
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewLayout.RowStyles.Add(new RowStyle());
            previewLayout.Size = new Size(730, 750);
            previewLayout.TabIndex = 0;
            // 
            // previewPicture
            // 
            previewPicture.BackColor = Color.LightGray;
            previewPicture.Dock = DockStyle.Fill;
            previewPicture.Location = new Point(3, 3);
            previewPicture.Name = "previewPicture";
            previewPicture.Size = new Size(724, 744);
            previewPicture.SizeMode = PictureBoxSizeMode.CenterImage;
            previewPicture.TabIndex = 1;
            previewPicture.TabStop = false;
            previewPicture.SizeChanged += PreviewPictureSizeChanged;
            // 
            // ExportView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(mainLayout);
            Name = "ExportView";
            Size = new Size(1184, 780);
            mainLayout.ResumeLayout(false);
            optionsGroup.ResumeLayout(false);
            optionsPanel.ResumeLayout(false);
            optionsPanel.PerformLayout();
            exposurePanel.ResumeLayout(false);
            exposurePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)exposureNumeric).EndInit();
            anchorPanel.ResumeLayout(false);
            anchorPanel.PerformLayout();
            anchorGrid.ResumeLayout(false);
            anchorGrid.PerformLayout();
            offsetPanel.ResumeLayout(false);
            offsetPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)offsetXNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)offsetYNumeric).EndInit();
            rotationPanel.ResumeLayout(false);
            rotationPanel.PerformLayout();
            copiesPanel.ResumeLayout(false);
            copiesPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)copySpacingNumeric).EndInit();
            previewGroup.ResumeLayout(false);
            previewLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)previewPicture).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel mainLayout;
        private GroupBox optionsGroup;
        private FlowLayoutPanel optionsPanel;
        private LayerExportList layersList;
        private ColumnHeader layersFileColumn;
        private ColumnHeader layersLayerColumn;
        private ColumnHeader layersFlipHorizontalColumn;
        private ColumnHeader layersFlipVerticalColumn;
        private Label exposureLabel;
        private FlowLayoutPanel exposurePanel;
        private NumericUpDown exposureNumeric;
        private Label exposureUnitLabel;
        private Label anchorLabel;
        private FlowLayoutPanel anchorPanel;
        private TableLayoutPanel anchorGrid;
        private RadioButton topLeftRadio;
        private RadioButton topRightRadio;
        private RadioButton centerRadio;
        private RadioButton bottomLeftRadio;
        private RadioButton bottomRightRadio;
        private Label printerFrontLabel;
        private Label offsetLabel;
        private FlowLayoutPanel offsetPanel;
        private Label offsetXLabel;
        private NumericUpDown offsetXNumeric;
        private Label offsetYLabel;
        private NumericUpDown offsetYNumeric;
        private Label offsetUnitLabel;
        private Label rotationLabel;
        private FlowLayoutPanel rotationPanel;
        private RadioButton rotateNoneRadio;
        private RadioButton rotateLeftRadio;
        private RadioButton rotateRightRadio;
        private FlowLayoutPanel copiesPanel;
        private Label copiesLabel;
        private CopyMatrix copyMatrix;
        private Label copySpacingLabel;
        private NumericUpDown copySpacingNumeric;
        private Label copySpacingUnitLabel;
        private GroupBox previewGroup;
        private TableLayoutPanel previewLayout;
        private PictureBox previewPicture;
        private Button saveButton;
        private ToolTip toolTip;
    }
}
