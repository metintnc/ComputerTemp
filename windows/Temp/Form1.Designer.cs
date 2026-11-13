namespace Temp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Panel headerPanel;
        private Label titleLabel;
        private Label processorNameLabel;
        private Label temperatureLabel;
        private Label temperatureCaptionLabel;
        private ProgressBar temperatureProgressBar;
        private Label progressCaptionLabel;
        private Panel controlsPanel;
        private Label intervalLabel;
        private NumericUpDown intervalNumericUpDown;
        private Label criticalTemperatureLabel;
        private NumericUpDown criticalTemperatureNumericUpDown;
        private Label logIntervalLabel;
        private NumericUpDown logIntervalNumericUpDown;
        private CheckBox loggingCheckBox;
        private Button exportLogButton;
        private CheckBox useTemperatureColorsCheckBox;
        private CheckBox startWithWindowsCheckBox;
        private CheckBox darkThemeCheckBox;
        private Button toggleButton;
        private Label statusLabel;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayContextMenu;
        private ToolStripMenuItem showHideMenuItem;
        private ToolStripMenuItem widgetModeMenuItem;
        private ToolStripMenuItem exitMenuItem;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            headerPanel = new Panel();
            titleLabel = new Label();
            processorNameLabel = new Label();
            temperatureLabel = new Label();
            temperatureCaptionLabel = new Label();
            temperatureProgressBar = new ProgressBar();
            progressCaptionLabel = new Label();
            controlsPanel = new Panel();
            intervalLabel = new Label();
            intervalNumericUpDown = new NumericUpDown();
            criticalTemperatureLabel = new Label();
            criticalTemperatureNumericUpDown = new NumericUpDown();
            logIntervalLabel = new Label();
            logIntervalNumericUpDown = new NumericUpDown();
            loggingCheckBox = new CheckBox();
            exportLogButton = new Button();
            useTemperatureColorsCheckBox = new CheckBox();
            startWithWindowsCheckBox = new CheckBox();
            darkThemeCheckBox = new CheckBox();
            toggleButton = new Button();
            statusLabel = new Label();
            trayIcon = new NotifyIcon(components);
            trayContextMenu = new ContextMenuStrip(components);
            showHideMenuItem = new ToolStripMenuItem();
            widgetModeMenuItem = new ToolStripMenuItem();
            exitMenuItem = new ToolStripMenuItem();
            headerPanel.SuspendLayout();
            controlsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)intervalNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)criticalTemperatureNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)logIntervalNumericUpDown).BeginInit();
            trayContextMenu.SuspendLayout();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(37, 99, 235);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(processorNameLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Margin = new Padding(3, 4, 3, 4);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(914, 123);
            headerPanel.TabIndex = 5;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(32, 16);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(247, 41);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Sistem Monitörü";
            // 
            // processorNameLabel
            // 
            processorNameLabel.AutoEllipsis = true;
            processorNameLabel.Font = new Font("Segoe UI", 10F);
            processorNameLabel.ForeColor = Color.FromArgb(219, 234, 254);
            processorNameLabel.Location = new Point(34, 67);
            processorNameLabel.Name = "processorNameLabel";
            processorNameLabel.Size = new Size(743, 31);
            processorNameLabel.TabIndex = 1;
            processorNameLabel.Text = "İşlemci: okunuyor...";
            // 
            // temperatureLabel
            // 
            temperatureLabel.Font = new Font("Segoe UI", 42F, FontStyle.Bold);
            temperatureLabel.ForeColor = Color.FromArgb(15, 23, 42);
            temperatureLabel.Location = new Point(0, 176);
            temperatureLabel.Name = "temperatureLabel";
            temperatureLabel.Size = new Size(914, 200);
            temperatureLabel.TabIndex = 0;
            temperatureLabel.Text = "-- °C";
            temperatureLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // temperatureCaptionLabel
            // 
            temperatureCaptionLabel.AutoSize = true;
            temperatureCaptionLabel.Font = new Font("Segoe UI", 11F);
            temperatureCaptionLabel.ForeColor = Color.FromArgb(71, 85, 105);
            temperatureCaptionLabel.Location = new Point(354, 149);
            temperatureCaptionLabel.Name = "temperatureCaptionLabel";
            temperatureCaptionLabel.Size = new Size(167, 25);
            temperatureCaptionLabel.TabIndex = 1;
            temperatureCaptionLabel.Text = "Anlık CPU sıcaklığı";
            // 
            // temperatureProgressBar
            // 
            temperatureProgressBar.Location = new Point(91, 376);
            temperatureProgressBar.Margin = new Padding(3, 4, 3, 4);
            temperatureProgressBar.Name = "temperatureProgressBar";
            temperatureProgressBar.Size = new Size(731, 24);
            temperatureProgressBar.Style = ProgressBarStyle.Continuous;
            temperatureProgressBar.TabIndex = 2;
            // 
            // progressCaptionLabel
            // 
            progressCaptionLabel.AutoSize = true;
            progressCaptionLabel.Font = new Font("Segoe UI", 9F);
            progressCaptionLabel.ForeColor = Color.FromArgb(100, 116, 139);
            progressCaptionLabel.Location = new Point(91, 408);
            progressCaptionLabel.Name = "progressCaptionLabel";
            progressCaptionLabel.Size = new Size(185, 20);
            progressCaptionLabel.TabIndex = 3;
            progressCaptionLabel.Text = "Sıcaklık seviyesi (0–100 °C)";
            // 
            // controlsPanel
            // 
            controlsPanel.BackColor = Color.FromArgb(248, 250, 252);
            controlsPanel.Controls.Add(intervalLabel);
            controlsPanel.Controls.Add(intervalNumericUpDown);
            controlsPanel.Controls.Add(criticalTemperatureLabel);
            controlsPanel.Controls.Add(criticalTemperatureNumericUpDown);
            controlsPanel.Controls.Add(logIntervalLabel);
            controlsPanel.Controls.Add(logIntervalNumericUpDown);
            controlsPanel.Controls.Add(loggingCheckBox);
            controlsPanel.Controls.Add(exportLogButton);
            controlsPanel.Controls.Add(useTemperatureColorsCheckBox);
            controlsPanel.Controls.Add(startWithWindowsCheckBox);
            controlsPanel.Controls.Add(darkThemeCheckBox);
            controlsPanel.Controls.Add(toggleButton);
            controlsPanel.Dock = DockStyle.Bottom;
            controlsPanel.Location = new Point(0, 822);
            controlsPanel.Margin = new Padding(3, 4, 3, 4);
            controlsPanel.Name = "controlsPanel";
            controlsPanel.Size = new Size(914, 173);
            controlsPanel.TabIndex = 4;
            // 
            // intervalLabel
            // 
            intervalLabel.AutoSize = true;
            intervalLabel.Location = new Point(217, 36);
            intervalLabel.Name = "intervalLabel";
            intervalLabel.Size = new Size(127, 20);
            intervalLabel.TabIndex = 1;
            intervalLabel.Text = "Yenileme (saniye):";
            // 
            // intervalNumericUpDown
            // 
            intervalNumericUpDown.DecimalPlaces = 1;
            intervalNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            intervalNumericUpDown.Location = new Point(377, 33);
            intervalNumericUpDown.Margin = new Padding(3, 4, 3, 4);
            intervalNumericUpDown.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            intervalNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            intervalNumericUpDown.Name = "intervalNumericUpDown";
            intervalNumericUpDown.Size = new Size(80, 27);
            intervalNumericUpDown.TabIndex = 2;
            intervalNumericUpDown.Value = new decimal(new int[] { 1, 0, 0, 0 });
            intervalNumericUpDown.ValueChanged += intervalNumericUpDown_ValueChanged;
            // 
            // criticalTemperatureLabel
            // 
            criticalTemperatureLabel.AutoSize = true;
            criticalTemperatureLabel.Location = new Point(17, 36);
            criticalTemperatureLabel.Name = "criticalTemperatureLabel";
            criticalTemperatureLabel.Size = new Size(97, 20);
            criticalTemperatureLabel.TabIndex = 5;
            criticalTemperatureLabel.Text = "Kritik sıcaklık:";
            // 
            // criticalTemperatureNumericUpDown
            // 
            criticalTemperatureNumericUpDown.DecimalPlaces = 1;
            criticalTemperatureNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            criticalTemperatureNumericUpDown.Location = new Point(143, 33);
            criticalTemperatureNumericUpDown.Margin = new Padding(3, 4, 3, 4);
            criticalTemperatureNumericUpDown.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            criticalTemperatureNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            criticalTemperatureNumericUpDown.Name = "criticalTemperatureNumericUpDown";
            criticalTemperatureNumericUpDown.Size = new Size(69, 27);
            criticalTemperatureNumericUpDown.TabIndex = 6;
            criticalTemperatureNumericUpDown.Value = new decimal(new int[] { 85, 0, 0, 0 });
            // 
            // logIntervalLabel
            // 
            logIntervalLabel.AutoSize = true;
            logIntervalLabel.Location = new Point(17, 76);
            logIntervalLabel.Name = "logIntervalLabel";
            logIntervalLabel.Size = new Size(92, 20);
            logIntervalLabel.TabIndex = 7;
            logIntervalLabel.Text = "Log (saniye):";
            // 
            // logIntervalNumericUpDown
            // 
            logIntervalNumericUpDown.DecimalPlaces = 1;
            logIntervalNumericUpDown.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            logIntervalNumericUpDown.Location = new Point(143, 73);
            logIntervalNumericUpDown.Margin = new Padding(3, 4, 3, 4);
            logIntervalNumericUpDown.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            logIntervalNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            logIntervalNumericUpDown.Name = "logIntervalNumericUpDown";
            logIntervalNumericUpDown.Size = new Size(69, 27);
            logIntervalNumericUpDown.TabIndex = 8;
            logIntervalNumericUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // loggingCheckBox
            // 
            loggingCheckBox.AutoSize = true;
            loggingCheckBox.Location = new Point(240, 76);
            loggingCheckBox.Margin = new Padding(3, 4, 3, 4);
            loggingCheckBox.Name = "loggingCheckBox";
            loggingCheckBox.Size = new Size(168, 24);
            loggingCheckBox.TabIndex = 9;
            loggingCheckBox.Text = "Loglamayı etkinleştir";
            // 
            // exportLogButton
            // 
            exportLogButton.Location = new Point(446, 69);
            exportLogButton.Margin = new Padding(3, 4, 3, 4);
            exportLogButton.Name = "exportLogButton";
            exportLogButton.Size = new Size(171, 43);
            exportLogButton.TabIndex = 10;
            exportLogButton.Text = "JSON dışa aktar";
            exportLogButton.UseVisualStyleBackColor = true;
            exportLogButton.Click += exportLogButton_Click;
            // 
            // useTemperatureColorsCheckBox
            // 
            useTemperatureColorsCheckBox.AutoSize = true;
            useTemperatureColorsCheckBox.Location = new Point(640, 76);
            useTemperatureColorsCheckBox.Margin = new Padding(3, 4, 3, 4);
            useTemperatureColorsCheckBox.Name = "useTemperatureColorsCheckBox";
            useTemperatureColorsCheckBox.Size = new Size(133, 24);
            useTemperatureColorsCheckBox.TabIndex = 11;
            useTemperatureColorsCheckBox.Text = "Sıcaklık renkleri";
            useTemperatureColorsCheckBox.CheckedChanged += useTemperatureColorsCheckBox_CheckedChanged;
            // 
            // startWithWindowsCheckBox
            // 
            startWithWindowsCheckBox.AutoSize = true;
            startWithWindowsCheckBox.Location = new Point(640, 109);
            startWithWindowsCheckBox.Margin = new Padding(3, 4, 3, 4);
            startWithWindowsCheckBox.Name = "startWithWindowsCheckBox";
            startWithWindowsCheckBox.Size = new Size(156, 24);
            startWithWindowsCheckBox.TabIndex = 12;
            startWithWindowsCheckBox.Text = "Windows ile başlat";
            startWithWindowsCheckBox.CheckedChanged += startWithWindowsCheckBox_CheckedChanged;
            // 
            // darkThemeCheckBox
            // 
            darkThemeCheckBox.AutoSize = true;
            darkThemeCheckBox.Location = new Point(640, 143);
            darkThemeCheckBox.Margin = new Padding(3, 4, 3, 4);
            darkThemeCheckBox.Name = "darkThemeCheckBox";
            darkThemeCheckBox.Size = new Size(102, 24);
            darkThemeCheckBox.TabIndex = 13;
            darkThemeCheckBox.Text = "Koyu tema";
            darkThemeCheckBox.CheckedChanged += darkThemeCheckBox_CheckedChanged;
            // 
            // toggleButton
            // 
            toggleButton.Location = new Point(514, 31);
            toggleButton.Margin = new Padding(3, 4, 3, 4);
            toggleButton.Name = "toggleButton";
            toggleButton.Size = new Size(229, 53);
            toggleButton.TabIndex = 3;
            toggleButton.Text = "Başlat";
            toggleButton.UseVisualStyleBackColor = true;
            toggleButton.Click += toggleButton_Click;
            // 
            // statusLabel
            // 
            statusLabel.Dock = DockStyle.Bottom;
            statusLabel.ForeColor = Color.DimGray;
            statusLabel.Location = new Point(0, 995);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(914, 60);
            statusLabel.TabIndex = 4;
            statusLabel.Text = "Ölçüm başlatılmadı.";
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // trayIcon
            // 
            trayIcon.ContextMenuStrip = trayContextMenu;
            trayIcon.Icon = (Icon)resources.GetObject("trayIcon.Icon");
            trayIcon.Text = "CPU Monitörü";
            trayIcon.DoubleClick += trayIcon_DoubleClick;
            // 
            // trayContextMenu
            // 
            trayContextMenu.ImageScalingSize = new Size(20, 20);
            trayContextMenu.Items.AddRange(new ToolStripItem[] { showHideMenuItem, widgetModeMenuItem, exitMenuItem });
            trayContextMenu.Name = "trayContextMenu";
            trayContextMenu.Size = new Size(229, 76);
            // 
            // showHideMenuItem
            // 
            showHideMenuItem.Name = "showHideMenuItem";
            showHideMenuItem.Size = new Size(228, 24);
            showHideMenuItem.Text = "Göster";
            showHideMenuItem.Click += showHideMenuItem_Click;
            // 
            // widgetModeMenuItem
            // 
            widgetModeMenuItem.Name = "widgetModeMenuItem";
            widgetModeMenuItem.Size = new Size(228, 24);
            widgetModeMenuItem.Text = "Floating Widget modu";
            widgetModeMenuItem.Click += widgetModeMenuItem_Click;
            // 
            // exitMenuItem
            // 
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new Size(228, 24);
            exitMenuItem.Text = "Çıkış";
            exitMenuItem.Click += exitMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(914, 1055);
            Controls.Add(temperatureCaptionLabel);
            Controls.Add(temperatureLabel);
            Controls.Add(temperatureProgressBar);
            Controls.Add(progressCaptionLabel);
            Controls.Add(controlsPanel);
            Controls.Add(statusLabel);
            Controls.Add(headerPanel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimumSize = new Size(930, 1018);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sistem Monitörü";
            Resize += form1_Resize;
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            controlsPanel.ResumeLayout(false);
            controlsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)intervalNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)criticalTemperatureNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)logIntervalNumericUpDown).EndInit();
            trayContextMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
