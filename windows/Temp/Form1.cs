using System.Management;
using System.Globalization;
using System.Media;
using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.Win32;

namespace Temp
{
    public partial class Form1 : Form
    {
        private readonly HardwareMonitor hardwareMonitor;
        private readonly Logger logger;
        private readonly AppSettings appSettings;
        private readonly SafetyMonitor safetyMonitor;
        private readonly AlertService alertService;
        private readonly MovingAverage cpuTemperatureAverage = new(5);
        private readonly MonitoringCoordinator monitoringCoordinator;
        private bool resourcesDisposed;
        private DateTime? lastLogUtc;
        private Label gpuCardValue = null!;
        private Label memoryCardValue = null!;
        private Label diskCardValue = null!;
        private readonly Dictionary<string, Label[]> coreValueLabels = new(StringComparer.OrdinalIgnoreCase);
        private FlowLayoutPanel coreFlowPanel = null!;
        private Label cpuCardValue = null!;
        private Label cpuCardTitle = null!;
        private Label gpuCardTitle = null!;
        private ProgressBar gpuTemperatureProgressBar = null!;
        private AnimatedCardPanel cpuCardPanel = null!;
        private AnimatedCardPanel gpuCardPanel = null!;
        private readonly System.Windows.Forms.Timer pulseTimer = new()
        {
            Interval = 180
        };
        private bool cpuTemperatureCritical;
        private bool gpuTemperatureCritical;
        private bool pulseVisible;
        private CircularProgressGauge cpuTemperatureGauge = null!;
        private CircularProgressGauge cpuUsageGauge = null!;
        private CircularProgressGauge gpuTemperatureGauge = null!;
        private CircularProgressGauge gpuUsageGauge = null!;
        private Button minimizeButton = null!;
        private Button closeButton = null!;
        private Chart historyChart = null!;
        private ChartHistoryController chartHistoryController = null!;
        private static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(5);
        private DateTime? lastTemperatureAlertUtc;
        private WidgetController widgetController = null!;
        private Point titleDragStart;
        private bool titleDragging;
        private ContextMenuStrip historyChartMenu = null!;

        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            CreateCustomTitleBar();
            appSettings = AppSettings.Load(out string? settingsError);
            hardwareMonitor = new HardwareMonitor();
            monitoringCoordinator = new MonitoringCoordinator(
                hardwareMonitor.ReadMetrics,
                metrics => InvokeOnUi(() => ApplyMetricsToUi(metrics)),
                error => HandleMonitoringError(error));
            logger = new Logger();
            alertService = new AlertService(
                trayIcon,
                AlertCooldown);
            safetyMonitor = new SafetyMonitor(
                (float)Math.Clamp(
                    appSettings.SafetyTemperatureCelsius,
                    1M,
                    150M),
                TimeSpan.FromSeconds(Math.Clamp(
                    appSettings.SafetyDurationSeconds,
                    1,
                    3600)),
                appSettings.SafetyAction,
                appSettings.SafetyExecutablePath,
                appSettings.SafetyTestMode);
            intervalNumericUpDown.Value = Math.Clamp(
                appSettings.RefreshIntervalSeconds,
                intervalNumericUpDown.Minimum,
                intervalNumericUpDown.Maximum);
            criticalTemperatureNumericUpDown.Value =
                Math.Clamp(
                    appSettings.CriticalTemperatureCelsius,
                    criticalTemperatureNumericUpDown.Minimum,
                    criticalTemperatureNumericUpDown.Maximum);
            useTemperatureColorsCheckBox.Checked =
                appSettings.UseTemperatureColors;
            startWithWindowsCheckBox.Checked =
                appSettings.StartWithWindows;
            darkThemeCheckBox.Checked =
                appSettings.Theme == ThemeMode.Dark;
            if (settingsError is not null)
            {
                statusLabel.Text = settingsError;
            }

            processorNameLabel.Text = $"İşlemci: {GetProcessorName()}";
            CreateMetricCards();
            CreateCircularGauges();
            CreateHistoryChart();
            chartHistoryController = new ChartHistoryController(historyChart);
            widgetController = new WidgetController(
                this,
                controlsPanel,
                coreFlowPanel,
                historyChart,
                trayIcon,
                showHideMenuItem,
                widgetModeMenuItem,
                SetMetricCardsVisible);
            pulseTimer.Tick += pulseTimer_Tick;
            pulseTimer.Start();
            ApplySelectedTheme();
            widgetController.AddDragHandlers(this);
            AddButtonHoverEffects(this);
            Load += Form1_Load;
            FormClosing += form1_FormClosing;
        }

        private void pulseTimer_Tick(object? sender, EventArgs e)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            pulseVisible = !pulseVisible;
            cpuCardPanel.SetPulse(cpuTemperatureCritical, pulseVisible);
            gpuCardPanel.SetPulse(gpuTemperatureCritical, pulseVisible);
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            ConfigureHistoryChart();
            ApplySelectedTheme();
        }

        private void toggleButton_Click(object? sender, EventArgs e)
        {
            if (monitoringCoordinator.IsRunning)
            {
                monitoringCoordinator.Stop();
                toggleButton.Text = "Başlat";
                statusLabel.Text = "Ölçüm durduruldu.";
                return;
            }

            monitoringCoordinator.Start();
            toggleButton.Text = "Durdur";
            statusLabel.Text = "CPU sıcaklığı okunuyor...";
        }

        private void intervalNumericUpDown_ValueChanged(object? sender, EventArgs e)
        {
            if (!monitoringCoordinator.SetInterval(intervalNumericUpDown.Value))
            {
                intervalNumericUpDown.Value = 1.0M;
                statusLabel.Text = "Yenileme sıklığı 0,1–60 saniye arasında olmalıdır.";
            }
        }

        private void HandleMonitoringError(Exception exception)
        {
            AppLogger.Error("Donanım okuması başarısız.", exception);
            InvokeOnUi(() =>
            {
                temperatureLabel.Text = "-- °C";
                temperatureProgressBar.Value = 0;
                cpuTemperatureGauge.Value = null;
                cpuUsageGauge.Value = null;
                gpuTemperatureGauge.Value = null;
                gpuUsageGauge.Value = null;
                cpuTemperatureCritical = false;
                gpuTemperatureCritical = false;
                cpuCardPanel.SetPulse(false, false);
                gpuCardPanel.SetPulse(false, false);
                ResetTemperatureColors();
                statusLabel.Text = $"Sıcaklık okunamadı: {exception.Message}";
            });
        }

        private void ApplyMetricsToUi(HardwareMetrics metrics)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (metrics.CpuTemperature is float rawTemperature)
            {
                float smoothedTemperature =
                    cpuTemperatureAverage.Add(rawTemperature);
                metrics = metrics with
                {
                    CpuTemperature = smoothedTemperature
                };
            }
            else
            {
                cpuTemperatureAverage.Clear();
            }

            UpdateMetricCards(metrics);
            UpdateCoreCards(metrics.Cores);
            chartHistoryController.Update(metrics);
            LogMetricsIfDue(metrics);

            if (metrics.CpuTemperature is float value)
            {
                temperatureLabel.Text =
                    $"{value.ToString("F1", CultureInfo.GetCultureInfo("tr-TR"))} °C";
                temperatureProgressBar.Value = Math.Clamp(
                    (int)Math.Round(value),
                    0,
                    temperatureProgressBar.Maximum);
                UpdateTemperatureColors(value);
                CheckCriticalTemperature(value);

                if (appSettings.SafetyEnabled)
                {
                    try
                    {
                        safetyMonitor.Observe(value, ShowSafetyAlert);
                    }
                    catch (InvalidOperationException ex)
                    {
                        AppLogger.Error("Güvenlik eylemi çalıştırılamadı.", ex);
                        statusLabel.Text =
                            $"Güvenlik eylemi çalıştırılamadı: {ex.Message}";
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        AppLogger.Error("Güvenlik eylemi başlatılamadı.", ex);
                        statusLabel.Text =
                            $"Güvenlik eylemi başlatılamadı: {ex.Message}";
                    }
                }

                statusLabel.Text = $"Son güncelleme: {DateTime.Now:T}";
            }
            else
            {
                temperatureLabel.Text = "-- °C";
                temperatureProgressBar.Value = 0;
                cpuTemperatureGauge.Value = null;
                cpuUsageGauge.Value = null;
                gpuTemperatureGauge.Value = null;
                gpuUsageGauge.Value = null;
                cpuTemperatureCritical = false;
                gpuTemperatureCritical = false;
                cpuCardPanel.SetPulse(false, false);
                gpuCardPanel.SetPulse(false, false);
                ResetTemperatureColors();
                lastTemperatureAlertUtc = null;
                statusLabel.Text =
                    "Bu bilgisayarda CPU sıcaklık sensörü bulunamadı.";
            }
        }

        private void InvokeOnUi(Action action)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch (System.ComponentModel.InvalidAsynchronousStateException)
                {
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            action();
        }

        private void CheckCriticalTemperature(float temperature)
        {
            float threshold = (float)criticalTemperatureNumericUpDown.Value;

            if (temperature < threshold)
            {
                lastTemperatureAlertUtc = null;
                alertService.Reset();
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (lastTemperatureAlertUtc.HasValue &&
                now - lastTemperatureAlertUtc.Value < AlertCooldown)
            {
                return;
            }

            if (alertService.Show(
                    "CPU sıcaklık uyarısı",
                    $"CPU sıcaklığı {temperature.ToString("F1", CultureInfo.GetCultureInfo("tr-TR"))} °C. " +
                    $"Kritik eşik: {threshold.ToString("F1", CultureInfo.GetCultureInfo("tr-TR"))} °C.",
                    ToolTipIcon.Warning,
                    5000))
            {
                lastTemperatureAlertUtc = now;
            }
        }

        private void ShowSafetyAlert(string message)
        {
            alertService.Show(
                "Donanım güvenliği uyarısı",
                message,
                ToolTipIcon.Error,
                10000,
                force: true);
            statusLabel.Text = message;
        }

        private void form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (resourcesDisposed)
            {
                return;
            }

            monitoringCoordinator.Dispose();
            pulseTimer.Stop();
            pulseTimer.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            hardwareMonitor.Dispose();
            logger.Dispose();
            SaveApplicationSettings();
            resourcesDisposed = true;
        }

        private void SaveApplicationSettings()
        {
            appSettings.RefreshIntervalSeconds =
                intervalNumericUpDown.Value;
            appSettings.CriticalTemperatureCelsius =
                criticalTemperatureNumericUpDown.Value;
            appSettings.UseTemperatureColors =
                useTemperatureColorsCheckBox.Checked;
            appSettings.StartWithWindows =
                startWithWindowsCheckBox.Checked;
            appSettings.Theme = darkThemeCheckBox.Checked
                ? ThemeMode.Dark
                : ThemeMode.Light;

            try
            {
                appSettings.Save();
            }
            catch (IOException ex)
            {
                statusLabel.Text = $"Ayarlar kaydedilemedi: {ex.Message}";
            }
        }

        private void startWithWindowsCheckBox_CheckedChanged(
            object? sender,
            EventArgs e)
        {
            try
            {
                using RegistryKey? runKey = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run");

                if (runKey is null)
                {
                    throw new InvalidOperationException(
                        "Windows başlangıç kayıt anahtarı açılamadı.");
                }

                if (startWithWindowsCheckBox.Checked)
                {
                    string executablePath = Environment.ProcessPath
                        ?? Application.ExecutablePath;
                    runKey.SetValue(
                        "CpuMonitor",
                        $"\"{executablePath}\"");
                }
                else
                {
                    runKey.DeleteValue("CpuMonitor", throwOnMissingValue: false);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Error("Windows başlangıç ayarı değiştirilemedi.", ex);
                startWithWindowsCheckBox.CheckedChanged -=
                    startWithWindowsCheckBox_CheckedChanged;
                startWithWindowsCheckBox.Checked = !startWithWindowsCheckBox.Checked;
                startWithWindowsCheckBox.CheckedChanged +=
                    startWithWindowsCheckBox_CheckedChanged;
                MessageBox.Show(
                    this,
                    $"Windows başlangıç ayarı değiştirilemedi:\n{ex.Message}",
                    "Ayar hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void useTemperatureColorsCheckBox_CheckedChanged(
            object? sender,
            EventArgs e)
        {
            if (!useTemperatureColorsCheckBox.Checked)
            {
                ResetTemperatureColors();
            }
        }

        private void darkThemeCheckBox_CheckedChanged(
            object? sender,
            EventArgs e)
        {
            ApplySelectedTheme();
        }

        private void ApplySelectedTheme()
        {
            ThemeManager.Apply(
                this,
                darkThemeCheckBox.Checked
                    ? ThemeMode.Dark
                    : ThemeMode.Light);
        }

        private void CreateCustomTitleBar()
        {
            minimizeButton = CreateTitleBarButton("—", "Pencereyi küçült");
            closeButton = CreateTitleBarButton("×", "Uygulamayı kapat");

            minimizeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            minimizeButton.Location = new Point(
                headerPanel.ClientSize.Width - 92,
                8);
            closeButton.Location = new Point(
                headerPanel.ClientSize.Width - 48,
                8);
            headerPanel.Controls.Add(minimizeButton);
            headerPanel.Controls.Add(closeButton);

            minimizeButton.Click += (_, _) =>
                WindowState = FormWindowState.Minimized;
            closeButton.Click += (_, _) => Close();

            headerPanel.MouseDown += titleBar_MouseDown;
            headerPanel.MouseMove += titleBar_MouseMove;
            headerPanel.MouseUp += titleBar_MouseUp;
            titleLabel.MouseDown += titleBar_MouseDown;
            titleLabel.MouseMove += titleBar_MouseMove;
            titleLabel.MouseUp += titleBar_MouseUp;
            processorNameLabel.MouseDown += titleBar_MouseDown;
            processorNameLabel.MouseMove += titleBar_MouseMove;
            processorNameLabel.MouseUp += titleBar_MouseUp;
        }

        private static Button CreateTitleBarButton(string text, string tooltip)
        {
            var button = new Button
            {
                AccessibleName = tooltip,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13F),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(40, 32),
                Text = text,
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(71, 85, 105);
            return button;
        }

        private void titleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            titleDragging = true;
            titleDragStart = System.Windows.Forms.Cursor.Position;
        }

        private void titleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!titleDragging)
            {
                return;
            }

            Point currentPosition = System.Windows.Forms.Cursor.Position;
            Location = new Point(
                Left + currentPosition.X - titleDragStart.X,
                Top + currentPosition.Y - titleDragStart.Y);
            titleDragStart = currentPosition;
        }

        private void titleBar_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                titleDragging = false;
            }
        }

        private void AddButtonHoverEffects(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child is Button button &&
                    !ReferenceEquals(button, minimizeButton) &&
                    !ReferenceEquals(button, closeButton))
                {
                    button.MouseEnter += button_MouseEnter;
                    button.MouseLeave += button_MouseLeave;
                }

                AddButtonHoverEffects(child);
            }
        }

        private void button_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = darkThemeCheckBox.Checked
                    ? Color.FromArgb(58, 58, 82)
                    : Color.FromArgb(235, 240, 248);
                button.FlatAppearance.BorderColor =
                    Color.FromArgb(96, 165, 250);
            }
        }

        private void button_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = darkThemeCheckBox.Checked
                    ? Color.FromArgb(42, 42, 61)
                    : Color.White;
                button.FlatAppearance.BorderColor =
                    darkThemeCheckBox.Checked
                        ? Color.FromArgb(78, 78, 103)
                        : Color.FromArgb(203, 213, 225);
            }
        }

        private void LogMetricsIfDue(HardwareMetrics metrics)
        {
            if (!loggingCheckBox.Checked)
            {
                lastLogUtc = null;
                return;
            }

            TimeSpan interval = TimeSpan.FromSeconds(
                (double)logIntervalNumericUpDown.Value);
            DateTime now = DateTime.UtcNow;

            if (lastLogUtc.HasValue &&
                now - lastLogUtc.Value < interval)
            {
                return;
            }

            logger.LogMetrics(metrics);
            lastLogUtc = now;
        }

        private void exportLogButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "json",
                FileName = $"cpu-log-{DateTime.Now:yyyyMMdd-HHmmss}.json",
                Filter = "JSON dosyası (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
                Title = "Logları dışa aktar"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                logger.ExportJson(dialog.FileName);
                statusLabel.Text = "Loglar JSON olarak dışa aktarıldı.";
            }
            catch (IOException ex)
            {
                AppLogger.Error("Log dışa aktarılamadı.", ex);
                MessageBox.Show(
                    this,
                    $"Log dışa aktarılamadı:\n{ex.Message}",
                    "Dışa aktarma hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void form1_Resize(object? sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                return;
            }

            widgetController.HideToTray();
        }

        private void trayIcon_DoubleClick(object? sender, EventArgs e)
        {
            widgetController.ToggleWindowVisibility();
        }

        private void showHideMenuItem_Click(object? sender, EventArgs e)
        {
            widgetController.ToggleWindowVisibility();
        }

        private void widgetModeMenuItem_Click(object? sender, EventArgs e)
        {
            widgetController.SetWidgetMode(!widgetController.IsWidgetMode);
        }

        private void SetMetricCardsVisible(bool visible)
        {
            foreach (Control control in Controls)
            {
                if (control is FlowLayoutPanel panel &&
                    !ReferenceEquals(panel, coreFlowPanel))
                {
                    panel.Visible = visible;
                }
            }
        }

        private void exitMenuItem_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void CreateMetricCards()
        {
            var cardsPanel = new FlowLayoutPanel
            {
                Location = new Point(30, 330),
                Size = new Size(740, 105),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            (AnimatedCardPanel card, Label title, Label value) cpuCard =
                CreateMetricCard("CPU", "-- °C / -- %");
            (AnimatedCardPanel card, Label title, Label value) gpuCard =
                CreateMetricCard("GPU", "-- °C / -- %");
            (Panel card, Label title, Label value) memoryCard =
                CreateMetricCard("RAM", "-- %");
            (Panel card, Label title, Label value) diskCard =
                CreateMetricCard("Disk", "-- / -- MB/s");

            cpuCardValue = cpuCard.value;
            cpuCardTitle = cpuCard.title;
            cpuCardPanel = cpuCard.card;
            gpuCardValue = gpuCard.value;
            gpuCardTitle = gpuCard.title;
            gpuCardPanel = gpuCard.card;
            memoryCardValue = memoryCard.value;
            diskCardValue = diskCard.value;

            cardsPanel.Controls.Add(cpuCard.card);
            cardsPanel.Controls.Add(gpuCard.card);
            cardsPanel.Controls.Add(memoryCard.card);
            cardsPanel.Controls.Add(diskCard.card);
            Controls.Add(cardsPanel);
            cardsPanel.BringToFront();

            gpuTemperatureProgressBar = new ProgressBar
            {
                Location = new Point(30 + 190 + 18, 430),
                Maximum = 100,
                Size = new Size(165, 10),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            Controls.Add(gpuTemperatureProgressBar);
            gpuTemperatureProgressBar.BringToFront();
            CreateCoreCards();
        }

        private void CreateCircularGauges()
        {
            temperatureLabel.Visible = false;
            temperatureCaptionLabel.Visible = false;
            progressCaptionLabel.Visible = false;
            temperatureProgressBar.Visible = false;
            gpuTemperatureProgressBar.Visible = false;

            cpuTemperatureGauge = CreateGauge(
                "CPU Sıcaklık",
                new Point(45, 112),
                100,
                "°C");
            cpuUsageGauge = CreateGauge(
                "CPU Kullanım",
                new Point(220, 112),
                100,
                "%");
            gpuTemperatureGauge = CreateGauge(
                "GPU Sıcaklık",
                new Point(395, 112),
                100,
                "°C");
            gpuUsageGauge = CreateGauge(
                "GPU Kullanım",
                new Point(570, 112),
                100,
                "%");
        }

        private CircularProgressGauge CreateGauge(
            string caption,
            Point location,
            float maximum,
            string unit)
        {
            var gauge = new CircularProgressGauge
            {
                Caption = caption,
                Location = location,
                Maximum = maximum,
                Unit = unit,
                Anchor = AnchorStyles.Top
            };
            Controls.Add(gauge);
            gauge.BringToFront();
            return gauge;
        }

        private void CreateCoreCards()
        {
            coreFlowPanel = new FlowLayoutPanel
            {
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(30, 450),
                Padding = new Padding(8),
                Size = new Size(740, 145),
                WrapContents = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(coreFlowPanel);
            coreFlowPanel.BringToFront();
        }

        private void CreateHistoryChart()
        {
            historyChart = new Chart
            {
                BackColor = Color.White,
                AntiAliasing = AntiAliasingStyles.All,
                TextAntiAliasingQuality = TextAntiAliasingQuality.High,
                Location = new Point(30, 610),
                Size = new Size(740, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var area = new ChartArea("History")
            {
                BackColor = Color.FromArgb(42, 42, 61),
                BackSecondaryColor = Color.FromArgb(30, 30, 46),
                BackGradientStyle = GradientStyle.TopBottom
            };

            area.AxisX.LabelStyle.Format = "mm:ss";
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisY.Minimum = 0;
            area.AxisY.Maximum = 100;
            area.AxisY.Title = "°C / %";
            area.AxisX.Title = "Son 5 dakika";
            historyChart.ChartAreas.Add(area);

            AddHistorySeries("CPU sıcaklığı", Color.FromArgb(57, 255, 20));
            AddHistorySeries("CPU kullanımı", Color.FromArgb(0, 191, 255));
            AddHistorySeries("GPU sıcaklığı", Color.FromArgb(255, 170, 0));
            AddHistorySeries("GPU kullanımı", Color.FromArgb(180, 90, 255));
            historyChart.Legends.Add(new Legend
            {
                Docking = Docking.Top,
                Alignment = StringAlignment.Center
            });
            historyChartMenu = new ContextMenuStrip();
            historyChartMenu.Items.Add(
                "Son 1 dakika",
                null,
                (_, _) => chartHistoryController.SetWindow(60));
            historyChartMenu.Items.Add(
                "Son 5 dakika",
                null,
                (_, _) => chartHistoryController.SetWindow(300));
            historyChartMenu.Items.Add(
                "Son 15 dakika",
                null,
                (_, _) => chartHistoryController.SetWindow(900));
            historyChartMenu.Items.Add(new ToolStripSeparator());
            historyChartMenu.Items.Add(
                CreateSeriesToggleItem("CPU sıcaklığı"));
            historyChartMenu.Items.Add(
                CreateSeriesToggleItem("CPU kullanımı"));
            historyChartMenu.Items.Add(
                CreateSeriesToggleItem("GPU sıcaklığı"));
            historyChartMenu.Items.Add(
                CreateSeriesToggleItem("GPU kullanımı"));
            historyChartMenu.Items.Add(new ToolStripSeparator());
            historyChartMenu.Items.Add(
                "Grafiği PNG olarak dışa aktar",
                null,
                (_, _) => chartHistoryController.ExportPng(
                    this,
                    _ => statusLabel.Text = "Grafik PNG olarak dışa aktarıldı."));
            historyChart.ContextMenuStrip = historyChartMenu;
            Controls.Add(historyChart);
            historyChart.BringToFront();
        }

        private ToolStripMenuItem CreateSeriesToggleItem(string seriesName)
        {
            var item = new ToolStripMenuItem(seriesName)
            {
                Checked = true,
                CheckOnClick = true
            };
            item.CheckedChanged += (_, _) =>
            {
                if (historyChart.Series.FindByName(seriesName) is null)
                {
                    return;
                }

                historyChart.Series[seriesName].Enabled = item.Checked;
            };
            return item;
        }

        private void ConfigureHistoryChart()
        {
            historyChart.SuspendLayout();
            try
            {
                historyChart.ChartAreas.Clear();
                historyChart.Series.Clear();
                historyChart.Legends.Clear();

                var area = new ChartArea("History")
                {
                    BackColor = Color.FromArgb(42, 42, 61),
                    BackSecondaryColor = Color.FromArgb(30, 30, 46),
                    BackGradientStyle = GradientStyle.TopBottom
                };

                area.AxisX.LabelStyle.Format = "HH:mm:ss";
                area.AxisX.LabelStyle.IsEndLabelVisible = true;
                area.AxisX.MajorGrid.LineColor =
                    Color.FromArgb(226, 232, 240);
                area.AxisY.MajorGrid.LineColor =
                    Color.FromArgb(226, 232, 240);
                area.AxisY.Minimum = 0;
                area.AxisY.Maximum = 100;
                area.AxisY.Interval = 10;
                area.AxisY.Title = "°C / %";
                area.AxisX.Title = "Zaman";

                DateTime now = DateTime.Now;
                area.AxisX.Minimum =
                    now.AddSeconds(-60).ToOADate();
                area.AxisX.Maximum = now.ToOADate();

                historyChart.ChartAreas.Add(area);
                AddHistorySeries("CPU sıcaklığı", Color.FromArgb(57, 255, 20));
                AddHistorySeries("CPU kullanımı", Color.FromArgb(0, 191, 255));
                AddHistorySeries("GPU sıcaklığı", Color.FromArgb(255, 170, 0));
                AddHistorySeries("GPU kullanımı", Color.FromArgb(180, 90, 255));

                historyChart.Legends.Add(new Legend
                {
                    Docking = Docking.Top,
                    Alignment = StringAlignment.Center
                });
            }
            finally
            {
                historyChart.ResumeLayout();
            }
        }

        private void AddHistorySeries(string name, Color color)
        {
            var series = new Series(name)
            {
                ChartArea = "History",
                ChartType = SeriesChartType.Spline,
                BorderWidth = 2,
                Color = color,
                ShadowColor = Color.FromArgb(150, color),
                ShadowOffset = 2,
                XValueType = ChartValueType.DateTime,
                IsXValueIndexed = false,
                IsVisibleInLegend = true
            };
            historyChart.Series.Add(series);
        }

        private static float? GetAverageCoreLoad(
            IReadOnlyList<CoreMetric> cores)
        {
            float[] values = cores
                .Where(core => core.Load.HasValue)
                .Select(core => core.Load!.Value)
                .ToArray();

            return values.Length == 0
                ? null
                : values.Average();
        }

        private void UpdateCoreCards(IReadOnlyList<CoreMetric> cores)
        {
            foreach (CoreMetric core in cores)
            {
                if (!coreValueLabels.TryGetValue(core.Name, out Label[]? labels))
                {
                    labels = CreateCoreCard(core.Name);
                    coreValueLabels.Add(core.Name, labels);
                }

                labels[0].Text = FormatTemperature(core.Temperature);
                labels[1].Text = FormatPercentage(core.Load);
            }
        }

        private Label[] CreateCoreCard(string coreName)
        {
            var card = new Panel
            {
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(4),
                Size = new Size(160, 58)
            };

            var nameLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Location = new Point(8, 6),
                Text = coreName
            };

            var temperatureValue = new Label
            {
                AutoSize = true,
                Location = new Point(8, 29),
                Text = "-- °C"
            };

            var loadValue = new Label
            {
                AutoSize = true,
                Location = new Point(78, 29),
                Text = "-- %"
            };

            card.Controls.Add(nameLabel);
            card.Controls.Add(temperatureValue);
            card.Controls.Add(loadValue);
            coreFlowPanel.Controls.Add(card);
            return new[] { temperatureValue, loadValue };
        }

        private static (AnimatedCardPanel card, Label title, Label value) CreateMetricCard(
            string title,
            string initialValue)
        {
            var card = new AnimatedCardPanel
            {
                BackColor = Color.FromArgb(42, 42, 61),
                NormalBackColor = Color.FromArgb(42, 42, 61),
                HoverBackColor = Color.FromArgb(58, 58, 82),
                Margin = new Padding(0, 0, 10, 0),
                Size = new Size(175, 95)
            };
            card.Region = CreateRoundedRegion(card.Size, 12);

            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(12, 10),
                Text = title
            };

            var valueLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Height = 48,
                Text = initialValue,
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            return (card, titleLabel, valueLabel);
        }

        private static Region CreateRoundedRegion(Size size, int radius)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddArc(size.Width - diameter, 0, diameter, diameter, 270, 90);
            path.AddArc(
                size.Width - diameter,
                size.Height - diameter,
                diameter,
                diameter,
                0,
                90);
            path.AddArc(0, size.Height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }

        private void UpdateMetricCards(HardwareMetrics metrics)
        {
            cpuCardTitle.Text = string.IsNullOrWhiteSpace(metrics.CpuName)
                ? "CPU"
                : metrics.CpuName;
            gpuCardTitle.Text = string.IsNullOrWhiteSpace(metrics.GpuName)
                ? "GPU"
                : metrics.GpuName;
            cpuCardValue.Text =
                $"{FormatTemperature(metrics.CpuTemperature)} / {FormatPercentage(metrics.CpuUsage)}";
            gpuCardValue.Text =
                $"{FormatTemperature(metrics.GpuTemperature)} / {FormatPercentage(metrics.GpuUsage)}";
            cpuCardValue.ForeColor = GetTemperatureColor(metrics.CpuTemperature);
            gpuCardValue.ForeColor = GetTemperatureColor(metrics.GpuTemperature);
            cpuTemperatureCritical = metrics.CpuTemperature is float cpuTemperature &&
                cpuTemperature > 80;
            gpuTemperatureCritical = metrics.GpuTemperature is float gpuTemperature &&
                gpuTemperature > 80;
            cpuCardPanel.SetPulse(cpuTemperatureCritical, pulseVisible);
            gpuCardPanel.SetPulse(gpuTemperatureCritical, pulseVisible);
            cpuTemperatureGauge.Value = metrics.CpuTemperature;
            cpuUsageGauge.Value = metrics.CpuUsage;
            gpuTemperatureGauge.Value = metrics.GpuTemperature;
            gpuUsageGauge.Value = metrics.GpuUsage;
            gpuTemperatureProgressBar.Value = Math.Clamp(
                (int)Math.Round(metrics.GpuTemperature ?? 0),
                0,
                gpuTemperatureProgressBar.Maximum);
            memoryCardValue.Text = FormatPercentage(metrics.MemoryUsage);
            diskCardValue.Text =
                $"{FormatRate(metrics.DiskRead)} / {FormatRate(metrics.DiskWrite)}";
        }

        private static string FormatTemperature(float? value)
        {
            return value is float temperature
                ? $"{temperature.ToString("F1", CultureInfo.GetCultureInfo("tr-TR"))} °C"
                : "-- °C";
        }

        private static string FormatPercentage(float? value)
        {
            return value is float percentage
                ? $"{percentage.ToString("F1", CultureInfo.GetCultureInfo("tr-TR"))} %"
                : "-- %";
        }

        private static string FormatRate(float? value)
        {
            return value is float rate
                ? $"{rate.ToString("F1", CultureInfo.GetCultureInfo("tr-TR"))} MB/s"
                : "--";
        }

        private void UpdateTemperatureColors(float temperature)
        {
            if (!useTemperatureColorsCheckBox.Checked)
            {
                ResetTemperatureColors();
                return;
            }

            Color temperatureColor = GetTemperatureColor(temperature);

            temperatureLabel.ForeColor = temperatureColor;
            temperatureProgressBar.ForeColor = temperatureColor;
        }

        private Color GetTemperatureColor(float? temperature)
        {
            if (!useTemperatureColorsCheckBox.Checked || temperature is not float value)
            {
                return Color.FromArgb(15, 23, 42);
            }

            if (value < 60)
            {
                return Color.SeaGreen;
            }

            return value <= 80
                ? Color.DarkOrange
                : Color.Firebrick;
        }

        private void ResetTemperatureColors()
        {
            temperatureLabel.ForeColor = Color.FromArgb(15, 23, 42);
            temperatureProgressBar.ForeColor = SystemColors.Highlight;
        }

        private static string GetProcessorName()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_Processor");

                foreach (ManagementObject item in searcher.Get())
                {
                    if (item["Name"] is string name && !string.IsNullOrWhiteSpace(name))
                    {
                        return name.Trim();
                    }
                }
            }
            catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException)
            {
                // WMI kullanılamadığında işletim sistemi değişkenine geçilir.
            }

            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
                ?? "Bilinmeyen işlemci";
        }
    }
}
