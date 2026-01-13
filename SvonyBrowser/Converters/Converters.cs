using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SvonyBrowser.Converters
{

    /// <summary>
    /// Converts boolean to Visibility.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Check for inverse parameter
                if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
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

    /// <summary>
    /// Converts null to Visibility.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
        
            // Check for inverse parameter
            if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts string to Visibility based on whether it's empty.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = string.IsNullOrEmpty(value as string);
        
            // Check for inverse parameter
            if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts MCP connection status to brush color.
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is McpConnectionStatus status)
            {
                return status switch
                {
                    McpConnectionStatus.Connected => new SolidColorBrush(Color.FromRgb(16, 185, 129)),    // Green
                    McpConnectionStatus.Connecting => new SolidColorBrush(Color.FromRgb(245, 158, 11)),  // Yellow
                    McpConnectionStatus.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),        // Red
                    _ => new SolidColorBrush(Color.FromRgb(148, 163, 184))                               // Gray
                };
            }
            return new SolidColorBrush(Color.FromRgb(148, 163, 184));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts MCP connection status to status text.
    /// </summary>
    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is McpConnectionStatus status)
            {
                return status switch
                {
                    McpConnectionStatus.Connected => "Connected",
                    McpConnectionStatus.Connecting => "Connecting...",
                    McpConnectionStatus.Disconnected => "Disconnected",
                    McpConnectionStatus.Error => "Error",
                    _ => "Unknown"
                };
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts chat role to alignment.
    /// </summary>
    public class RoleToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ChatRole role)
            {
                return role == ChatRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts chat role to background brush.
    /// </summary>
    public class RoleToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ChatRole role)
            {
                return role == ChatRole.User
                    ? new SolidColorBrush(Color.FromRgb(59, 130, 246))   // Blue for user
                    : new SolidColorBrush(Color.FromRgb(55, 65, 81));    // Gray for assistant
            }
            return new SolidColorBrush(Color.FromRgb(55, 65, 81));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts byte size to human-readable format.
    /// </summary>
    public class ByteSizeConverter : IValueConverter
    {
        private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                if (bytes == 0) return "0 B";
            
                int magnitude = (int)Math.Log(bytes, 1024);
                magnitude = Math.Min(magnitude, SizeSuffixes.Length - 1);
            
                double adjustedSize = bytes / Math.Pow(1024, magnitude);
                return $"{adjustedSize:N1} {SizeSuffixes[magnitude]}";
            }
            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts timestamp to relative time string.
    /// </summary>
    public class RelativeTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var span = DateTime.Now - dateTime;
            
                if (span.TotalSeconds < 60)
                    return "Just now";
                if (span.TotalMinutes < 60)
                    return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24)
                    return $"{(int)span.TotalHours}h ago";
                if (span.TotalDays < 7)
                    return $"{(int)span.TotalDays}d ago";
            
                return dateTime.ToString("MMM d");
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts HTTP method to color.
    /// </summary>
    public class HttpMethodToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string method)
            {
                return method.ToUpperInvariant() switch
                {
                    "GET" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),     // Green
                    "POST" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),    // Blue
                    "PUT" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),     // Yellow
                    "DELETE" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
                    "PATCH" => new SolidColorBrush(Color.FromRgb(139, 92, 246)),   // Purple
                    _ => new SolidColorBrush(Color.FromRgb(148, 163, 184))         // Gray
                };
            }
            return new SolidColorBrush(Color.FromRgb(148, 163, 184));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts HTTP status code to color.
    /// </summary>
    public class StatusCodeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int statusCode)
            {
                return statusCode switch
                {
                    >= 200 and < 300 => new SolidColorBrush(Color.FromRgb(16, 185, 129)),   // Green - Success
                    >= 300 and < 400 => new SolidColorBrush(Color.FromRgb(59, 130, 246)),   // Blue - Redirect
                    >= 400 and < 500 => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Yellow - Client Error
                    >= 500 => new SolidColorBrush(Color.FromRgb(239, 68, 68)),              // Red - Server Error
                    _ => new SolidColorBrush(Color.FromRgb(148, 163, 184))                  // Gray - Unknown
                };
            }
            return new SolidColorBrush(Color.FromRgb(148, 163, 184));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts boolean to Visibility with inverse logic.
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            if (value is int intValue)
            {
                return intValue > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
    }

    /// <summary>
    /// Multi-value converter for combining multiple boolean values with AND logic.
    /// </summary>
    public class MultiBoolAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multi-value converter for combining multiple boolean values with OR logic.
    /// </summary>
    public class MultiBoolOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool boolValue && boolValue)
                {
                    return true;
                }
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}