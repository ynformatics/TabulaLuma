namespace FrameCodeGenerator
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
            components = new System.ComponentModel.Container();
            nudFrameId = new NumericUpDown();
            label1 = new Label();
            printPreviewControl1 = new PrintPreviewControl();
            butPreview = new Button();
            butPrint = new Button();
            chkLandscape = new CheckBox();
            txtFrameWidth = new TextBox();
            txtFrameHeight = new TextBox();
            label2 = new Label();
            label3 = new Label();
            cbPresets = new ComboBox();
            label4 = new Label();
            butPrintSettings = new Button();
            labPrinterName = new Label();
            butSavePreset = new Button();
            butDeletePreset = new Button();
            toolTip1 = new ToolTip(components);
            nudFrameSize = new NumericUpDown();
            chkPrintId = new CheckBox();
            groupBox1 = new GroupBox();
            label5 = new Label();
            label6 = new Label();
            ((System.ComponentModel.ISupportInitialize)nudFrameId).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudFrameSize).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // nudFrameId
            // 
            nudFrameId.Location = new Point(83, 17);
            nudFrameId.Maximum = new decimal(new int[] { 4095, 0, 0, 0 });
            nudFrameId.Name = "nudFrameId";
            nudFrameId.Size = new Size(85, 23);
            nudFrameId.TabIndex = 1;
            toolTip1.SetToolTip(nudFrameId, "Frame Id (0 - 4095)");
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(21, 19);
            label1.Name = "label1";
            label1.Size = new Size(56, 15);
            label1.TabIndex = 2;
            label1.Text = "Frame Id:";
            // 
            // printPreviewControl1
            // 
            printPreviewControl1.Location = new Point(434, 17);
            printPreviewControl1.Name = "printPreviewControl1";
            printPreviewControl1.Size = new Size(414, 403);
            printPreviewControl1.TabIndex = 4;
            // 
            // butPreview
            // 
            butPreview.Location = new Point(336, 220);
            butPreview.Name = "butPreview";
            butPreview.Size = new Size(75, 23);
            butPreview.TabIndex = 5;
            butPreview.Text = "Preview >>";
            butPreview.UseVisualStyleBackColor = true;
            butPreview.Click += butPreview_Click;
            // 
            // butPrint
            // 
            butPrint.Location = new Point(135, 387);
            butPrint.Name = "butPrint";
            butPrint.Size = new Size(120, 23);
            butPrint.TabIndex = 6;
            butPrint.Text = "Print";
            butPrint.UseVisualStyleBackColor = true;
            butPrint.Click += butPrint_Click;
            // 
            // chkLandscape
            // 
            chkLandscape.AutoSize = true;
            chkLandscape.Location = new Point(25, 122);
            chkLandscape.Name = "chkLandscape";
            chkLandscape.Size = new Size(82, 19);
            chkLandscape.TabIndex = 7;
            chkLandscape.Text = "Landscape";
            chkLandscape.UseVisualStyleBackColor = true;
            // 
            // txtFrameWidth
            // 
            txtFrameWidth.Location = new Point(144, 30);
            txtFrameWidth.Name = "txtFrameWidth";
            txtFrameWidth.Size = new Size(100, 23);
            txtFrameWidth.TabIndex = 8;
            // 
            // txtFrameHeight
            // 
            txtFrameHeight.Location = new Point(144, 73);
            txtFrameHeight.Name = "txtFrameHeight";
            txtFrameHeight.Size = new Size(100, 23);
            txtFrameHeight.TabIndex = 9;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(25, 33);
            label2.Name = "label2";
            label2.Size = new Size(109, 15);
            label2.TabIndex = 10;
            label2.Text = "Frame width (mm):";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(25, 76);
            label3.Name = "label3";
            label3.Size = new Size(113, 15);
            label3.TabIndex = 11;
            label3.Text = "Frame height (mm):";
            // 
            // cbPresets
            // 
            cbPresets.FormattingEnabled = true;
            cbPresets.Location = new Point(83, 123);
            cbPresets.Name = "cbPresets";
            cbPresets.Size = new Size(151, 23);
            cbPresets.TabIndex = 12;
            toolTip1.SetToolTip(cbPresets, "Select from drop-down or edit text to create a new preset");
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(21, 126);
            label4.Name = "label4";
            label4.Size = new Size(47, 15);
            label4.TabIndex = 13;
            label4.Text = "Presets:";
            // 
            // butPrintSettings
            // 
            butPrintSettings.Location = new Point(306, 17);
            butPrintSettings.Name = "butPrintSettings";
            butPrintSettings.Size = new Size(105, 23);
            butPrintSettings.TabIndex = 14;
            butPrintSettings.Text = "Print Settings";
            butPrintSettings.UseVisualStyleBackColor = true;
            butPrintSettings.Click += butPrintSettings_Click;
            // 
            // labPrinterName
            // 
            labPrinterName.AutoSize = true;
            labPrinterName.Location = new Point(83, 58);
            labPrinterName.Name = "labPrinterName";
            labPrinterName.Size = new Size(58, 15);
            labPrinterName.TabIndex = 15;
            labPrinterName.Text = "Unknown";
            // 
            // butSavePreset
            // 
            butSavePreset.AccessibleDescription = "Save";
            butSavePreset.Image = Properties.Resources.save__v1;
            butSavePreset.Location = new Point(265, 123);
            butSavePreset.Name = "butSavePreset";
            butSavePreset.Size = new Size(22, 23);
            butSavePreset.TabIndex = 16;
            toolTip1.SetToolTip(butSavePreset, "Save preset");
            butSavePreset.UseVisualStyleBackColor = true;
            butSavePreset.Click += butSavePreset_Click;
            // 
            // butDeletePreset
            // 
            butDeletePreset.AccessibleDescription = "Delete";
            butDeletePreset.Image = Properties.Resources.trash;
            butDeletePreset.Location = new Point(293, 123);
            butDeletePreset.Name = "butDeletePreset";
            butDeletePreset.Size = new Size(22, 23);
            butDeletePreset.TabIndex = 17;
            toolTip1.SetToolTip(butDeletePreset, "Delete preset");
            butDeletePreset.UseVisualStyleBackColor = true;
            butDeletePreset.Click += butDeletePreset_Click;
            // 
            // nudFrameSize
            // 
            nudFrameSize.Location = new Point(248, 17);
            nudFrameSize.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            nudFrameSize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudFrameSize.Name = "nudFrameSize";
            nudFrameSize.Size = new Size(39, 23);
            nudFrameSize.TabIndex = 20;
            toolTip1.SetToolTip(nudFrameSize, "Corner Frame edge size (mm)");
            nudFrameSize.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // chkPrintId
            // 
            chkPrintId.AutoSize = true;
            chkPrintId.Checked = true;
            chkPrintId.CheckState = CheckState.Checked;
            chkPrintId.Location = new Point(25, 147);
            chkPrintId.Name = "chkPrintId";
            chkPrintId.Size = new Size(64, 19);
            chkPrintId.TabIndex = 12;
            chkPrintId.Text = "Print Id";
            toolTip1.SetToolTip(chkPrintId, "Print Frame Id ");
            chkPrintId.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(chkPrintId);
            groupBox1.Controls.Add(txtFrameHeight);
            groupBox1.Controls.Add(chkLandscape);
            groupBox1.Controls.Add(txtFrameWidth);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label3);
            groupBox1.Location = new Point(21, 148);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(294, 181);
            groupBox1.TabIndex = 18;
            groupBox1.TabStop = false;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(21, 58);
            label5.Name = "label5";
            label5.Size = new Size(45, 15);
            label5.TabIndex = 19;
            label5.Text = "Printer:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(183, 21);
            label6.Name = "label6";
            label6.Size = new Size(63, 15);
            label6.TabIndex = 21;
            label6.Text = "Size (mm):";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(860, 450);
            Controls.Add(label6);
            Controls.Add(nudFrameSize);
            Controls.Add(label5);
            Controls.Add(groupBox1);
            Controls.Add(butDeletePreset);
            Controls.Add(butSavePreset);
            Controls.Add(labPrinterName);
            Controls.Add(butPrintSettings);
            Controls.Add(label4);
            Controls.Add(cbPresets);
            Controls.Add(butPrint);
            Controls.Add(butPreview);
            Controls.Add(printPreviewControl1);
            Controls.Add(label1);
            Controls.Add(nudFrameId);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Form1";
            Text = "Corner Frame Generator";
            ((System.ComponentModel.ISupportInitialize)nudFrameId).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudFrameSize).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private NumericUpDown nudFrameId;
        private Label label1;
        private PrintPreviewControl printPreviewControl1;
        private Button butPreview;
        private Button butPrint;
        private CheckBox chkLandscape;
        private TextBox txtFrameWidth;
        private TextBox txtFrameHeight;
        private Label label2;
        private Label label3;
        private ComboBox cbPresets;
        private Label label4;
        private Button butPrintSettings;
        private Label labPrinterName;
        private Button butSavePreset;
        private Button butDeletePreset;
        private ToolTip toolTip1;
        private GroupBox groupBox1;
        private Label label5;
        private NumericUpDown nudFrameSize;
        private Label label6;
        private CheckBox chkPrintId;
    }
}
