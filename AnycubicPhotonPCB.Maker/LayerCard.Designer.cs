#nullable disable

namespace AnycubicPhotonPCB.Maker
{
    partial class LayerCard
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
            layout = new TableLayoutPanel();
            headerPanel = new FlowLayoutPanel();
            titleLabel = new Label();
            picture = new PictureBox();
            footerPanel = new FlowLayoutPanel();
            layout.SuspendLayout();
            headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picture).BeginInit();
            SuspendLayout();
            // 
            // layout
            // 
            layout.BackColor = SystemColors.Window;
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(headerPanel, 0, 0);
            layout.Controls.Add(picture, 0, 1);
            layout.Controls.Add(footerPanel, 0, 2);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(1, 1);
            layout.Margin = new Padding(0);
            layout.Name = "layout";
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle());
            layout.Size = new Size(478, 318);
            layout.TabIndex = 0;
            // 
            // headerPanel
            // 
            headerPanel.Anchor = AnchorStyles.Top;
            headerPanel.AutoSize = true;
            headerPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            headerPanel.BackColor = Color.White;
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Location = new Point(164, 0);
            headerPanel.Margin = new Padding(0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(150, 23);
            headerPanel.TabIndex = 0;
            headerPanel.WrapContents = false;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            titleLabel.Location = new Point(4, 2);
            titleLabel.Margin = new Padding(4, 2, 4, 4);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(142, 17);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Layer (Side) - file name";
            // 
            // picture
            // 
            picture.BackColor = Color.White;
            picture.Dock = DockStyle.Fill;
            picture.Location = new Point(0, 23);
            picture.Margin = new Padding(0);
            picture.Name = "picture";
            picture.Size = new Size(478, 295);
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            picture.TabIndex = 1;
            picture.TabStop = false;
            // 
            // footerPanel
            // 
            footerPanel.AutoSize = true;
            footerPanel.Dock = DockStyle.Fill;
            footerPanel.Location = new Point(0, 318);
            footerPanel.Margin = new Padding(0);
            footerPanel.Name = "footerPanel";
            footerPanel.Size = new Size(478, 1);
            footerPanel.TabIndex = 2;
            footerPanel.WrapContents = false;
            // 
            // LayerCard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            Controls.Add(layout);
            Margin = new Padding(0);
            Name = "LayerCard";
            Padding = new Padding(1);
            Size = new Size(480, 320);
            layout.ResumeLayout(false);
            layout.PerformLayout();
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picture).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel layout;
        private FlowLayoutPanel headerPanel;
        private Label titleLabel;
        private PictureBox picture;
        private FlowLayoutPanel footerPanel;
    }
}
