using System;
using System.Globalization;
using System.Windows.Data;
using MahApps.Metro.IconPacks;

namespace Bitwarden.AutoType.Desktop.Helpers;

public class PinButtonIconConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isPinned && culture is CultureInfo cultureInfo)
        {
            return isPinned ? PackIconJamIconsKind.PinAltF : PackIconJamIconsKind.PinAlt;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}