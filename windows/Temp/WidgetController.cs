namespace Temp;

public sealed class WidgetController
{
    private readonly Form form;
    private readonly Panel controlsPanel;
    private readonly Control corePanel;
    private readonly Control historyChart;
    private readonly NotifyIcon trayIcon;
    private readonly ToolStripMenuItem showHideMenuItem;
    private readonly ToolStripMenuItem widgetModeMenuItem;
    private readonly Action<bool> setMetricCardsVisible;
    private Rectangle normalBounds;
    private FormBorderStyle normalBorderStyle;
    private bool normalTopMost;
    private double normalOpacity;
    private FormWindowState normalWindowState;
    private bool widgetMode;
    private Point dragStart;
    private bool dragging;

    public WidgetController(
        Form form,
        Panel controlsPanel,
        Control corePanel,
        Control historyChart,
        NotifyIcon trayIcon,
        ToolStripMenuItem showHideMenuItem,
        ToolStripMenuItem widgetModeMenuItem,
        Action<bool> setMetricCardsVisible)
    {
        this.form = form;
        this.controlsPanel = controlsPanel;
        this.corePanel = corePanel;
        this.historyChart = historyChart;
        this.trayIcon = trayIcon;
        this.showHideMenuItem = showHideMenuItem;
        this.widgetModeMenuItem = widgetModeMenuItem;
        this.setMetricCardsVisible = setMetricCardsVisible;
    }

    public bool IsWidgetMode => widgetMode;

    public void AddDragHandlers(Control root)
    {
        root.MouseDown += Drag_MouseDown;
        root.MouseMove += Drag_MouseMove;
        root.MouseUp += Drag_MouseUp;

        foreach (Control child in root.Controls)
        {
            AddDragHandlers(child);
        }
    }

    public void SetWidgetMode(bool enabled)
    {
        if (enabled == widgetMode)
        {
            return;
        }

        if (enabled)
        {
            normalBounds = form.Bounds;
            normalBorderStyle = form.FormBorderStyle;
            normalTopMost = form.TopMost;
            normalOpacity = form.Opacity;
            normalWindowState = form.WindowState;

            widgetMode = true;
            form.FormBorderStyle = FormBorderStyle.None;
            form.TopMost = true;
            form.Opacity = 0.88;
            form.Size = new Size(360, 180);

            Rectangle workArea = Screen.FromControl(form).WorkingArea;
            form.Location = new Point(
                workArea.Right - form.Width - 20,
                workArea.Bottom - form.Height - 20);

            controlsPanel.Visible = false;
            corePanel.Visible = false;
            historyChart.Visible = false;
            setMetricCardsVisible(false);
            widgetModeMenuItem.Text = "Normal moda dön";
        }
        else
        {
            widgetMode = false;
            controlsPanel.Visible = true;
            corePanel.Visible = true;
            historyChart.Visible = true;
            setMetricCardsVisible(true);

            form.FormBorderStyle = normalBorderStyle;
            form.TopMost = normalTopMost;
            form.Opacity = normalOpacity;
            form.Bounds = normalBounds;
            form.WindowState = normalWindowState;
            widgetModeMenuItem.Text = "Floating Widget modu";
        }
    }

    public void HideToTray()
    {
        form.Hide();
        trayIcon.Visible = true;
        showHideMenuItem.Text = "Göster";
    }

    public void ToggleWindowVisibility()
    {
        if (form.Visible)
        {
            HideToTray();
            return;
        }

        form.Show();
        form.WindowState = FormWindowState.Normal;
        form.Activate();
        trayIcon.Visible = false;
        showHideMenuItem.Text = "Gizle";
    }

    private void Drag_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!widgetMode || e.Button != MouseButtons.Left)
        {
            return;
        }

        dragging = true;
        dragStart = Cursor.Position;
    }

    private void Drag_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!widgetMode || !dragging)
        {
            return;
        }

        Point currentPosition = Cursor.Position;
        form.Location = new Point(
            form.Left + currentPosition.X - dragStart.X,
            form.Top + currentPosition.Y - dragStart.Y);
        dragStart = currentPosition;
    }

    private void Drag_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            dragging = false;
        }
    }
}
