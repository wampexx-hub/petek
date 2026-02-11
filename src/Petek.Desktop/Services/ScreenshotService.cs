using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using Rectangle = System.Drawing.Rectangle;

namespace Petek.Desktop.Services;

public interface IScreenshotService
{
    Task<byte[]?> CaptureRegionAsync();
    Task<byte[]?> CaptureFullScreenAsync();
    Task<byte[]?> CaptureAndSelectRegionAsync();
    BitmapImage? BytesToBitmapImage(byte[] bytes);
}

public class ScreenshotService : IScreenshotService
{
    public async Task<byte[]?> CaptureRegionAsync()
    {
        return await CaptureAndSelectRegionAsync();
    }

    public async Task<byte[]?> CaptureAndSelectRegionAsync()
    {
        // Ekranin tam goruntusunu al
        var fullScreenBytes = await CaptureFullScreenAsync();
        if (fullScreenBytes == null) return null;

        // UI thread'de secim penceresini goster
        var tcs = new TaskCompletionSource<byte[]?>();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var overlayWindow = new ScreenshotOverlayWindow(fullScreenBytes, tcs);
            overlayWindow.Show();
        });

        return await tcs.Task;
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

/// <summary>
/// Ekran goruntusu bolge secimi icin tam ekran overlay penceresi
/// </summary>
internal class ScreenshotOverlayWindow : Window
{
    private readonly byte[] _fullScreenBytes;
    private readonly TaskCompletionSource<byte[]?> _tcs;
    private System.Windows.Point _startPoint;
    private System.Windows.Shapes.Rectangle? _selectionRect;
    private readonly Canvas _canvas;
    private bool _isSelecting;

    public ScreenshotOverlayWindow(byte[] fullScreenBytes, TaskCompletionSource<byte[]?> tcs)
    {
        _fullScreenBytes = fullScreenBytes;
        _tcs = tcs;

        // Tam ekran, cercevesiz, yari saydam pencere
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;

        // Tum ekranlari kapsayacak sekilde boyutlandir
        var allScreens = Screen.AllScreens;
        var left = allScreens.Min(s => s.Bounds.Left);
        var top = allScreens.Min(s => s.Bounds.Top);
        var right = allScreens.Max(s => s.Bounds.Right);
        var bottom = allScreens.Max(s => s.Bounds.Bottom);

        Left = left;
        Top = top;
        Width = right - left;
        Height = bottom - top;

        _canvas = new Canvas();

        // Arka plan olarak ekran goruntusunu koy
        var bgImage = new Image();
        var bitmapImage = new BitmapImage();
        using (var ms = new MemoryStream(fullScreenBytes))
        {
            ms.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
        }
        bitmapImage.Freeze();
        bgImage.Source = bitmapImage;
        bgImage.Width = Width;
        bgImage.Height = Height;
        _canvas.Children.Add(bgImage);

        // Karanlik overlay
        var overlay = new System.Windows.Shapes.Rectangle
        {
            Width = Width,
            Height = Height,
            Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 0, 0))
        };
        _canvas.Children.Add(overlay);

        // Bilgi metni
        var infoText = new TextBlock
        {
            Text = "Ekran goruntusunu almak icin bir bolge secin. ESC ile iptal edin.",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 16,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0, 0, 0)),
            Padding = new Thickness(12, 8, 12, 8)
        };
        Canvas.SetLeft(infoText, Width / 2 - 250);
        Canvas.SetTop(infoText, 30);
        _canvas.Children.Add(infoText);

        Content = _canvas;

        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _tcs.TrySetResult(null);
            Close();
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(_canvas);
        _isSelecting = true;

        _selectionRect = new System.Windows.Shapes.Rectangle
        {
            Stroke = System.Windows.Media.Brushes.DodgerBlue,
            StrokeThickness = 2,
            Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 30, 144, 255)),
            StrokeDashArray = new DoubleCollection { 4, 2 }
        };
        Canvas.SetLeft(_selectionRect, _startPoint.X);
        Canvas.SetTop(_selectionRect, _startPoint.Y);
        _canvas.Children.Add(_selectionRect);

        _canvas.CaptureMouse();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isSelecting || _selectionRect == null) return;

        var pos = e.GetPosition(_canvas);
        var x = Math.Min(pos.X, _startPoint.X);
        var y = Math.Min(pos.Y, _startPoint.Y);
        var w = Math.Abs(pos.X - _startPoint.X);
        var h = Math.Abs(pos.Y - _startPoint.Y);

        Canvas.SetLeft(_selectionRect, x);
        Canvas.SetTop(_selectionRect, y);
        _selectionRect.Width = w;
        _selectionRect.Height = h;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting || _selectionRect == null) return;

        _isSelecting = false;
        _canvas.ReleaseMouseCapture();

        var x = (int)Canvas.GetLeft(_selectionRect);
        var y = (int)Canvas.GetTop(_selectionRect);
        var w = (int)_selectionRect.Width;
        var h = (int)_selectionRect.Height;

        Close();

        if (w < 10 || h < 10)
        {
            // Cok kucuk secim - tam ekran gonder
            _tcs.TrySetResult(_fullScreenBytes);
            return;
        }

        try
        {
            // Secilen bolgeyi kirp
            using var fullBitmap = new Bitmap(new MemoryStream(_fullScreenBytes));
            using var croppedBitmap = new Bitmap(w, h);
            using (var g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(fullBitmap,
                    new Rectangle(0, 0, w, h),
                    new Rectangle(x, y, w, h),
                    GraphicsUnit.Pixel);
            }

            using var ms = new MemoryStream();
            croppedBitmap.Save(ms, ImageFormat.Png);
            _tcs.TrySetResult(ms.ToArray());
        }
        catch
        {
            _tcs.TrySetResult(_fullScreenBytes);
        }
    }
}
