using System.Media;

namespace Temp;

public sealed class AlertService
{
    private readonly NotifyIcon trayIcon;
    private readonly TimeSpan cooldown;
    private DateTimeOffset? lastAlert;

    public AlertService(NotifyIcon trayIcon, TimeSpan cooldown)
    {
        this.trayIcon = trayIcon;
        this.cooldown = cooldown;
    }

    public bool Show(
        string title,
        string message,
        ToolTipIcon icon,
        int durationMilliseconds,
        bool force = false)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (!force &&
            lastAlert.HasValue &&
            now - lastAlert.Value < cooldown)
        {
            return false;
        }

        lastAlert = now;
        trayIcon.Visible = true;
        trayIcon.ShowBalloonTip(
            durationMilliseconds,
            title,
            message,
            icon);

        if (icon == ToolTipIcon.Error)
        {
            SystemSounds.Hand.Play();
        }
        else
        {
            SystemSounds.Exclamation.Play();
        }

        return true;
    }

    public void Reset()
    {
        lastAlert = null;
    }
}
