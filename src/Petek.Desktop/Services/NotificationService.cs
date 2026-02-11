using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using Application = System.Windows.Application;

namespace Petek.Desktop.Services;

public interface INotificationService : IDisposable
{
    bool IsEnabled { get; set; }
    bool SoundEnabled { get; set; }
    bool ShowInTaskbar { get; set; }

    void ShowMessageNotification(string senderName, string content, MessageType type, Guid conversationId);
    void ShowNotification(string title, string message);
    void SetupSystemTray(Window mainWindow);
    void UpdateUnreadBadge(int count);
    void FlashTaskbar(Window window);
}

public class NotificationService : INotificationService
{
    private NotifyIcon? _trayIcon;
    private Window? _mainWindow;
    private int _unreadCount;
    private bool _disposed;

    public bool IsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = true;

    public event Action<Guid>? NotificationClicked;

    public void SetupSystemTray(Window mainWindow)
    {
        _mainWindow = mainWindow;

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Petek Messenger"
        };

        // Ikon yukle
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(iconPath))
            {
                _trayIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                // Varsayilan sistem ikonu
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        catch
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        // Sag tiklama menusu
        var contextMenu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("Petek'i Goster");
        showItem.Click += (s, e) => ShowMainWindow();
        showItem.Font = new System.Drawing.Font(showItem.Font, System.Drawing.FontStyle.Bold);
        contextMenu.Items.Add(showItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var statusMenu = new ToolStripMenuItem("Durum");
        var statusAvailable = new ToolStripMenuItem("Uygun");
        statusAvailable.Click += async (s, e) => await ChangeStatusFromTrayAsync(UserStatus.Available);
        var statusBusy = new ToolStripMenuItem("Mesgul");
        statusBusy.Click += async (s, e) => await ChangeStatusFromTrayAsync(UserStatus.Busy);
        var statusAway = new ToolStripMenuItem("Disarida");
        statusAway.Click += async (s, e) => await ChangeStatusFromTrayAsync(UserStatus.Away);
        statusMenu.DropDownItems.AddRange(new ToolStripItem[] { statusAvailable, statusBusy, statusAway });
        contextMenu.Items.Add(statusMenu);

        contextMenu.Items.Add(new ToolStripSeparator());

        var muteItem = new ToolStripMenuItem("Bildirimleri Kapat");
        muteItem.Click += (s, e) =>
        {
            IsEnabled = !IsEnabled;
            muteItem.Text = IsEnabled ? "Bildirimleri Kapat" : "Bildirimleri Ac";
            muteItem.Checked = !IsEnabled;
        };
        contextMenu.Items.Add(muteItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Cikis");
        exitItem.Click += (s, e) =>
        {
            _trayIcon.Visible = false;
            Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = contextMenu;

        // Cift tikla pencereyi goster
        _trayIcon.DoubleClick += (s, e) => ShowMainWindow();

        // Balon tiklandiginda
        _trayIcon.BalloonTipClicked += (s, e) => ShowMainWindow();

        // Pencere minimize edildiginde tray'e gonder
        mainWindow.StateChanged += (s, e) =>
        {
            if (mainWindow.WindowState == WindowState.Minimized && ShowInTaskbar)
            {
                mainWindow.ShowInTaskbar = false;
            }
            else
            {
                mainWindow.ShowInTaskbar = true;
            }
        };

        // Pencere kapatildiginda tray'e kucult (cikis yerine)
        mainWindow.Closing += (s, e) =>
        {
            if (_trayIcon.Visible)
            {
                e.Cancel = true;
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.ShowInTaskbar = false;

                if (_unreadCount == 0)
                {
                    _trayIcon.ShowBalloonTip(
                        2000,
                        "Petek Messenger",
                        "Uygulama arka planda calismaya devam ediyor.",
                        ToolTipIcon.Info);
                }
            }
        };
    }

    public void ShowMessageNotification(string senderName, string content, MessageType type, Guid conversationId)
    {
        if (!IsEnabled) return;

        // Pencere aktifse ve on plandaysa bildirim gosterme
        if (_mainWindow != null && _mainWindow.IsActive && _mainWindow.WindowState != WindowState.Minimized)
        {
            return;
        }

        // Okunmamis sayisini artir
        _unreadCount++;
        UpdateUnreadBadge(_unreadCount);

        // Bildirim metni
        var notificationText = type switch
        {
            MessageType.File => $"Dosya gonderdi",
            MessageType.Screenshot => $"Ekran goruntusu gonderdi",
            MessageType.Image => $"Resim gonderdi",
            _ => content.Length > 100 ? content[..100] + "..." : content
        };

        // Ses cal
        if (SoundEnabled)
        {
            PlayNotificationSound();
        }

        // Windows bildirim balon goster
        if (_trayIcon != null)
        {
            _trayIcon.ShowBalloonTip(
                5000,
                senderName,
                notificationText,
                ToolTipIcon.Info);
        }

        // Taskbar'i yanip sondur
        if (_mainWindow != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FlashTaskbar(_mainWindow);
            });
        }
    }

    public void ShowNotification(string title, string message)
    {
        if (!IsEnabled) return;

        if (_trayIcon != null)
        {
            _trayIcon.ShowBalloonTip(
                4000,
                title,
                message,
                ToolTipIcon.Info);
        }

        if (SoundEnabled)
        {
            PlayNotificationSound();
        }
    }

    public void UpdateUnreadBadge(int count)
    {
        _unreadCount = count;

        if (_trayIcon == null) return;

        if (count > 0)
        {
            _trayIcon.Text = $"Petek Messenger ({count} yeni mesaj)";
        }
        else
        {
            _trayIcon.Text = "Petek Messenger";
        }
    }

    public void FlashTaskbar(Window window)
    {
        if (window.IsActive) return;

        try
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            if (handle != IntPtr.Zero)
            {
                var flashInfo = new FLASHWINFO
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<FLASHWINFO>(),
                    hwnd = handle,
                    dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                    uCount = 3,
                    dwTimeout = 0
                };
                FlashWindowEx(ref flashInfo);
            }
        }
        catch { }
    }

    private void PlayNotificationSound()
    {
        try
        {
            // Ozel ses dosyasi varsa onu cal
            var soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "notification.wav");
            if (File.Exists(soundPath))
            {
                using var player = new SoundPlayer(soundPath);
                player.Play();
            }
            else
            {
                // Windows varsayilan bildirim sesi
                SystemSounds.Asterisk.Play();
            }
        }
        catch { }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            _mainWindow.Show();
            _mainWindow.ShowInTaskbar = true;
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Focus();

            // Okunmamis sayisini sifirla
            _unreadCount = 0;
            UpdateUnreadBadge(0);
        });
    }

    private async Task ChangeStatusFromTrayAsync(UserStatus status)
    {
        try
        {
            var signalR = App.Services.GetRequiredService<ISignalRService>();
            if (signalR.IsConnected)
            {
                await signalR.UpdateStatusAsync(status);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    // Windows API - Taskbar flash
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    private const uint FLASHW_ALL = 3;
    private const uint FLASHW_TIMERNOFG = 12;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }
}
