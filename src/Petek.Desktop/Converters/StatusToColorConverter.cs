using Petek.Shared.Enums;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Petek.Desktop.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UserStatus status)
        {
            return status switch
            {
                UserStatus.Available => Application.Current.FindResource("StatusAvailableBrush") as Brush ?? Brushes.Green,
                UserStatus.Busy => Application.Current.FindResource("StatusBusyBrush") as Brush ?? Brushes.Red,
                UserStatus.Away => Application.Current.FindResource("StatusAwayBrush") as Brush ?? Brushes.Orange,
                _ => Application.Current.FindResource("StatusOfflineBrush") as Brush ?? Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var invert = parameter?.ToString() == "Invert";
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString() == "Invert";
        var isNull = value == null;
        return (isNull ^ invert) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MessageStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageStatus status)
        {
            return status switch
            {
                MessageStatus.Sent => "\uE73E",       // Single check
                MessageStatus.Delivered => "\uE73E", // Single check (could use different icon)
                MessageStatus.Read => "\uE73E",      // Double check (could use different icon)
                MessageStatus.Failed => "\uE711",    // X mark
                _ => ""
            };
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DateTimeToRelativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var now = DateTime.Now;
            var diff = now - dateTime;

            if (diff.TotalMinutes < 1) return "Şimdi";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk önce";
            if (diff.TotalHours < 24 && dateTime.Date == now.Date) return dateTime.ToString("HH:mm");
            if (dateTime.Date == now.Date.AddDays(-1)) return "Dün";
            if (diff.TotalDays < 7) return dateTime.ToString("dddd");
            return dateTime.ToString("dd.MM.yyyy");
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
