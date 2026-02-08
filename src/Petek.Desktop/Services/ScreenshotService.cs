using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Petek.Desktop.Services;

public interface IScreenshotService
{
    Task<byte[]?> CaptureRegionAsync();
    Task<byte[]?> CaptureFullScreenAsync();
    BitmapImage? BytesToBitmapImage(byte[] bytes);
}

public class ScreenshotService : IScreenshotService
{
    public async Task<byte[]?> CaptureRegionAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Get all screens bounds
                var allScreensBounds = GetAllScreensBounds();

                // Capture full screen
                using var bitmap = new Bitmap(allScreensBounds.Width, allScreensBounds.Height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        allScreensBounds.Left,
                        allScreensBounds.Top,
                        0, 0,
                        allScreensBounds.Size);
                }

                // Show selection overlay (simplified - in production use WPF overlay window)
                // For now, just return full screen capture
                return BitmapToBytes(bitmap);
            }
            catch
            {
                return null;
            }
        });
    }

    public async Task<byte[]?> CaptureFullScreenAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var bounds = GetAllScreensBounds();

                using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        bounds.Left,
                        bounds.Top,
                        0, 0,
                        bounds.Size);
                }

                return BitmapToBytes(bitmap);
            }
            catch
            {
                return null;
            }
        });
    }

    public BitmapImage? BytesToBitmapImage(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;

        var image = new BitmapImage();
        using (var stream = new MemoryStream(bytes))
        {
            stream.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
        }
        image.Freeze();
        return image;
    }

    private static Rectangle GetAllScreensBounds()
    {
        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;

        foreach (var screen in Screen.AllScreens)
        {
            left = Math.Min(left, screen.Bounds.Left);
            top = Math.Min(top, screen.Bounds.Top);
            right = Math.Max(right, screen.Bounds.Right);
            bottom = Math.Max(bottom, screen.Bounds.Bottom);
        }

        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static byte[] BitmapToBytes(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
